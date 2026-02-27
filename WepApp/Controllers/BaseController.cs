using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class BaseController : Controller
    {
        // void yapalım çünkü bir şey döndürmesine gerek yok
        protected void LoadCommonData()
        {
            // Ürün ve Kategori verilerini yükle
            KategoriRepository kategoriRepository = new KategoriRepository();
            List<Kategori> kategoriler = kategoriRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Kategoriler = kategoriler;

            UrunRepository urunRepository = new UrunRepository();
            List<string> list = new List<string>();
            list.Add("UrunGaleri");
            list.Add("Kategori");
            List<Urun> urunler = urunRepository.GetirList(x => x.Durumu == 1, list);
            ViewBag.Urunler = urunler;

            IletisimBilgileriRepository ıletisimBilgileriRepository = new IletisimBilgileriRepository();
            IletisimBilgileri ıletisimBilgileri = ıletisimBilgileriRepository.Getir(x => x.Durumu == 1);
            ViewBag.Iletisim = ıletisimBilgileri;
        }
    }
}