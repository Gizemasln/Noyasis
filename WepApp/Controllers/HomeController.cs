using Microsoft.AspNetCore.Mvc;
using WebApp.Repositories;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            // BaseController'daki LoadCommonData metodunu çağır
            LoadCommonData();

            AnaSayfaBannerResimRepository anaSayfaBannerResimRepository = new AnaSayfaBannerResimRepository();
            AnaSayfaBannerResim anaSayfaBannerResim = new AnaSayfaBannerResim();
            anaSayfaBannerResim = anaSayfaBannerResimRepository.Getir(x => x.Durumu == 1);
            ViewBag.AnaSayfaBanner = anaSayfaBannerResim;

            HakkimizdaBilgileriRepository hakkimizdaBilgileriRepository = new HakkimizdaBilgileriRepository();
            HakkimizdaBilgileri hakkimizdaBilgileri = hakkimizdaBilgileriRepository.Getir(x => x.Durumu == 1);
            ViewBag.Hakkimizda = hakkimizdaBilgileri;

            HakkimizdaFotografRepository hakkimizdaFotografRepository = new HakkimizdaFotografRepository();
            HakkimizdaFotograf hakkimizdaFotograf = hakkimizdaFotografRepository.Getir(x => x.Durumu == 1);
            ViewBag.HakkimizdaFotograf = hakkimizdaFotograf;

            // SSS verilerini yükle
            SSSRepository sssRepository = new SSSRepository();
            List<SSS> sssList = sssRepository.GetirList(x => x.Durumu == 1);
            ViewBag.SSSList = sssList;

            MakaleRepository makaleRepository = new MakaleRepository();
            List<Makale> makale = makaleRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Makale = makale;

            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
    }
}