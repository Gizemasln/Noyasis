using Microsoft.AspNetCore.Mvc;
using WebApp.Controllers;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class LokasyonlarimizController : BaseController
    {
        private readonly LokasyonRepository _lokasyonRepository;

        public LokasyonlarimizController()
        {
            _lokasyonRepository = new LokasyonRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<Lokasyon> lokasyonList = _lokasyonRepository.GetirList(x => x.Durumu == 1);
            ViewBag.LokasyonList = lokasyonList;

            return View();
        }
    }
}