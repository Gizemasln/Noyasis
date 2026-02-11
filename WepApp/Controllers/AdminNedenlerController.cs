using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminNedenlerController : AdminBaseController
    {
        private readonly NedenlerRepository _nedenlerRepository = new NedenlerRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<Nedenler> nedenler = _nedenlerRepository.GetirList(x => x.Durumu == 1)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.Nedenler = nedenler;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                // Aynı isimde aktif neden kontrolü
                Nedenler existing = _nedenlerRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir neden zaten mevcut.";
                    return RedirectToAction("Index");
                }

                Nedenler yeniNeden = new Nedenler
                {
                    Adi = Adi ?? "",
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _nedenlerRepository.Ekle(yeniNeden);
                TempData["Success"] = "Neden başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Neden eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                Nedenler existing = _nedenlerRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Neden bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Aynı isimde başka neden kontrolü
                Nedenler duplicate = _nedenlerRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir neden zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _nedenlerRepository.Guncelle(existing);
                TempData["Success"] = "Neden başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Neden güncellenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                Nedenler existing = _nedenlerRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Neden bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Soft delete (Durumu 0 yap)
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _nedenlerRepository.Guncelle(existing);
                TempData["Success"] = "Neden başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Neden silinirken hata oluştu: {ex.Message}";
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

                Nedenler item = _nedenlerRepository.Getir(id);
                if (item == null || item.Durumu == 0)
                {
                    return NotFound(new { error = "Neden bulunamadı." });
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