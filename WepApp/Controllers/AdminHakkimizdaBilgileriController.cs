using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminHakkimizdaBilgileriController : AdminBaseController
    {
        private readonly HakkimizdaBilgileriRepository _repository;

        public AdminHakkimizdaBilgileriController()
        {
            _repository = new HakkimizdaBilgileriRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<HakkimizdaBilgileri> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.HakkimizdaList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Baslik, string AltBaslik, string Metin)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Baslik))
            {
                HakkimizdaBilgileri model = new HakkimizdaBilgileri
                {
                    Baslik = Baslik,
                    AltBaslik = AltBaslik,
                    Metin = Metin,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Bilgi başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen başlık girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Baslik, string AltBaslik, string Metin)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            HakkimizdaBilgileri existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Baslik = Baslik;
                existingEntity.AltBaslik = AltBaslik;
                existingEntity.Metin = Metin;
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
            HakkimizdaBilgileri hakkimizdaBilgileri = _repository.Getir(Id);
            if (hakkimizdaBilgileri != null)
            {
                hakkimizdaBilgileri.Durumu = 0;
                hakkimizdaBilgileri.GuncellenmeTarihi = DateTime.Now;
                hakkimizdaBilgileri.KullanicilarId=kullanici.Id;
                _repository.Guncelle(hakkimizdaBilgileri);
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

            HakkimizdaBilgileri item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                baslik = item.Baslik,
                altBaslik = item.AltBaslik,
                metin = item.Metin
            });
        }
    }
}