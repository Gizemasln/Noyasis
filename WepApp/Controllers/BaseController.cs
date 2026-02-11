using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class BaseController : Controller
    {
        protected IActionResult LoadCommonData()
        {

            // Kategorileri ve ürünleri yükle
            KategoriRepository kategoriRepository = new KategoriRepository();
            List<Kategori> kategoriler = kategoriRepository.Listele().Where(x => x.Durumu == 1).OrderBy(x => x.Adi).ToList();
            ViewBag.Kategoriler = kategoriler;

            UrunRepository urunRepository = new UrunRepository();
            List<Urun> urunler = urunRepository.Listele().Where(x => x.Durumu == 1).OrderBy(x => x.Adi).ToList();
            ViewBag.Urunler = urunler;
            IletisimBilgileriRepository ıletisimBilgileriRepository = new IletisimBilgileriRepository();
            IletisimBilgileri ıletisimBilgileri = new IletisimBilgileri();
            ıletisimBilgileri = ıletisimBilgileriRepository.Getir(x => x.Durumu == 1);
            ViewBag.Iletisim = ıletisimBilgileri;

            return null;
        }
    }
}
