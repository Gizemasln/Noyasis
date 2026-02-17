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
    public class AdminBayiController : AdminBaseController
    {
        private readonly BayiRepository _bayiRepository;
        private readonly BayiSozlesmeRepository _sozlesmeRepo;
        private readonly DepartmanRepository _departmanRepo;
        private readonly BayiYetkililerRepository _yetkiliRepo;
        private readonly MusteriRepository _musteriRepository;
        private readonly TeklifRepository _teklifRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly MusteriSozlesmeRepository _musteriSozlesmeRepo;

        public AdminBayiController(IWebHostEnvironment environment)
        {
            _bayiRepository = new BayiRepository();
            _sozlesmeRepo = new BayiSozlesmeRepository();
            _departmanRepo = new DepartmanRepository();
            _yetkiliRepo = new BayiYetkililerRepository();
            _musteriRepository = new MusteriRepository();
            _teklifRepository = new TeklifRepository();
            _musteriSozlesmeRepo = new MusteriSozlesmeRepository();
            _environment = environment;
        }

        public IActionResult Index()
        {
            LoadCommonData();

            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");

            List<Bayi> bayiList;

            if (bayi != null)
            {
                // Bayi girişi: Sadece kendisi ve alt bayileri
                bayiList = GetBayiVeAltBayiler(bayi.Id);
            }
            if (musteri != null)
            {
                bayiList = GetBayi(musteri.BayiId ?? 0);
            }
            else
            {
                // Kurumsal giriş: Tüm aktif bayiler
                bayiList = _bayiRepository.GetirList(x => x.Durumu == 1).ToList();
            }

            List<BayiViewModel> bayiViewModelList = bayiList.Select(b => new BayiViewModel
            {
                Id = b.Id,
                Unvan = b.Unvan,
                KullaniciAdi = b.KullaniciAdi,
                Email = b.Email,
                Distributor = b.Distributor,
                Kodu = b.Kodu,
                Telefon = b.Telefon,
                Adres = b.Adres,
                UstBayiId = b.UstBayiId,
                Seviye = b.Seviye ?? 0,
                UstBayiAd = b.UstBayi != null ? b.UstBayi.Unvan : "Ana Bayi",
                AltBayiSayisi = _bayiRepository.GetirList(x => x.UstBayiId == b.Id && x.Durumu == 1).Count,
                MusteriSayisi = _musteriRepository.GetirList(x => x.BayiId == b.Id && x.Durum == 1).Count
            })
            .OrderBy(x => x.Seviye).ThenBy(x => x.Unvan)
            .ToList();

            ViewBag.BayiList = bayiViewModelList;
            ViewBag.TumBayiler = bayiList;
            ViewBag.AnaBayiler = bayiList.Where(x => x.UstBayiId == null).ToList();
            ViewBag.CurrentBayi = bayi;
            ViewBag.CurrentKullanici = kullanici;

            return View();
        }

        [HttpGet]
        public IActionResult DetayGetir(int id)
        {
            LoadCommonData();

            try
            {
                List<string> join = new List<string> { "UstBayi" };
                Bayi bayi = _bayiRepository.Getir(x => x.Id == id && x.Durumu == 1, join);
                

                if (bayi == null)
                    return Json(new { success = false, message = "Bayi bulunamadı." });

                // Yetki kontrolü
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, id))
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });

                return Json(new
                {
                    success = true,
                    id = bayi.Id,
                    unvan = bayi.Unvan ?? "",
                    kullaniciAdi = bayi.KullaniciAdi ?? "",
                    email = bayi.Email ?? "",
                    telefon = bayi.Telefon ?? "",
                    adres = bayi.Adres ?? "",
                    kodu = bayi.Kodu ?? "",
                    bolge = bayi.Bolge ?? "",
                    il = bayi.Il ?? "",
                    ilce = bayi.Ilce ?? "",
                    belde = bayi.Belde ?? "",
                    tcvNo = bayi.TCVNo ?? "",
                    distributor = bayi.Distributor, // YENİ ALAN

                    vergiDairesi = bayi.VergiDairesi ?? "",
                    kepAdresi = bayi.KepAdresi ?? "",
                    webAdresi = bayi.WebAdresi ?? "",
                    aciklama = bayi.Aciklama ?? "",
                    alpemixFirmaAdi = bayi.AlpemixFirmaAdi ?? "",
                    alpemixGrupAdi = bayi.AlpemixGrupAdi ?? "",
                    alpemixSifre = bayi.AlpemixSifre ?? "",
                    ustBayiId = bayi.UstBayiId,
                    ustBayiAd = bayi.UstBayi?.Unvan ?? "Ana Bayi",
                    seviye = bayi.Seviye ?? 0,
                    logoUzanti = bayi.LogoUzanti,
                    imzaUzanti = bayi.ImzaUzanti,
                    eklenmeTarihi = bayi.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    guncellenmeTarihi = bayi.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bayi detayı getirilirken hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetBayiMusterileri(int bayiId)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayiId))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                // Bayi ve alt bayilerine ait müşteriler
                List<Bayi> bayiVeAltlar = GetBayiVeAltBayiler(bayiId);
                List<int> bayiIdList = bayiVeAltlar.Select(b => b.Id).ToList();

                var musteriler = _musteriRepository.GetirQueryable()
                    .Where(x => bayiIdList.Contains(x.BayiId ?? 0) && x.Durum == 1)
                    .Include(m => m.Bayi)
                    .Include(m => m.MusteriTipi)
                    .OrderBy(m => m.Ad)
                    .ThenBy(m => m.Soyad)
                    .Select(m => new
                    {
                        m.Id,
                        AdSoyad = m.Ad + " " + m.Soyad,
                        m.TicariUnvan,
                        m.KullaniciAdi,
                        m.Email,
                        m.Telefon,
                        m.TCVNo,
                        BayiAd = m.Bayi != null ? m.Bayi.Unvan : "-",
                        MusteriTipAdi = m.MusteriTipi != null ? m.MusteriTipi.Adi : "-",
                        EklenmeTarihi = m.EklenmeTarihi.ToString("dd.MM.yyyy")
                    })
                    .ToList();

                return Json(new { success = true, data = musteriler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Müşteriler yüklenirken hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetBayiMusteriTeklifleri(int bayiId)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayiId))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                // Bayi ve alt bayilerine ait müşteriler
                List<Bayi> bayiVeAltlar = GetBayiVeAltBayiler(bayiId);
                List<int> bayiIdList = bayiVeAltlar.Select(b => b.Id).ToList();

                List<int> musteriIds = _musteriRepository.GetirList(x => bayiIdList.Contains(x.BayiId ?? 0) && x.Durum == 1)
                    .Select(m => m.Id)
                    .ToList();

                if (!musteriIds.Any())
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var teklifler = _teklifRepository.GetirQueryable()
                    .Where(t => musteriIds.Contains(t.MusteriId) && t.Aktif)
                    .Include(t => t.Musteri)
                    .Include(t => t.TeklifDurum)
                    .Include(t => t.LisansTip)
                    .OrderByDescending(t => t.EklenmeTarihi)
                    .Select(t => new
                    {
                        t.Id,
                        t.TeklifNo,
                        EklenmeTarihi = t.EklenmeTarihi.HasValue ? t.EklenmeTarihi.Value.ToString("dd.MM.yyyy") : "-",
                        GecerlilikTarihi = t.GecerlilikTarihi.HasValue ? t.GecerlilikTarihi.Value.ToString("dd.MM.yyyy") : "-",
                        t.NetToplam,
                        t.OnaylandiMi,
                        TeklifDurumAdi = t.TeklifDurum != null ? t.TeklifDurum.Adi : "-",
                        LisansTipAdi = t.LisansTip != null ? t.LisansTip.Adi : "-",
                        MusteriAdi = t.Musteri.Ad + " " + (t.Musteri.Soyad ?? ""),
                        MusteriTicariUnvan = t.Musteri.TicariUnvan ?? "",
                        MusteriId = t.Musteri.Id
                    })
                    .ToList();

                return Json(new { success = true, data = teklifler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Teklifler yüklenirken hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetBayiMusteriSozlesmeleri(int bayiId)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayiId))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                List<Bayi> bayiVeAltlar = GetBayiVeAltBayiler(bayiId);
                List<int> bayiIdList = bayiVeAltlar.Select(b => b.Id).ToList();

                List<int> musteriIds = _musteriRepository.GetirList(x => bayiIdList.Contains(x.BayiId ?? 0) && x.Durum == 1)
                    .Select(m => m.Id)
                    .ToList();

                if (!musteriIds.Any())
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                MusteriSozlesmeRepository sozlesmeRepo = new MusteriSozlesmeRepository();
                var sozlesmeler = sozlesmeRepo.GetirQueryable()
                    .Where(s => musteriIds.Contains(s.MusteriId) && s.Durumu == 1)
                    .Include(s => s.Musteri)
                    .Include(s => s.SozlesmeDurumu)
                    .Include(s => s.Entegrator)
                    .Include(s => s.Teklif)
                    .OrderByDescending(s => s.EklenmeTarihi)
                    .Select(s => new
                    {
                        s.Id,
                        s.LisansNo,
                        s.DokumanNo,
                        EklenmeTarihi = s.EklenmeTarihi.ToString("dd.MM.yyyy"),
                        SozlesmeTipi = s.SozlesmeTipi ?? "-",
                        s.YillikBakim,
                        SozlesmeDurumAdi = s.SozlesmeDurumu != null ? s.SozlesmeDurumu.Adi : "-",
                        EntegratorAdi = s.Entegrator != null ? s.Entegrator.Adi : "-",
                        s.OdemeBekleme,
                        MusteriAdi = s.Musteri.Ad + " " + (s.Musteri.Soyad ?? ""),
                        MusteriTicariUnvan = s.Musteri.TicariUnvan ?? "",
                        MusteriId = s.Musteri.Id,
                        TeklifNo = s.Teklif != null ? s.Teklif.TeklifNo : "-",
                        DosyaVarMi = !string.IsNullOrEmpty(s.DosyaAdi)
                    })
                    .ToList();

                return Json(new { success = true, data = sozlesmeler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sözleşmeler yüklenirken hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetBayiMusteriSozlesmeDosyalari(int bayiId)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayiId))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                List<Bayi> bayiVeAltlar = GetBayiVeAltBayiler(bayiId);
                List<int> bayiIdList = bayiVeAltlar.Select(b => b.Id).ToList();

                List<int> musteriIds = _musteriRepository.GetirList(x => bayiIdList.Contains(x.BayiId ?? 0) && x.Durum == 1)
                    .Select(m => m.Id)
                    .ToList();

                if (!musteriIds.Any())
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                MusteriSozlesmeRepository sozlesmeRepo = new MusteriSozlesmeRepository();
                var sozlesmeler = sozlesmeRepo.GetirQueryable()
                    .Where(x => musteriIds.Contains(x.MusteriId) && x.Durumu == 1)
                    .Include(x => x.Musteri)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .Select(s => new
                    {
                        s.Id,
                        s.LisansNo,
                        s.DokumanNo,
                        EklenmeTarihi = s.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                        s.SozlesmeTipi,
                        MusteriAdi = s.Musteri.Ad + " " + (s.Musteri.Soyad ?? ""),
                        MusteriTicariUnvan = s.Musteri.TicariUnvan ?? "",
                        VergiKimlikLevhasiVar = !string.IsNullOrEmpty(s.VergiKimlikLevhasıDosyaAdi),
                        TicariSicilGazetesiVar = !string.IsNullOrEmpty(s.TicariSicilGazetesiDosyaAdi),
                        KimlikOnYuzuVar = !string.IsNullOrEmpty(s.KimlikOnYuzuDosyaAdi),
                        ImzaSirkusuVar = !string.IsNullOrEmpty(s.ImzaSirkusuDosyaAdi)
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
        public IActionResult SozlesmeDosyaGoster(int sozlesmeId, string tip)
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
            string contentType = GetContentType(dosyaAdi);
            return File(bytes, contentType);
        }

        [HttpGet]
        public IActionResult SozlesmeDosyaIndir(int sozlesmeId, string tip)
        {
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
                        x.Aktif,
                        DepartmanId = x.DepartmanId,
                        DepartmanAdi = x.Departman != null ? x.Departman.Adi : "-"
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
        public IActionResult GetBayiYetkililer(int bayiId)
        {
            LoadCommonData();

            try
            {
                var yetkililer = _yetkiliRepo.GetirQueryable()
                    .Where(x => x.BayiId == bayiId && x.Durumu == 1)
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
        public IActionResult EkleBayiYetkililer([FromBody] BayiYetkiliEkleModel model)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                Bayi existingBayi = _bayiRepository.Getir(model.BayiId);

                if (existingBayi == null)
                {
                    return Json(new { success = false, message = "Bayi bulunamadı." });
                }

                // Yetki kontrolü
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, model.BayiId))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
                }

                BayiYetkililer yeniYetkili = new BayiYetkililer
                {
                    BayiId = model.BayiId,
                    Adi = model.Adi,
                    Soyadi = model.Soyadi,
                    Gorevi = model.Gorevi,
                    Email = model.Email,
                    Cep = model.Cep,
                    DahiliNo = model.DahiliNo,
                    DepartmanId = model.DepartmanId,
                    Cinsiyet = model.Cinsiyet,
                    Kodu = model.Kodu,
                    Aktif = model.Aktif ?? 1,
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
        public IActionResult SilBayiYetkili(int id)
        {
            LoadCommonData();

            try
            {
                BayiYetkililer yetkili = _yetkiliRepo.Getir(id);
                if (yetkili == null)
                {
                    return Json(new { success = false, message = "Yetkili bulunamadı." });
                }

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, yetkili.BayiId.Value))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });
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
            string Unvan, string KullaniciAdi, string Sifre,
            string Email, string Telefon, string Adres, string Kodu,
            string Bolge, string Il, string Ilce, string Belde,
            string TCVNo, string VergiDairesi, string KepAdresi,
            string WebAdresi, string Aciklama, string AlpemixFirmaAdi,
            string AlpemixGrupAdi, string AlpemixSifre,
            int? UstBayiId, int? Aktif, IFormFile Logo, IFormFile Imza, bool Distributor = false, // YENİ PARAMETRE
            List<BayiYetkiliEkleModel> Yetkililer = null)
        {
            LoadCommonData();
            try
            {
                Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");

                // Yetki kontrolü
                if (bayi != null && UstBayiId.HasValue && UstBayiId.Value != bayi.Id)
                {
                    TempData["Error"] = "Sadece kendi altınıza bayi ekleyebilirsiniz.";
                    return RedirectToAction("Index");
                }

                int seviye = 0;
                if (UstBayiId.HasValue)
                {
                    Bayi ust = _bayiRepository.Getir(UstBayiId.Value);
                    if (ust != null) seviye = (ust.Seviye ?? 0) + 1;
                }

                Bayi yeniBayi = new Bayi
                {
                    Unvan = Unvan ?? "",
                    KullaniciAdi = KullaniciAdi ?? "",
                    Sifre = Sifre ?? "",
                    Email = Email ?? "",
                    Telefon = Telefon ?? "",
                    Adres = Adres ?? "",
                    Kodu = Kodu ?? "",
                    Bolge = Bolge ?? "",
                    Il = Il ?? "",
                    Ilce = Ilce ?? "",
                    Belde = Belde ?? "",
                    TCVNo = TCVNo ?? "",
                    VergiDairesi = VergiDairesi ?? "",
                    Distributor = Distributor, // YENİ ALAN

                    KepAdresi = KepAdresi ?? "",
                    WebAdresi = WebAdresi ?? "",
                    Aciklama = Aciklama ?? "",
                    AlpemixFirmaAdi = AlpemixFirmaAdi ?? "",
                    AlpemixGrupAdi = AlpemixGrupAdi ?? "",
                    AlpemixSifre = AlpemixSifre ?? "",
                    Durumu = 1,
                    UstBayiId = UstBayiId,
                    Seviye = seviye,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                };

                // Logo ve imza işlemleri
                if (Logo != null && Logo.Length > 0)
                {
                    string logoDosyaAdi = await DosyaKaydet(Logo, "logo");
                    yeniBayi.LogoUzanti = logoDosyaAdi;

                    using MemoryStream ms = new MemoryStream();
                    await Logo.CopyToAsync(ms);
                    yeniBayi.Logo = ms.ToArray();
                }

                if (Imza != null && Imza.Length > 0)
                {
                    string imzaDosyaAdi = await DosyaKaydet(Imza, "imza");
                    yeniBayi.ImzaUzanti = imzaDosyaAdi;

                    using MemoryStream ms = new MemoryStream();
                    await Imza.CopyToAsync(ms);
                    yeniBayi.Imza = ms.ToArray();
                }

                _bayiRepository.Ekle(yeniBayi);

                // YETKİLİLERİ EKLE
                if (Yetkililer != null && Yetkililer.Any())
                {
                    foreach (BayiYetkiliEkleModel yetkiliModel in Yetkililer)
                    {
                        BayiYetkililer yeniYetkili = new BayiYetkililer
                        {
                            BayiId = yeniBayi.Id,
                            Adi = yetkiliModel.Adi?.Trim() ?? "",
                            Soyadi = yetkiliModel.Soyadi?.Trim() ?? "",
                            Gorevi = yetkiliModel.Gorevi?.Trim() ?? "",
                            Email = yetkiliModel.Email?.Trim() ?? "",
                            Cep = yetkiliModel.Cep?.Trim() ?? "",
                            DahiliNo = yetkiliModel.DahiliNo?.Trim() ?? "",
                            DepartmanId = yetkiliModel.DepartmanId,
                            Cinsiyet = yetkiliModel.Cinsiyet?.Trim() ?? "",
                            Kodu = yetkiliModel.Kodu?.Trim() ?? "",
                            Aktif = yetkiliModel.Aktif ?? 1,
                            Durumu = 1,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _yetkiliRepo.Ekle(yeniYetkili);
                    }
                }

                TempData["Success"] = "Bayi ve yetkililer başarıyla eklendi.";
                TempData["YeniBayiId"] = yeniBayi.Id;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(
            int Id, string Unvan, string KullaniciAdi, string Sifre,
            string Email, string Telefon, string Adres, string Kodu,
            string Bolge, string Il, string Ilce, string Belde,
            string TCVNo, string VergiDairesi, string KepAdresi,
            string WebAdresi, string Aciklama, string AlpemixFirmaAdi,
            string AlpemixGrupAdi, string AlpemixSifre,
            int? UstBayiId, int? Aktif, IFormFile Logo, IFormFile Imza, bool Distributor = false, // YENİ PARAMETRE
            List<BayiYetkiliEkleModel> YeniYetkililer = null)
        {
            LoadCommonData();
            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                Bayi existing = _bayiRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Bayi bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Yetki kontrolü
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, Id))
                {
                    TempData["Error"] = "Sadece kendi altınızdaki bayileri düzenleyebilirsiniz.";
                    return RedirectToAction("Index");
                }

                // Döngü kontrolü
                if (UstBayiId.HasValue && (UstBayiId.Value == Id || IsAltBayi(Id, UstBayiId.Value)))
                {
                    TempData["Error"] = "Bir bayi kendisini veya alt bayisini üst bayi yapamaz.";
                    return RedirectToAction("Index");
                }

                bool seviyeDegisti = existing.UstBayiId != UstBayiId;

                existing.Unvan = Unvan ?? "";
                existing.KullaniciAdi = KullaniciAdi ?? "";
                if (!string.IsNullOrEmpty(Sifre)) existing.Sifre = Sifre;
                existing.Email = Email ?? "";
                existing.Telefon = Telefon ?? "";
                existing.Adres = Adres ?? "";
                existing.Kodu = Kodu ?? "";
                existing.Bolge = Bolge ?? "";
                existing.Distributor = Distributor; // YENİ ALAN

                existing.Il = Il ?? "";
                existing.Ilce = Ilce ?? "";
                existing.Belde = Belde ?? "";
                existing.TCVNo = TCVNo ?? "";
                existing.VergiDairesi = VergiDairesi ?? "";
                existing.KepAdresi = KepAdresi ?? "";
                existing.WebAdresi = WebAdresi ?? "";
                existing.Aciklama = Aciklama ?? "";
                existing.AlpemixFirmaAdi = AlpemixFirmaAdi ?? "";
                existing.AlpemixGrupAdi = AlpemixGrupAdi ?? "";
                existing.AlpemixSifre = AlpemixSifre ?? "";
                existing.GuncellenmeTarihi = DateTime.Now;

                if (seviyeDegisti)
                {
                    existing.UstBayiId = UstBayiId;
                    existing.Seviye = UstBayiId.HasValue
                        ? (_bayiRepository.Getir(UstBayiId.Value)?.Seviye ?? 0) + 1
                        : 0;
                    UpdateAltBayiSeviyeler(Id);
                }

                // Logo ve imza işlemleri
                if (Logo != null && Logo.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.LogoUzanti))
                        EskiDosyayiSil(existing.LogoUzanti);

                    string logoDosyaAdi = await DosyaKaydet(Logo, "logo");
                    existing.LogoUzanti = logoDosyaAdi;

                    using MemoryStream ms = new MemoryStream();
                    await Logo.CopyToAsync(ms);
                    existing.Logo = ms.ToArray();
                }

                if (Imza != null && Imza.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.ImzaUzanti))
                        EskiDosyayiSil(existing.ImzaUzanti);

                    string imzaDosyaAdi = await DosyaKaydet(Imza, "imza");
                    existing.ImzaUzanti = imzaDosyaAdi;

                    using MemoryStream ms = new MemoryStream();
                    await Imza.CopyToAsync(ms);
                    existing.Imza = ms.ToArray();
                }

                _bayiRepository.Guncelle(existing);

                // YENİ YETKİLİLERİ EKLE
                if (YeniYetkililer != null && YeniYetkililer.Any())
                {
                    foreach (BayiYetkiliEkleModel y in YeniYetkililer)
                    {
                        BayiYetkililer yeniYetkili = new BayiYetkililer
                        {
                            BayiId = Id,
                            Adi = y.Adi?.Trim() ?? "",
                            Soyadi = y.Soyadi?.Trim() ?? "",
                            Gorevi = y.Gorevi?.Trim() ?? "",
                            Email = y.Email?.Trim() ?? "",
                            Cep = y.Cep?.Trim() ?? "",
                            DahiliNo = y.DahiliNo?.Trim() ?? "",
                            DepartmanId = y.DepartmanId,
                            Cinsiyet = y.Cinsiyet?.Trim() ?? "",
                            Kodu = y.Kodu?.Trim() ?? "",
                            Aktif = y.Aktif ?? 1,
                            Durumu = 1,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _yetkiliRepo.Ekle(yeniYetkili);
                    }
                }

                TempData["Success"] = "Bayi ve yeni yetkililer başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                Bayi existing = _bayiRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Bayi bulunamadı.";
                    return RedirectToAction("Index");
                }

                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, Id))
                {
                    TempData["Error"] = "Sadece kendi altınızdaki bayileri silebilirsiniz.";
                    return RedirectToAction("Index");
                }

                List<Bayi>  altBayiler = _bayiRepository.GetirList(x => x.UstBayiId == Id && x.Durumu == 1);
                if (altBayiler.Any())
                {
                    TempData["Error"] = "Önce alt bayileri silmelisiniz.";
                    return RedirectToAction("Index");
                }

                // Logo ve imzaları sil
                if (!string.IsNullOrEmpty(existing.LogoUzanti))
                {
                    EskiDosyayiSil(existing.LogoUzanti);
                }
                if (!string.IsNullOrEmpty(existing.ImzaUzanti))
                {
                    EskiDosyayiSil(existing.ImzaUzanti);
                }

                existing.Durumu = 0;
                existing.GuncellenmeTarihi = DateTime.Now;
                _bayiRepository.Guncelle(existing);
                TempData["Success"] = "Bayi silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            List<string> join = new List<string> { "UstBayi" };
            try
            {
                Bayi item = _bayiRepository.Getir(x => x.Id == id && x.Durumu == 1, join);
                if (item == null)
                    return Json(new { success = false, message = "Bayi bulunamadı." });

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, id))
                    return Json(new { success = false, message = "Bu işlem için yetkiniz yok." });

                return Json(new
                {
                    success = true,
                    id = item.Id,
                    unvan = item.Unvan ?? "",
                    kullaniciAdi = item.KullaniciAdi ?? "",
                    email = item.Email ?? "",
                    telefon = item.Telefon ?? "",
                    adres = item.Adres ?? "",
                    kodu = item.Kodu ?? "",
                    bolge = item.Bolge ?? "",
                    il = item.Il ?? "",
                    ilce = item.Ilce ?? "",
                    belde = item.Belde ?? "",
                    tcvNo = item.TCVNo ?? "",
                    distributor = item.Distributor, // YENİ ALAN

                    vergiDairesi = item.VergiDairesi ?? "",
                    kepAdresi = item.KepAdresi ?? "",
                    webAdresi = item.WebAdresi ?? "",
                    aciklama = item.Aciklama ?? "",
                    alpemixFirmaAdi = item.AlpemixFirmaAdi ?? "",
                    alpemixGrupAdi = item.AlpemixGrupAdi ?? "",
                    alpemixSifre = item.AlpemixSifre ?? "",
                    ustBayiId = item.UstBayiId,
                    seviye = item.Seviye ?? 0,
                    logoUzanti = item.LogoUzanti,
                    imzaUzanti = item.ImzaUzanti
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bayi bilgileri getirilirken hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult LogoGoster(int id)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = _bayiRepository.Getir(id);
                if (bayi == null || bayi.Logo == null || bayi.Logo.Length == 0)
                    return NotFound();

                string contentType = GetContentType(bayi.LogoUzanti);
                return File(bayi.Logo, contentType);
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
                Bayi bayi = _bayiRepository.Getir(id);
                if (bayi == null || bayi.Imza == null || bayi.Imza.Length == 0)
                    return NotFound();

                string contentType = GetContentType(bayi.ImzaUzanti);
                return File(bayi.Imza, contentType);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult LogoIndir(int id)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = _bayiRepository.Getir(id);
                if (bayi == null || bayi.Logo == null || bayi.Logo.Length == 0)
                    return NotFound();

                string contentType = GetContentType(bayi.LogoUzanti);
                string fileName = $"logo_{bayi.Id}_{Path.GetFileName(bayi.LogoUzanti ?? "logo")}";
                return File(bayi.Logo, contentType, fileName);
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
                Bayi bayi = _bayiRepository.Getir(id);
                if (bayi == null || bayi.Imza == null || bayi.Imza.Length == 0)
                    return NotFound();

                string contentType = GetContentType(bayi.ImzaUzanti);
                string fileName = $"imza_{bayi.Id}_{Path.GetFileName(bayi.ImzaUzanti ?? "imza")}";
                return File(bayi.Imza, contentType, fileName);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SilLogo(int Id)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = _bayiRepository.Getir(Id);
                if (bayi != null)
                {
                    // Dosyayı sunucudan sil
                    if (!string.IsNullOrEmpty(bayi.LogoUzanti))
                    {
                        EskiDosyayiSil(bayi.LogoUzanti);
                    }

                    // Veritabanından sil
                    bayi.Logo = null;
                    bayi.LogoUzanti = null;
                    bayi.GuncellenmeTarihi = DateTime.Now;
                    _bayiRepository.Guncelle(bayi);

                    TempData["Success"] = "Logo başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Bayi bulunamadı.";
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
                Bayi bayi = _bayiRepository.Getir(Id);
                if (bayi != null)
                {
                    // Dosyayı sunucudan sil
                    if (!string.IsNullOrEmpty(bayi.ImzaUzanti))
                    {
                        EskiDosyayiSil(bayi.ImzaUzanti);
                    }

                    // Veritabanından sil
                    bayi.Imza = null;
                    bayi.ImzaUzanti = null;
                    bayi.GuncellenmeTarihi = DateTime.Now;
                    _bayiRepository.Guncelle(bayi);

                    TempData["Success"] = "İmza başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Bayi bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İmza silinirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetDosyaBilgisi(int id)
        {
            LoadCommonData();

            try
            {
                Bayi bayi = _bayiRepository.Getir(id);
                if (bayi == null)
                    return Json(new { success = false, message = "Bayi bulunamadı." });

                long logoBoyut = 0;
                long imzaBoyut = 0;

                if (bayi.Logo != null && bayi.Logo.Length > 0)
                {
                    logoBoyut = bayi.Logo.Length;
                }

                if (bayi.Imza != null && bayi.Imza.Length > 0)
                {
                    imzaBoyut = bayi.Imza.Length;
                }

                return Json(new
                {
                    success = true,
                    logoUzanti = bayi.LogoUzanti,
                    imzaUzanti = bayi.ImzaUzanti,
                    logoVar = bayi.Logo != null && bayi.Logo.Length > 0,
                    imzaVar = bayi.Imza != null && bayi.Imza.Length > 0,
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

        [HttpGet]
        public IActionResult GetSozlesmelerByBayiId(int bayiId)
        {
            LoadCommonData();

            var sozlesmeler = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.BayiId == bayiId && x.Durumu == 1)
                .Include(s => s.SozlesmeDurumu)
                .OrderByDescending(s => s.EklenmeTarihi)
                .Select(s => new
                {
                    s.Id,
                    s.DokumanNo,
                    RevizyonNo = s.RevizyonNo ?? "-",
                    s.YayinTarihi,
                    s.BitisTarihi,
                    DurumAdi = s.SozlesmeDurumu.Adi,
                    s.DosyaYolu,
                    KriterSayisi = s.BayiSozlesmeBayiKriter.Count(k => k.Durumu == 1)
                })
                .ToList();

            return Json(sozlesmeler);
        }

        [HttpPost]
        public IActionResult GuncelleBayiYetkili([FromBody] BayiYetkililerGuncelleModel model)
        {
            LoadCommonData();

            try
            {
                BayiYetkililer yetkili = _yetkiliRepo.Getir(model.Id);
                if (yetkili == null || yetkili.Durumu != 1)
                    return Json(new { success = false, message = "Yetkili bulunamadı." });

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, yetkili.BayiId.Value))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                yetkili.Adi = model.Adi;
                yetkili.Soyadi = model.Soyadi;
                yetkili.Gorevi = model.Gorevi;
                yetkili.Email = model.Email;
                yetkili.Cep = model.Cep;
                yetkili.DahiliNo = model.DahiliNo;
                yetkili.Kodu = model.Kodu;
                yetkili.Cinsiyet = model.Cinsiyet;
                yetkili.DepartmanId = model.DepartmanId;
                yetkili.Aktif = model.Aktif;
                yetkili.GuncellenmeTarihi = DateTime.Now;

                _yetkiliRepo.Guncelle(yetkili);

                return Json(new { success = true, message = "Yetkili güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleYetkiliAktif([FromBody] ToggleRequest request)
        {
            LoadCommonData();

            try
            {
                BayiYetkililer yetkili = _yetkiliRepo.Getir(request.Id);
                if (yetkili == null || yetkili.Durumu != 1)
                    return Json(new { success = false, message = "Yetkili bulunamadı." });

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, yetkili.BayiId.Value))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                yetkili.Aktif = yetkili.Aktif == 1 ? 0 : 1;
                yetkili.GuncellenmeTarihi = DateTime.Now;
                _yetkiliRepo.Guncelle(yetkili);

                return Json(new { success = true, message = "Durum güncellendi.", aktif = yetkili.Aktif });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ToggleBayiAktif([FromBody] ToggleRequest request)
        {
            LoadCommonData();
            try
            {
                Bayi bayi = _bayiRepository.Getir(request.Id);
                if (bayi == null || bayi.Durumu != 1)
                    return Json(new { success = false, message = "Bayi bulunamadı." });

                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayi.Id))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                bayi.GuncellenmeTarihi = DateTime.Now;
                _bayiRepository.Guncelle(bayi);

                return Json(new { success = true, message = "Bayi durumu güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============= DOSYA İŞLEMLERİ =============
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
            string uploadsKlasoru = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "BayiDosyalari");
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

        private void EskiDosyayiSil(string dosyaAdi)
        {
            if (string.IsNullOrEmpty(dosyaAdi))
                return;

            try
            {
                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "BayiDosyalari", dosyaAdi);

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

        private string GetContentType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

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
        [HttpGet]
        public async Task<IActionResult> GetBayiSozlesmeDokumanlari(int bayiId)
        {
            LoadCommonData();

            try
            {
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                if (currentBayi != null && !IsBayiAltinda(currentBayi.Id, bayiId))
                    return Json(new { success = false, message = "Yetkiniz yok." });

                List<Bayi> bayiVeAltlar = GetBayiVeAltBayiler(bayiId);
                List<int> bayiIdList = bayiVeAltlar.Select(b => b.Id).ToList();

                List<string> includeList = new List<string> { "Musteri", "SozlesmeDurumu", "Entegrator" };

                List<MusteriSozlesme> sozlesmeler = await _musteriSozlesmeRepo.GetirListAsync(
                    s => bayiIdList.Contains(s.Musteri.BayiId ?? 0) && s.Durumu == 1,
                    includeList
                );

                var result = sozlesmeler
                    .OrderByDescending(s => s.YayinTarihi)
                    .Select(s => new
                    {
                        s.Id,
                        s.DokumanNo,
                        s.LisansNo,
                        s.SozlesmeTipi,
                        YayinTarihi = s.YayinTarihi.ToString("dd.MM.yyyy"),
                        RevizeTarihi = s.RevizeTarihi.ToString("dd.MM.yyyy"),
                        s.RevizyonNo,
                        s.YillikBakim,
                        s.DosyaAdi,
                        VergiKimlikLevhasıDosyaAdi = s.VergiKimlikLevhasıDosyaAdi,
                        TicariSicilGazetesiDosyaAdi = s.TicariSicilGazetesiDosyaAdi,
                        KimlikOnYuzuDosyaAdi = s.KimlikOnYuzuDosyaAdi,
                        ImzaSirkusuDosyaAdi = s.ImzaSirkusuDosyaAdi,
                        s.SmsBilgilendirme,
                        s.EmailBilgilendirme,
                        s.TelefonBilgilendirme,
                        s.HaberPaylasimi,
                        MusteriAdi = s.Musteri != null ? $"{s.Musteri.Ad} {s.Musteri.Soyad ?? ""}".Trim() : "Bilinmiyor",
                        MusteriTicariUnvan = s.Musteri?.TicariUnvan ?? "",
                        SozlesmeDurumu = s.SozlesmeDurumu?.Adi ?? "Belirtilmemiş",
                        EntegratorAdi = s.Entegrator?.Adi ?? "",
                        OlusturmaTarihi = s.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                        VergiKimlikLevhasiVar = !string.IsNullOrEmpty(s.VergiKimlikLevhasıDosyaAdi),
                        TicariSicilGazetesiVar = !string.IsNullOrEmpty(s.TicariSicilGazetesiDosyaAdi),
                        KimlikOnYuzuVar = !string.IsNullOrEmpty(s.KimlikOnYuzuDosyaAdi),
                        ImzaSirkusuVar = !string.IsNullOrEmpty(s.ImzaSirkusuDosyaAdi)
                    })
                    .ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSozlesmeDokumanDetay(int id)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme dokuman = await _musteriSozlesmeRepo.GetirQueryable()
                    .Include(s => s.Musteri)
                    .Include(s => s.SozlesmeDurumu)
                    .Include(s => s.Entegrator)
                    .FirstOrDefaultAsync(s => s.Id == id && s.Durumu == 1);

                if (dokuman == null)
                    return Json(new { success = false, message = "Doküman bulunamadı" });

                var detay = new
                {
                    dokuman.Id,
                    dokuman.DokumanNo,
                    dokuman.LisansNo,
                    dokuman.SozlesmeTipi,
                    YayinTarihi =  dokuman.YayinTarihi.ToString("dd.MM.yyyy"),
                    RevizeTarihi = dokuman.RevizeTarihi.ToString("dd.MM.yyyy") ,
                    dokuman.RevizyonNo,
                    dokuman.YillikBakim,
                    dokuman.DosyaAdi,
                    dokuman.VergiKimlikLevhasıDosyaAdi,
                    dokuman.TicariSicilGazetesiDosyaAdi,
                    dokuman.KimlikOnYuzuDosyaAdi,
                    dokuman.ImzaSirkusuDosyaAdi,
                    dokuman.SmsBilgilendirme,
                    dokuman.EmailBilgilendirme,
                    dokuman.TelefonBilgilendirme,
                    dokuman.HaberPaylasimi,
                    MusteriAdi = dokuman.Musteri.Ad + " " + (dokuman.Musteri.Soyad ?? ""),
                    SozlesmeDurumu = dokuman.SozlesmeDurumu?.Adi ?? "Belirtilmemiş",
                    EntegratorAdi = dokuman.Entegrator?.Adi ?? "",
                    OlusturmaTarihi = dokuman.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm")
                };

                return Json(new { success = true, data = detay });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Rekürsif alt bayi getir
        private List<Bayi> GetBayiVeAltBayiler(int bayiId)
        {
            List<Bayi> result = new List<Bayi>();
            Bayi anaBayi = _bayiRepository.Getir(bayiId);
            if (anaBayi != null && anaBayi.Durumu == 1)
            {
                result.Add(anaBayi);
                AddAltBayiler(anaBayi.Id, result);
            }
            return result;
        }
        private List<Bayi> GetBayi(int bayiId)
        {
            List<Bayi> result = new List<Bayi>();
            Bayi anaBayi = _bayiRepository.Getir(bayiId);
            if (anaBayi != null && anaBayi.Durumu == 1)
            {
                result.Add(anaBayi);
            }
            return result;
        }
        private void AddAltBayiler(int ustBayiId, List<Bayi> resultList)
        {
            List<Bayi> altBayiler = _bayiRepository.GetirList(x => x.UstBayiId == ustBayiId && x.Durumu == 1);
            foreach (Bayi altBayi in altBayiler)
            {
                resultList.Add(altBayi);
                AddAltBayiler(altBayi.Id, resultList);
            }
        }

        // Hiyerarşi gösterimleri
        [HttpGet]
        public IActionResult BayiSemasi(int bayiId) => HiyerarsiGoster(bayiId);

        [HttpGet]
        public IActionResult TumHiyerarsi() => HiyerarsiGoster(null);

        private IActionResult HiyerarsiGoster(int? kokBayiId = null)
        {
            LoadCommonData();
            Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");

            List<Bayi> bayiList = currentBayi != null
                ? GetBayiVeAltBayiler(currentBayi.Id)
                : _bayiRepository.GetirList(x => x.Durumu == 1).ToList();

            List<BayiViewModel> viewModels = bayiList.Select(b => new BayiViewModel
            {
                Id = b.Id,
                Unvan = b.Unvan,
                KullaniciAdi = b.KullaniciAdi,
                Email = b.Email,
                Telefon = b.Telefon,
                Adres = b.Adres,
                UstBayiId = b.UstBayiId,
                Seviye = b.Seviye ?? 0,
                UstBayiAd = b.UstBayi != null ? b.UstBayi.Unvan : "Ana Bayi",
                AltBayiSayisi = _bayiRepository.GetirList(x => x.UstBayiId == b.Id && x.Durumu == 1).Count,
                MusteriSayisi = _musteriRepository.GetirList(x => x.BayiId == b.Id && x.Durum == 1).Count
            }).ToList();

            ViewBag.BayiList = viewModels;
            ViewBag.KokBayiId = kokBayiId;
            ViewBag.AnaBayiIdleri = kokBayiId == null ? bayiList.Where(x => x.UstBayiId == null).Select(x => x.Id).ToList() : null;
            ViewBag.SemaBaslik = kokBayiId.HasValue ? "Bayi Hiyerarşisi" : "Tüm Bayi Hiyerarşisi";

            return View("Index");
        }

        // Yardımcı metodlar
        private bool IsBayiAltinda(int ustId, int kontrolId)
        {
            if (ustId == kontrolId) return true;
            List<Bayi> altlar = _bayiRepository.GetirList(x => x.UstBayiId == ustId && x.Durumu == 1);
            return altlar.Any(a => a.Id == kontrolId || IsBayiAltinda(a.Id, kontrolId));
        }

        private bool IsAltBayi(int ustId, int kontrolId)
        {
            List<Bayi> altlar = _bayiRepository.GetirList(x => x.UstBayiId == ustId && x.Durumu == 1);
            return altlar.Any(a => a.Id == kontrolId || IsAltBayi(a.Id, kontrolId));
        }

        private void UpdateAltBayiSeviyeler(int bayiId)
        {
            Bayi bayi = _bayiRepository.Getir(bayiId);
            if (bayi == null) return;

            List<Bayi> altlar = _bayiRepository.GetirList(x => x.UstBayiId == bayiId && x.Durumu == 1);
            foreach (Bayi alt in altlar)
            {
                alt.Seviye = (bayi.Seviye ?? 0) + 1;
                alt.GuncellenmeTarihi = DateTime.Now;
                _bayiRepository.Guncelle(alt);
                UpdateAltBayiSeviyeler(alt.Id);
            }
        }
        [HttpGet]
        public IActionResult GoruntuleVergiKimlikDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.VergiKimlikLevhasıDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.VergiKimlikLevhasıDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.VergiKimlikLevhasıDosyaAdi);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult IndirVergiKimlikDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.VergiKimlikLevhasıDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.VergiKimlikLevhasıDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.VergiKimlikLevhasıDosyaAdi);
                string dosyaAdi = $"Vergi_Kimlik_Levhasi_{dokumanNo}{Path.GetExtension(sozlesme.VergiKimlikLevhasıDosyaAdi)}";

                return File(bytes, contentType, dosyaAdi);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GoruntuleTicariSicilDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.TicariSicilGazetesiDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.TicariSicilGazetesiDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.TicariSicilGazetesiDosyaAdi);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult IndirTicariSicilDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.TicariSicilGazetesiDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.TicariSicilGazetesiDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.TicariSicilGazetesiDosyaAdi);
                string dosyaAdi = $"Ticari_Sicil_Gazetesi_{dokumanNo}{Path.GetExtension(sozlesme.TicariSicilGazetesiDosyaAdi)}";

                return File(bytes, contentType, dosyaAdi);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GoruntuleKimlikDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.KimlikOnYuzuDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.KimlikOnYuzuDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.KimlikOnYuzuDosyaAdi);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult IndirKimlikDosya(string dokumanNo)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.KimlikOnYuzuDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.KimlikOnYuzuDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.KimlikOnYuzuDosyaAdi);
                string dosyaAdi = $"Kimlik_On_Yuzu_{dokumanNo}{Path.GetExtension(sozlesme.KimlikOnYuzuDosyaAdi)}";

                return File(bytes, contentType, dosyaAdi);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GoruntuleImzaSirkusuDosya(string dokumanNo)
        {
            LoadCommonData();

            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.ImzaSirkusuDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.ImzaSirkusuDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.ImzaSirkusuDosyaAdi);

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult IndirImzaSirkusuDosya(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.ImzaSirkusuDosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.ImzaSirkusuDosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string contentType = GetContentType(sozlesme.ImzaSirkusuDosyaAdi);
                string dosyaAdi = $"Imza_Sirkusu_{dokumanNo}{Path.GetExtension(sozlesme.ImzaSirkusuDosyaAdi)}";

                return File(bytes, contentType, dosyaAdi);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GoruntuleSozlesmePdf(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.DosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.DosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);

                return File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult IndirSozlesmePdf(string dokumanNo)
        {
            try
            {
                MusteriSozlesme sozlesme = _musteriSozlesmeRepo.GetirQueryable()
                    .FirstOrDefault(s => s.DokumanNo == dokumanNo && s.Durumu == 1);

                if (sozlesme == null || string.IsNullOrEmpty(sozlesme.DosyaAdi))
                    return NotFound("Dosya bulunamadı.");

                string dosyaYolu = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "MusteriSozlesme", sozlesme.DosyaAdi);

                if (!System.IO.File.Exists(dosyaYolu))
                    return NotFound("Dosya fiziksel olarak bulunamadı.");

                var bytes = System.IO.File.ReadAllBytes(dosyaYolu);
                string dosyaAdi = $"Sozlesme_{dokumanNo}{Path.GetExtension(sozlesme.DosyaAdi)}";

                return File(bytes, "application/pdf", dosyaAdi);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }
    }

    // MODELS
    public class ToggleRequest
    {
        public int Id { get; set; }
    }

    public class BayiYetkililerGuncelleModel
    {
        public int Id { get; set; }
        public string Adi { get; set; }
        public string Soyadi { get; set; }
        public string Gorevi { get; set; }
        public string Email { get; set; }
        public string Cep { get; set; }
        public string DahiliNo { get; set; }
        public string Kodu { get; set; }
        public string Cinsiyet { get; set; }
        public int? DepartmanId { get; set; }
        public int? Aktif { get; set; }
    }

    public class BayiViewModel
    {
        public int Id { get; set; }
        public string Unvan { get; set; } = string.Empty;
        public bool Distributor { get; set; }

        public string Kodu { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public int? UstBayiId { get; set; }
        public int Seviye { get; set; }
        public int? Aktif { get; set; }
        public string UstBayiAd { get; set; } = string.Empty;
        public int AltBayiSayisi { get; set; }
        public int MusteriSayisi { get; set; }
    }

    public class BayiYetkiliEkleModel
    {
        public int BayiId { get; set; }
        public string Adi { get; set; } = string.Empty;
        public string Soyadi { get; set; } = string.Empty;
        public string Gorevi { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string DahiliNo { get; set; } = string.Empty;
        public int? DepartmanId { get; set; }
        public string Cinsiyet { get; set; } = string.Empty;
        public string Kodu { get; set; } = string.Empty;
        public int? Aktif { get; set; }

        public BayiYetkiliEkleModel() { }
    }

}