using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebApp.Repositories;

namespace WepApp.Controllers
{
    public class ArgeController : AdminBaseController
    {
        private readonly ArgeHataRepository _repository = new ArgeHataRepository();
        private readonly GenericRepository<LisansTip> _lisansTipRepository = new GenericRepository<LisansTip>();
        private readonly GenericRepository<Musteri> _musteriRepository = new GenericRepository<Musteri>();
        private readonly GenericRepository<Bayi> _bayiRepository = new GenericRepository<Bayi>();
        private const int PageSize = 10;

        public IActionResult Index(int page = 1, string tip = null, int? durum = 1)
        {
            IActionResult redirectResult = LoadCommonData();
            if (redirectResult != null) return redirectResult;

            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;

            // Bayi müşterilerini ve bayi bilgisini yükle
            List<Musteri> bayiMusterileri = null;
            Bayi bayiBilgi = null;

            if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                bayiMusterileri = _musteriRepository
                    .GetirList(x => x.BayiId == kullaniciId.Value && x.Durum == 1)
                    .OrderBy(x => x.Ad)
                    .ThenBy(x => x.Soyad)
                    .ToList();

                bayiBilgi = _bayiRepository.Getir(x => x.Id == kullaniciId.Value);
            }

            ViewBag.BayiMusterileri = bayiMusterileri;
            ViewBag.BayiBilgi = bayiBilgi;

            IQueryable<ArgeHata> query = _repository
                .GetirQueryable()
                .Include(x => x.LisansTip)
                .Include(x => x.Musteri)
                .Include(x => x.Bayi)
                .Include(x => x.ARGEDurum);

            // Filtreler
            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                query = query.Where(x => x.MusteriId == kullaniciId.Value);
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                // Bayi hem kendi kayıtlarını hem de müşterilerinin kayıtlarını görebilir
                query = query.Where(x => x.BayiId == kullaniciId.Value ||
                                        (x.Musteri != null && x.Musteri.BayiId == kullaniciId.Value));
            }

            if (!string.IsNullOrEmpty(tip))
            {
                query = query.Where(x => x.Tipi == tip);
            }

            if (durum.HasValue)
            {
                query = query.Where(x => x.Durumu == durum.Value);
            }

            // Sayfalama
            int toplam = query.Count();
            List<ArgeHata> liste = query
                .OrderByDescending(x => x.EklenmeTarihi)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1);

            ViewBag.LisansTipleri = lisansTipleri;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)toplam / PageSize);
            ViewBag.TotalCount = toplam;
            ViewBag.TipiListesi = new[] { "ARGE", "Hata" };
            ViewBag.SelectedTip = tip;
            ViewBag.SelectedDurum = durum;

            return View(liste);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Tipi, string Adi, string Soyadi, int? LisansTipId,
                                 string Metni, IFormFile Dosya = null, string bildirimSahibi = "bayi",
                                 int? SecilenMusteriId = null)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası. Lütfen tekrar giriş yapın." });

            // Bayi için müşteri kontrolü
            if (kullaniciTipi == "Bayi" && bildirimSahibi == "musteri" && !SecilenMusteriId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen bir müşteri seçiniz." });
            }

            if (string.IsNullOrWhiteSpace(Tipi) || string.IsNullOrWhiteSpace(Adi) ||
                string.IsNullOrWhiteSpace(Soyadi) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Zorunlu alanları doldurunuz." });

            try
            {
                string dosyaYolu = null;
                if (Dosya != null && Dosya.Length > 0)
                {
                    // Dosya boyutu kontrolü (10MB)
                    if (Dosya.Length > 10 * 1024 * 1024)
                        return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalıdır." });

                    // Dosya tipi kontrolü
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
                    string fileExtension = Path.GetExtension(Dosya.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        return Json(new { success = false, message = "Geçersiz dosya formatı. PDF, Word, Excel, JPG veya PNG dosyaları yükleyebilirsiniz." });

                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "WebAdminTheme", "Arge");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Dosya.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        Dosya.CopyToAsync(stream).Wait();
                    }
                    dosyaYolu = $"/WebAdminTheme/Arge/{fileName}";
                }

                ArgeHata yeni = new ArgeHata
                {
                    Tipi = Tipi.Trim(),
                    Adi = Adi.Trim(),
                    Soyadi = Soyadi.Trim(),
                    Metni = Metni.Trim(),
                    DosyaYolu = dosyaYolu,
                    Durumu = 1,
                    EkleyenKullaniciId = kullaniciId.Value,
                    GuncelleyenKullaniciId = kullaniciId.Value,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    ARGEDurumId = 1 // Yeni durum
                };

                if (LisansTipId.HasValue)
                    yeni.LisansTipId = LisansTipId.Value;

                // Kullanıcı tipine göre atamalar
                if (kullaniciTipi == "Musteri")
                {
                    yeni.MusteriId = kullaniciId.Value;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    if (bildirimSahibi == "bayi")
                    {
                        yeni.BayiId = kullaniciId.Value;
                    }
                    else if (bildirimSahibi == "musteri" && SecilenMusteriId.HasValue)
                    {
                        yeni.MusteriId = SecilenMusteriId.Value;
                        // Müşterinin bayi bilgisini de set et
                        Musteri musteri = _musteriRepository.Getir(x => x.Id == SecilenMusteriId.Value);
                        if (musteri != null && musteri.BayiId.HasValue)
                        {
                            yeni.BayiId = musteri.BayiId.Value;
                        }
                        else
                        {
                            yeni.BayiId = kullaniciId.Value;
                        }
                    }
                }

                _repository.Ekle(yeni);
                return Json(new { success = true, message = "Kayıt başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ekleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirDuzenle(int id)
        {
            ArgeHata kayit = _repository.Getir(x => x.Id == id,
                new List<string> { "LisansTip", "Musteri", "Bayi", "ARGEDurum" });

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1);

            var data = new
            {
                kayit.Id,
                kayit.Tipi,
                kayit.Adi,
                kayit.Soyadi,
                kayit.LisansTipId,
                kayit.Metni,
                kayit.DosyaYolu,
                kayit.MusteriId,
                kayit.BayiId,
                kayit.DistributorCevap,
                kayit.DistributorCevapVerdiMi,
                kayit.DistributorCevapTarihi,
                kayit.ARGEDurumId,
                ArgeDurumAdi = kayit.ARGEDurum?.Adi ?? "Beklemede",
                MusteriAdi = kayit.Musteri != null ? kayit.Musteri.Ad + " " + kayit.Musteri.Soyad : null,
                BayiAdi = kayit.Bayi != null ? kayit.Bayi.Unvan : null,
                LisansTipleri = lisansTipleri.Select(g => new { g.Id, g.Adi })
            };

            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(int Id, string Tipi, string Adi, string Soyadi,
                                         int? LisansTipId, string Metni, IFormFile Dosya = null,
                                         string duzenleBildirimSahibi = "bayi", int? SecilenMusteriId = null)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            ArgeHata mevcut = _repository.Getir(x => x.Id == Id);
            if (mevcut == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            // HATA DURUMU KONTROLÜ: Durumu 1 (İncelenecek) değilse düzenlemeyi engelle
            if (mevcut.Durumu != 1)
            {
                return Json(new { success = false, message = "Bu kayıt incelenmeye alındığı için düzenlenemez. Yalnızca 'İncelenecek' durumundaki kayıtlar düzenlenebilir." });
            }

            // Yetki kontrolü
            if (kullaniciTipi == "Musteri" && mevcut.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı düzenleme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && mevcut.BayiId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı düzenleme yetkiniz yok." });
            }

            // Bayi için müşteri kontrolü
            if (kullaniciTipi == "Bayi" && duzenleBildirimSahibi == "musteri" && !SecilenMusteriId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen bir müşteri seçiniz." });
            }

            if (string.IsNullOrWhiteSpace(Tipi) || string.IsNullOrWhiteSpace(Adi) ||
                string.IsNullOrWhiteSpace(Soyadi) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Zorunlu alanları doldurunuz." });

            try
            {
                string dosyaYolu = mevcut.DosyaYolu;
                if (Dosya != null && Dosya.Length > 0)
                {
                    // Dosya boyutu kontrolü
                    if (Dosya.Length > 10 * 1024 * 1024)
                        return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalıdır." });

                    // Dosya tipi kontrolü
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
                    string fileExtension = Path.GetExtension(Dosya.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        return Json(new { success = false, message = "Geçersiz dosya formatı." });

                    if (!string.IsNullOrEmpty(dosyaYolu))
                    {
                        string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                            "wwwroot", dosyaYolu.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),
                        "wwwroot", "WebAdminTheme", "Arge");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Dosya.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Dosya.CopyToAsync(stream);
                    }
                    dosyaYolu = $"/WebAdminTheme/Arge/{fileName}";
                }

                mevcut.Tipi = Tipi.Trim();
                mevcut.Adi = Adi.Trim();
                mevcut.Soyadi = Soyadi.Trim();
                mevcut.Metni = Metni.Trim();
                mevcut.DosyaYolu = dosyaYolu;
                mevcut.LisansTipId = LisansTipId;
                mevcut.GuncelleyenKullaniciId = kullaniciId.Value;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                // Bayi için müşteri/bayi bilgisi güncelleme
                if (kullaniciTipi == "Bayi")
                {
                    if (duzenleBildirimSahibi == "bayi")
                    {
                        mevcut.MusteriId = null;
                        mevcut.BayiId = kullaniciId.Value;
                    }
                    else if (duzenleBildirimSahibi == "musteri" && SecilenMusteriId.HasValue)
                    {
                        mevcut.MusteriId = SecilenMusteriId.Value;
                        // Müşterinin bayi bilgisini de set et
                        Musteri musteri = _musteriRepository.Getir(x => x.Id == SecilenMusteriId.Value);
                        if (musteri != null && musteri.BayiId.HasValue)
                        {
                            mevcut.BayiId = musteri.BayiId.Value;
                        }
                        else
                        {
                            mevcut.BayiId = kullaniciId.Value;
                        }
                    }
                }

                _repository.Guncelle(mevcut);
                return Json(new { success = true, message = "Güncelleme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Güncelleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            ArgeHata kayit = _repository.Getir(x => x.Id == id);
            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            // Yetki kontrolü
            if (kullaniciTipi == "Musteri" && kayit.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı silme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && kayit.BayiId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı silme yetkiniz yok." });
            }

            try
            {
                if (!string.IsNullOrEmpty(kayit.DosyaYolu))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(),
                        "wwwroot", kayit.DosyaYolu.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                kayit.Durumu = 0;
                kayit.GuncellenmeTarihi = DateTime.Now;
                kayit.GuncelleyenKullaniciId = kullaniciId.Value;
                _repository.Guncelle(kayit);

                return Json(new { success = true, message = "Silme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CevapDetayGetir(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            ArgeHata kayit = _repository.Getir(x => x.Id == id,
                new List<string> { "ARGEDurum" });

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            // Yetki kontrolü
            if (kullaniciTipi == "Musteri" && kayit.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu cevabı görüntüleme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && kayit.BayiId != kullaniciId.Value)
            {
                // Bayi hem kendi kayıtlarını hem de müşterilerinin kayıtlarını görebilir
                if (kayit.Musteri != null && kayit.Musteri.BayiId != kullaniciId.Value)
                {
                    return Json(new { success = false, message = "Bu cevabı görüntüleme yetkiniz yok." });
                }
            }

            var data = new
            {
                success = true,
                distributorCevap = kayit.DistributorCevap ?? "",
                distributorCevapVerdiMi = kayit.DistributorCevapVerdiMi,
                distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                argeDurumAdi = kayit.ARGEDurum?.Adi ?? "Beklemede"
            };

            return Json(data);
        }
    }
}