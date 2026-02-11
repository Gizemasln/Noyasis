using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminLokasyonController : AdminBaseController
    {
        private readonly LokasyonRepository _lokasyonRepository;

        public AdminLokasyonController()
        {
            _lokasyonRepository = new LokasyonRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<Lokasyon> lokasyonList = _lokasyonRepository.GetirList(x => x.Durumu == 1);
            ViewBag.LokasyonList = lokasyonList;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string SehirAdi, string Adres, string Tip, string Telefon, string Email)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            Lokasyon model = new Lokasyon
            {
                SehirAdi = SehirAdi ?? "",
                Adres = Adres ?? "",
                Tip = Tip ?? "",
                Telefon = Telefon ?? "",
                Email = Email ?? "",
                Durumu = 1,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now,
                KullanicilarId = kullanici.Id
            };
            _lokasyonRepository.Ekle(model);
            TempData["Success"] = "Lokasyon başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string SehirAdi, string Adres, string Tip, string Telefon, string Email)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Lokasyon existingEntity = _lokasyonRepository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.SehirAdi = SehirAdi;
                existingEntity.Adres = Adres;
                existingEntity.Tip = Tip;
                existingEntity.Telefon = Telefon;
                existingEntity.Email = Email;
                existingEntity.Durumu = 1;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;
                _lokasyonRepository.Guncelle(existingEntity);
                TempData["Success"] = "Lokasyon başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Lokasyon bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Lokasyon lokasyon = _lokasyonRepository.Getir(Id);
            if (lokasyon != null)
            {
                lokasyon.Durumu = 0;
                lokasyon.GuncellenmeTarihi = DateTime.Now;
                lokasyon.KullanicilarId = kullanici.Id;
                _lokasyonRepository.Guncelle(lokasyon);
                TempData["Success"] = "Lokasyon başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Lokasyon bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            Lokasyon item = _lokasyonRepository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                sehirAdi = item.SehirAdi,
                adres = item.Adres,
                tip = item.Tip,
                telefon = item.Telefon,
                email = item.Email
            });
        }
    }
}