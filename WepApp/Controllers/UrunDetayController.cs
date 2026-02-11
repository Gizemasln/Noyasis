using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.IO;
using WebApp.Controllers;

namespace WepApp.Controllers
{
    public class UrunDetayController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public UrunDetayController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index(int id)
        {
            LoadCommonData();
            List<string> list = new List<string>();
            list.Add("UrunGaleri");
            UrunRepository urunRepository = new UrunRepository();
            Urun urun = urunRepository.Getir(x => x.Id == id, list);
            ViewBag.Urun = urun;

            // Sadece başarılı gönderimden sonra modal gösterilsin
            bool basarili = TempData["Basarili"] as bool? == true;
            if (basarili)
            {
                ViewData["Basarili"] = true;
            }

            return View();
        }

        [HttpPost]
        public IActionResult Kaydet(int id, Teklifler teklif)
        {
            LoadCommonData();

            TekliflerRepository teklifRepository = new TekliflerRepository();

            try
            {
                teklif.UrunId = id;

                Teklifler teklif1 = new Teklifler
                {
                    AdiSoyadi = teklif.AdiSoyadi ?? "",
                    Telefon = teklif.Telefon ?? "",
                    Eposta = teklif.Eposta ?? "",
                    Aciklama = teklif.Aciklama ?? "",
                    UrunId = id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    Durumu = 1,
                    Onay=0
                };

                teklifRepository.Ekle(teklif1);

                // Başarı durumunu TempData ile gönder ve redirect yap (ürün detayına dön)
                TempData["Basarili"] = true;
                return RedirectToAction("Index", new { id = id });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Kayıt sırasında hata oluştu: " + ex.Message;
                // Hata durumunda ViewBag ile ürün bilgisini yeniden yükle
                List<string> list = new List<string>();
                list.Add("UrunGaleri");
                UrunRepository urunRepository = new UrunRepository();
                Urun urun = urunRepository.Getir(x => x.Id == id, list);
                ViewBag.Urun = urun;
                return View("Index");
            }
        }
    }
}