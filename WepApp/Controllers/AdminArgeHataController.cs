using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using WebApp.Models;
using System.IO;
using System.Collections.Generic;
using WebApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminArgeHataController : AdminBaseController
    {
        private readonly ArgeHataRepository _argeHataRepository = new ArgeHataRepository();
        private readonly ARGEDurumRepository _argeDurumRepository = new ARGEDurumRepository();
        private readonly BayiRepository _bayiRepository = new BayiRepository();
        private readonly KullanicilarRepository _kullaniciRepository = new KullanicilarRepository();
        private readonly MusteriRepository _musteriRepository = new MusteriRepository();

        public IActionResult Index()
        {
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            // Join listesi
            List<string> join = new List<string>();
            join.Add("LisansTip");
            join.Add("Musteri");
            join.Add("Bayi");
            join.Add("ARGEDurum");

            // Temel sorgu - sadece aktif kayıtlar
            var query = _argeHataRepository.GetirQueryable(x => x.Durumu == 1, join);

            // Kullanıcı bilgilerini al
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();

            // Kullanıcı tipine göre filtreleme yap
            if (kullaniciTipi == "Musteri" && musteri != null)
            {
                query = query.Where(x => x.MusteriId == musteri.Id);
            }
            else if (kullaniciTipi == "Bayi" && bayi != null)
            {
                var altBayiIdleri = GetAllSubBayiIds(bayi.Id);
                var tumBayiIdleri = new List<int> { bayi.Id };
                tumBayiIdleri.AddRange(altBayiIdleri);
                var musteriIdleri = _musteriRepository.GetMusteriIdleriByBayiIdleri(tumBayiIdleri);

                query = query.Where(x =>
                    (x.BayiId.HasValue && tumBayiIdleri.Contains(x.BayiId.Value)) ||
                    (x.MusteriId.HasValue && musteriIdleri.Contains(x.MusteriId.Value))
                );

                ViewBag.BayiInfo = bayi;
            }

            // Listeyi sırala ve ViewModel'e dönüştür
            List<ArgeHataViewModel> liste = query
                .OrderByDescending(x => x.EklenmeTarihi)
                .Select(x => new ArgeHataViewModel
                {
                    Id = x.Id,
                    Tipi = x.Tipi,
                    Adi = x.Adi,
                    Soyadi = x.Soyadi,
                    MusteriAdi = x.Musteri != null ? (x.Musteri.AdSoyad ?? x.Musteri.TicariUnvan) : null,
                    MusteriTelefon = x.Musteri != null ? x.Musteri.Telefon : null,
                    BayiUnvan = x.Bayi != null ? x.Bayi.Unvan : null,
                    LisansTipAdi = x.LisansTip != null ? x.LisansTip.Adi : null,
                    LisansNo = x.LisansNo,
                    Metni = x.Metni,
                    DosyaYolu = x.DosyaYolu,
                    DistributorCevap = x.DistributorCevap,
                    AdminCevap = x.AdminCevap,
                    DistributorCevapVerdiMi = x.DistributorCevapVerdiMi,
                    AdminCevapVerdiMi = x.AdminCevapVerdiMi,
                    DistributorBayiId = x.DistributorBayiId,
                    AdminKullaniciId = x.AdminKullaniciId,
                    ARGEDurumId = x.ARGEDurumId,
                    ARGEDurumAdi = x.ARGEDurum != null ? x.ARGEDurum.Adi : "Yeni",
                    BayiId = x.BayiId,
                    MusteriId = x.MusteriId,
                    EklenmeTarihi = x.EklenmeTarihi,
                    GuncellenmeTarihi = x.GuncellenmeTarihi
                })
                .ToList();

            // Durum listesini al
            List<ARGEDurum> durumListesi = _argeDurumRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Sira)
                .ToList();

            ViewBag.DurumListesi = durumListesi;
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;
            ViewBag.ArgeHataListesi = liste;

            return View();
        }

        // Alt bayileri recursive olarak bulan yardımcı metod
        private List<int> GetAllSubBayiIds(int bayiId)
        {
            var altBayiIdleri = new List<int>();

            // Bu bayinin direkt alt bayilerini bul
            var altBayiler = _bayiRepository.GetirList(x => x.UstBayiId == bayiId && x.Durumu == 1);

            foreach (var altBayi in altBayiler)
            {
                altBayiIdleri.Add(altBayi.Id);
                // Recursive olarak alt bayilerin alt bayilerini bul
                altBayiIdleri.AddRange(GetAllSubBayiIds(altBayi.Id));
            }

            return altBayiIdleri;
        }

        [HttpPost]
        public async Task<IActionResult> CevapGuncelle(int id, string aciklama)
        {
            try
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(aciklama))
                {
                    return Json(new { success = false, message = "Geçersiz veri" });
                }

                var kayit = _argeHataRepository.Getir(id);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }

                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Oturum bulunamadı" });
                }

                string cevapVerenTip = "";
                bool yetkili = false;

                if (kullaniciTipi == "Admin")  // veya "Kurumsal" ise buna göre değiştir
                {
                    yetkili = true;
                    cevapVerenTip = "Admin";
                    kayit.AdminCevap = aciklama.Trim();
                    kayit.AdminCevapTarihi = DateTime.Now;
                    kayit.AdminCevapVerdiMi = true;
                    kayit.AdminKullaniciId = kullaniciId;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    var bayis = _bayiRepository.Getir(x => x.Id == kullaniciId);
                    if (bayis == null)
                    {
                        return Json(new { success = false, message = "Bayi bilgileri alınamadı" });
                    }

                    // Distributor kontrolü - sadece distributor bayiler cevap düzenleyebilir
                    if (bayis.Distributor == true)
                    {
                        if (kayit.DistributorBayiId == kullaniciId && kayit.DistributorCevapVerdiMi)
                        {
                            yetkili = true;
                            cevapVerenTip = "Bayi";
                            kayit.DistributorCevap = aciklama.Trim();
                            kayit.DistributorCevapTarihi = DateTime.Now;
                            kayit.DistributorCevapVerdiMi = true;
                        }
                    }
                }

                if (!yetkili)
                {
                    return Json(new { success = false, message = "Bu cevabı düzenleme yetkiniz yok" });
                }

                // Güncelleme
                kayit.GuncellenmeTarihi = DateTime.Now;
                _argeHataRepository.Guncelle(kayit);

                return Json(new
                {
                    success = true,
                    message = "Cevap başarıyla güncellendi",
                    cevapVerenTip = cevapVerenTip,
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                // Gerçek projede logger kullan
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = "Güncelleme sırasında hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                ArgeHata mevcut = _argeHataRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Soft delete
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _argeHataRepository.Guncelle(mevcut);

                return Json(new { success = true, message = "Kayıt başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Silme işlemi sırasında hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult DurumGuncelle(int id, int durumId)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                ArgeHata mevcut = _argeHataRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Bayi kontrolü - sadece kendi kayıtlarını güncelleyebilir
                if (kullaniciTipi == "Bayi")
                {
                    if (mevcut.BayiId != kullaniciId)
                    {
                        return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                    }
                }

                // Durum güncelleme
                mevcut.ARGEDurumId = durumId;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _argeHataRepository.Guncelle(mevcut);

                // Yeni durum adını getir
                ARGEDurum yeniDurum = _argeDurumRepository.Getir(durumId);
                string durumAdi = yeniDurum?.Adi ?? "Belirtilmemiş";

                return Json(new
                {
                    success = true,
                    message = "Durum başarıyla güncellendi.",
                    durumAdi = durumAdi
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Durum güncelleme sırasında hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult CevapVer(int id, string aciklama)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                ArgeHata mevcut = _argeHataRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Bayi kontrolü - sadece kendi kayıtlarına cevap verebilir
                if (kullaniciTipi == "Bayi")
                {
                    if (mevcut.BayiId != kullaniciId)
                    {
                        return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                    }

                    // Zaten cevap verilmiş mi kontrol et
                    if (mevcut.DistributorCevapVerdiMi)
                    {
                        return Json(new { success = false, message = "Bu kayıt için zaten cevap verilmiş. Tekrar cevap veremezsiniz." });
                    }

                    // Bayi cevabını ve durumu güncelle
                    mevcut.DistributorCevap = aciklama;
                    mevcut.DistributorCevapTarihi = DateTime.Now;
                    mevcut.DistributorCevapVerdiMi = true;
                    mevcut.DistributorBayiId = kullaniciId;
                }

                // Admin kontrolü
                if (kullaniciTipi == "Admin")
                {
                    // Zaten cevap verilmiş mi kontrol et
                    if (mevcut.AdminCevapVerdiMi)
                    {
                        return Json(new { success = false, message = "Bu kayıt için zaten cevap verilmiş. Tekrar cevap veremezsiniz." });
                    }

                    // Admin cevabını ve durumu güncelle
                    mevcut.AdminCevap = aciklama;
                    mevcut.AdminCevapTarihi = DateTime.Now;
                    mevcut.AdminCevapVerdiMi = true;
                    mevcut.AdminKullaniciId = kullaniciId;
                }

                // Ortak alanlar
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _argeHataRepository.Guncelle(mevcut);

                // Cevap veren bilgilerini al
                string cevapVerenAdi = "";
                if (kullaniciTipi == "Admin")
                {
                    var admin = _kullaniciRepository.Getir(x => x.Id == kullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    var bayi = _bayiRepository.Getir(x => x.Id == kullaniciId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                return Json(new
                {
                    success = true,
                    message = "Cevap ve durum başarıyla kaydedildi.",
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    cevapVerenTip = kullaniciTipi,
                    cevapVerenAdi = cevapVerenAdi
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem sırasında hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetirCevap(int id)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();

                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                ArgeHata kayit = _argeHataRepository.Getir(id);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                if (kullaniciTipi == "Bayi")
                {
                    Bayi bayis = _bayiRepository.Getir(x => x.Id == kullaniciId);

                    // Bayi sadece kendi kayıtlarını görebilir
                    if (bayis.Distributor == false)
                    {
                        return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                    }
                }

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                string cevapMetni = "";
                string cevapTarihi = "";

                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    cevapMetni = kayit.AdminCevap ?? "";
                    cevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    cevapMetni = kayit.DistributorCevap ?? "";
                    cevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                ARGEDurum durum = _argeDurumRepository.Getir(kayit.ARGEDurumId ?? 0);

                return Json(new
                {
                    success = true,
                    cevapMetni = cevapMetni,
                    cevapTarihi = cevapTarihi,
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    durumAdi = durum?.Adi ?? "Belirtilmemiş",
                    durumDegisimTarihi = kayit.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult DetayGetir(int id)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Yetkiniz yok" });
                }

                List<string> join = new List<string>();
                join.Add("LisansTip");
                join.Add("Musteri");
                join.Add("Bayi");
                join.Add("ARGEDurum");

                ArgeHata kayit = _argeHataRepository.Getir(x => x.Id == id, join);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }

                // Bayi sadece kendi kayıtlarını görebilir
                if (kullaniciTipi == "Bayi" && kayit.BayiId != kullaniciId)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                string cevapMetni = "";
                string cevapTarihi = "";

                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    cevapMetni = kayit.AdminCevap ?? "";
                    cevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    cevapMetni = kayit.DistributorCevap ?? "";
                    cevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                // Dosya yolunu kontrol et
                string dosyaGorunum = "";
                if (!string.IsNullOrEmpty(kayit.DosyaYolu))
                {
                    string dosyaAdi = Path.GetFileName(kayit.DosyaYolu);
                    dosyaGorunum = $"<a href='/{kayit.DosyaYolu}' target='_blank' class='text-primary'><i class='fas fa-download'></i> {dosyaAdi}</a>";
                }

                // Müşteri bilgisi - AdSoyad birleşik
                string musteriAdi = "Belirtilmemiş";
                if (kayit.Musteri != null)
                {
                    musteriAdi = kayit.Musteri.AdSoyad ?? kayit.Musteri.TicariUnvan ?? "Belirtilmemiş";
                }

                return Json(new
                {
                    success = true,
                    id = kayit.Id,
                    tipi = kayit.Tipi,
                    adSoyad = kayit.Adi + " " + kayit.Soyadi, // Ad ve Soyad'ı birleştir
                    metni = kayit.Metni,
                    dosyaGorunum = dosyaGorunum,
                    musteriAdi = musteriAdi,
                    bayiAdi = kayit.Bayi?.Unvan ?? "Belirtilmemiş",
                    lisansTipAdi = kayit.LisansTip?.Adi ?? "Belirtilmemiş",
                    argeDurumId = kayit.ARGEDurumId,
                    argeDurumAdi = kayit.ARGEDurum?.Adi ?? "Belirtilmemiş",
                    cevapMetni = cevapMetni,
                    cevapTarihi = cevapTarihi,
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    cevapVerdiMi = kayit.AdminCevapVerdiMi || kayit.DistributorCevapVerdiMi,
                    eklenmeTarihi = kayit.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    guncellenmeTarihi = kayit.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetirDurumlar()
        {
            try
            {
                var durumlar = _argeDurumRepository.GetirList(x => x.Durumu == 1)
                    .OrderBy(x => x.Sira)
                    .Select(d => new {
                        id = d.Id,
                        adi = d.Adi,
                        sira = d.Sira
                    })
                    .ToList();

                return Json(new { success = true, durumlar = durumlar });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        public class ArgeHataViewModel
        {
            public int Id { get; set; }
            public string Tipi { get; set; }
            public string Adi { get; set; }
            public string Soyadi { get; set; }
            public string MusteriAdi { get; set; }
            public string MusteriTelefon { get; set; }
            public string BayiUnvan { get; set; }
            public string LisansTipAdi { get; set; }
            public string LisansNo { get; set; }
            public string Metni { get; set; }
            public string DosyaYolu { get; set; }
            public string DistributorCevap { get; set; }
            public string AdminCevap { get; set; }
            public bool DistributorCevapVerdiMi { get; set; }
            public bool AdminCevapVerdiMi { get; set; }
            public int? DistributorBayiId { get; set; }
            public int? AdminKullaniciId { get; set; }
            public int? ARGEDurumId { get; set; }
            public string ARGEDurumAdi { get; set; }
            public int? BayiId { get; set; }
            public int? MusteriId { get; set; }
            public DateTime EklenmeTarihi { get; set; }
            public DateTime? GuncellenmeTarihi { get; set; }
            public LisansTip LisansTip { get; set; }
        }
    }
}