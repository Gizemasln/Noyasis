using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminSozlesmeDurumuController : AdminBaseController
    {
        private readonly SozlesmeDurumuRepository _sozlesmeDurumuRepository = new SozlesmeDurumuRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<SozlesmeDurumu> durumlar = _sozlesmeDurumuRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();
            ViewBag.SozlesmeDurumlari = durumlar;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string? Aciklama)
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

                SozlesmeDurumu existing = _sozlesmeDurumuRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir sözleşme durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                SozlesmeDurumu yeniDurum = new SozlesmeDurumu
                {
                    Adi = Adi ?? "",
                    Aciklama = Aciklama,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _sozlesmeDurumuRepository.Ekle(yeniDurum);
                TempData["Success"] = "Sözleşme durumu başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Sözleşme durumu eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string? Aciklama)
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

                SozlesmeDurumu existing = _sozlesmeDurumuRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Sözleşme durumu bulunamadı.";
                    return RedirectToAction("Index");
                }

                SozlesmeDurumu duplicate = _sozlesmeDurumuRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir sözleşme durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.Aciklama = Aciklama;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeDurumuRepository.Guncelle(existing);
                TempData["Success"] = "Sözleşme durumu başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Sözleşme durumu güncellenirken hata oluştu: {ex.Message}";
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

                SozlesmeDurumu existing = _sozlesmeDurumuRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Sözleşme durumu bulunamadı.";
                    return RedirectToAction("Index");
                }
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _sozlesmeDurumuRepository.Guncelle(existing);
                TempData["Success"] = "Sözleşme durumu başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Sözleşme durumu silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();
            SozlesmeDurumu item = _sozlesmeDurumuRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                aciklama = item.Aciklama
            });
        }
    }
}