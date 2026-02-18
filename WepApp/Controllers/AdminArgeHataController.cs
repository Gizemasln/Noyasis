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

        public IActionResult Index()
        {
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return redirectResult;

            List<string> join = new List<string>();
            join.Add("LisansTip");
            join.Add("Musteri");
            join.Add("Bayi");
            join.Add("ARGEDurum");

            List<ArgeHata> liste = _argeHataRepository.GetirList(x => x.Durumu == 1, join)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();

            List<ARGEDurum> durumListesi = _argeDurumRepository.GetirList(x => x.Durumu == 1 && x.Id!=6)
                .OrderBy(x => x.Sira)
                .ToList();
            ViewBag.DurumListesi = durumListesi;
            List<ARGEDurum> durumListesis = _argeDurumRepository.GetirList(x => x.Durumu == 1 && x.Id != 6)
             .OrderBy(x => x.Sira)
             .ToList();
            ViewBag.DurumListesi = durumListesi;

            // Kullanıcı bilgilerini al
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;

            // Eğer bayi ise bayi bilgilerini al
            Bayi bayi = null;
            if (kullaniciTipi == "Bayi")
            {
                bayi = _bayiRepository.Getir(x => x.Id == kullaniciId && x.Durumu == 1);
                ViewBag.BayiInfo = bayi;
            }

            ViewBag.ArgeHataListesi = liste;
            return View();
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

                // Cevap zaten verilmişse güncelleme yapılabilir
                mevcut.DistributorCevap = aciklama;
                mevcut.DistributorCevapVerdiMi = true;
                mevcut.DistributorCevapTarihi = DateTime.Now;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;
                mevcut.ARGEDurumId = 6;

                // Hangi tip kullanıcı cevap verdiğini kaydet
                if (kullaniciTipi == "Kurumsal")
                {
                    mevcut.AdminCevapVerdiMi = true;
                    mevcut.AdminCevapTarihi = DateTime.Now;
                    mevcut.AdminKullaniciId = kullaniciId;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    mevcut.DistributorCevapVerdiMi = true;
                    mevcut.DistributorCevapTarihi = DateTime.Now;
                    mevcut.DistributorBayiId = kullaniciId;
                }

                _argeHataRepository.Guncelle(mevcut);

                // Cevap veren kullanıcı bilgilerini al
                string cevapVerenAdi = "";
                if (kullaniciTipi == "Kurumsal")
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
                    message = "Cevabınız başarıyla kaydedildi.",
                    distributorCevap = aciklama,
                    cevapVerdiMi = true,
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

        [HttpPost]
        public IActionResult CevapGuncelle(int id, string aciklama)
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

                // Sadece cevap veren kişi güncelleyebilir
                if (kullaniciTipi == "Kurumsal" && !mevcut.AdminCevapVerdiMi )
                {
                    return Json(new { success = false, message = "Sadece cevap veren admin bu cevabı güncelleyebilir." });
                }
                if (kullaniciTipi == "Bayi" && mevcut.DistributorBayiId != kullaniciId)
                {
                    return Json(new { success = false, message = "Sadece cevap veren bayi bu cevabı güncelleyebilir." });
                }

                // Cevap güncelleme
                mevcut.DistributorCevap = aciklama;
                mevcut.DistributorCevapTarihi = DateTime.Now;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _argeHataRepository.Guncelle(mevcut);

                return Json(new
                {
                    success = true,
                    message = "Cevabınız başarıyla güncellendi.",
                    distributorCevap = aciklama,
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
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

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                return Json(new
                {
                    success = true,
                    distributorCevap = kayit.DistributorCevap ?? "",
                    distributorCevapVerdiMi = kayit.DistributorCevapVerdiMi,
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    adminCevapVerdiMi = kayit.AdminCevapVerdiMi,
                    adminCevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    // Kullanıcının bu cevabı düzenleyip düzenleyemeyeceğini kontrol et
                    duzenleyebilir = (kullaniciTipi == "Kurumsal" && kayit.AdminCevapVerdiMi && kayit.AdminKullaniciId == kullaniciId) ||
                                   (kullaniciTipi == "Bayi" && kayit.DistributorCevapVerdiMi && kayit.DistributorBayiId == kullaniciId)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi) || kullaniciTipi != "Kurumsal")
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
            var redirectResult = LoadCommonData();
            if (redirectResult != null) return Json(new { success = false, message = "Bu işlem için giriş yapmanız gerekmektedir." });

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi) || (kullaniciTipi != "Kurumsal" && kullaniciTipi != "Bayi"))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                ArgeHata mevcut = _argeHataRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Bayiler sadece kendi müşterilerinin durumunu güncelleyebilir
                if (kullaniciTipi == "Bayi" && mevcut.BayiId != kullaniciId)
                {
                    return Json(new { success = false, message = "Sadece kendi müşterilerinizin durumunu güncelleyebilirsiniz." });
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

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                // Dosya yolunu kontrol et ve tam yolunu oluştur
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
                    distributorCevap = kayit.DistributorCevap ?? "",
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    adminCevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    eklenmeTarihi = kayit.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    guncellenmeTarihi = kayit.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    // Kullanıcının bu kaydı düzenleyip düzenleyemeyeceğini kontrol et
                    cevapDuzenleyebilir = (kullaniciTipi == "Kurumsal") ||
                                         (kullaniciTipi == "Bayi" && kayit.BayiId == kullaniciId && !kayit.DistributorCevapVerdiMi && !kayit.AdminCevapVerdiMi),
                    durumDuzenleyebilir = (kullaniciTipi == "Kurumsal") || (kullaniciTipi == "Bayi" && kayit.BayiId == kullaniciId),
                    silebilir = (kullaniciTipi == "Kurumsal")
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