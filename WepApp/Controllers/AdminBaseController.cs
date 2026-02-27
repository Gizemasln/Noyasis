using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Models;
using WepApp.Models;

namespace WepApp.Controllers
{
    public class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            ViewBag.Musteri = musteri;
            ViewBag.Bayi = bayi;
            ViewBag.Kullanici = kullanici;

            // Eğer hiçbir kullanıcı tipi yoksa Login'e yönlendir
            if (kullanici == null && musteri == null && bayi == null)
            {
                context.Result = RedirectToAction("Index", "Login");
            }

            base.OnActionExecuting(context);
        }

        protected (string tip, int? id) GetCurrentUserInfo()
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");

            if (kullanici != null) return ("Admin", kullanici.Id);
            if (musteri != null) return ("Musteri", musteri.Id);
            if (bayi != null) return ("Bayi", bayi.Id);

            return (null, null);
        }
    }
}