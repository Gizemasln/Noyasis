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


            MakaleRepository makaleRepository = new MakaleRepository();
            List<Makale> makale = makaleRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Makale = makale;
            return View();


        }
        public IActionResult Login()
        {
            LoadCommonData();
            return View();
        }

    }
    }