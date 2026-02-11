using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminLisansTipController : AdminBaseController
    {
        private readonly LisansTipRepository _lisansTipRepository = new LisansTipRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1)
           
                .ToList();
            ViewBag.LisansTipleri = lisansTipleri;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi,int? sayi)
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

                LisansTip existing = _lisansTipRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir lisans tipi zaten mevcut.";
                    return RedirectToAction("Index");
                }

                LisansTip yeniLisansTip = new LisansTip
                {
                    Adi = Adi ?? "",
                    Sayi=sayi,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };
                _lisansTipRepository.Ekle(yeniLisansTip);
                TempData["Success"] = "Lisans tipi başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lisans tipi eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi,int? sayi)
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

                LisansTip existing = _lisansTipRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Lisans tipi bulunamadı.";
                    return RedirectToAction("Index");
                }

                LisansTip duplicate = _lisansTipRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir lisans tipi zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.Sayi = sayi;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _lisansTipRepository.Guncelle(existing);
                TempData["Success"] = "Lisans tipi başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lisans tipi güncellenirken hata oluştu: {ex.Message}";
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

                LisansTip existing = _lisansTipRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Lisans tipi bulunamadı.";
                    return RedirectToAction("Index");
                }
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _lisansTipRepository.Guncelle(existing);
                TempData["Success"] = "Lisans tipi başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lisans tipi silinirken hata oluştu: {ex.Message}";
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

                LisansTip item = _lisansTipRepository.Getir(id);
                if (item == null || item.Durumu == 0)
                {
                    return NotFound(new { error = "Lisans tipi bulunamadı." });
                }

                return Json(new
                {
                    id = item.Id,
                    adi = item.Adi,
                    sayi = item.Sayi
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
            }
        }
    }
}