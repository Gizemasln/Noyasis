using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Repository'nizin namespace'i

namespace WepApp.Controllers
{
    public class AdminIKController : AdminBaseController
    {
        private readonly IKFormuRepository _repository;
        private readonly IWebHostEnvironment _environment;

        public AdminIKController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _repository = new IKFormuRepository(); // Dependency injection ile inject etmek daha iyi olur
        }

        public IActionResult Index()
        {
            LoadCommonData();

            // LoadCommonData(); // Eğer BaseController'dan gelmiyorsa kaldırın
            List<IKFormu> list = _repository.Listele().Where(x => x.Durumu == 1).ToList();
            ViewBag.IKList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string AdiSoyadi, string Telefon, string Eposta, string TC, IFormFile? Dosya, string Mesaj)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(AdiSoyadi))
            {
                TempData["Error"] = "Lütfen ad soyad girin.";
                return RedirectToAction("Index");
            }

            try
            {
                string dosyaYolu = null;
                if (Dosya != null && Dosya.Length > 0)
                {
                    // wwwroot/WebAdminTheme/IK/ klasörüne kaydet
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "IK");

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
                    dosyaYolu = "/WebAdminTheme/IK/" + uniqueFileName; // URL için göreli yol
                }

                IKFormu model = new IKFormu
                {
                    AdiSoyadi = AdiSoyadi,
                    Telefon = Telefon,
                    Eposta = Eposta,
                    TC = TC,
                    DosyaYolu = dosyaYolu,
                    Mesaj = Mesaj,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id
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
        public IActionResult Guncelle(int Id, string AdiSoyadi, string Telefon, string Eposta, string TC, IFormFile? Dosya, string Mesaj)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(AdiSoyadi))
            {
                TempData["Error"] = "Lütfen ad soyad girin.";
                return RedirectToAction("Index");
            }

            IKFormu existingEntity = _repository.Getir(Id);
            if (existingEntity == null)
            {
                TempData["Error"] = "Kayıt bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                existingEntity.AdiSoyadi = AdiSoyadi;
                existingEntity.Telefon = Telefon;
                existingEntity.Eposta = Eposta;
                existingEntity.TC = TC;
                existingEntity.Mesaj = Mesaj;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId=kullanici.Id;

                if (Dosya != null && Dosya.Length > 0)
                {
                    // Eski dosyayı sil
                    if (!string.IsNullOrEmpty(existingEntity.DosyaYolu))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, existingEntity.DosyaYolu.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Yeni dosyayı yükle
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "IK");

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
                    existingEntity.DosyaYolu = "/WebAdminTheme/IK/" + uniqueFileName;
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
            IKFormu ikFormu = _repository.Getir(Id);
            if (ikFormu != null)
            {
                try
                {
                    // Dosyayı da sil
                    if (!string.IsNullOrEmpty(ikFormu.DosyaYolu))
                    {
                        string filePath = Path.Combine(_environment.WebRootPath, ikFormu.DosyaYolu.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    ikFormu.Durumu = 0;
                    ikFormu.GuncellenmeTarihi = DateTime.Now;
                    ikFormu.KullanicilarId =kullanici.Id;
                    _repository.Guncelle(ikFormu);
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
            IKFormu item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                adiSoyadi = item.AdiSoyadi,
                telefon = item.Telefon,
                eposta = item.Eposta,
                tc = item.TC,
                dosyaYolu = item.DosyaYolu,
                mesaj = item.Mesaj
            });
        }
    }
}