using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminDepartmanController : AdminBaseController
    {
        private readonly DepartmanRepository _departmanRepository = new DepartmanRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<Departman> departmanlar = _departmanRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();

            ViewBag.Departmanlar = departmanlar;
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

                Departman existing = _departmanRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir departman zaten mevcut.";
                    return RedirectToAction("Index");
                }

                Departman yeniDepartman = new Departman
                {
                    Adi = Adi ?? "",
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _departmanRepository.Ekle(yeniDepartman);
                TempData["Success"] = "Departman başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Departman eklenirken hata oluştu: {ex.Message}";
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

                Departman existing = _departmanRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Departman bulunamadı.";
                    return RedirectToAction("Index");
                }

                Departman duplicate = _departmanRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir departman zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _departmanRepository.Guncelle(existing);
                TempData["Success"] = "Departman başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Departman güncellenirken hata oluştu: {ex.Message}";
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

                Departman existing = _departmanRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Departman bulunamadı.";
                    return RedirectToAction("Index");
                }

                // İlişkili kayıt kontrolü yapmak isterseniz buraya eklersiniz (örneğin Personel tablosunda DepartmanId kullananlar)
                // Şimdilik direkt soft delete yapıyoruz
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _departmanRepository.Guncelle(existing);
                TempData["Success"] = "Departman başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Departman silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
                return Unauthorized();

            Departman item = _departmanRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new { id = item.Id, adi = item.Adi });
        }
    }
}