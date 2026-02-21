using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class AdminAnaSayfaController : AdminBaseController
    {
        public IActionResult Index()
        {
            LoadCommonData();

            // Web Duyuruları
            DuyuruRepository duyuruRepository = new DuyuruRepository();
            List<Duyuru> duyuru = duyuruRepository.GetirList(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderByDescending(x => x.Oncelik)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.Duyuru = duyuru;

            // Bayi Duyuruları
            BayiDuyuruRepository bayiduyuruRepository = new BayiDuyuruRepository();
            List<BayiDuyuru> bayiduyuru = bayiduyuruRepository.GetirList(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderByDescending(x => x.Oncelik)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.BayiDuyuru = bayiduyuru;

            // Kampanyalar
            KampanyaRepository kampanyaRepository = new KampanyaRepository();
            List<Kampanya> kampanyas = kampanyaRepository.GetirList(x => x.Durumu == 1 && x.BitisTarihi >= DateTime.Now)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.Kampanya = kampanyas;

            // Sliderlar - Video ve görsel birlikte çalışacak şekilde
            SliderRepository sliderRepository = new SliderRepository();
            List<Slider> sliders = sliderRepository.GetirList(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderBy(x => x.SlaytSiraNo)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();

            // Slider listesini kontrol et
            foreach (var slider in sliders)
            {
                // Video varsa öncelik video gösterilecek
                if (!string.IsNullOrEmpty(slider.VideoYolu))
                {
                    // Video yolunun geçerliliğini kontrol et
                    if (!System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", slider.VideoYolu.TrimStart('~', '/'))))
                    {
                        slider.VideoYolu = null; // Video dosyası yoksa null yap
                    }
                }

                // Görsel varsa kontrol et
                if (!string.IsNullOrEmpty(slider.GorselYolu))
                {
                    if (!System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", slider.GorselYolu.TrimStart('~', '/'))))
                    {
                        slider.GorselYolu = null; // Görsel dosyası yoksa null yap
                    }
                }
            }

            ViewBag.SliderList = sliders;

            return View();
        }
    }
}