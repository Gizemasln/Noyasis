using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;

namespace WepApp.Controllers
{
    public class AdminBaseController : Controller
    {
        protected IActionResult LoadCommonData()
        {
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            ViewBag.Musteri = musteri;
            ViewBag.Bayi = bayi;
            ViewBag.Kullanici = kullanici;

            // Kullanıcı girişi kontrolü - sadece giriş yapmamış kullanıcıları login'e yönlendir
            if (kullanici == null && musteri == null && bayi == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return null; // yönlendirme yapılmadıysa null dönebilir
        }

        // Kullanıcı tipini döndüren yardımcı metod
        protected (string tip, int? id) GetCurrentUserInfo()
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");

            if (kullanici != null) return ("Kurumsal", kullanici.Id);
            if (musteri != null) return ("Musteri", musteri.Id);
            if (bayi != null) return ("Bayi", bayi.Id);

            return (null, null);
        }
    }
}