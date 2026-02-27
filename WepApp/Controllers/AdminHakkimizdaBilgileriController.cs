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
        
            // Sadece aktif olan Hakkımızda bilgisini getir
            HakkimizdaBilgileri hakkimizda = _repository.Listele().Where(x => x.Durumu == 1).FirstOrDefault();
            ViewBag.Hakkimizda = hakkimizda;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Baslik, string AltBaslik, string Metin)
        {
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            // Önce mevcut aktif kayıt varsa pasif yap
            var mevcutKayit = _repository.Listele().Where(x => x.Durumu == 1).FirstOrDefault();
            if (mevcutKayit != null)
            {
                mevcutKayit.Durumu = 0;
                mevcutKayit.GuncellenmeTarihi = DateTime.Now;
                mevcutKayit.KullanicilarId = kullanici.Id;
                _repository.Guncelle(mevcutKayit);
            }

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
                    KullanicilarId = kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Hakkımızda bilgisi başarıyla eklendi.";
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
                TempData["Success"] = "Hakkımızda bilgisi başarıyla güncellendi.";
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
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            HakkimizdaBilgileri hakkimizdaBilgileri = _repository.Getir(Id);
            if (hakkimizdaBilgileri != null)
            {
                hakkimizdaBilgileri.Durumu = 0;
                hakkimizdaBilgileri.GuncellenmeTarihi = DateTime.Now;
                hakkimizdaBilgileri.KullanicilarId = kullanici.Id;
                _repository.Guncelle(hakkimizdaBilgileri);
                TempData["Success"] = "Hakkımızda bilgisi başarıyla silindi.";
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