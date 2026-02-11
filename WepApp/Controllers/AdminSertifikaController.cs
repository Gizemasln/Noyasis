using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;
using System.Collections.Generic;

namespace WepApp.Controllers
{
    public class AdminSertifikaController : AdminBaseController
    {
        private readonly BayiSertifikaRepository _sertifikaRepo = new();
        private readonly BayiRepository _bayiRepo = new();
        private readonly MusteriRepository _musteriRepo = new();
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminSertifikaController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();


            var (userType, userId) = GetCurrentUserInfo();
            List<string> join = new List<string> { "Bayi", "Kullanicilar" };

            List<BayiSertifika> sertifikalar;

            // Kullanıcı tipine göre filtreleme
            switch (userType)
            {
                case "Musteri":
                    // Müşteri girişi: Müşteriye ait tüm bayilerin sertifikaları
                    List<int> musteriBayiIds = _bayiRepo.GetirList(x =>  x.Durumu == 1)
                                               .Select(b => b.Id).ToList();
                    sertifikalar = _sertifikaRepo.GetirList(x => x.Durumu == 1 && musteriBayiIds.Contains(x.BayiId), join);
                    break;

                case "Bayi":
                    // Bayi girişi: Sadece kendi sertifikaları
                    sertifikalar = _sertifikaRepo.GetirList(x => x.Durumu == 1 && x.BayiId == userId, join);
                    break;

                case "Kurumsal":
                default:
                    // Kurumsal/admin girişi: Tüm sertifikalar
                    sertifikalar = _sertifikaRepo.GetirList(x => x.Durumu == 1, join);
                    break;
            }

            ViewBag.Sertifikalar = sertifikalar;
            ViewBag.Musteriler = _musteriRepo.GetirList(x => x.Durum == 1);
            ViewBag.CurrentUserType = userType;
            ViewBag.CurrentUserId = userId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(int MusteriId, int BayiId, string SertifikaAdi, string Aciklama,
            DateTime VerilisTarihi, DateTime? GecerlilikTarihi, IFormFile Dosya)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            var (userType, userId) = GetCurrentUserInfo();

            // Bayi kullanıcısı sadece kendine sertifika ekleyebilir
            if (userType == "Bayi")
            {
                BayiId = userId.Value;
            }

            if (Dosya == null || Dosya.Length == 0)
            {
                TempData["Error"] = "Lütfen bir sertifika dosyası yükleyin.";
                return RedirectToAction("Index");
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Dosya.FileName);
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "WebAdminTheme/Sertifika", fileName);

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                await Dosya.CopyToAsync(stream);
            }

            BayiSertifika sertifika = new BayiSertifika
            {
                BayiId = BayiId,
                SertifikaAdi = SertifikaAdi,
                Aciklama = Aciklama,
                VerilisTarihi = VerilisTarihi,
                GecerlilikTarihi = GecerlilikTarihi,
                DosyaYolu = $"/WebAdminTheme/Sertifika/{fileName}",
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now,
                Durumu = 1,
                KullanicilarId = kullanici.Id
            };

            _sertifikaRepo.Ekle(sertifika);
            TempData["Success"] = "Sertifika başarıyla eklendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Guncelle(int Id, string SertifikaAdi, string Aciklama,
            DateTime VerilisTarihi, DateTime? GecerlilikTarihi, IFormFile? Dosya)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            var (userType, userId) = GetCurrentUserInfo();

            BayiSertifika s = _sertifikaRepo.Getir(Id);
            if (s == null)
            {
                TempData["Error"] = "Sertifika bulunamadı.";
                return RedirectToAction("Index");
            }

            // Yetki kontrolü - Bayi sadece kendi sertifikalarını düzenleyebilir
            if (userType == "Bayi" && s.BayiId != userId)
            {
                TempData["Error"] = "Bu sertifikayı düzenleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (Dosya != null && Dosya.Length > 0)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(s.DosyaYolu))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, s.DosyaYolu.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Dosya.FileName);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "WebAdminTheme/Sertifika", fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    await Dosya.CopyToAsync(stream);
                }
                s.DosyaYolu = $"/WebAdminTheme/Sertifika/{fileName}";
            }

            s.SertifikaAdi = SertifikaAdi;
            s.Aciklama = Aciklama;
            s.VerilisTarihi = VerilisTarihi;
            s.GecerlilikTarihi = GecerlilikTarihi;
            s.GuncellenmeTarihi = DateTime.Now;

            if (userType == "Kurumsal")
            {
                s.KullanicilarId = kullanici.Id;
            }

            _sertifikaRepo.Guncelle(s);
            TempData["Success"] = "Sertifika güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            var (userType, userId) = GetCurrentUserInfo();

            BayiSertifika s = _sertifikaRepo.Getir(Id);
            if (s != null)
            {
                // Yetki kontrolü - Bayi sadece kendi sertifikalarını silebilir
                if (userType == "Bayi" && s.BayiId != userId)
                {
                    TempData["Error"] = "Bu sertifikayı silme yetkiniz yok.";
                    return RedirectToAction("Index");
                }

                s.Durumu = 0; // Soft delete

                if (userType == "Kurumsal")
                {
                    s.KullanicilarId = kullanici.Id;
                }

                _sertifikaRepo.Guncelle(s);
                TempData["Success"] = "Sertifika pasif yapıldı.";
            }
            else
            {
                TempData["Error"] = "Sertifika bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            var (userType, userId) = GetCurrentUserInfo();
            BayiSertifika s = _sertifikaRepo.Getir(id);

            if (s == null) return NotFound();

            // Yetki kontrolü - Bayi sadece kendi sertifikalarını görebilir
            if (userType == "Bayi" && s.BayiId != userId)
            {
                return Forbid();
            }

            return Json(new
            {
                id = s.Id,
                sertifikaAdi = s.SertifikaAdi,
                aciklama = s.Aciklama,
                verilisTarihi = s.VerilisTarihi.ToString("yyyy-MM-ddTHH:mm"),
                gecerlilikTarihi = s.GecerlilikTarihi?.ToString("yyyy-MM-ddTHH:mm"),
                dosyaYolu = s.DosyaYolu
            });
        }

        [HttpGet]
        public IActionResult GetBayilerByMusteri(int musteriId)
        {
            LoadCommonData();

            var (userType, userId) = GetCurrentUserInfo();

            // Müşteri girişi yapmışsa sadece kendi bayilerini görebilir
            if (userType == "Musteri")
            {
                musteriId = userId.Value;
            }

            List<Bayi> bayiler = _bayiRepo.GetirList(x => x.Durumu == 1);
            var result = bayiler.Select(b => new { b.Id, b.Unvan }).ToList();
            return Json(result);
        }

        [HttpGet]
        public IActionResult ViewSertifika(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, path.TrimStart('/'))))
            {
                return NotFound();
            }
            return File(path, "application/pdf");
        }
    }
}