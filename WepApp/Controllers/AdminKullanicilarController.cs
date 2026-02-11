using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class AdminKullanicilarController : AdminBaseController
    {
        private readonly KullanicilarRepository _kullaniciRepository;
        private readonly YetkiRepository _yetkiRepository;

        public AdminKullanicilarController()
        {
            _kullaniciRepository = new KullanicilarRepository();
            _yetkiRepository = new YetkiRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<string> join = new List<string>();
            join.Add("Yetki");
            LoadCommonData();
            List<Kullanicilar> list = _kullaniciRepository.GetirList(x => x.Durumu == 1 ,join);
            ViewBag.KullanicilarList = list;

            List<Yetki> yetkiList = _yetkiRepository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.YetkiList = yetkiList;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string Sifre, int? YetkiId)
        {
            LoadCommonData();


            Kullanicilar model = new Kullanicilar
            {
                Adi = Adi ?? "",
                Sifre = Sifre ?? "",
                YetkiId = YetkiId ?? 0,
                Durumu = 1,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now,
            };
            _kullaniciRepository.Ekle(model);
            TempData["Success"] = "Kullanıcı başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string Sifre, int? YetkiId)
        {
            LoadCommonData();

            Kullanicilar existingEntity = _kullaniciRepository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Adi = Adi;
                existingEntity.Sifre = Sifre;
                existingEntity.YetkiId = YetkiId ?? 0;
                existingEntity.Durumu = 1;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                _kullaniciRepository.Guncelle(existingEntity);
                TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanicilar = _kullaniciRepository.Getir(Id);
            if (kullanicilar != null)
            {
                kullanicilar.Durumu = 0;
                kullanicilar.GuncellenmeTarihi = DateTime.Now;
                _kullaniciRepository.Guncelle(kullanicilar);
                TempData["Success"] = "Kullanıcı başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            Kullanicilar item = _kullaniciRepository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                yetkiId = item.YetkiId
                // Sifre göndermiyoruz
            });
        }
    }
}