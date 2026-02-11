using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminUYBController : AdminBaseController // AdminBaseController'dan miras alıyor (yetki vs.)
    {
        private readonly UYBRepository _uybRepository = new UYBRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            UYB uyb = _uybRepository.Getir(x => x.Durumu == 1);
            ViewBag.UYB = uyb;
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

                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "UYB oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                // Zaten bir kayıt varsa eklemeye izin verme
                UYB mevcut = _uybRepository.Getir(x=> x.Durumu==1);
                if (mevcut != null)
                {
                    TempData["Error"] = "UYB oranı zaten tanımlı. Yeni ekleme yapılamaz, düzenleme yapabilirsiniz.";
                    return RedirectToAction("Index");
                }

                UYB yeniUYB = new UYB
                {
                    Oran = Oran,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _uybRepository.Ekle(yeniUYB);
                TempData["Success"] = "UYB oranı başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"UYB oranı eklenirken hata oluştu: {ex.Message}";
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

                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "UYB oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                UYB mevcut = _uybRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "UYB oranı bulunamadı.";
                    return RedirectToAction("Index");
                }

                mevcut.Oran = Oran;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _uybRepository.Guncelle(mevcut);
                TempData["Success"] = "UYB oranı başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"UYB oranı güncellenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
                return Unauthorized();

            UYB item = _uybRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new { id = item.Id, oran = item.Oran });
        }
    }
}