using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using System.IO;

namespace WebApp.Controllers
{
    public class AdminAnaSayfaSliderController : AdminBaseController
    {
        private readonly SliderRepository _repository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminAnaSayfaSliderController(IWebHostEnvironment hostEnvironment)
        {
            _repository = new SliderRepository();
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<Slider> sliders = _repository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.SlaytSiraNo)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();
            ViewBag.SliderList = sliders;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(int SlaytSiraNo, string SlaytBaslik, string SlaytUrl,
            string SlaytButonAdi, bool YeniSekmeMi, bool YayindaMi, string Aciklama,
            IFormFile? Gorsel = null, IFormFile? Video = null)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            Slider model = new Slider
            {
                SlaytSiraNo = SlaytSiraNo,
                SlaytBaslik = SlaytBaslik,
                SlaytUrl = SlaytUrl,
                SlaytButonAdi = SlaytButonAdi,
                YeniSekmeMi = YeniSekmeMi ? 1 : 0,
                YayindaMi = YayindaMi ? 1 : 0,
                Aciklama = Aciklama,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now,
      
                Durumu = 1
            };

            // Görsel yükleme
            if (Gorsel != null && Gorsel.Length > 0)
            {
                model.GorselYolu = DosyaYukle(Gorsel, "Slider");
            }

            // Video yükleme
            if (Video != null && Video.Length > 0)
            {
                model.VideoYolu = DosyaYukle(Video, "SliderVideo");
            }

            _repository.Ekle(model);
            TempData["Success"] = "Slider başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, int SlaytSiraNo, string SlaytBaslik, string SlaytUrl,
            string SlaytButonAdi, bool YeniSekmeMi, bool YayindaMi, string Aciklama,
            IFormFile? Gorsel = null, IFormFile? Video = null,
            string? MevcutGorsel = null, string? MevcutVideo = null)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Slider existingEntity = _repository.Getir(Id);

            if (existingEntity != null)
            {
                existingEntity.SlaytSiraNo = SlaytSiraNo;
                existingEntity.SlaytBaslik = SlaytBaslik;
                existingEntity.SlaytUrl = SlaytUrl;
                existingEntity.SlaytButonAdi = SlaytButonAdi;
                existingEntity.YeniSekmeMi = YeniSekmeMi ? 1 : 0;
                existingEntity.YayindaMi = YayindaMi ? 1 : 0;
                existingEntity.Aciklama = Aciklama;
                existingEntity.GuncellenmeTarihi = DateTime.Now;

                // Görsel yükleme
                if (Gorsel != null && Gorsel.Length > 0)
                {
                    // Eski görseli sil
                    if (!string.IsNullOrEmpty(existingEntity.GorselYolu))
                    {
                        DosyaSil(existingEntity.GorselYolu);
                    }
                    existingEntity.GorselYolu = DosyaYukle(Gorsel, "Slider");
                }

                // Video yükleme
                if (Video != null && Video.Length > 0)
                {
                    // Eski videoyu sil
                    if (!string.IsNullOrEmpty(existingEntity.VideoYolu))
                    {
                        DosyaSil(existingEntity.VideoYolu);
                    }
                    existingEntity.VideoYolu = DosyaYukle(Video, "SliderVideo");
                }

                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Slider başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Slider bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Slider slider = _repository.Getir(Id);
            if (slider != null)
            {
                // Görseli sil
                if (!string.IsNullOrEmpty(slider.GorselYolu))
                {
                    DosyaSil(slider.GorselYolu);
                }

                // Videoyu sil
                if (!string.IsNullOrEmpty(slider.VideoYolu))
                {
                    DosyaSil(slider.VideoYolu);
                }

                slider.Durumu = 0;
                slider.GuncellenmeTarihi = DateTime.Now;
                _repository.Guncelle(slider);
                TempData["Success"] = "Slider başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Slider bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            Slider item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                slaytSiraNo = item.SlaytSiraNo,
                slaytBaslik = item.SlaytBaslik,
                slaytUrl = item.SlaytUrl,
                slaytButonAdi = item.SlaytButonAdi,
                yeniSekmeMi = item.YeniSekmeMi == 1,
                yayindaMi = item.YayindaMi == 1,
                aciklama = item.Aciklama,
                gorselyolu = item.GorselYolu,
                videoyolu = item.VideoYolu
            });
        }

        [HttpPost]
        public IActionResult GorselSilAjax(int id)
        {
            try
            {
                Slider slider = _repository.Getir(id);
                if (slider != null && !string.IsNullOrEmpty(slider.GorselYolu))
                {
                    DosyaSil(slider.GorselYolu);
                    slider.GorselYolu = null;
                    slider.GuncellenmeTarihi = DateTime.Now;
                    _repository.Guncelle(slider);
                    return Json(new { success = true, message = "Görsel başarıyla silindi." });
                }
                return Json(new { success = false, message = "Görsel bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult VideoSilAjax(int id)
        {
            try
            {
                Slider slider = _repository.Getir(id);
                if (slider != null && !string.IsNullOrEmpty(slider.VideoYolu))
                {
                    DosyaSil(slider.VideoYolu);
                    slider.VideoYolu = null;
                    slider.GuncellenmeTarihi = DateTime.Now;
                    _repository.Guncelle(slider);
                    return Json(new { success = true, message = "Video başarıyla silindi." });
                }
                return Json(new { success = false, message = "Video bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        private string DosyaYukle(IFormFile file, string klasorAdi)
        {
            try
            {
                // Ana klasör yolunu oluştur
                string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", klasorAdi);

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Benzersiz dosya adı oluştur (GUID + orijinal dosya adı)
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                // Veritabanına kaydedilecek yol
                return "/WebAdminTheme/" + klasorAdi + "/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dosya yüklenirken hata: {ex.Message}");
                return null;
            }
        }

        private void DosyaSil(string dosyaYolu)
        {
            try
            {
                if (!string.IsNullOrEmpty(dosyaYolu))
                {
                    string fileName = Path.GetFileName(dosyaYolu);

                    // Görsel mi video mu kontrol et
                    string klasorAdi = dosyaYolu.Contains("/SliderVideo/") ? "SliderVideo" : "Slider";
                    string filePath = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", klasorAdi, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dosya silinirken hata: {ex.Message}");
            }
        }
    }
}