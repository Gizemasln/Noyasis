using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminYetkiController : AdminBaseController // Assuming AdminBaseController exists; adjust if needed
    {
        private readonly YetkiRepository _repository;

        public AdminYetkiController()
        {
            _repository = new YetkiRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData(); // Assuming this method exists in base controller
            List<Yetki> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.YetkiList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Adi))
            {
                Yetki model = new Yetki
                {
                    Adi = Adi,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId = kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Yetki başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen yetki adı girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Yetki existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Adi = Adi;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;
                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Yetki yetki = _repository.Getir(Id);
            if (yetki != null)
            {
                yetki.Durumu = 0;
                yetki.GuncellenmeTarihi = DateTime.Now;
                yetki.KullanicilarId = kullanici.Id;
                _repository.Guncelle(yetki);
                TempData["Success"] = "Kayıt başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            Yetki item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                adi = item.Adi
            });
        }
    }
}