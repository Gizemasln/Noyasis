using Microsoft.AspNetCore.Mvc;
using WebApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class KVKKController : BaseController
    {
        public IActionResult Index()
        {
            LoadCommonData();
            KVKKRepository kVKKRepository = new KVKKRepository();
            KVKK kvkk = kVKKRepository.Getir(x => x.Durumu == 1);
            ViewBag.KVKK = kvkk;
            return View();
        }
    }
}
