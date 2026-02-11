using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
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
            DuyuruRepository duyuruRepository = new DuyuruRepository();
            List<Duyuru> duyuru = duyuruRepository.GetirList(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderByDescending(x => x.Oncelik)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.Duyuru = duyuru;
            BayiDuyuruRepository bayiduyuruRepository = new BayiDuyuruRepository();
            List<BayiDuyuru> bayiduyuru = bayiduyuruRepository.GetirList(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderByDescending(x => x.Oncelik)
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.BayiDuyuru = bayiduyuru;
            List<string> join = new List<string>();
            KampanyaRepository kampanyaRepository = new KampanyaRepository();
            List<Kampanya> kampanyas = kampanyaRepository.GetirList(x => x.Durumu == 1 && x.BitisTarihi >= DateTime.Now);
            ViewBag.Kampanya = kampanyas;

            return View();
        }
    }
}
