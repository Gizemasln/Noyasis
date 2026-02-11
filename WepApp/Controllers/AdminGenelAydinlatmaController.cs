using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Assuming GenelAydinlatmaRepository is in Repositories

namespace WepApp.Controllers
{
    public class AdminGenelAydinlatmaController : AdminBaseController
    {
        private readonly GenelAydinlatmaRepository _repository;

        public AdminGenelAydinlatmaController()
        {
            _repository = new GenelAydinlatmaRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<GenelAydinlatma> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.GenelAydinlatmaList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Metin)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Metin))
            {
                GenelAydinlatma model = new GenelAydinlatma
                {
                    Metin = Metin,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId =kullanici.Id    
                };
                _repository.Ekle(model);
                TempData["Success"] = "Genel Aydınlatma bilgisi başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen metin girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Metin)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            GenelAydinlatma existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
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
            GenelAydinlatma genelAydinlatma = _repository.Getir(Id);
            if (genelAydinlatma != null)
            {
                genelAydinlatma.Durumu = 0;
                genelAydinlatma.GuncellenmeTarihi = DateTime.Now;
                genelAydinlatma.KullanicilarId =kullanici.Id;   
                _repository.Guncelle(genelAydinlatma);
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

            GenelAydinlatma item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                metin = item.Metin
            });
        }
    }
}