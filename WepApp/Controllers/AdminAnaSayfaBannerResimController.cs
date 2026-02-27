using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.IO;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminAnaSayfaBannerResimController : AdminBaseController
    {
        AnaSayfaBannerResimRepository _repository = new AnaSayfaBannerResimRepository();
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminAnaSayfaBannerResimController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
        
            // Sadece aktif olan banner'ı getir (en son eklenen veya tek aktif kayıt)
            AnaSayfaBannerResim banner = _repository.Listele().Where(x => x.Durumu == 1).FirstOrDefault();
            ViewBag.Banner = banner;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(IFormFile imagefile, string Metin, string Baslik)
        {
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            // Önce mevcut aktif banner varsa pasif yap
            var mevcutBanner = _repository.Listele().Where(x => x.Durumu == 1).FirstOrDefault();
            if (mevcutBanner != null)
            {
                mevcutBanner.Durumu = 0;
                mevcutBanner.GuncellenmeTarihi = DateTime.Now;
                mevcutBanner.KullanicilarId = kullanici.Id;
                _repository.Guncelle(mevcutBanner);
            }

            if (imagefile != null && imagefile.Length > 0)
            {
                string serverpath = _hostEnvironment.ContentRootPath;

                string extension = Path.GetExtension(imagefile.FileName);
                string newimagename = Guid.NewGuid() + extension;
                string bigLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "AnaSayfaBanner", "Buyuk", newimagename);
                string smallLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "AnaSayfaBanner", "Kucuk", newimagename);

                // Klasörlerin varlığını kontrol et, yoksa oluştur
                string bigDirectory = Path.GetDirectoryName(bigLocation);
                string smallDirectory = Path.GetDirectoryName(smallLocation);

                if (!Directory.Exists(bigDirectory))
                    Directory.CreateDirectory(bigDirectory);

                if (!Directory.Exists(smallDirectory))
                    Directory.CreateDirectory(smallDirectory);

                using (FileStream stream = new FileStream(bigLocation, FileMode.Create))
                {
                    await imagefile.CopyToAsync(stream);
                }

                using (Bitmap orjinal = new Bitmap(bigLocation))
                using (Bitmap kucuk = new Bitmap(orjinal, new Size(400, 400)))
                {
                    kucuk.Save(smallLocation);
                }

                AnaSayfaBannerResim fotograf = new AnaSayfaBannerResim
                {
                    Durumu = 1,
                    Baslik = Baslik,
                    KullanicilarId = kullanici.Id,
                    Metin = Metin,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    FotografBuyuk = "/WebAdminTheme/AnaSayfaBanner/Buyuk/" + newimagename,
                    FotografKucuk = "/WebAdminTheme/AnaSayfaBanner/Kucuk/" + newimagename
                };
                _repository.Ekle(fotograf);

                TempData["Success"] = "Banner başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen bir resim seçin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Metin, string Baslik)
        {
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            AnaSayfaBannerResim existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Metin = Metin;
                existingEntity.Baslik = Baslik;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;
                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Banner başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            AnaSayfaBannerResim anaSayfaFotograf = _repository.Getir(Id);
            if (anaSayfaFotograf != null)
            {
                string pathBig = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot" + anaSayfaFotograf.FotografBuyuk.Replace("/", "\\"));
                string pathSmall = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot" + anaSayfaFotograf.FotografKucuk.Replace("/", "\\"));

                if (System.IO.File.Exists(pathBig))
                {
                    System.IO.File.Delete(pathBig);
                }
                if (System.IO.File.Exists(pathSmall))
                {
                    System.IO.File.Delete(pathSmall);
                }
                anaSayfaFotograf.Durumu = 0;
                anaSayfaFotograf.GuncellenmeTarihi = DateTime.Now;
                anaSayfaFotograf.KullanicilarId = kullanici.Id;
                _repository.Guncelle(anaSayfaFotograf);
                TempData["Success"] = "Banner başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
        

            AnaSayfaBannerResim item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(item);
        }
    }
}