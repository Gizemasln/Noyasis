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

            var redirectResult = LoadCommonData();
            if (redirectResult != null) return redirectResult;

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
                // Müşteri: Sadece kendi kayıtları
                query = query.Where(x => x.MusteriId == musteri.Id);
            }
            else if (kullaniciTipi == "Bayi" && bayi != null)
            {
                // Bayi: Kendi kayıtları + alt bayilerin kayıtları + kendi bayisine bağlı müşteri kayıtları

                // Önce bu bayinin tüm alt bayilerini bul (alt bayiler ve onların alt bayileri)
                var altBayiIdleri = GetAllSubBayiIds(bayi.Id);

                // Bu bayinin kendi Id'sini de listeye ekle
                var tumBayiIdleri = new List<int> { bayi.Id };
                tumBayiIdleri.AddRange(altBayiIdleri);

                // Repository'deki yeni metodu kullanarak müşteri ID'lerini bul
                var musteriIdleri = _musteriRepository.GetMusteriIdleriByBayiIdleri(tumBayiIdleri);

                // Filtrele:
                query = query.Where(x =>
                    (x.BayiId.HasValue && tumBayiIdleri.Contains(x.BayiId.Value)) ||
                    (x.MusteriId.HasValue && musteriIdleri.Contains(x.MusteriId.Value))
                );

                // Bayi bilgilerini ViewBag'e ekle
                ViewBag.BayiInfo = bayi;
            }

            // Listeyi sırala ve çek
            List<ArgeHata> liste = query
                .OrderByDescending(x => x.EklenmeTarihi)
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

                    if (bayis.Distributor == true)  // Distributor ise yetkili kabul et (senin mantığına göre)
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
                    // Distributor olmayan bayi → yetkisiz
                }

                if (!yetkili)
                {
                    return Json(new { success = false, message = "Bu cevabı düzenleme yetkiniz yok" });
                }

                // Güncelleme
                kayit.GuncellenmeTarihi = DateTime.Now;
                 _argeHataRepository.Guncelle(kayit);  // ← await ekledim (async metod)

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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return RedirectToAction("Index");

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                ArgeHata mevcut = _argeHataRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "Kayıt bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Soft delete
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _argeHataRepository.Guncelle(mevcut);
                TempData["Success"] = "Kayıt başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Silme işlemi sırasında hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DurumGuncelle(int id, int durumId)
        {
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });
            LoadCommonData();

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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

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

                return Json(new
                {
                    success = true,
                    id = kayit.Id,
                    tipi = kayit.Tipi,
                    adi = kayit.Adi,
                    soyadi = kayit.Soyadi,
                    metni = kayit.Metni,
                    dosyaGorunum = dosyaGorunum,
                    musteriAdi = kayit.Musteri?.Ad ?? "Belirtilmemiş",
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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

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
    }
}