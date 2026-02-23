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

            // Ürün ve Kategori verilerini yükle (yeni eklenen kısım)
            KategoriRepository kategoriRepository = new KategoriRepository(); // Varsayım: Bu repository mevcut veya oluşturun
            List<Kategori> kategoriler = kategoriRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Kategoriler = kategoriler;

            UrunRepository urunRepository = new UrunRepository(); // Varsayım: Bu repository mevcut veya oluşturun
            List<string> list = new List<string>();
            list.Add("UrunGaleri");
            list.Add("Kategori");
            List<Urun> urunler = urunRepository.GetirList(x => x.Durumu == 1, list); // Include ile ilişkili verileri yükleyin (EF Core için)
            ViewBag.Urunler = urunler;

            IletisimBilgileriRepository ıletisimBilgileriRepository = new IletisimBilgileriRepository();
            IletisimBilgileri ıletisimBilgileri = new IletisimBilgileri();
            ıletisimBilgileri = ıletisimBilgileriRepository.Getir(x => x.Durumu == 1);
            ViewBag.Iletisim = ıletisimBilgileri;

            return null;
        }
    }
}
