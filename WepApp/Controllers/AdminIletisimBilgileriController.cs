using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminIletisimBilgileriController : AdminBaseController
    {
        private readonly IletisimBilgileriRepository _repository;

        public AdminIletisimBilgileriController()
        {
            _repository = new IletisimBilgileriRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<IletisimBilgileri> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.IletisimList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string Adres, string BankaAdi, string Email1, string Email2, string Facebook, string Faks, string GoogleMapsBaglanti, string IbanNo, string Instagram, string Linkedin, string Telefon1, string Telefon2, string Telefon3, string Telefon4, string Twitter, string WhatsApp, string YouTube)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Adi))
            {
                IletisimBilgileri model = new IletisimBilgileri
                {
                    Adi = Adi,
                    Adres = Adres,
                    BankaAdi = BankaAdi,
                    Email1 = Email1,
                    Email2 = Email2,
                    Facebook = Facebook,
                    Faks = Faks,
                    GoogleMapsBaglanti = GoogleMapsBaglanti,
                    IbanNo = IbanNo,
                    Instagram = Instagram,
                    Linkedin = Linkedin,
                    Telefon1 = Telefon1,
                    Telefon2 = Telefon2,
                    Telefon3 = Telefon3,
                    Telefon4 = Telefon4,
                    Twitter = Twitter,
                    WhatsApp = WhatsApp,
                    YouTube = YouTube,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id,
                };
                _repository.Ekle(model);
                TempData["Success"] = "İletişim bilgisi başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen firma adı girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string Adres, string BankaAdi, string Email1, string Email2, string Facebook, string Faks, string GoogleMapsBaglanti, string IbanNo, string Instagram, string Linkedin, string Telefon1, string Telefon2, string Telefon3, string Telefon4, string Twitter, string WhatsApp, string YouTube)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            IletisimBilgileri existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Adi = Adi;
                existingEntity.Adres = Adres;
                existingEntity.BankaAdi = BankaAdi;
                existingEntity.Email1 = Email1;
                existingEntity.Email2 = Email2;
                existingEntity.Facebook = Facebook;
                existingEntity.Faks = Faks;
                existingEntity.GoogleMapsBaglanti = GoogleMapsBaglanti;
                existingEntity.IbanNo = IbanNo;
                existingEntity.Instagram = Instagram;
                existingEntity.Linkedin = Linkedin;
                existingEntity.Telefon1 = Telefon1;
                existingEntity.Telefon2 = Telefon2;
                existingEntity.Telefon3 = Telefon3;
                existingEntity.Telefon4 = Telefon4;
                existingEntity.Twitter = Twitter;
                existingEntity.WhatsApp = WhatsApp;
                existingEntity.YouTube = YouTube;
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
            IletisimBilgileri iletisimBilgileri = _repository.Getir(Id);
            if (iletisimBilgileri != null)
            {
                iletisimBilgileri.Durumu = 0;
                iletisimBilgileri.GuncellenmeTarihi = DateTime.Now;
                iletisimBilgileri.KullanicilarId=kullanici.Id;
                _repository.Guncelle(iletisimBilgileri);
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
            IletisimBilgileri item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                adres = item.Adres,
                bankaAdi = item.BankaAdi,
                email1 = item.Email1,
                email2 = item.Email2,
                facebook = item.Facebook,
                faks = item.Faks,
                googleMapsBaglanti = item.GoogleMapsBaglanti,
                ibanNo = item.IbanNo,
                instagram = item.Instagram,
                linkedin = item.Linkedin,
                telefon1 = item.Telefon1,
                telefon2 = item.Telefon2,
                telefon3 = item.Telefon3,
                telefon4 = item.Telefon4,
                twitter = item.Twitter,
                whatsApp = item.WhatsApp,
                youTube = item.YouTube
            });
        }
    }
}