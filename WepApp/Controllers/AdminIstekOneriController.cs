using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using WebApp.Models;
using System.IO;
using System.Collections.Generic;

namespace WepApp.Controllers
{
    public class AdminIstekOneriController : AdminBaseController
    {
        private readonly IstekOneriRepository _istekOneriRepository = new IstekOneriRepository();
        private readonly IstekOneriDurumRepository _istekOneriDurumRepository = new IstekOneriDurumRepository();
        private readonly BayiRepository _bayiRepository = new BayiRepository();

        public IActionResult Index()
        {
            LoadCommonData();

            // Bayi bilgilerini al
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            bool isDistributor = bayi != null && bayi.Distributor;
            ViewBag.Bayi = bayi;
            ViewBag.IsDistributor = isDistributor;

            List<string> join = new List<string>();
            join.Add("LisansTip");
            join.Add("Musteri");
            join.Add("Bayi");
            join.Add("IstekOneriDurum");

            List<IstekOneriler> liste = _istekOneriRepository.GetirList(x => x.Durumu == 1, join)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();

            // Eğer distributor ise, sadece kendi bayi kayıtlarını göster
            if (isDistributor && bayi != null)
            {
                liste = liste.Where(x => x.BayiId == bayi.Id).ToList();
            }

            // Durum listesini ViewBag'e ekle
            List<IstekOneriDurum> durumListesi = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Sira)
                .ToList();
            ViewBag.DurumListesi = durumListesi;

            ViewBag.IstekOneriListesi = liste;
            return View();
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }
                IstekOneriler mevcut = _istekOneriRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "Kayıt bulunamadı.";
                    return RedirectToAction("Index");
                }
                // Soft delete
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;
                _istekOneriRepository.Guncelle(mevcut);
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
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler mevcut = _istekOneriRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Distributor kontrolü
                Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                bool isDistributor = bayi != null && bayi.Distributor;

                if (isDistributor)
                {
                    // Distributor sadece kendi bayi kayıtlarını güncelleyebilir
                    if (mevcut.BayiId != bayi.Id)
                    {
                        return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                    }

                    // Distributor zaten cevap vermiş mi kontrol et
                    if (mevcut.DistributorCevapVerdiMi)
                    {
                        return Json(new { success = false, message = "Bu kayıt için zaten cevap verilmiş. Durum değiştiremezsiniz." });
                    }
                }

                // Durum güncelleme
                mevcut.IstekOneriDurumId = durumId;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _istekOneriRepository.Guncelle(mevcut);

                // Yeni durum adını getir
                IstekOneriDurum yeniDurum = _istekOneriDurumRepository.Getir(durumId);
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
        public IActionResult DistributorCevapVer(int id, string aciklama, int durumId)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (bayi == null || !bayi.Distributor)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler mevcut = _istekOneriRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Sadece kendi bayi kayıtlarına cevap verebilir
                if (mevcut.BayiId != bayi.Id)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                // Zaten cevap verilmiş mi kontrol et
                if (mevcut.DistributorCevapVerdiMi)
                {
                    return Json(new { success = false, message = "Bu kayıt için zaten cevap verilmiş. Tekrar cevap veremezsiniz." });
                }

                // Distributor cevabını ve durumu güncelle
                mevcut.DistributorCevap = aciklama;
                mevcut.DistributorCevapTarihi = DateTime.Now;
                mevcut.DistributorCevapVerdiMi = true;
                mevcut.IstekOneriDurumId = durumId;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _istekOneriRepository.Guncelle(mevcut);

                return Json(new
                {
                    success = true,
                    message = "Cevap ve durum başarıyla güncellendi.",
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem sırasında hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult DetayGetir(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Yetkiniz yok" });
                }
                List<string> join = new List<string>();
                join.Add("LisansTip");
                join.Add("Musteri");
                join.Add("Bayi");
                join.Add("IstekOneriDurum");

                IstekOneriler kayit = _istekOneriRepository.Getir(x => x.Id == id, join);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }

                return Json(new
                {
                    success = true,
                    id = kayit.Id,
                    konu = kayit.Konu,
                    metni = kayit.Metni,
                    musteriAdi = kayit.Musteri?.Ad ?? "Belirtilmemiş",
                    bayiAdi = kayit.Bayi?.Unvan ?? "Belirtilmemiş",
                    lisansTipAdi = kayit.LisansTip?.Adi ?? "Belirtilmemiş",
                    istekDurumId = kayit.IstekOneriDurumId,
                    istekDurumAdi = kayit.IstekOneriDurum?.Adi ?? "Belirtilmemiş",
                    distributorCevap = kayit.DistributorCevap,
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm"),
                    distributorCevapVerdiMi = kayit.DistributorCevapVerdiMi,
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
        public IActionResult GetirDistributorCevap(int id)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (bayi == null || !bayi.Distributor)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler kayit = _istekOneriRepository.Getir(x => x.Id == id);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }

                // Sadece kendi bayi kayıtlarını görebilir
                if (kayit.BayiId != bayi.Id)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriDurum durum = _istekOneriDurumRepository.Getir(kayit.IstekOneriDurumId);

                return Json(new
                {
                    success = true,
                    distributorCevap = kayit.DistributorCevap,
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm"),
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
        public IActionResult GetirDurumlar()
        {
            LoadCommonData();

            try
            {
                var durumlar = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1)
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