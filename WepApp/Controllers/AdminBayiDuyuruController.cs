using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using NuGet.Protocol.Core.Types;
using System.IO;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminBayiDuyuruController : AdminBaseController
    {
        BayiDuyuruRepository _repository = new BayiDuyuruRepository();
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminBayiDuyuruController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<BayiDuyuru> list = _repository.Listele()
                .Where(x => x.Durumu == 1)
                .OrderByDescending(x => x.Oncelik)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();

            ViewBag.DuyuruList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Baslik, string Icerik, int Oncelik = 0,
            DateTime? YayinBaslangicTarihi = null, DateTime? YayinBitisTarihi = null,
            bool YayindaMi = false, IFormFile? Gorsel = null)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Baslik))
            {
                BayiDuyuru model = new BayiDuyuru
                {
                    Baslik = Baslik,
                    Metin = Icerik,
                    Oncelik = Oncelik,
                    YayinBaslangicTarihi = YayinBaslangicTarihi,
                    YayinBitisTarihi = YayinBitisTarihi,
                    YayindaMi = YayindaMi ? 1 : 0,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId = kullanici.Id,
                    Durumu = 1
                };

                // Görsel yükleme işlemi
                if (Gorsel != null && Gorsel.Length > 0)
                {
                    model.GorselYolu = GorselYukle(Gorsel);
                }

                _repository.Ekle(model);
                TempData["Success"] = "Duyuru başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen başlık girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Baslik, string Icerik, int Oncelik = 0,
            DateTime? YayinBaslangicTarihi = null, DateTime? YayinBitisTarihi = null,
            bool YayindaMi = false, IFormFile? Gorsel = null, string? MevcutGorsel = null)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            BayiDuyuru existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Baslik = Baslik;
                existingEntity.Metin = Icerik;
                existingEntity.Oncelik = Oncelik;
                existingEntity.YayinBaslangicTarihi = YayinBaslangicTarihi;
                existingEntity.YayinBitisTarihi = YayinBitisTarihi;
                existingEntity.YayindaMi = YayindaMi ? 1 : 0;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;

                // Görsel yükleme işlemi
                if (Gorsel != null && Gorsel.Length > 0)
                {
                    // Eski görseli sil
                    if (!string.IsNullOrEmpty(existingEntity.GorselYolu))
                    {
                        GorselSil(existingEntity.GorselYolu);
                    }
                    existingEntity.GorselYolu = GorselYukle(Gorsel);
                }

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
            BayiDuyuru duyuru = _repository.Getir(Id);
            if (duyuru != null)
            {
                // Görseli sil
                if (!string.IsNullOrEmpty(duyuru.GorselYolu))
                {
                    GorselSil(duyuru.GorselYolu);
                }

                duyuru.GuncellenmeTarihi = DateTime.Now;
                duyuru.KullanicilarId = kullanici.Id;
                duyuru.Durumu = 0;
                _repository.Guncelle(duyuru);
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
            BayiDuyuru item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                baslik = item.Baslik,
                icerik = item.Metin,
                oncelik = item.Oncelik,
                yayinbaslangictarihi = item.YayinBaslangicTarihi?.ToString("yyyy-MM-dd"),
                yayinbitistarihi = item.YayinBitisTarihi?.ToString("yyyy-MM-dd"),
                yayindami = item.YayindaMi,
                gorselyolu = item.GorselYolu
            });
        }

        [HttpPost]
        public IActionResult GorselSilAjax(int id)
        {
            try
            {
                BayiDuyuru duyuru = _repository.Getir(id);
                if (duyuru != null && !string.IsNullOrEmpty(duyuru.GorselYolu))
                {
                    GorselSil(duyuru.GorselYolu);
                    duyuru.GorselYolu = null;
                    _repository.Guncelle(duyuru);
                    return Json(new { success = true, message = "Görsel başarıyla silindi." });
                }
                return Json(new { success = false, message = "Görsel bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        private string GorselYukle(IFormFile file)
        {
            try
            {
                // Klasör yolunu oluştur
                string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", "Duyuru");

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Benzersiz dosya adı oluştur
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                // Veritabanına kaydedilecek yol
                return "/WebAdminTheme/Duyuru/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapılabilir
                Console.WriteLine($"Görsel yüklenirken hata: {ex.Message}");
                return null;
            }
        }

        private void GorselSil(string gorselYolu)
        {
            try
            {
                if (!string.IsNullOrEmpty(gorselYolu))
                {
                    // Fiziksel dosya yolunu oluştur
                    string fileName = Path.GetFileName(gorselYolu);
                    string filePath = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", "Duyuru", fileName);

                    // Dosya varsa sil
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapılabilir
                Console.WriteLine($"Görsel silinirken hata: {ex.Message}");
            }
        }
    }
}