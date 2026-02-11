using Microsoft.AspNetCore.Mvc;
using WebApp.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.IO;
using WepApp.Models;
using WebApp.Models;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminHakkimizdaFotografController : AdminBaseController
    {
        private readonly HakkimizdaFotografRepository _repository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminHakkimizdaFotografController(IWebHostEnvironment hostEnvironment)
        {
            _repository = new HakkimizdaFotografRepository();
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<HakkimizdaFotograf> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.FotografList = list;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(List<IFormFile> imagefile)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            if (imagefile != null && imagefile.Count > 0)
            {
                string serverpath = _hostEnvironment.ContentRootPath;
                foreach (IFormFile item in imagefile)
                {
                    string extension = Path.GetExtension(item.FileName);
                    string newimagename = Guid.NewGuid() + extension;
                    string bigLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "Hakkimizda", "Buyuk", newimagename);
                    string smallLocation = Path.Combine(serverpath, "wwwroot", "WebAdminTheme", "Hakkimizda", "Kucuk", newimagename);

                    using (FileStream stream = new FileStream(bigLocation, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
                    }

                    using (Bitmap orjinal = new Bitmap(bigLocation))
                    using (Bitmap kucuk = new Bitmap(orjinal, new Size(400, 400)))
                    {
                        kucuk.Save(smallLocation);
                    }

                    HakkimizdaFotograf fotograf = new HakkimizdaFotograf
                    {
                        Durumu = 1,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now,
                        FotografBuyuk = "/WebAdminTheme/Hakkimizda/Buyuk/" + newimagename,
                        FotografKucuk = "/WebAdminTheme/Hakkimizda/Kucuk/" + newimagename,
                        KullanicilarId=kullanici.Id
                    };
                    _repository.Ekle(fotograf);
                }
                TempData["Success"] = "Resim(ler) başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen en az bir resim seçin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            HakkimizdaFotograf foto = _repository.Getir(Id);
            if (foto != null)
            {
                string pathBig = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot" + foto.FotografBuyuk.Replace("/", "\\"));
                string pathSmall = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot" + foto.FotografKucuk.Replace("/", "\\"));

                if (System.IO.File.Exists(pathBig))
                {
                    System.IO.File.Delete(pathBig);
                }
                if (System.IO.File.Exists(pathSmall))
                {
                    System.IO.File.Delete(pathSmall);
                }
                foto.Durumu = 0;
                foto.GuncellenmeTarihi=DateTime.Now;
                foto.KullanicilarId =kullanici.Id;
                _repository.Guncelle(foto);
                TempData["Success"] = "Resim başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }
            return RedirectToAction("Index");
        }
    }
}