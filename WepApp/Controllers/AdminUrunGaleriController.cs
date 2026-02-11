using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.IO;
using WepApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class AdminUrunGaleriController : AdminBaseController
    {
        private readonly UrunGaleriRepository _galeriRepository = new UrunGaleriRepository();
        private readonly UrunRepository _urunRepository = new UrunRepository();
        private readonly KategoriRepository _kategoriRepository = new KategoriRepository();
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminUrunGaleriController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            ViewBag.Kategoriler = _kategoriRepository.Listele().Where(x => x.Durumu == 1).ToList();

            List<UrunGaleri> list = _galeriRepository.Listele().Where(x => x.Durumu == 1).ToList();
            foreach (UrunGaleri item in list)
            {
                item.Urun = _urunRepository.Getir(item.UrunId);
                if (item.Urun != null)
                {
                    item.Urun.Kategori = _kategoriRepository.Getir(item.Urun.KategoriId);
                }
            }
            ViewBag.GaleriList = list;

            return View();
        }

        [HttpGet]
        public IActionResult UrunlerByKategori(int kategoriId)
        {
            LoadCommonData();

            var urunler = _urunRepository.Listele().Where(u => u.KategoriId == kategoriId && u.Durumu == 1)
                .Select(u => new { u.Id, u.Adi }).ToList();
            return Json(urunler);
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(List<IFormFile> imagefile, int UrunId)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (imagefile != null && imagefile.Count != 0 && UrunId > 0)
            {
                Urun urun = _urunRepository.Getir(UrunId);
                if (urun == null)
                {
                    TempData["Error"] = "Ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                string webRootPath = _hostEnvironment.WebRootPath;
                foreach (IFormFile item in imagefile)
                {
                    string extension = Path.GetExtension(item.FileName);
                    string newimagename = Guid.NewGuid() + extension;
                    string bigLocation = Path.Combine(webRootPath, "WebAdminTheme", "UrunGaleri", "Buyuk", newimagename);
                    string smallLocation = Path.Combine(webRootPath, "WebAdminTheme", "UrunGaleri", "Kucuk", newimagename);

                    using (FileStream stream = new FileStream(bigLocation, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
                    }

                    using (Bitmap orjinal = new Bitmap(bigLocation))
                    using (Bitmap kucuk = new Bitmap(orjinal, new Size(400, 400)))
                    {
                        kucuk.Save(smallLocation);
                    }

                    UrunGaleri fotograf = new UrunGaleri
                    {
                        Durumu = 1,
                        KullanicilarId=kullanici.Id,
                        UrunId = UrunId,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now,
                        FotografBuyuk = "/WebAdminTheme/UrunGaleri/Buyuk/" + newimagename,
                        FotografKucuk = "/WebAdminTheme/UrunGaleri/Kucuk/" + newimagename
                    };
                    _galeriRepository.Ekle(fotograf);
                }
                TempData["Success"] = "Resimler başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen bir ürün seçin ve en az bir resim yükleyin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            UrunGaleri urunGaleri = _galeriRepository.Getir(Id);
            if (urunGaleri != null)
            {
                string pathBig = Path.Combine(_hostEnvironment.WebRootPath, urunGaleri.FotografBuyuk.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                string pathSmall = Path.Combine(_hostEnvironment.WebRootPath, urunGaleri.FotografKucuk.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(pathBig))
                {
                    System.IO.File.Delete(pathBig);
                }
                if (System.IO.File.Exists(pathSmall))
                {
                    System.IO.File.Delete(pathSmall);
                }
                urunGaleri.Durumu = 0;
            urunGaleri.KullanicilarId=kullanici.Id;
                urunGaleri.GuncellenmeTarihi=DateTime.Now;
                _galeriRepository.Guncelle(urunGaleri);
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
