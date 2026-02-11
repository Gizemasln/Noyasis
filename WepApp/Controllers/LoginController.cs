using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly KullanicilarRepository _kullanicilarRepository = new KullanicilarRepository();
        private readonly MusteriRepository _musteriRepository = new MusteriRepository();
        private readonly BayiRepository _bayiRepository = new BayiRepository();

        [HttpGet]
        public IActionResult Index()
        {
            // Ortak veriler (banner, iletişim vs.)
            ViewBag.Iletisim = new IletisimBilgileriRepository().Getir(x => x.Durumu == 1);
            ViewBag.Hakkimizda = new HakkimizdaBilgileriRepository().Getir(x => x.Durumu == 1);
            ViewBag.AnaSayfaBanner = new AnaSayfaBannerResimRepository().GetirList(x => x.Durumu == 1);

            return View();
        }

        [HttpPost]
        public IActionResult Giris([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.KullaniciAdi) || string.IsNullOrEmpty(model.Sifre))
                return Json(new { success = false, message = "Kullanıcı adı ve şifre zorunludur." });

            switch (model.tip)
            {
                case 0: // Kurumsal
                    Kullanicilar kurumsal = _kullanicilarRepository.Listele()
                        .FirstOrDefault(x => x.Adi == model.KullaniciAdi && x.Sifre == model.Sifre);
                    if (kurumsal != null)
                    {
                        SessionHelper.SetObjectAsJson(HttpContext.Session, "Kullanici", kurumsal);
                        return Json(new { success = true, redirect = Url.Action("Index", "AdminAnaSayfa") });
                    }
                    break;

                case 1: // Müşteri
                    Musteri musteri = _musteriRepository.GetirList(x => x.Durum == 1 && x.KullaniciAdi == model.KullaniciAdi && x.Sifre == model.Sifre)
                        .FirstOrDefault();
                    if (musteri != null)
                    {
                        SessionHelper.SetObjectAsJson(HttpContext.Session, "Musteri", musteri);
                        return Json(new { success = true, redirect = Url.Action("Index", "AdminAnaSayfa") });
                    }
                    break;

                case 2: // Bayi
                    Bayi bayi = _bayiRepository.GetirList(x => x.Durumu == 1 && x.KullaniciAdi == model.KullaniciAdi && x.Sifre == model.Sifre)
                        .FirstOrDefault();
                    if (bayi != null)
                    {
                        SessionHelper.SetObjectAsJson(HttpContext.Session, "Bayi", bayi);
                        return Json(new { success = true, redirect = Url.Action("Index", "AdminAnaSayfa") });
                    }
                    break;
            }

            return Json(new { success = false, message = "Giriş başarısız. Bilgilerinizi kontrol edin." });
        }
        [HttpGet]
        public IActionResult Logout()
        {
            // Tüm session'ları temizle
            HttpContext.Session.Clear();

            // Login sayfasına yönlendir
            return RedirectToAction("Index", "Login");
        }
    }


    public class LoginModel
    {
        public string KullaniciAdi { get; set; }
        public string Sifre { get; set; }
        public int tip { get; set; }
    }
}