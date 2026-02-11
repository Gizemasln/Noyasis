// WepApp/Controllers/AdminNeredenDuyduController.cs
using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System;
using System.Linq;
using WebApp.Models;

namespace WepApp.Controllers
{
    public class AdminNeredenDuyduController : AdminBaseController
    {
        NeredenDuyduRepository _neredenDuyduRepository = new NeredenDuyduRepository();

      
        public IActionResult Index()
        {
            LoadCommonData();
            List<NeredenDuydu> neredenDuyduListesi = _neredenDuyduRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();
            ViewBag.NeredenDuyduListesi = neredenDuyduListesi;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi)
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

                NeredenDuydu existing = _neredenDuyduRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir kayıt zaten mevcut.";
                    return RedirectToAction("Index");
                }

                NeredenDuydu yeniKayit = new NeredenDuydu
                {
                    Adi = Adi ?? "",
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };
                _neredenDuyduRepository.Ekle(yeniKayit);
                TempData["Success"] = "Kayıt başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Kayıt eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi)
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

                NeredenDuydu existing = _neredenDuyduRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Kayıt bulunamadı.";
                    return RedirectToAction("Index");
                }

                NeredenDuydu duplicate = _neredenDuyduRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir kayıt zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _neredenDuyduRepository.Guncelle(existing);
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Kayıt güncellenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
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

                NeredenDuydu existing = _neredenDuyduRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Kayıt bulunamadı.";
                    return RedirectToAction("Index");
                }
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _neredenDuyduRepository.Guncelle(existing);
                TempData["Success"] = "Kayıt başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Kayıt silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Unauthorized(new { error = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                NeredenDuydu item = _neredenDuyduRepository.Getir(id);
                if (item == null || item.Durumu == 0)
                {
                    return NotFound(new { error = "Kayıt bulunamadı." });
                }

                return Json(new
                {
                    id = item.Id,
                    adi = item.Adi
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
            }
        }
    }
}