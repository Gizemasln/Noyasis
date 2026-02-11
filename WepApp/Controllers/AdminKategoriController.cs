using Microsoft.AspNetCore.Mvc;

using WepApp.Models;
using WepApp.Controllers;
using WepApp.Repositories;
using WebApp.Models; // Assuming this is for base class; adjust if needed

namespace WebApp.Controllers
{
    public class AdminKategoriController : AdminBaseController // Assuming AdminBaseController exists; adjust if needed
    {
        private readonly KategoriRepository _repository;

        public AdminKategoriController()
        {
            _repository = new KategoriRepository(); 
        }

        public IActionResult Index()
        {
            LoadCommonData(); // Assuming this method exists in base controller
            List<Kategori> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.KategoriList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Adi))
            {
                Kategori model = new Kategori
                {
                    Adi = Adi,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Kategori başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen kategori adı girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Kategori existingEntity = _repository.Getir(Id);
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
            Kategori kategori = _repository.Getir(Id);
            if (kategori != null)
            {
                kategori.Durumu = 0;
                kategori.GuncellenmeTarihi = DateTime.Now;
                kategori.KullanicilarId= kullanici.Id;
                _repository.Guncelle(kategori);
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
            LoadCommonData();

            Kategori item = _repository.Getir(id);
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