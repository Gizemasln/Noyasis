using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Repository'nizin namespace'i

namespace WepApp.Controllers
{
    public class AdminMakaleController : AdminBaseController
    {
        private readonly MakaleRepository _repository;
        private readonly IWebHostEnvironment _environment;

        public AdminMakaleController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _repository = new MakaleRepository(); // Dependency injection ile inject etmek daha iyi olur
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<Makale> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.MakaleList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Baslik, string Metin, IFormFile? Dosya)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(Baslik))
            {
                TempData["Error"] = "Lütfen başlık girin.";
                return RedirectToAction("Index");
            }

            try
            {
                string dosyaYolu = null;
                if (Dosya != null && Dosya.Length > 0)
                {
                    // wwwroot/WebAdminTheme/Makale/ klasörüne kaydet
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
                    dosyaYolu = "/WebAdminTheme/Makale/" + uniqueFileName; // URL için göreli yol
                }

                Makale model = new Makale
                {
                    Baslik = Baslik,
                    Metin = Metin,
                    Fotograf = dosyaYolu,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId = kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Bilgi başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Kayıt sırasında hata oluştu: " + ex.Message;
                // Log ekleyin: ILogger ile
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Baslik, string Metin, IFormFile? Dosya)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(Baslik))
            {
                TempData["Error"] = "Lütfen başlık girin.";
                return RedirectToAction("Index");
            }

            Makale existingEntity = _repository.Getir(Id);
            if (existingEntity == null)
            {
                TempData["Error"] = "Kayıt bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                existingEntity.Baslik = Baslik;
                existingEntity.Metin = Metin;
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
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
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
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Makale makale = _repository.Getir(Id);
            if (makale != null)
            {
                try
                {
                    // Dosyayı da sil
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
                    TempData["Success"] = "Kayıt başarıyla silindi.";
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
            LoadCommonData();

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