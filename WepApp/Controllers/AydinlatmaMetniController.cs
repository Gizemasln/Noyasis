using Microsoft.AspNetCore.Mvc;
using WebApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AydinlatmaMetniController : BaseController
    {
        public IActionResult Index()
        {
            LoadCommonData();
            GenelAydinlatmaRepository kVKKRepository = new GenelAydinlatmaRepository();
            GenelAydinlatma kvkk = kVKKRepository.Getir(x => x.Durumu == 1);
            ViewBag.Aydinlatma = kvkk;
            return View();
        }
    }
}
