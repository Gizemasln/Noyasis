using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using System.Text.Json;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;
using IOFile = System.IO.File;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace WepApp.Controllers
{
    public class AdminMusteriController : AdminBaseController
    {
        private readonly MusteriRepository _musteriRepository;
        private readonly TeklifRepository _teklifRepository;
        private readonly MusteriTipiRepository _musteriTipiRepository;
        private readonly BayiRepository _bayiRepository;
        private readonly MusteriDurumuRepository _musteriDurumuRepository;
        private readonly MusteriYetkililerRepository _yetkiliRepo;
        private readonly DepartmanRepository _departmanRepo;
        private readonly IWebHostEnvironment _environment;

        public AdminMusteriController(IWebHostEnvironment environment)
        {
            _musteriRepository = new MusteriRepository();
            _teklifRepository = new TeklifRepository();
            _musteriTipiRepository = new MusteriTipiRepository();
            _bayiRepository = new BayiRepository();
            _musteriDurumuRepository = new MusteriDurumuRepository();
            _yetkiliRepo = new MusteriYetkililerRepository();
            _departmanRepo = new DepartmanRepository();
            _environment = environment;
        }

        public IActionResult Index(int musteriId)
        {
            LoadCommonData();

            try
            {
                ViewBag.MusteriDurumu = _musteriDurumuRepository.Listele() ?? new List<MusteriDurumu>();
                ViewBag.MusteriTipleri = _musteriTipiRepository.GetirList(x => x.Durumu == 1)?.OrderBy(x => x.Adi).ToList() ?? new List<MusteriTipi>();

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                List<Bayi> bayiList;
                if (currentBayi != null)
                    bayiList = _bayiRepository.GetBayiVeAltBayiler(currentBayi.Id) ?? new List<Bayi>();
                else
                    bayiList = _bayiRepository.GetirList(x => x.Durumu == 1)?.ToList() ?? new List<Bayi>();

                // Seviye null olmasın diye garanti
                foreach (Bayi b in bayiList)
                    b.Seviye = b.Seviye ?? 0;

                ViewBag.TumBayiler = bayiList;
                if (musteriId != 0)
                {
                    List<Musteri> musteri = _musteriRepository.GetirList(x => x.Id == musteriId && x.Durum == 1, new List<string> { "MusteriTipi", "Bayi" });
                    ViewBag.MusteriList = musteri;

                }
                else
                {
                    List<Musteri> musteriler = _musteriRepository.GetirList(x => x.Durum == 1, new List<string> { "MusteriTipi", "Bayi" })
                       ?.OrderBy(x => x.Ad).ThenBy(x => x.Soyad).ToList() ?? new List<Musteri>();
                    ViewBag.MusteriList = musteriler;

                }


                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sayfa yüklenirken hata: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult KullaniciAdiKontrol(string kullaniciAdi, int? musteriId = null)
        {
            LoadCommonData();

            try
            {
                if (string.IsNullOrWhiteSpace(kullaniciAdi))
                    return Json(new { success = false, message = "Kullanıcı adı boş olamaz." });

                IQueryable<Musteri> query = _musteriRepository.GetirQueryable(x => x.Durum == 1 && x.KullaniciAdi == kullaniciAdi);
                if (musteriId.HasValue)
                    query = query.Where(x => x.Id != musteriId.Value);

                bool exists = query.Any();
                return Json(new
                {
                    success = !exists,
                    message = exists ? "Bu kullanıcı adı kullanılıyor." : "Kullanılabilir."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Kontrol sırasında hata oluştu." });
            }
        }

        [HttpGet]
        public IActionResult GetYetkili(int id)
        {
            LoadCommonData();

            try
            {
                var yetkili = _yetkiliRepo.GetirQueryable()
                    .Where(x => x.Id == id && x.Durumu == 1)
                    .Include(x => x.Departman)
                    .Select(x => new
                    {
                        x.Id,
                        x.Adi,
                        x.Soyadi,
                        x.Gorevi,
                        x.Email,
                        x.Cep,
                        x.DahiliNo,
                        x.Kodu,
                        x.Cinsiyet,
                        DepartmanId = x.DepartmanId,
                        DepartmanAdi = x.Departman != null ? x.Departman.Adi : "-",
                        x.Aktif
                    })
                    .FirstOrDefault();

                if (yetkili == null)
                    return Json(new { success = false, message = "Yetkili bulunamadı." });

                return Json(new { success = true, data = yetkili });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult SozlesmeDosyaGoster(int sozlesmeId, string tip)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesmeRepository repo = new MusteriSozlesmeRepository();
                MusteriSozlesme sozlesme = repo.Getir(sozlesmeId);

                if (sozlesme == null || sozlesme.Durumu != 1)
                {
                    return NotFound($"Sözleşme bulunamadı (ID: {sozlesmeId})");
                }

                // Debug için log ekleyin
                Console.WriteLine($"Sözleşme ID: {sozlesmeId}");
                Console.WriteLine($"Dosya tipi: {tip}");
                Console.WriteLine($"Vergi Kimlik Levhası Dosya: {sozlesme.VergiKimlikLevhasıDosyaAdi}");
                Console.WriteLine($"Ticari Sicil Gazetesi Dosya: {sozlesme.TicariSicilGazetesiDosyaAdi}");
                Console.WriteLine($"Kimlik Ön Yüzü Dosya: {sozlesme.KimlikOnYuzuDosyaAdi}");
                Console.WriteLine($"İmza Sirküsü Dosya: {sozlesme.ImzaSirkusuDosyaAdi}");

                string dosyaAdi = tip switch
                {
                    "vergi" => sozlesme.VergiKimlikLevhasıDosyaAdi,
                    "sicil" => sozlesme.TicariSicilGazetesiDosyaAdi,
                    "kimlik" => sozlesme.KimlikOnYuzuDosyaAdi,
                    "imza" => sozlesme.ImzaSirkusuDosyaAdi,
                    _ => null
                };

                Console.WriteLine($"Seçilen dosya adı: {dosyaAdi}");

                if (string.IsNullOrEmpty(dosyaAdi))
                {
                    return NotFound($"{tip} tipinde dosya adı boş veya null");
                }

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);
                Console.WriteLine($"Dosya yolu: {dosyaYolu}");

                if (!System.IO.File.Exists(dosyaYolu))
                {
                    return NotFound($"Dosya bulunamadı: {dosyaYolu}");
                }

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(dosyaAdi);

                Console.WriteLine($"Dosya başarıyla bulundu, boyut: {bytes.Length} bytes");

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                // Hata mesajını detaylı loglayın
                Console.WriteLine($"Hata: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Dosya gösterilirken hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult SozlesmeDosyaIndir(int sozlesmeId, string tip)
        {
            LoadCommonData();

            MusteriSozlesmeRepository repo = new MusteriSozlesmeRepository();
            MusteriSozlesme sozlesme = repo.Getir(sozlesmeId);
            if (sozlesme == null || sozlesme.Durumu != 1) return NotFound();

            string dosyaAdi = tip switch
            {
                "vergi" => sozlesme.VergiKimlikLevhasıDosyaAdi,
                "sicil" => sozlesme.TicariSicilGazetesiDosyaAdi,
                "kimlik" => sozlesme.KimlikOnYuzuDosyaAdi,
                "imza" => sozlesme.ImzaSirkusuDosyaAdi,
                _ => null
            };

            if (string.IsNullOrEmpty(dosyaAdi)) return NotFound();

            string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);
            if (!System.IO.File.Exists(dosyaYolu)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
            string ext = Path.GetExtension(dosyaAdi).ToLowerInvariant();
            string contentType = GetContentType(dosyaAdi);

            // İndirme için dosya adı
            string dosyaTipiAdi = tip switch
            {
                "vergi" => "Vergi_Kimlik_Levhasi",
                "sicil" => "Ticari_Sicil_Gazetesi",
                "kimlik" => "Kimlik_On_Yuzu",
                "imza" => "Imza_Sirkusu",
                _ => "Dosya"
            };

            string indirmeAdi = $"{dosyaTipiAdi}_{sozlesme.DokumanNo}{ext}";
            return File(bytes, contentType, indirmeAdi);
        }

        [HttpGet]
        public IActionResult GetMusteriSozlesmeDosyalari(int musteriId)
        {
            LoadCommonData();

            try
            {
                var sozlesmeler = new MusteriSozlesmeRepository().GetirList(x => x.MusteriId == musteriId && x.Durumu == 1)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .Select(s => new
                    {
                        s.Id,
                        s.LisansNo,
                        s.DokumanNo,
                        EklenmeTarihi = s.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                        s.SozlesmeTipi,
                        VergiKimlikLevhasiVar = !string.IsNullOrEmpty(s.VergiKimlikLevhasıDosyaAdi),
                        TicariSicilGazetesiVar = !string.IsNullOrEmpty(s.TicariSicilGazetesiDosyaAdi),
                        KimlikOnYuzuVar = !string.IsNullOrEmpty(s.KimlikOnYuzuDosyaAdi),
                        ImzaSirkusuVar = !string.IsNullOrEmpty(s.ImzaSirkusuDosyaAdi),
                        s.VergiKimlikLevhasıDosyaAdi,
                        s.TicariSicilGazetesiDosyaAdi,
                        s.KimlikOnYuzuDosyaAdi,
                        s.ImzaSirkusuDosyaAdi,
                        DosyaSayisi =
                            (!string.IsNullOrEmpty(s.VergiKimlikLevhasıDosyaAdi) ? 1 : 0) +
                            (!string.IsNullOrEmpty(s.TicariSicilGazetesiDosyaAdi) ? 1 : 0) +
                            (!string.IsNullOrEmpty(s.KimlikOnYuzuDosyaAdi) ? 1 : 0) +
                            (!string.IsNullOrEmpty(s.ImzaSirkusuDosyaAdi) ? 1 : 0)
                    })
                    .ToList();

                return Json(new { success = true, data = sozlesmeler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMusteriYetkililer(int musteriId)
        {
            LoadCommonData();

            try
            {
                var yetkililer = _yetkiliRepo.GetirQueryable()
                    .Where(x => x.MusteriId == musteriId && x.Durumu == 1)
                    .Include(y => y.Departman)
                    .OrderBy(y => y.Adi)
                    .Select(y => new
                    {
                        y.Id,
                        y.Adi,
                        y.Soyadi,
                        y.Gorevi,
                        y.Email,
                        y.Cep,
                        y.DahiliNo,
                        DepartmanAdi = y.Departman != null ? y.Departman.Adi : "-",
                        y.Cinsiyet,
                        y.Kodu,
                        y.Aktif
                    })
                    .ToList();

                return Json(new { success = true, data = yetkililer });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult EkleMusteriYetkililer([FromBody] MusteriYetkiliEkleModel model)
        {
            LoadCommonData();

            try
            {
                Musteri currentMusteri = _musteriRepository.Getir(model.MusteriId);
                if (currentMusteri == null)
                {
                    return Json(new { success = false, message = "Müşteri bulunamadı." });
                }

                MusteriYetkililer yeniYetkili = new MusteriYetkililer
                {
                    MusteriId = model.MusteriId,
                    Adi = model.Adi,
                    Soyadi = model.Soyadi,
                    Gorevi = model.Gorevi,
                    Email = model.Email,
                    Cep = model.Cep,
                    DahiliNo = model.DahiliNo,
                    DepartmanId = model.DepartmanId,
                    Cinsiyet = model.Cinsiyet,
                    Kodu = model.Kodu,
                    Aktif = 1,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _yetkiliRepo.Ekle(yeniYetkili);
                return Json(new { success = true, message = "Yetkili başarıyla eklendi.", id = yeniYetkili.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult SilMusteriYetkili(int id)
        {
            LoadCommonData();

            try
            {
                MusteriYetkililer yetkili = _yetkiliRepo.Getir(id);
                if (yetkili == null)
                {
                    return Json(new { success = false, message = "Yetkili bulunamadı." });
                }

                yetkili.Durumu = 0;
                yetkili.GuncellenmeTarihi = DateTime.Now;
                _yetkiliRepo.Guncelle(yetkili);

                return Json(new { success = true, message = "Yetkili silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult GuncelleMusteriYetkili([FromBody] MusteriYetkililerGuncelleModel model)
        {
            LoadCommonData();
            try
            {
                MusteriYetkililer yetkili = _yetkiliRepo.Getir(model.Id);
                if (yetkili == null || yetkili.Durumu != 1)
                    return Json(new { success = false, message = "Yetkili bulunamadı." });

                yetkili.Adi = model.Adi;
                yetkili.Soyadi = model.Soyadi;
                yetkili.Gorevi = model.Gorevi;
                yetkili.Email = model.Email;
                yetkili.Aktif = model.Aktif;
                yetkili.DahiliNo = model.DahiliNo;
                yetkili.Kodu = model.Kodu;
                yetkili.Cinsiyet = model.Cinsiyet;
                yetkili.DepartmanId = model.DepartmanId;
                yetkili.GuncellenmeTarihi = DateTime.Now;

                _yetkiliRepo.Guncelle(yetkili);
                return Json(new { success = true, message = "Yetkili güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetDepartmanlar()
        {
            LoadCommonData();
            try
            {
                var departmanlar = _departmanRepo.GetirList(x => x.Durumu == 1)
                    .OrderBy(d => d.Adi)
                    .Select(d => new { d.Id, d.Adi })
                    .ToList();

                return Json(new { success = true, data = departmanlar });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(
            string Ad, string Soyad, string KullaniciAdi, string Sifre,
            string Email, string Telefon, string Adres, string Il, string Ilce, string Belde, string Bolge,
            string TCVNo, string VergiDairesi, string KepAdresi, string WebAdresi, string Aciklama,
            string AlpemixFirmaAdi, string AlpemixGrupAdi, string AlpemixSifre,
            int? MusteriTipiId, int MusteriDurumuId, int? BayiId, string TicariUnvan,
            string Diger,
            IFormFile Logo, IFormFile Imza, List<MusteriYetkiliEkleModel> Yetkililer = null)
        {
            LoadCommonData();

            try
            {
                LoadCommonData();

                IEnumerable<MusteriTipi> musteriTipleri = ViewBag.MusteriTipleri as IEnumerable<MusteriTipi>;
                int? digerTipiId = musteriTipleri?
                    .FirstOrDefault(x => x.Adi.ToLower() == "diğer")
                    ?.Id;

                if (MusteriTipiId == digerTipiId && string.IsNullOrWhiteSpace(Diger))
                {
                    TempData["Error"] = "Müşteri tipi 'Diğer' seçildiyse, lütfen diğer tipi belirtin.";
                    return RedirectToAction("Index");
                }

                if (_musteriRepository.GetirList(x => x.KullaniciAdi == KullaniciAdi && x.Durum == 1).Any())
                {
                    TempData["Error"] = "Bu kullanıcı adı zaten alınmış.";
                    return RedirectToAction("Index");
                }

                Musteri model = new Musteri
                {
                    Ad = Ad ?? "",
                    Soyad = Soyad ?? "",
                    TicariUnvan = TicariUnvan ?? "",
                    KullaniciAdi = KullaniciAdi ?? "",
                    Sifre = Sifre ?? "",
                    Email = Email ?? "",
                    Telefon = Telefon ?? "",
                    Adres = Adres ?? "",
                    Il = Il ?? "",
                    Ilce = Ilce ?? "",
                    Belde = Belde ?? "",
                    Bolge = Bolge ?? "",
                    TCVNo = TCVNo ?? "",
                    VergiDairesi = VergiDairesi ?? "",
                    KepAdresi = KepAdresi ?? "",
                    WebAdresi = WebAdresi ?? "",
                    Aciklama = Aciklama ?? "",
                    AlpemixFirmaAdi = AlpemixFirmaAdi ?? "",
                    AlpemixGrupAdi = AlpemixGrupAdi ?? "",
                    AlpemixSifre = AlpemixSifre ?? "",
                    MusteriTipiId = MusteriTipiId,
                    MusteriDurumuId = MusteriDurumuId,
                    BayiId = BayiId,
                    Diger = Diger ?? "",
                    Durum = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    EkleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0,
                    GuncelleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0
                };

                // Logo ve İmza işlemleri
                if (Logo != null && Logo.Length > 0)
                {
                    string logoDosyaAdi = await DosyaKaydet(Logo, "logo");
                    model.LogoUzanti = logoDosyaAdi;
                }

                if (Imza != null && Imza.Length > 0)
                {
                    string imzaDosyaAdi = await DosyaKaydet(Imza, "imza");
                    model.ImzaUzanti = imzaDosyaAdi;
                }

                _musteriRepository.Ekle(model);

                // YETKİLİLERİ EKLE
                if (Yetkililer != null && Yetkililer.Any())
                {
                    foreach (MusteriYetkiliEkleModel yetkiliModel in Yetkililer)
                    {
                        MusteriYetkililer yeniYetkili = new MusteriYetkililer
                        {
                            MusteriId = model.Id,
                            Adi = yetkiliModel.Adi?.Trim() ?? "",
                            Soyadi = yetkiliModel.Soyadi?.Trim() ?? "",
                            Gorevi = yetkiliModel.Gorevi?.Trim() ?? "",
                            Email = yetkiliModel.Email?.Trim() ?? "",
                            Cep = yetkiliModel.Cep?.Trim() ?? "",
                            DahiliNo = yetkiliModel.DahiliNo?.Trim() ?? "",
                            DepartmanId = yetkiliModel.DepartmanId,
                            Cinsiyet = yetkiliModel.Cinsiyet?.Trim() ?? "",
                            Kodu = yetkiliModel.Kodu?.Trim() ?? "",
                            Durumu = 1,
                            Aktif = 1,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _yetkiliRepo.Ekle(yeniYetkili);
                    }
                }

                TempData["Success"] = "Müşteri ve yetkililer başarıyla eklendi.";
                TempData["YeniMusteriId"] = model.Id;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Müşteri eklenirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(
            int Id, string Ad, string Soyad, string KullaniciAdi, string Sifre,
            string Email, string Telefon, string Adres, string Il, string Ilce, string Belde, string Bolge,
            string TCVNo, string VergiDairesi, string KepAdresi, string WebAdresi, string Aciklama,
            string AlpemixFirmaAdi, string AlpemixGrupAdi, string AlpemixSifre,
            int? MusteriTipiId, int? MusteriDurumuId, int? BayiId, string TicariUnvan,
            string Diger,
            IFormFile Logo, IFormFile Imza, List<MusteriYetkiliEkleModel> YeniYetkililer = null)
        {
            LoadCommonData();

            try
            {
                Musteri existing = _musteriRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Müşteri bulunamadı.";
                    return RedirectToAction("Index");
                }

                IEnumerable<MusteriTipi> musteriTipleri = ViewBag.MusteriTipleri as IEnumerable<MusteriTipi>;
                int? digerTipiId = musteriTipleri?
                    .FirstOrDefault(x => x.Adi.ToLower() == "diğer")
                    ?.Id;

                if (MusteriTipiId == digerTipiId && string.IsNullOrWhiteSpace(Diger))
                {
                    TempData["Error"] = "Müşteri tipi 'Diğer' seçildiyse, lütfen diğer tipi belirtin.";
                    return RedirectToAction("Index");
                }

                Musteri duplicate = _musteriRepository.GetirList(x => x.KullaniciAdi == KullaniciAdi && x.Id != Id && x.Durum == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu kullanıcı adı başka bir müşteriye ait.";
                    return RedirectToAction("Index");
                }

                // Güncelleme işlemleri
                existing.Ad = Ad ?? "";
                existing.TicariUnvan = TicariUnvan;
                existing.Soyad = Soyad ?? "";
                existing.KullaniciAdi = KullaniciAdi ?? "";
                if (!string.IsNullOrEmpty(Sifre)) existing.Sifre = Sifre;
                existing.Email = Email ?? "";
                existing.Telefon = Telefon ?? "";
                existing.Adres = Adres ?? "";
                existing.Il = Il ?? "";
                existing.Ilce = Ilce ?? "";
                existing.Belde = Belde ?? "";
                existing.Bolge = Bolge ?? "";
                existing.TCVNo = TCVNo ?? "";
                existing.VergiDairesi = VergiDairesi ?? "";
                existing.KepAdresi = KepAdresi ?? "";
                existing.WebAdresi = WebAdresi ?? "";
                existing.Aciklama = Aciklama ?? "";
                existing.AlpemixFirmaAdi = AlpemixFirmaAdi ?? "";
                existing.AlpemixGrupAdi = AlpemixGrupAdi ?? "";
                existing.AlpemixSifre = AlpemixSifre ?? "";
                existing.MusteriTipiId = MusteriTipiId;
                existing.MusteriDurumuId = MusteriDurumuId;
                existing.BayiId = BayiId;
                existing.Diger = Diger ?? "";
                existing.GuncellenmeTarihi = DateTime.Now;
                existing.GuncelleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0;

                // Logo ve İmza işlemleri
                if (Logo != null && Logo.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.LogoUzanti))
                        EskiDosyayiSil(existing.LogoUzanti);

                    string logoDosyaAdi = await DosyaKaydet(Logo, "logo");
                    existing.LogoUzanti = logoDosyaAdi;
                }

                if (Imza != null && Imza.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.ImzaUzanti))
                        EskiDosyayiSil(existing.ImzaUzanti);

                    string imzaDosyaAdi = await DosyaKaydet(Imza, "imza");
                    existing.ImzaUzanti = imzaDosyaAdi;
                }

                _musteriRepository.Guncelle(existing);

                // YENİ YETKİLİLERİ EKLE
                if (YeniYetkililer != null && YeniYetkililer.Any())
                {
                    foreach (MusteriYetkiliEkleModel y in YeniYetkililer)
                    {
                        MusteriYetkililer yeniYetkili = new MusteriYetkililer
                        {
                            MusteriId = Id,
                            Adi = y.Adi?.Trim() ?? "",
                            Soyadi = y.Soyadi?.Trim() ?? "",
                            Gorevi = y.Gorevi?.Trim() ?? "",
                            Email = y.Email?.Trim() ?? "",
                            Cep = y.Cep?.Trim() ?? "",
                            DahiliNo = y.DahiliNo?.Trim() ?? "",
                            DepartmanId = y.DepartmanId,
                            Cinsiyet = y.Cinsiyet?.Trim() ?? "",
                            Kodu = y.Kodu?.Trim() ?? "",
                            Durumu = 1,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _yetkiliRepo.Ekle(yeniYetkili);
                    }
                }

                TempData["Success"] = "Müşteri ve yeni yetkililer başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Müşteri güncellenirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            List<string> join = new List<string> { "MusteriDurumu", "MusteriTipi", "Bayi" };
            try
            {
                Musteri item = _musteriRepository.Getir(x => x.Id == id && x.Durum == 1, join);
                if (item == null)
                    return Json(new { success = false, message = "Müşteri bulunamadı." });

                return Json(new
                {
                    success = true,
                    id = item.Id,
                    ad = item.Ad ?? "",
                    ticariUnvan = item.TicariUnvan ?? "",
                    soyad = item.Soyad ?? "",
                    kullaniciAdi = item.KullaniciAdi ?? "",
                    email = item.Email ?? "",
                    telefon = item.Telefon ?? "",
                    adres = item.Adres ?? "",
                    il = item.Il ?? "",
                    ilce = item.Ilce ?? "",
                    belde = item.Belde ?? "",
                    bolge = item.Bolge ?? "",
                    tcvNo = item.TCVNo ?? "",
                    vergiDairesi = item.VergiDairesi ?? "",
                    kepAdresi = item.KepAdresi ?? "",
                    webAdresi = item.WebAdresi ?? "",
                    aciklama = item.Aciklama ?? "",
                    alpemixFirmaAdi = item.AlpemixFirmaAdi ?? "",
                    alpemixGrupAdi = item.AlpemixGrupAdi ?? "",
                    alpemixSifre = item.AlpemixSifre ?? "",
                    musteriTipiId = item.MusteriTipiId ?? 0,
                    musteriDurumuId = item.MusteriDurumuId ?? 0,
                    bayiId = item.BayiId,
                    diger = item.Diger ?? "",
                    musteriTipiAdi = item.MusteriTipi?.Adi ?? "-",
                    musteriDurumuAdi = item.MusteriDurumu?.Adi ?? "-",
                    bayiUnvan = item.Bayi?.Unvan ?? "Bağımsız Müşteri",
                    logoUzanti = item.LogoUzanti,
                    imzaUzanti = item.ImzaUzanti
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Veri getirilirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            try
            {
                Musteri m = _musteriRepository.Getir(Id);
                if (m != null)
                {
                    if (!string.IsNullOrEmpty(m.LogoUzanti))
                    {
                        EskiDosyayiSil(m.LogoUzanti);
                    }
                    if (!string.IsNullOrEmpty(m.ImzaUzanti))
                    {
                        EskiDosyayiSil(m.ImzaUzanti);
                    }
                    m.Durum = 0;
                    m.GuncellenmeTarihi = DateTime.Now;
                    m.GuncelleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0;
                    _musteriRepository.Guncelle(m);
                    TempData["Success"] = "Müşteri ve dosyaları silindi.";
                }
                else
                {
                    TempData["Error"] = "Müşteri bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Müşteri silinirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // ============= DOSYA GÖRÜNTÜLEME =============
        [HttpGet]
        public IActionResult LogoGoster(int id)
        {
            LoadCommonData();
            try
            {
                Musteri musteri = _musteriRepository.Getir(id);
                if (musteri == null || string.IsNullOrEmpty(musteri.LogoUzanti))
                    return NotFound();

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.LogoUzanti);
                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound();

                string contentType = GetContentType(musteri.LogoUzanti);
                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult ImzaGoster(int id)
        {
            LoadCommonData();

            try
            {
                Musteri musteri = _musteriRepository.Getir(id);
                if (musteri == null || string.IsNullOrEmpty(musteri.ImzaUzanti))
                    return NotFound();

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.ImzaUzanti);
                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound();

                string contentType = GetContentType(musteri.ImzaUzanti);
                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        // ============= DOSYA İNDİRME =============
        [HttpGet]
        public IActionResult LogoIndir(int id)
        {
            LoadCommonData();
            try
            {
                Musteri musteri = _musteriRepository.Getir(id);
                if (musteri == null || string.IsNullOrEmpty(musteri.LogoUzanti))
                    return NotFound();

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.LogoUzanti);
                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound();

                string contentType = GetContentType(musteri.LogoUzanti);
                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string fileName = $"logo_{musteri.Id}_{Path.GetFileName(musteri.LogoUzanti)}";

                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult ImzaIndir(int id)
        {
            LoadCommonData();

            try
            {
                Musteri musteri = _musteriRepository.Getir(id);
                if (musteri == null || string.IsNullOrEmpty(musteri.ImzaUzanti))
                    return NotFound();

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.ImzaUzanti);
                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound();

                string contentType = GetContentType(musteri.ImzaUzanti);
                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string fileName = $"imza_{musteri.Id}_{Path.GetFileName(musteri.ImzaUzanti)}";

                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        // ============= DOSYA SİLME =============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SilLogo(int Id)
        {
            LoadCommonData();

            try
            {
                Musteri musteri = _musteriRepository.Getir(Id);
                if (musteri != null && !string.IsNullOrEmpty(musteri.LogoUzanti))
                {
                    EskiDosyayiSil(musteri.LogoUzanti);
                    musteri.LogoUzanti = null;
                    musteri.GuncellenmeTarihi = DateTime.Now;
                    musteri.GuncelleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0;
                    _musteriRepository.Guncelle(musteri);
                    TempData["Success"] = "Logo başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Logo bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Logo silinirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SilImza(int Id)
        {
            LoadCommonData();

            try
            {
                Musteri musteri = _musteriRepository.Getir(Id);
                if (musteri != null && !string.IsNullOrEmpty(musteri.ImzaUzanti))
                {
                    EskiDosyayiSil(musteri.ImzaUzanti);
                    musteri.ImzaUzanti = null;
                    musteri.GuncellenmeTarihi = DateTime.Now;
                    musteri.GuncelleyenKullaniciId = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici")?.Id ?? 0;
                    _musteriRepository.Guncelle(musteri);
                    TempData["Success"] = "İmza başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "İmza bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İmza silinirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // ============= DOSYA BİLGİSİ GETİR =============
        [HttpGet]
        public IActionResult GetDosyaBilgisi(int id)
        {
            LoadCommonData();

            try
            {
                Musteri musteri = _musteriRepository.Getir(id);
                if (musteri == null)
                    return Json(new { success = false, message = "Müşteri bulunamadı." });

                // Dosya boyutlarını hesapla
                long logoBoyut = 0;
                long imzaBoyut = 0;

                if (!string.IsNullOrEmpty(musteri.LogoUzanti))
                {
                    string logoYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.LogoUzanti);
                    if (System.IO.File.Exists(logoYolu))
                    {
                        FileInfo fileInfo = new FileInfo(logoYolu);
                        logoBoyut = fileInfo.Length;
                    }
                }

                if (!string.IsNullOrEmpty(musteri.ImzaUzanti))
                {
                    string imzaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", musteri.ImzaUzanti);
                    if (System.IO.File.Exists(imzaYolu))
                    {
                        FileInfo fileInfo = new FileInfo(imzaYolu);
                        imzaBoyut = fileInfo.Length;
                    }
                }

                return Json(new
                {
                    success = true,
                    logoUzanti = musteri.LogoUzanti,
                    imzaUzanti = musteri.ImzaUzanti,
                    logoVar = !string.IsNullOrEmpty(musteri.LogoUzanti),
                    imzaVar = !string.IsNullOrEmpty(musteri.ImzaUzanti),
                    logoBoyut = logoBoyut,
                    imzaBoyut = imzaBoyut,
                    toplamBoyut = logoBoyut + imzaBoyut
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Dosya bilgisi alınamadı: " + ex.Message });
            }
        }

        // ============= DOSYA KAYDETME =============
        private async Task<string> DosyaKaydet(IFormFile dosya, string dosyaTipi)
        {
            if (dosya == null || dosya.Length == 0)
                return null;

            // Dosya boyutu kontrolü (5MB)
            if (dosya.Length > 5 * 1024 * 1024)
                throw new Exception($"{dosyaTipi} boyutu 5MB'tan büyük olamaz. Seçilen dosya: {(dosya.Length / 1024 / 1024).ToString("0.00")}MB");

            // Dosya tipi kontrolü
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            string extension = Path.GetExtension(dosya.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new Exception($"{dosyaTipi} için sadece JPG, PNG, GIF, BMP ve WEBP dosyaları yüklenebilir.");

            // MIME type kontrolü
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
            if (!allowedMimeTypes.Contains(dosya.ContentType))
                throw new Exception($"{dosyaTipi} için geçersiz dosya formatı: {dosya.ContentType}");

            // Klasör oluşturma
            string uploadsKlasoru = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme");
            if (!Directory.Exists(uploadsKlasoru))
            {
                Directory.CreateDirectory(uploadsKlasoru);
            }

            // GUID tabanlı dosya adı
            string dosyaAdi = $"{Guid.NewGuid():N}{extension}";
            string dosyaYolu = Path.Combine(uploadsKlasoru, dosyaAdi);

            // Dosyayı kaydet
            using (FileStream stream = new FileStream(dosyaYolu, FileMode.Create))
            {
                await dosya.CopyToAsync(stream);
            }

            // Resim optimizasyonu
            try
            {
                await OptimizeImageAsync(dosyaYolu, extension);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{dosyaTipi} optimizasyon hatası: {ex.Message}");
                // Optimizasyon başarısız olsa bile dosyayı kaydet
            }

            return dosyaAdi;
        }

        // ============= RESİM OPTİMİZASYONU =============
        private async Task OptimizeImageAsync(string dosyaYolu, string extension)
        {
            var optimizableExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!optimizableExtensions.Contains(extension.ToLower()))
                return;

            try
            {
                using (Image image = await Image.LoadAsync(dosyaYolu))
                {
                    // Maksimum boyut kontrolü (1200px genişlik)
                    if (image.Width > 1200)
                    {
                        int newWidth = 1200;
                        int newHeight = (int)(image.Height * (1200.0 / image.Width));
                        image.Mutate(x => x.Resize(newWidth, newHeight));
                    }

                    // Kalite ayarları
                    if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        await image.SaveAsync(dosyaYolu, new JpegEncoder { Quality = 85 });
                    }
                    else if (extension.ToLower() == ".png")
                    {
                        await image.SaveAsync(dosyaYolu, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Optimizasyon hatası: {ex.Message}");
            }
        }

        // ============= DOSYA SİLME =============
        private void EskiDosyayiSil(string dosyaAdi)
        {
            LoadCommonData();

            if (string.IsNullOrEmpty(dosyaAdi))
                return;

            try
            {
                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", dosyaAdi);

                // Güvenlik kontrolü
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                string extension = Path.GetExtension(dosyaAdi).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    System.Diagnostics.Debug.WriteLine($"Geçersiz dosya uzantısı: {dosyaAdi}");
                    return;
                }

                if (System.IO.File.Exists(dosyaYolu))
                {
                    System.IO.File.Delete(dosyaYolu);
                    System.Diagnostics.Debug.WriteLine($"Dosya silindi: {dosyaAdi}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dosya silinirken hata: {ex.Message}");
            }
        }

        // ============= DOSYA TİPİ BELİRLEME =============
        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        public record ToggleRequest(int Id);

        [HttpPost]
        public IActionResult ToggleYetkiliAktif([FromBody] ToggleRequest request)
        {
            LoadCommonData();

            MusteriYetkililer yetkili = _yetkiliRepo.Getir(request.Id);
            if (yetkili == null || yetkili.Durumu != 1)
                return Json(new { success = false, message = "Yetkili bulunamadı." });

            yetkili.Aktif = yetkili.Aktif == 1 ? 0 : 1;
            yetkili.GuncellenmeTarihi = DateTime.Now;
            _yetkiliRepo.Guncelle(yetkili);

            return Json(new { success = true, message = "Durum güncellendi." });
        }

        [HttpGet]
        public IActionResult GetMusteriTeklifleri(int musteriId)
        {
            LoadCommonData();

            try
            {
                var teklifler = _teklifRepository.GetirList(x => x.MusteriId == musteriId && x.Aktif)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .Select(t => new
                    {
                        t.Id,
                        t.TeklifNo,
                        EklenmeTarihi = t.EklenmeTarihi?.ToString("dd.MM.yyyy"),
                        GecerlilikTarihi = t.GecerlilikTarihi?.ToString("dd.MM.yyyy"),
                        t.NetToplam,
                        t.OnaylandiMi,
                        TeklifDurumAdi = t.TeklifDurum?.Adi ?? "-",
                        LisansTipAdi = t.LisansTip?.Adi ?? "-"
                    })
                    .ToList();

                return Json(new { success = true, data = teklifler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Teklifler yüklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMusteriSozlesmeleri(int musteriId)
        {
            LoadCommonData();

            MusteriSozlesmeRepository _sozlesmeRepository = new MusteriSozlesmeRepository();
            try
            {
                var sozlesmeler = _sozlesmeRepository.GetirList(x =>
                        x.MusteriId == musteriId && x.Durumu == 1)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .Select(s => new
                    {
                        s.Id,
                        s.LisansNo,
                        s.DokumanNo,
                        EklenmeTarihi = s.EklenmeTarihi.ToString("dd.MM.yyyy"),
                        SozlesmeTipi = s.SozlesmeTipi ?? "-",
                        YillikBakim = s.YillikBakim,
                        SozlesmeDurumAdi = s.SozlesmeDurumu != null ? s.SozlesmeDurumu.Adi : "-",
                        EntegratorAdi = s.Entegrator != null ? s.Entegrator.Adi : "-",
                        s.OdemeBekleme,
                        DosyaVarMi = !string.IsNullOrEmpty(s.DosyaAdi),
                        TeklifNo = s.Teklif != null ? s.Teklif.TeklifNo : "-"
                    })
                    .ToList();

                return Json(new { success = true, data = sozlesmeler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sözleşmeler yüklenirken hata oluştu: " + ex.Message });
            }
        }
    }

    // MODELLER
    public class MusteriYetkililerGuncelleModel
    {
        public int Id { get; set; }
        public string Adi { get; set; }
        public string Soyadi { get; set; }
        public int Aktif { get; set; }
        public string Gorevi { get; set; }
        public string Email { get; set; }
        public string Cep { get; set; }
        public string DahiliNo { get; set; }
        public string Kodu { get; set; }
        public string Cinsiyet { get; set; }
        public int? DepartmanId { get; set; }
    }

    public class MusteriYetkiliEkleModel
    {
        public int MusteriId { get; set; }
        public string Adi { get; set; } = string.Empty;
        public string Soyadi { get; set; } = string.Empty;
        public string Gorevi { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string DahiliNo { get; set; } = string.Empty;
        public int? DepartmanId { get; set; }
        public string Cinsiyet { get; set; } = string.Empty;
        public string Kodu { get; set; } = string.Empty;

        public MusteriYetkiliEkleModel() { }
    }
}