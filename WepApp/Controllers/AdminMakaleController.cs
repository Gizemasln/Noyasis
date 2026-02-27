using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminMakaleController : AdminBaseController
    {
        private readonly MakaleRepository _repository;
        private readonly IWebHostEnvironment _environment;

        public AdminMakaleController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _repository = new MakaleRepository();
        }

        public IActionResult Index()
        {
            List<Makale> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.MakaleList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(Makale model, IFormFile? Dosya)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");


            try
            {
                string dosyaYolu = null;
                if (Dosya != null && Dosya.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "Makale");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Dosya.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        Dosya.CopyTo(fileStream);
                    }

                    dosyaYolu = "/WebAdminTheme/Makale/" + uniqueFileName;
                }

                Makale yeniMakale = new Makale
                {
                    Baslik = model.Baslik,
                    Metin = model.Metin,
                    Fotograf = dosyaYolu,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId = kullanici.Id
                };

                _repository.Ekle(yeniMakale);
                TempData["Success"] = "Makale başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Kayıt sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(Makale model, IFormFile? Dosya)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

          

            Makale existingEntity = _repository.Getir(model.Id);
            if (existingEntity == null)
            {
                TempData["Error"] = "Kayıt bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                existingEntity.Baslik = model.Baslik;
                existingEntity.Metin = model.Metin;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;

                if (Dosya != null && Dosya.Length > 0)
                {
                    // Eski dosyayı sil
                    if (!string.IsNullOrEmpty(existingEntity.Fotograf))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, existingEntity.Fotograf.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Yeni dosyayı yükle
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "Makale");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Dosya.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        Dosya.CopyTo(fileStream);
                    }

                    existingEntity.Fotograf = "/WebAdminTheme/Makale/" + uniqueFileName;
                }

                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Makale başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Güncelleme sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Makale makale = _repository.Getir(Id);

            if (makale != null)
            {
                try
                {
                    // Dosyayı sil
                    if (!string.IsNullOrEmpty(makale.Fotograf))
                    {
                        string filePath = Path.Combine(_environment.WebRootPath, makale.Fotograf.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    makale.Durumu = 0;
                    makale.GuncellenmeTarihi = DateTime.Now;
                    makale.KullanicilarId = kullanici.Id;
                    _repository.Guncelle(makale);
                    TempData["Success"] = "Makale başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Silme sırasında hata oluştu: " + ex.Message;
                }
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
            Makale item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                baslik = item.Baslik,
                metin = item.Metin,
                fotograf = item.Fotograf
            });
        }
    }
}