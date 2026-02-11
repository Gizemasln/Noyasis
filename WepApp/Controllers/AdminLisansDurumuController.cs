using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminLisansDurumuController : AdminBaseController
    {
        private readonly LisansDurumuRepository _repository = new LisansDurumuRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<LisansDurumu> durumlar = _repository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();
            ViewBag.LisansDurumlari = durumlar;
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

                if (string.IsNullOrWhiteSpace(Adi))
                {
                    TempData["Error"] = "Durum adı zorunludur.";
                    return RedirectToAction("Index");
                }

                LisansDurumu mevcut = _repository.GetirList(x => x.Adi.Trim().ToLower() == Adi.Trim().ToLower() && x.Durumu == 1).FirstOrDefault();
                if (mevcut != null)
                {
                    TempData["Error"] = "Bu isimde bir lisans durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                LisansDurumu yeni = new LisansDurumu
                {
                    Adi = Adi.Trim(),
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _repository.Ekle(yeni);
                TempData["Success"] = "Lisans durumu başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ekleme hatası: {ex.Message}";
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

                LisansDurumu mevcut = _repository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "Lisans durumu bulunamadı.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(Adi))
                {
                    TempData["Error"] = "Durum adı zorunludur.";
                    return RedirectToAction("Index");
                }

                LisansDurumu duplicate = _repository.GetirList(x => x.Adi.Trim().ToLower() == Adi.Trim().ToLower() && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir lisans durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                mevcut.Adi = Adi.Trim();
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _repository.Guncelle(mevcut);
                TempData["Success"] = "Lisans durumu başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Güncelleme hatası: {ex.Message}";
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

                LisansDurumu mevcut = _repository.Getir(Id);
                if (mevcut == null)
                {
                    TempData["Error"] = "Lisans durumu bulunamadı.";
                    return RedirectToAction("Index");
                }
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;
                _repository.Guncelle(mevcut);

                TempData["Success"] = "Lisans durumu başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Silme hatası: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();
            LisansDurumu item = _repository.Getir(id);
            if (item == null || item.Durumu == 0) return NotFound();

            return Json(new { id = item.Id, adi = item.Adi });
        }
    }
}