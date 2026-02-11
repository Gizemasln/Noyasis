using Microsoft.AspNetCore.Mvc;
using WebApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class MakaleDetayController : BaseController
    {
        public IActionResult Index(int id)
        {
            LoadCommonData();
            MakaleRepository makaleRepository = new MakaleRepository();
            Makale makale = makaleRepository.Getir(x => x.Durumu == 1 && x.Id == id);
            ViewBag.Makale= makale;

            return View();
        }
    }
}
