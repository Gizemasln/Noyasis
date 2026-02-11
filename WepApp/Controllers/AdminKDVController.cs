using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminKDVController : AdminBaseController
    {
        private readonly KDVRepository _kdvRepository = new KDVRepository();

        public IActionResult Index()
        {
            LoadCommonData();

            List<KDV> kdvOranlari = _kdvRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Oran)
                .ToList();

            ViewBag.KDVListesi = kdvOranlari;

            return View();
        }

        [HttpPost]
        public IActionResult Ekle(decimal Oran)
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

                // Oran kontrolü (0-100 arası)
                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "KDV oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                KDV existing = _kdvRepository.GetirList(x => x.Oran == Oran && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu KDV oranı zaten mevcut.";
                    return RedirectToAction("Index");
                }

                KDV yeniKDV = new KDV
                {
                    Oran = Oran,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _kdvRepository.Ekle(yeniKDV);
                TempData["Success"] = "KDV oranı başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"KDV oranı eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, decimal Oran)
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

                // Oran kontrolü (0-100 arası)
                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "KDV oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                KDV existing = _kdvRepository.Getir(Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "KDV oranı bulunamadı.";
                    return RedirectToAction("Index");
                }

                KDV duplicate = _kdvRepository.GetirList(x => x.Oran == Oran && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu KDV oranı zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Oran = Oran;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _kdvRepository.Guncelle(existing);
                TempData["Success"] = "KDV oranı başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"KDV oranı güncellenirken hata oluştu: {ex.Message}";
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

                KDV existing = _kdvRepository.Getir(Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "KDV oranı bulunamadı.";
                    return RedirectToAction("Index");
                }
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _kdvRepository.Guncelle(existing);
                TempData["Success"] = "KDV oranı başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"KDV oranı silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                return Unauthorized();
            }

            KDV item = _kdvRepository.Getir(id);
            if (item == null || item.Durumu == 0)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                oran = item.Oran
            });
        }
    }
}