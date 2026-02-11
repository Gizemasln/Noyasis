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
            LoadCommonData();
            List<AnaSayfaBannerResim> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.FotografList = list;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(List<IFormFile> imagefile, string Metin, string Baslik)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (imagefile != null && imagefile.Count != 0)
            {
                string serverpath = _hostEnvironment.ContentRootPath;
                foreach (IFormFile item in imagefile)
                {
                    string extension = Path.GetExtension(item.FileName);
                    string newimagename = Guid.NewGuid() + extension;
                    string bigLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "AnaSayfaBanner", "Buyuk", newimagename);
                    string smallLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "AnaSayfaBanner", "Kucuk", newimagename);

                    using (FileStream stream = new FileStream(bigLocation, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
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
                        KullanicilarId= kullanici.Id,
                        Metin = Metin,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now,
                        FotografBuyuk = "/WebAdminTheme/AnaSayfaBanner/Buyuk/" + newimagename,
                        FotografKucuk = "/WebAdminTheme/AnaSayfaBanner/Kucuk/" + newimagename
                    };
                    _repository.Ekle(fotograf);
                }
                TempData["Success"] = "Resim başarıyla eklendi.";
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
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            AnaSayfaBannerResim existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Metin = Metin;
                existingEntity.Baslik = Baslik;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId=kullanici.Id;
                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
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
            LoadCommonData();

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
                anaSayfaFotograf.GuncellenmeTarihi=DateTime.Now;
                anaSayfaFotograf.KullanicilarId = kullanici.Id;
                _repository.Guncelle(anaSayfaFotograf);
                TempData["Success"] = "Kayıt başarıyla silindi.";
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
            LoadCommonData();

            AnaSayfaBannerResim item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(item);
        }
    }
}