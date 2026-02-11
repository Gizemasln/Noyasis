using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Assuming KVKKRepository is in Repositories

namespace WepApp.Controllers
{
    public class AdminKVKKController : AdminBaseController
    {
        private readonly KVKKRepository _repository;

        public AdminKVKKController()
        {
            _repository = new KVKKRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<KVKK> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.KVKKList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Metin)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Metin))
            {
                KVKK model = new KVKK
                {
                    Metin = Metin,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "KVKK bilgisi başarıyla eklendi.";
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
            KVKK existingEntity = _repository.Getir(Id);
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
            KVKK kvkk = _repository.Getir(Id);
            if (kvkk != null)
            {
                kvkk.Durumu = 0;
                kvkk.KullanicilarId= kullanici.Id;
                kvkk.GuncellenmeTarihi = DateTime.Now;
                _repository.Guncelle(kvkk);
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

            KVKK item = _repository.Getir(id);
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