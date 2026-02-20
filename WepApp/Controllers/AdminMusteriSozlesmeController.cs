using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WepApp.Controllers
{
    public class AdminMusteriSozlesmeController : AdminBaseController
    {
        private readonly MusteriSozlesmeRepository _sozlesmeRepo = new MusteriSozlesmeRepository();
        private readonly MusteriRepository _musteriRepo = new MusteriRepository();
        private readonly SozlesmeDurumuRepository _durumRepo = new SozlesmeDurumuRepository();
        private readonly TeklifRepository _teklifRepo = new TeklifRepository();
        private readonly IWebHostEnvironment _environment;

        public AdminMusteriSozlesmeController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<string> join = new List<string>();
            join.Add("Teklif");
            join.Add("Teklif.LisansTip");
            join.Add("Teklif.Detaylar");
            // Tüm aktif sözleşmeleri getir
            List<MusteriSozlesme> sozlesmeler = _sozlesmeRepo.GetirList(x => x.Durumu == 1, join)
                .OrderByDescending(s => s.EklenmeTarihi)
                .ToList();

            // Navigation property'leri manuel doldur
            foreach (MusteriSozlesme s in sozlesmeler)
            {
                s.Musteri = _musteriRepo.Getir(s.MusteriId);
                s.SozlesmeDurumu = _durumRepo.Getir(s.SozlesmeDurumuId);

                s.Teklif = _teklifRepo.Getir(
                    x => x.Id == s.TeklifId,
                    new List<string> { "LisansTip", "Detaylar" }
                );

                // Detaylar listesinden SADECE "grup" olanları tut
                if (s.Teklif?.Detaylar != null)
                {
                    s.Teklif.Detaylar = s.Teklif.Detaylar
                        .Where(d => d.Tip == "grup")
                        .ToList();
                }
            }
            ViewBag.Sozlesmeler = sozlesmeler;
            ViewBag.Durumlar = _durumRepo.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Id)
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult MuhasebelendiYap(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Lisanslandı kontrolü
                if (sozlesme.SozlesmeDurumuId != 10)
                    return Json(new { success = false, message = "Sadece 'Lisanslandı' durumundaki sözleşmeler muhasebelendi yapılabilir." });

                // Durumu "Muhasebelendi" yap
                sozlesme.SozlesmeDurumuId = 11; // Muhasebelendi
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                Teklif teklif = _teklifRepo.Getir(x => x.Id == sozlesme.TeklifId);
                teklif.TeklifDurumId = 8;
                _teklifRepo.Guncelle(teklif);
                return Json(new
                {
                    success = true,
                    message = "Sözleşme muhasebelendi olarak işaretlendi.",
                    durumId = 11,
                    durumAdi = "Muhasebelendi"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MuhasebelendiGeriAl(int id)
        {
            LoadCommonData();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Muhasebelendi kontrolü
                if (sozlesme.SozlesmeDurumuId != 11)
                    return Json(new { success = false, message = "Sadece 'Muhasebelendi' durumundaki sözleşmeler geri alınabilir." });

                // Durumu "Lisanslandı" yap
                sozlesme.SozlesmeDurumuId = 10; // Lisanslandı
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                Teklif teklif = _teklifRepo.Getir(x => x.Id == sozlesme.TeklifId);
                teklif.TeklifDurumId = 6;
                _teklifRepo.Guncelle(teklif);
                return Json(new
                {
                    success = true,
                    message = "Sözleşme muhasebelendi durumundan geri alındı.",
                    durumId = 10,
                    durumAdi = "Lisanslandı"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DurumDegistir(int id, int durumId, string? iptalNeden = null)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu. Lütfen tekrar giriş yapın." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });
                // YENİ EKLENEN KOD: Distribütör Onayı için belge kontrolü
                if (durumId == 9) // Distribütör Onayı
                {
                    bool belgelerTam = true;
                    List<string> eksikBelgeler = new List<string>();

                    // Vergi Kimlik Levhası kontrolü
                    if (!sozlesme.VergiKimlikLevhasıVar)
                    {
                        belgelerTam = false;
                        eksikBelgeler.Add("Vergi Kimlik Levhası");
                    }

                    // Ticari Sicil Gazetesi kontrolü
                    if (!sozlesme.TicariSicilGazetesiVar)
                    {
                        belgelerTam = false;
                        eksikBelgeler.Add("Ticari Sicil Gazetesi");
                    }

                    // Kimlik Ön Yüzü kontrolü
                    if (!sozlesme.KimlikOnYuzuVar)
                    {
                        belgelerTam = false;
                        eksikBelgeler.Add("Kimlik Ön Yüzü");
                    }

                    // İmza Sirküsü kontrolü
                    if (!sozlesme.ImzaSirkusuVar)
                    {
                        belgelerTam = false;
                        eksikBelgeler.Add("İmza Sirküsü");
                    }

                    if (!belgelerTam)
                    {
                        string eksikler = string.Join(", ", eksikBelgeler);
                        return Json(new
                        {
                            success = false,
                            message = $"Distribütör Onayı için gerekli belgeler eksik!<br>Eksik belgeler: {eksikler}",
                            belgelerEksik = true,
                            eksikBelgeler = eksikBelgeler
                        });
                    }
                }

                // Durum kontrolü
                SozlesmeDurumu yeniDurum = _durumRepo.Getir(x => x.Id == durumId && x.Durumu == 1);
                if (yeniDurum == null)
                    return Json(new { success = false, message = "Seçilen durum geçerli değil." });

                int eskiDurumId = sozlesme.SozlesmeDurumuId;

                // Muhasebelendi'ye butonla geçilecek, dropdown'dan seçilemez
                if (durumId == 11)
                    return Json(new { success = false, message = "Muhasebelendi durumuna sadece özel buton ile geçilebilir." });

                // Muhasebelendi'den geri alınırken dropdown'dan seçilemez
                if (eskiDurumId == 11 && durumId == 10)
                    return Json(new { success = false, message = "Muhasebelendi durumundan geri almak için 'Muhasebelendi Geri Al' butonunu kullanın." });

                // Özel kontrol: Muhasebelendi'den Onaylandı'ya geçiş
                if (eskiDurumId == 11 && durumId == 14)
                {
                    // Bu geçişe izin ver, özel buton gerekmez
                    // Onaylandı durumuna geçtiğinde teklif durumunu güncelle
                    Teklif teklif = _teklifRepo.Getir(sozlesme.TeklifId);
                    if (teklif != null)
                    {
                        teklif.TeklifDurumId = 8; // Onaylandı durumu (örnek ID, kendi sisteminize göre ayarlayın)
                        teklif.GuncellenmeTarihi = DateTime.Now;
                        _teklifRepo.Guncelle(teklif);
                    }
                }

                // Sıralı durum kontrolü (hem ileri hem geri)
                if (!IsValidTwoWayTransition(eskiDurumId, durumId))
                {
                    string mesaj = GetStateTransitionErrorMessage(eskiDurumId, durumId);
                    return Json(new { success = false, message = mesaj });
                }

                // Özel durum kontrolleri
                if (durumId == 12) // İptal durumu
                {
                    if (string.IsNullOrWhiteSpace(iptalNeden))
                        return Json(new { success = false, message = "İptal nedeni zorunludur." });

                    sozlesme.IptalNeden = iptalNeden;

                    // Teklif durumunu "Müşteri Onayladı" (durum id'si 6) yap
                    Teklif teklif = _teklifRepo.Getir(sozlesme.TeklifId);
                    if (teklif != null)
                    {
                        teklif.TeklifDurumId = 6; // Müşteri Onayladı
                        teklif.GuncellenmeTarihi = DateTime.Now;
                        _teklifRepo.Guncelle(teklif);
                    }
                }
                else if (durumId == 14) // Onaylandı durumu
                {
                    // Onaylandı durumuna geçtiğinde teklif durumunu güncelle
                    Teklif teklif = _teklifRepo.Getir(sozlesme.TeklifId);
                    if (teklif != null)
                    {
                        teklif.TeklifDurumId = 8; // Onaylandı durumu (örnek ID, kendi sisteminize göre ayarlayın)
                        teklif.GuncellenmeTarihi = DateTime.Now;
                        _teklifRepo.Guncelle(teklif);
                    }
                }
                else if (durumId == 6 && eskiDurumId > 6) // İptal'den geri dönüş
                {
                    // İptal'den geri dönersek teklif durumunu sözleşmeye göre güncelle
                    Teklif teklif = _teklifRepo.Getir(sozlesme.TeklifId);
                    if (teklif != null)
                    {
                        teklif.TeklifDurumId = 6; // Müşteri Onayladı
                        teklif.GuncellenmeTarihi = DateTime.Now;
                        _teklifRepo.Guncelle(teklif);
                    }
                }

                // Durum güncelle
                sozlesme.SozlesmeDurumuId = durumId;
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                // Lisanslama kontrolü (Distribütör Onayı sonrası)
                if (durumId == 9 && eskiDurumId != 9) // Distribütör Onayı
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Durum '{yeniDurum.Adi}' olarak güncellendi. Artık lisanslama yapılabilir.",
                        yeniDurum = yeniDurum.Adi,
                        durumId = durumId,
                        lisanslamaAktif = true
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Durum '{yeniDurum.Adi}' olarak güncellendi.",
                    yeniDurum = yeniDurum.Adi,
                    durumId = durumId,
                    lisanslamaAktif = durumId == 9 // Distribütör Onayı'nda lisanslama aktif
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult CheckBelgeler(int id)
        {
            try
            {
                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                bool belgelerTam = true;
                List<string> eksikBelgeler = new List<string>();

                // Vergi Kimlik Levhası kontrolü
                if (!sozlesme.VergiKimlikLevhasıVar)
                {
                    belgelerTam = false;
                    eksikBelgeler.Add("Vergi Kimlik Levhası");
                }

                // Ticari Sicil Gazetesi kontrolü
                if (!sozlesme.TicariSicilGazetesiVar)
                {
                    belgelerTam = false;
                    eksikBelgeler.Add("Ticari Sicil Gazetesi");
                }

                // Kimlik Ön Yüzü kontrolü
                if (!sozlesme.KimlikOnYuzuVar)
                {
                    belgelerTam = false;
                    eksikBelgeler.Add("Kimlik Ön Yüzü");
                }

                // İmza Sirküsü kontrolü
                if (!sozlesme.ImzaSirkusuVar)
                {
                    belgelerTam = false;
                    eksikBelgeler.Add("İmza Sirküsü");
                }

                return Json(new
                {
                    success = true,
                    belgelerTam = belgelerTam,
                    belgelerEksik = !belgelerTam,
                    eksikBelgeler = eksikBelgeler
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Kontrol sırasında hata: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult Lisansla(int id, string lisansNo)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Distribütör Onayı kontrolü
                if (sozlesme.SozlesmeDurumuId != 9)
                    return Json(new { success = false, message = "Sözleşme lisanslanabilir durumda değil. Önce Distribütör Onayı gereklidir." });

                if (string.IsNullOrWhiteSpace(lisansNo))
                    return Json(new { success = false, message = "Lisans numarası gerekli." });

                // Lisans numarasını güncelle ve durumu "Lisanslandı" yap
                sozlesme.LisansNo = lisansNo;
                sozlesme.SozlesmeDurumuId = 10; // Lisanslandı
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = "Sözleşme başarıyla lisanslandı.",
                    lisansNo = lisansNo,
                    durumId = 10,
                    durumAdi = "Lisanslandı"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult LisansGeriAl(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Lisanslandı kontrolü
                if (sozlesme.SozlesmeDurumuId != 10)
                    return Json(new { success = false, message = "Sadece 'Lisanslandı' durumundaki sözleşmeler lisans geri alınabilir." });

                // Lisans numarasını temizle ve durumu "Distribütör Onayı" yap
                sozlesme.LisansNo = null;
                sozlesme.SozlesmeDurumuId = 9; // Distribütör Onayı
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = "Sözleşme lisanslaması geri alındı.",
                    durumId = 9,
                    durumAdi = "Distribütör Onayı"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DosyaIndir(int id)
        {
            LoadCommonData();

            MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(id);
            if (sozlesme == null || string.IsNullOrEmpty(sozlesme.DosyaAdi))
            {
                TempData["Hata"] = "Dosya bulunamadı.";
                return RedirectToAction("Index");
            }

            string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.DosyaAdi);

            if (!System.IO.File.Exists(dosyaYolu))
            {
                TempData["Hata"] = "Dosya bulunamadı.";
                return RedirectToAction("Index");
            }

            string orjinalAd = sozlesme.DokumanNo + Path.GetExtension(sozlesme.DosyaAdi);
            var fileBytes = System.IO.File.ReadAllBytes(dosyaYolu);
            return File(fileBytes, "application/octet-stream", orjinalAd);
        }

        [HttpGet]
        public IActionResult DosyaGoruntule(int id)
        {
            LoadCommonData();
            MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(id);
            if (sozlesme == null || string.IsNullOrEmpty(sozlesme.DosyaAdi))
            {
                TempData["Hata"] = "Dosya bulunamadı.";
                return RedirectToAction("Index");
            }

            string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.DosyaAdi);

            if (!System.IO.File.Exists(dosyaYolu))
            {
                TempData["Hata"] = "Dosya bulunamadı.";
                return RedirectToAction("Index");
            }

            string contentType = GetContentType(sozlesme.DosyaAdi);
            return PhysicalFile(dosyaYolu, contentType);
        }

        [HttpPost]
        public IActionResult Sil(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Oturum süresi doldu." });
                }

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(id);
                if (sozlesme == null)
                {
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });
                }

                // Dosyayı fiziksel olarak sil
                if (!string.IsNullOrEmpty(sozlesme.DosyaAdi))
                {
                    string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.DosyaAdi);
                    if (System.IO.File.Exists(dosyaYolu))
                    {
                        System.IO.File.Delete(dosyaYolu);
                    }
                }

                Teklif teklif = _teklifRepo.Getir(x => x.Id == sozlesme.TeklifId);
                teklif.TeklifDurumId = 6;
                teklif.GuncellenmeTarihi = DateTime.Now;
                _teklifRepo.Guncelle(teklif);

                sozlesme.Durumu = 0;
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;
                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = $"{sozlesme.DokumanNo} numaralı sözleşme başarıyla silindi."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirSozlesmeDetay(int id)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(id);
                if (sozlesme == null || sozlesme.Durumu == 0)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                sozlesme.Musteri = _musteriRepo.Getir(sozlesme.MusteriId);
                sozlesme.SozlesmeDurumu = _durumRepo.Getir(sozlesme.SozlesmeDurumuId);

                var sozlesmeBilgileri = new
                {
                    id = sozlesme.Id,
                    dokumanNo = sozlesme.DokumanNo,
                    lisansNo = sozlesme.LisansNo,
                    musteriAdi = $"{sozlesme.Musteri?.Ad} {sozlesme.Musteri?.Soyad}",
                    musteriTelefon = sozlesme.Telefon,
                    musteriEmail = sozlesme.Email,
                    ticariUnvan = sozlesme.TicariUnvan,
                    adi = sozlesme.Adi,
                    soyadi = sozlesme.Soyadi,
                    adres1 = sozlesme.Adres1,
                    adres2 = sozlesme.Adres2,
                    il = sozlesme.Il,
                    ilce = sozlesme.Ilce,
                    vergiDairesi = sozlesme.VergiDairesi,
                    vergiNo = sozlesme.VergiNo,
                    yillikBakim = sozlesme.YillikBakim,
                    sozlesmeTipi = sozlesme.SozlesmeTipi,
                    odemeBekleme = sozlesme.OdemeBekleme,
                    smsBilgilendirme = sozlesme.SmsBilgilendirme,
                    emailBilgilendirme = sozlesme.EmailBilgilendirme,
                    telefonBilgilendirme = sozlesme.TelefonBilgilendirme,
                    haberPaylasimi = sozlesme.HaberPaylasimi,
                    yayinTarihi = sozlesme.YayinTarihi.ToString("dd.MM.yyyy"),
                    revizeTarihi = sozlesme.RevizeTarihi.ToString("dd.MM.yyyy"),
                    revizyonNo = sozlesme.RevizyonNo,
                    durumId = sozlesme.SozlesmeDurumuId,
                    durumAdi = sozlesme.SozlesmeDurumu?.Adi ?? "Belirtilmemiş",
                    dosyaAdi = sozlesme.DosyaAdi,
                    eklenmeTarihi = sozlesme.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    guncellenmeTarihi = sozlesme.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    teklifId = sozlesme.TeklifId,
                    // Doküman checkbox durumları
                    vergiKimlikLevhasıVar = sozlesme.VergiKimlikLevhasıVar,
                    ticariSicilGazetesiVar = sozlesme.TicariSicilGazetesiVar,
                    kimlikOnYuzuVar = sozlesme.KimlikOnYuzuVar,
                    imzaSirkusuVar = sozlesme.ImzaSirkusuVar,
                    // Doküman dosya adları
                    vergiKimlikLevhasıDosyaAdi = sozlesme.VergiKimlikLevhasıDosyaAdi,
                    ticariSicilGazetesiDosyaAdi = sozlesme.TicariSicilGazetesiDosyaAdi,
                    kimlikOnYuzuDosyaAdi = sozlesme.KimlikOnYuzuDosyaAdi,
                    imzaSirkusuDosyaAdi = sozlesme.ImzaSirkusuDosyaAdi
                };

                return Json(new
                {
                    success = true,
                    sozlesme = sozlesmeBilgileri
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // YENİ: Doküman yükleme işlemleri
        [HttpPost]
        public async Task<IActionResult> DokumanYukle(int sozlesmeId, string dokumanTipi, IFormFile dosya)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == sozlesmeId && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                if (dosya == null || dosya.Length == 0)
                    return Json(new { success = false, message = "Lütfen bir dosya seçin." });

                // Dosya boyutu kontrolü (10MB)
                if (dosya.Length > 10 * 1024 * 1024)
                    return Json(new { success = false, message = "Dosya boyutu 10MB'dan büyük olamaz." });

                // Dosya uzantısı kontrolü
                var izinliUzantilar = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
                if (!izinliUzantilar.Contains(uzanti))
                    return Json(new { success = false, message = "Geçersiz dosya formatı. İzinli formatlar: PDF, JPG, JPEG, PNG, DOC, DOCX" });

                // Klasör yolu
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme");

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Benzersiz dosya adı oluştur
                string guvenliDosyaAdi = $"{sozlesmeId}_{dokumanTipi}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{uzanti}";
                string filePath = Path.Combine(uploadsFolder, guvenliDosyaAdi);

                // Dosyayı kaydet
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    await dosya.CopyToAsync(stream);
                }

                // Sözleşmedeki ilgili alanları güncelle
                switch (dokumanTipi)
                {
                    case "VergiKimlikLevhası":
                        sozlesme.VergiKimlikLevhasıVar = true;
                        sozlesme.VergiKimlikLevhasıDosyaAdi = guvenliDosyaAdi;
                        break;
                    case "TicariSicilGazetesi":
                        sozlesme.TicariSicilGazetesiVar = true;
                        sozlesme.TicariSicilGazetesiDosyaAdi = guvenliDosyaAdi;
                        break;
                    case "KimlikOnYuzu":
                        sozlesme.KimlikOnYuzuVar = true;
                        sozlesme.KimlikOnYuzuDosyaAdi = guvenliDosyaAdi;
                        break;
                    case "ImzaSirkusu":
                        sozlesme.ImzaSirkusuVar = true;
                        sozlesme.ImzaSirkusuDosyaAdi = guvenliDosyaAdi;
                        break;
                }

                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;
                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = $"{dokumanTipi} başarıyla yüklendi.",
                    dokumanTipi = dokumanTipi,
                    dosyaAdi = guvenliDosyaAdi,
                    orjinalDosyaAdi = dosya.FileName,
                    dosyaBoyutu = dosya.Length,
                    yuklenmeTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Dosya yükleme hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DokumanSil(int id, string dokumanTipi)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                string? dosyaAdi = null;

                // Hangi dosya tipi silinecek
                switch (dokumanTipi)
                {
                    case "VergiKimlikLevhası":
                        dosyaAdi = sozlesme.VergiKimlikLevhasıDosyaAdi;
                        sozlesme.VergiKimlikLevhasıVar = false;
                        sozlesme.VergiKimlikLevhasıDosyaAdi = null;
                        break;
                    case "TicariSicilGazetesi":
                        dosyaAdi = sozlesme.TicariSicilGazetesiDosyaAdi;
                        sozlesme.TicariSicilGazetesiVar = false;
                        sozlesme.TicariSicilGazetesiDosyaAdi = null;
                        break;
                    case "KimlikOnYuzu":
                        dosyaAdi = sozlesme.KimlikOnYuzuDosyaAdi;
                        sozlesme.KimlikOnYuzuVar = false;
                        sozlesme.KimlikOnYuzuDosyaAdi = null;
                        break;
                    case "ImzaSirkusu":
                        dosyaAdi = sozlesme.ImzaSirkusuDosyaAdi;
                        sozlesme.ImzaSirkusuVar = false;
                        sozlesme.ImzaSirkusuDosyaAdi = null;
                        break;
                    default:
                        return Json(new { success = false, message = "Geçersiz doküman tipi." });
                }

                // Fiziksel dosyayı sil
                if (!string.IsNullOrEmpty(dosyaAdi))
                {
                    string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);
                    if (System.IO.File.Exists(dosyaYolu))
                    {
                        System.IO.File.Delete(dosyaYolu);
                    }
                }

                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;
                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = $"{dokumanTipi} başarıyla silindi."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DokumanIndir(int id, string dokumanTipi)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return NotFound("Sözleşme bulunamadı.");

                string? dosyaAdi = null;
                string? orjinalAd = null;

                // Hangi dosya tipi indirilecek
                switch (dokumanTipi)
                {
                    case "VergiKimlikLevhası":
                        dosyaAdi = sozlesme.VergiKimlikLevhasıDosyaAdi;
                        orjinalAd = $"VergiKimlikLevhası_{sozlesme.DokumanNo}";
                        break;
                    case "TicariSicilGazetesi":
                        dosyaAdi = sozlesme.TicariSicilGazetesiDosyaAdi;
                        orjinalAd = $"TicariSicilGazetesi_{sozlesme.DokumanNo}";
                        break;
                    case "KimlikOnYuzu":
                        dosyaAdi = sozlesme.KimlikOnYuzuDosyaAdi;
                        orjinalAd = $"KimlikOnYuzu_{sozlesme.DokumanNo}";
                        break;
                    case "ImzaSirkusu":
                        dosyaAdi = sozlesme.ImzaSirkusuDosyaAdi;
                        orjinalAd = $"ImzaSirkusu_{sozlesme.DokumanNo}";
                        break;
                    default:
                        return NotFound("Doküman tipi bulunamadı.");
                }

                if (string.IsNullOrEmpty(dosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya bulunamadı.");

                string contentType = GetContentType(dosyaAdi);
                string extension = Path.GetExtension(dosyaAdi);
                return PhysicalFile(dosyaYolu, contentType, $"{orjinalAd}{extension}");
            }
            catch (Exception ex)
            {
                return BadRequest("Dosya indirilirken hata oluştu: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult DokumanGoruntule(int id, string dokumanTipi)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return NotFound("Sözleşme bulunamadı.");

                string? dosyaAdi = null;

                // Hangi dosya tipi görüntülenecek
                switch (dokumanTipi)
                {
                    case "VergiKimlikLevhası":
                        dosyaAdi = sozlesme.VergiKimlikLevhasıDosyaAdi;
                        break;
                    case "TicariSicilGazetesi":
                        dosyaAdi = sozlesme.TicariSicilGazetesiDosyaAdi;
                        break;
                    case "KimlikOnYuzu":
                        dosyaAdi = sozlesme.KimlikOnYuzuDosyaAdi;
                        break;
                    case "ImzaSirkusu":
                        dosyaAdi = sozlesme.ImzaSirkusuDosyaAdi;
                        break;
                    default:
                        return NotFound("Doküman tipi bulunamadı.");
                }

                if (string.IsNullOrEmpty(dosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya bulunamadı.");

                string contentType = GetContentType(dosyaAdi);
                return PhysicalFile(dosyaYolu, contentType);
            }
            catch (Exception ex)
            {
                return BadRequest("Dosya görüntülenirken hata oluştu: " + ex.Message);
            }
        }
        [HttpPost]
        public IActionResult LisansIptalEt(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Lisanslandı kontrolü
                if (sozlesme.SozlesmeDurumuId != 10)
                    return Json(new { success = false, message = "Sadece 'Lisanslandı' durumundaki sözleşmeler lisans iptal edilebilir." });

                // Lisans numarasını temizle ve durumu "Distribütör Onayı" yap
                sozlesme.LisansNo = ""; // Boş string olarak ayarla
                sozlesme.SozlesmeDurumuId = 9; // Distribütör Onayı
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = "Lisans iptal edildi ve lisans numarası temizlendi.",
                    durumId = 9,
                    durumAdi = "Distribütör Onayı"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult FiltreliListe(DateTime? baslangicTarih, DateTime? bitisTarih, int? durumId, string? arama)
        {
            LoadCommonData();

            var sorgu = _sozlesmeRepo.GetirList(x => x.Durumu == 1).AsQueryable();

            // Tarih filtresi
            if (baslangicTarih.HasValue)
            {
                sorgu = sorgu.Where(s => s.YayinTarihi.Date >= baslangicTarih.Value.Date);
            }

            if (bitisTarih.HasValue)
            {
                sorgu = sorgu.Where(s => s.YayinTarihi.Date <= bitisTarih.Value.Date);
            }

            // Durum filtresi
            if (durumId.HasValue && durumId.Value > 0)
            {
                sorgu = sorgu.Where(s => s.SozlesmeDurumuId == durumId.Value);
            }

            // Arama
            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu = sorgu.Where(s =>
                    s.DokumanNo.Contains(arama) ||
                    s.LisansNo.Contains(arama) ||
                    (s.Musteri.Ad + " " + s.Musteri.Soyad).Contains(arama) ||
                    s.Email.Contains(arama)
                );
            }

            var sozlesmeler = sorgu.OrderByDescending(s => s.YayinTarihi).ToList();

            // Navigation property'leri doldur
            foreach (MusteriSozlesme s in sozlesmeler)
            {
                s.Musteri = _musteriRepo.Getir(s.MusteriId);
                s.SozlesmeDurumu = _durumRepo.Getir(s.SozlesmeDurumuId);
                s.Teklif = _teklifRepo.Getir(s.TeklifId);
            }

            return PartialView("_SozlesmeListesi", sozlesmeler);
        }
        [HttpPost]
        public IActionResult DokumanCheckboxGuncelle(int id, string dokumanTipi, bool durum)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                MusteriSozlesme sozlesme = _sozlesmeRepo.Getir(x => x.Id == id && x.Durumu == 1);
                if (sozlesme == null)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                // Hangi checkbox güncellenecek
                switch (dokumanTipi)
                {
                    case "VergiKimlikLevhası":
                        sozlesme.VergiKimlikLevhasıVar = durum;
                        // Eğer checkbox işareti kaldırılıyorsa dosya adını da temizle
                        if (!durum)
                            sozlesme.VergiKimlikLevhasıDosyaAdi = null;
                        break;
                    case "TicariSicilGazetesi":
                        sozlesme.TicariSicilGazetesiVar = durum;
                        if (!durum)
                            sozlesme.TicariSicilGazetesiDosyaAdi = null;
                        break;
                    case "KimlikOnYuzu":
                        sozlesme.KimlikOnYuzuVar = durum;
                        if (!durum)
                            sozlesme.KimlikOnYuzuDosyaAdi = null;
                        break;
                    case "ImzaSirkusu":
                        sozlesme.ImzaSirkusuVar = durum;
                        if (!durum)
                            sozlesme.ImzaSirkusuDosyaAdi = null;
                        break;
                    default:
                        return Json(new { success = false, message = "Geçersiz doküman tipi." });
                }

                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;
                _sozlesmeRepo.Guncelle(sozlesme);

                return Json(new
                {
                    success = true,
                    message = $"{dokumanTipi} durumu güncellendi."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Güncelleme sırasında hata: " + ex.Message });
            }
        }

        private string GetContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        // Çift yönlü durum geçişi kontrolü (hem ileri hem geri)
        private bool IsValidTwoWayTransition(int fromStateId, int toStateId)
        {
            // Aynı duruma geçiş yapılamaz
            if (fromStateId == toStateId) return false;

            // Muhasebelendi'ye ve geri alınmasına özel kontroller
            if (toStateId == 11 || (fromStateId == 11 && toStateId == 10)) return false;

            // Çift yönlü geçerli durum geçişleri
            Dictionary<int, List<int>> validTwoWayTransitions = new Dictionary<int, List<int>>
    {
        { 6, new List<int> { 7, 12 } },    // Müşteri SMS Onayında ↔ Bayi Onayı veya İptal
        { 7, new List<int> { 6, 9, 12 } }, // Bayi Onayı ↔ Müşteri SMS Onayında, Distribütör Onayı veya İptal
        { 9, new List<int> { 7, 10, 12 } }, // Distribütör Onayı ↔ Bayi Onayı, Lisanslandı veya İptal
        { 10, new List<int> { 9, 12, 11 } },   // Lisanslandı ↔ Distribütör Onayı, İptal veya Muhasebelendi
        { 11, new List<int> { 12, 14 } },      // Muhasebelendi ↔ İptal veya Onaylandı
        { 12, new List<int> { 6, 7, 9, 10, 11, 14 } }, // İptal ↔ Tüm durumlara dönebilir
        { 14, new List<int> { 11, 12 } }  // Onaylandı ↔ Muhasebelendi veya İptal
    };

            if (validTwoWayTransitions.ContainsKey(fromStateId))
            {
                return validTwoWayTransitions[fromStateId].Contains(toStateId);
            }

            return false;
        }

        private string GetStateTransitionErrorMessage(int fromStateId, int toStateId)
        {
            Dictionary<int, string> stateNames = new Dictionary<int, string>
    {
        { 6, "Müşteri SMS Onayında" },
        { 7, "Bayi Onayı" },
        { 9, "Distribütör Onayı" },
        { 10, "Lisanslandı" },
        { 11, "Muhasebelendi" },
        { 12, "İptal" },
        { 14, "Onaylandı" } // Yeni eklendi
    };

            string fromName = stateNames.ContainsKey(fromStateId) ? stateNames[fromStateId] : fromStateId.ToString();
            string toName = stateNames.ContainsKey(toStateId) ? stateNames[toStateId] : toStateId.ToString();

            // Özel mesajlar
            if (toStateId == 11)
                return "Muhasebelendi durumuna geçmek için 'Muhasebelendi Yap' butonunu kullanın.";

            if (fromStateId == 11 && toStateId == 10)
                return "Muhasebelendi durumundan geri almak için 'Muhasebelendi Geri Al' butonunu kullanın.";

            // Muhasebelendi'den Onaylandı'ya geçiş mesajı
            if (fromStateId == 11 && toStateId == 14)
                return $"'{fromName}' durumundan '{toName}' durumuna geçilebilir.";

            return $"'{fromName}' durumundan '{toName}' durumuna geçilemez. Geçerli durum geçişleri: Çift yönlü sıralama (İptal hariç): Müşteri SMS Onayında ↔ Bayi Onayı ↔ Distribütör Onayı ↔ Lisanslandı ↔ Muhasebelendi ↔ Onaylandı";
        }
    }
}