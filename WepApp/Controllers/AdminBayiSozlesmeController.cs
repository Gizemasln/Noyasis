using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;
using System.IO;
using System.Linq;

namespace WepApp.Controllers
{
    public class AdminBayiSozlesmeController : AdminBaseController
    {
        private readonly BayiSozlesmeRepository _sozlesmeRepo = new();
        private readonly BayiSozlesmeBayiKriterRepository _kriterRepo = new();
        private readonly BayiRepository _bayiRepo = new();
        private readonly SozlesmeDurumuRepository _durumRepo = new();
        private readonly BayiSozlesmeKriteriRepository _kriterTanimRepo = new();
        private readonly MusteriRepository _musteriRepo = new();
        private readonly IWebHostEnvironment _env;

        public AdminBayiSozlesmeController(IWebHostEnvironment env)
        {
            _env = env;
        }
        public IActionResult Index()
        {
        

            try
            {
                // Session'dan kullanıcı bilgilerini al
                Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
                Musteri currentMusteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
                Kullanicilar currentKullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

                List<BayiSozlesme> sozlesmeler = new List<BayiSozlesme>();
                var query = _sozlesmeRepo.GetirQueryable()
                    .Where(x => x.Durumu == 1)
                    .Include(s => s.Bayi)
                    .Include(s => s.SozlesmeDurumu)
                    .Include(s => s.BayiSozlesmeBayiKriter)
                        .ThenInclude(k => k.BayiSozlesmeKriteri);

                // Kullanıcı tipine göre sözleşmeleri filtrele
                if (currentBayi != null)
                {
                    // Bayi girişi: Kendi sözleşmeleri + alt bayilerin sözleşmeleri
                    var altBayiIds = _bayiRepo.GetBayiVeAltBayiler(currentBayi.Id)
                        .Select(b => b.Id)
                        .ToList();

                    sozlesmeler = query
                        .Where(s => altBayiIds.Contains(s.BayiId ??0))
                        .OrderByDescending(s => s.EklenmeTarihi)
                        .ToList();
                }
                else if (currentMusteri != null)
                {
                    // Müşteri girişi: Bağlı olduğu bayinin sözleşmeleri
                    // Müşterinin bağlı olduğu bayiyi bul
                    var musteri = _musteriRepo.Getir(currentMusteri.Id);

                    if (musteri?.BayiId != null)
                    {
                        sozlesmeler = query
                            .Where(s => s.BayiId == musteri.BayiId)
                            .OrderByDescending(s => s.EklenmeTarihi)
                            .ToList();
                    }
                }
                else if (currentKullanici != null)
                {
                    // Admin/Çalışan girişi: Tüm sözleşmeler
                    sozlesmeler = query
                        .OrderByDescending(s => s.EklenmeTarihi)
                        .ToList();
                }

                // ViewBag'e kullanıcı bilgilerini de ekle
                ViewBag.Sozlesmeler = sozlesmeler;
                ViewBag.CurrentBayi = currentBayi;
                ViewBag.CurrentMusteri = currentMusteri;
                ViewBag.CurrentKullanici = currentKullanici;

                // Ek bilgiler (opsiyonel)
                ViewBag.ToplamSozlesme = sozlesmeler.Count;
                ViewBag.AktifSozlesme = sozlesmeler.Count(s => s.SozlesmeDurumu?.Durumu == 1);

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Bayi sözleşmeleri yüklenirken hata: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
        

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null) return Unauthorized();

            BayiSozlesme sozlesme = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Id == id && x.Durumu == 1)
                .Include(s => s.BayiSozlesmeBayiKriter)
                    .ThenInclude(k => k.BayiSozlesmeKriteri)
                .FirstOrDefault();

            if (sozlesme == null) return NotFound();

            var kriterler = sozlesme.BayiSozlesmeBayiKriter
                .Where(k => k.Durumu == 1)
                .Select(k => new
                {
                    id = k.BayiSozlesmeKriteri.Id,
                    adi = k.BayiSozlesmeKriteri.Adi,
                    oran = k.BayiSozlesmeKriteri.Oran
                }).ToList();

            List<int> kriterIds = sozlesme.BayiSozlesmeBayiKriter
                .Where(k => k.Durumu == 1)
                .Select(k => k.BayiSozlesmeKriteriId)
                .ToList();

            return Json(new
            {
                id = sozlesme.Id,
                bayiId = sozlesme.BayiId,
                dokumanNo = sozlesme.DokumanNo,
                yayinTarihi = sozlesme.YayinTarihi.ToString(),
                revizeTarihi = sozlesme.RevizeTarihi.ToString() ?? "",
                revizyonNo = sozlesme.RevizyonNo ?? "",
                sozlesmeDurumuId = sozlesme.SozlesmeDurumuId,
                gecerlilikSuresi = sozlesme.GecerlilikSuresi,
                bitisTarihi = sozlesme.BitisTarihi.ToString("yyyy-MM-dd"),
                dosyaYolu = sozlesme.DosyaYolu ?? "",
                kriterIds,
                kriterList = kriterler
            });
        }
        [HttpGet]
        public IActionResult BenzersizKontrol(string dokumanNo, string revizyonNo, int? excludeId = null)
        {
        

            IQueryable<BayiSozlesme> query = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Durumu == 1 && x.DokumanNo == dokumanNo.Trim());

            if (!string.IsNullOrEmpty(revizyonNo))
                query = query.Where(x => x.RevizyonNo == revizyonNo.Trim());
            else
                query = query.Where(x => x.RevizyonNo == null || x.RevizyonNo == "");

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            bool varMi = query.Any();
            return Json(new { varMi });
        }
        [HttpPost]
        public async Task<IActionResult> Ekle(
                    int BayiId, string DokumanNo, DateTime YayinTarihi,
                    DateTime? RevizeTarihi, string RevizyonNo, int SozlesmeDurumuId,
                    decimal GecerlilikSuresi, DateTime BitisTarihi,
                    [FromForm] List<int> KriterIds, IFormFile? Dosya)
        {
        

            return await KaydetAsync(null, BayiId, DokumanNo, YayinTarihi, RevizeTarihi, RevizyonNo,
                SozlesmeDurumuId, GecerlilikSuresi, BitisTarihi, KriterIds ?? new List<int>(), Dosya, "eklendi");
        }
        [HttpPost]
        public async Task<IActionResult> Guncelle(
                    int Id, int BayiId, string DokumanNo, DateTime YayinTarihi,
                    DateTime? RevizeTarihi, string RevizyonNo, int SozlesmeDurumuId,
                    decimal GecerlilikSuresi, DateTime BitisTarihi,
                    [FromForm] List<int> KriterIds, IFormFile? Dosya)
        {
        

            return await KaydetAsync(Id, BayiId, DokumanNo, YayinTarihi, RevizeTarihi, RevizyonNo,
                SozlesmeDurumuId, GecerlilikSuresi, BitisTarihi, KriterIds ?? new List<int>(), Dosya, "güncellendi");
        }
        private async Task<IActionResult> KaydetAsync(int? Id, int BayiId, string DokumanNo, DateTime YayinTarihi,
                     DateTime? RevizeTarihi, string RevizyonNo, int SozlesmeDurumuId, decimal GecerlilikSuresi,
                     DateTime BitisTarihi, List<int> KriterIds, IFormFile? Dosya, string islem)
        {
        

            // Session'dan tüm kullanıcı tiplerini al
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Bayi bayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Musteri musteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");

            // Kullanıcı kontrolü - hiçbiri yoksa yetkisiz
            if (kullanici == null && bayi == null && musteri == null)
            {
                return Json(new { success = false, message = "Bu işlem için oturum açmanız gerekiyor." });
            }

            // Yetki kontrolü - bayi girişi yapmışsa, sadece kendi bayisine veya alt bayilerine ekleme yapabilir
            if (bayi != null)
            {
                // Bayinin kendisi veya alt bayilerinden biri mi kontrol et
                var yetkiliBayiIds = _bayiRepo.GetBayiVeAltBayiler(bayi.Id)
                    .Select(b => b.Id)
                    .ToList();

                if (!yetkiliBayiIds.Contains(BayiId))
                {
                    return Json(new { success = false, message = "Bu bayi için işlem yapma yetkiniz bulunmamaktadır." });
                }
            }
            // Müşteri girişi yapmışsa, sözleşme ekleyemez (sadece görüntüleyebilir)
            else if (musteri != null)
            {
                return Json(new { success = false, message = "Müşteriler sözleşme ekleyemez." });
            }
            // Admin girişi - herhangi bir kısıtlama yok

            if (string.IsNullOrWhiteSpace(DokumanNo))
                return Json(new { success = false, message = "Döküman No zorunludur." });

            try
            {
                bool benzersizKontrol = await BenzersizKontrolAsync(DokumanNo, RevizyonNo, Id);
                if (benzersizKontrol)
                    return Json(new { success = false, message = "Bu döküman no ve revizyon kombinasyonu zaten mevcut." });

                BayiSozlesme sozlesme;
                string? yeniDosyaYolu = null;

                // Hangi kullanıcı tipinin ID'sini kullanacağımızı belirle
                int? ekleyenKullaniciId = null;
                int? ekleyenBayiId = null;
                int? ekleyenMusteriId = null;

                if (kullanici != null)
                {
                    ekleyenKullaniciId = kullanici.Id;
                }
                else if (bayi != null)
                {
                    ekleyenBayiId = bayi.Id;
                }
                else if (musteri != null)
                {
                    ekleyenMusteriId = musteri.Id;
                }

                if (Id.HasValue && Id > 0)
                {
                    // GÜNCELLEME
                    sozlesme = _sozlesmeRepo.Getir(Id.Value);
                    if (sozlesme == null || sozlesme.Durumu == 0)
                        return Json(new { success = false, message = "Sözleşme bulunamadı." });

                    // Güncelleme yetkisi kontrolü
                    if (bayi != null)
                    {
                        // Bayi sadece kendi eklediği veya kendi bayisine ait sözleşmeleri güncelleyebilir
                        var yetkiliBayiIds = _bayiRepo.GetBayiVeAltBayiler(bayi.Id)
                            .Select(b => b.Id)
                            .ToList();

                        if (!yetkiliBayiIds.Contains(sozlesme.BayiId ?? 0))
                        {
                            return Json(new { success = false, message = "Bu sözleşmeyi güncelleme yetkiniz bulunmamaktadır." });
                        }
                    }

                    yeniDosyaYolu = sozlesme.DosyaYolu;
                }
                else
                {
                    // YENİ KAYIT
                    sozlesme = new BayiSozlesme
                    {
                        EklenmeTarihi = DateTime.Now,
                        Durumu = 1
                    };

                    // Ekleyen bilgilerini set et
                    if (ekleyenKullaniciId.HasValue)
                    {
                        sozlesme.EkleyenKullaniciId = ekleyenKullaniciId.Value;
                    }
                    else if (ekleyenBayiId.HasValue)
                    {
                        sozlesme.EkleyenBayiId = ekleyenBayiId.Value;
                    }
                    else if (ekleyenMusteriId.HasValue)
                    {
                        sozlesme.EkleyenMusteriId = ekleyenMusteriId.Value;
                    }
                }

                // Dosya yükleme
                if (Dosya != null && Dosya.Length > 0)
                {
                    if (!Dosya.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Sadece PDF dosyası yükleyebilirsiniz." });

                    if (Dosya.Length > 20 * 1024 * 1024)
                        return Json(new { success = false, message = "Dosya boyutu 20 MB'dan büyük olamaz." });

                    string dosyaAdi = Guid.NewGuid() + ".pdf";
                    string klasorYolu = Path.Combine(_env.WebRootPath, "WebAdminTheme", "BayiSozlesme");
                    Directory.CreateDirectory(klasorYolu);
                    string tamYol = Path.Combine(klasorYolu, dosyaAdi);

                    using (FileStream stream = new FileStream(tamYol, FileMode.Create))
                        await Dosya.CopyToAsync(stream);

                    // Eski dosyayı sil
                    if (!string.IsNullOrEmpty(yeniDosyaYolu))
                    {
                        string eskiYol = Path.Combine(_env.WebRootPath, yeniDosyaYolu.TrimStart('/').Replace("~", ""));
                        if (System.IO.File.Exists(eskiYol))
                            System.IO.File.Delete(eskiYol);
                    }

                    yeniDosyaYolu = "/WebAdminTheme/BayiSozlesme/" + dosyaAdi;
                }

                // Ana veriler
                sozlesme.BayiId = BayiId;
                sozlesme.DokumanNo = DokumanNo.Trim();
                sozlesme.YayinTarihi = YayinTarihi;
                sozlesme.RevizeTarihi = RevizeTarihi ?? DateTime.Now;
                sozlesme.RevizyonNo = string.IsNullOrWhiteSpace(RevizyonNo) ? null : RevizyonNo.Trim();
                sozlesme.SozlesmeDurumuId = SozlesmeDurumuId;
                sozlesme.GecerlilikSuresi = GecerlilikSuresi;
                sozlesme.BitisTarihi = BitisTarihi;
                sozlesme.DosyaYolu = yeniDosyaYolu;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                // Güncelleyen bilgilerini set et
                if (ekleyenKullaniciId.HasValue)
                {
                    sozlesme.GuncelleyenKullaniciId = ekleyenKullaniciId.Value;
                }
                else if (ekleyenBayiId.HasValue)
                {
                    sozlesme.GuncelleyenBayiId = ekleyenBayiId.Value;
                }
                else if (ekleyenMusteriId.HasValue)
                {
                    sozlesme.GuncelleyenMusteriId = ekleyenMusteriId.Value;
                }

                if (Id.HasValue && Id > 0)
                {
                    _sozlesmeRepo.Guncelle(sozlesme);
                }
                else
                {
                    _sozlesmeRepo.Ekle(sozlesme);
                }

                // KRİTERLERİ KAYDET
                // Kriterleri güncellerken de hangi kullanıcının yaptığını belirt
                int kriterGuncelleyenId = ekleyenKullaniciId ?? ekleyenBayiId ?? ekleyenMusteriId ?? 0;
                await KriterleriGuncelleAsync(sozlesme.Id, KriterIds, kriterGuncelleyenId,
                    ekleyenKullaniciId.HasValue ? "kullanici" :
                    (ekleyenBayiId.HasValue ? "bayi" : "musteri"));

                string kullaniciTipi = kullanici != null ? "admin" : (bayi != null ? "bayi" : "müşteri");
                return Json(new { success = true, message = $"Bayi sözleşmesi başarıyla {islem}. (İşlem yapan: {kullaniciTipi})" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem sırasında hata: {ex.Message}" });
            }
        }

        // Kriterleri güncelleme metodunu da güncelle
        private async Task KriterleriGuncelleAsync(int sozlesmeId, List<int> kriterIds, int guncelleyenId, string kullaniciTipi = "kullanici")
        {
        

            // Eski kriterleri pasif yap
            List<BayiSozlesmeBayiKriter> eskiKriterler = _kriterRepo.GetirList(x => x.BayiSozlesmeId == sozlesmeId && x.Durumu == 1).ToList();
            foreach (BayiSozlesmeBayiKriter eski in eskiKriterler)
            {
                eski.Durumu = 0;
                eski.GuncellenmeTarihi = DateTime.Now;

                // Kullanıcı tipine göre güncelleyen ID'sini set et
                if (kullaniciTipi == "kullanici")
                {
                    eski.GuncelleyenKullaniciId = guncelleyenId;
                }
                else if (kullaniciTipi == "bayi")
                {
                    eski.GuncelleyenBayiId = guncelleyenId;
                }
                else if (kullaniciTipi == "musteri")
                {
                    eski.GuncelleyenMusteriId = guncelleyenId;
                }

                _kriterRepo.Guncelle(eski);
            }

            // Yeni kriterleri ekle
            if (kriterIds != null && kriterIds.Any())
            {
                foreach (int kriterId in kriterIds.Distinct())
                {
                    BayiSozlesmeBayiKriter yeni = new BayiSozlesmeBayiKriter
                    {
                        BayiSozlesmeId = sozlesmeId,
                        BayiSozlesmeKriteriId = kriterId,
                        Durumu = 1,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now
                    };

                    // Ekleyen ve güncelleyen bilgilerini kullanıcı tipine göre set et
                    if (kullaniciTipi == "kullanici")
                    {
                        yeni.EkleyenKullaniciId = guncelleyenId;
                        yeni.GuncelleyenKullaniciId = guncelleyenId;
                    }
                    else if (kullaniciTipi == "bayi")
                    {
                        yeni.EkleyenBayiId = guncelleyenId;
                        yeni.GuncelleyenBayiId = guncelleyenId;
                    }
                    else if (kullaniciTipi == "musteri")
                    {
                        yeni.EkleyenMusteriId = guncelleyenId;
                        yeni.GuncelleyenMusteriId = guncelleyenId;
                    }

                    _kriterRepo.Ekle(yeni);
                }
            }

            await Task.CompletedTask;
        }

        private async Task<bool> BenzersizKontrolAsync(string dokumanNo, string revizyonNo, int? excludeId = null)
        {
        

            IQueryable<BayiSozlesme> query = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Durumu == 1 && x.DokumanNo == dokumanNo.Trim());

            if (!string.IsNullOrEmpty(revizyonNo))
                query = query.Where(x => x.RevizyonNo == revizyonNo.Trim());
            else
                query = query.Where(x => x.RevizyonNo == null || x.RevizyonNo == "");

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        private async Task KriterleriGuncelleAsync(int sozlesmeId, List<int> kriterIds, int kullaniciId)
        {
        

            // Eski kriterleri pasif yap
            List<BayiSozlesmeBayiKriter> eskiKriterler = _kriterRepo.GetirList(x => x.BayiSozlesmeId == sozlesmeId && x.Durumu == 1).ToList();
            foreach (BayiSozlesmeBayiKriter eski in eskiKriterler)
            {
                eski.Durumu = 0;
                eski.GuncelleyenKullaniciId = kullaniciId;
                eski.GuncellenmeTarihi = DateTime.Now;
                _kriterRepo.Guncelle(eski);
            }

            // Yeni kriterleri ekle
            if (kriterIds != null && kriterIds.Any())
            {
                foreach (int kriterId in kriterIds.Distinct())
                {
                    BayiSozlesmeBayiKriter yeni = new BayiSozlesmeBayiKriter
                    {
                        BayiSozlesmeId = sozlesmeId,
                        BayiSozlesmeKriteriId = kriterId,
                        Durumu = 1,
                        EkleyenKullaniciId = kullaniciId,
                        GuncelleyenKullaniciId = kullaniciId,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now
                    };
                    _kriterRepo.Ekle(yeni);
                }
            }
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
        

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });

                BayiSozlesme sozlesme = _sozlesmeRepo.Getir(Id);
                if (sozlesme == null || sozlesme.Durumu == 0)
                    return Json(new { success = false, message = "Sözleşme bulunamadı." });

                sozlesme.Durumu = 0;
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;
                _sozlesmeRepo.Guncelle(sozlesme);

                // İlişkili kriterleri de pasifleştir
                List<BayiSozlesmeBayiKriter> kriterler = _kriterRepo.GetirList(x => x.BayiSozlesmeId == Id && x.Durumu == 1).ToList();
                foreach (BayiSozlesmeBayiKriter kriter in kriterler)
                {
                    kriter.Durumu = 0;
                    kriter.GuncelleyenKullaniciId = kullanici.Id;
                    kriter.GuncellenmeTarihi = DateTime.Now;
                    _kriterRepo.Guncelle(kriter);
                }

                // Dosyayı fiziksel olarak sil
                if (!string.IsNullOrEmpty(sozlesme.DosyaYolu))
                {
                    string dosyaYolu = Path.Combine(_env.WebRootPath, sozlesme.DosyaYolu.TrimStart('/'));
                    if (System.IO.File.Exists(dosyaYolu))
                        System.IO.File.Delete(dosyaYolu);
                }

                return Json(new { success = true, message = "Sözleşme başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Silme sırasında hata: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult BenzersizKontrol(string dokumanNo, int? excludeId = null)
        {
        

            if (string.IsNullOrWhiteSpace(dokumanNo))
                return Json(new { varMi = false });

            IQueryable<BayiSozlesme> query = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Durumu == 1 && x.DokumanNo.Trim() == dokumanNo.Trim());

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            bool varMi = query.Any();
            return Json(new { varMi });
        }

        private async Task<bool> BenzersizKontrolAsync(string dokumanNo, int? excludeId = null)
        {
            IQueryable<BayiSozlesme> query = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Durumu == 1 && x.DokumanNo.Trim() == dokumanNo.Trim());

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }
        [HttpGet]
        public IActionResult GetBayiler()
        {
        

            // Session'dan kullanıcı bilgilerini al
            Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            Musteri currentMusteri = SessionHelper.GetObjectFromJson<Musteri>(HttpContext.Session, "Musteri");
            Kullanicilar currentKullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            List<Bayi> bayiler = new List<Bayi>();

            // Kullanıcı tipine göre bayi listesini filtrele
            if (currentBayi != null)
            {
                // Bayi girişi: SADECE alt bayileri (kendisi hariç)
                var tumBayiler = _bayiRepo.GetBayiVeAltBayiler(currentBayi.Id) ?? new List<Bayi>();
                // Kendisi hariç sadece alt bayileri al
                bayiler = tumBayiler.Where(b => b.Id != currentBayi.Id).ToList();
            }
            else if (currentMusteri != null)
            {
                // Müşteri girişi: Sadece bağlı olduğu bayi
                var musteri = _musteriRepo.Getir(currentMusteri.Id);
                if (musteri?.BayiId != null)
                {
                    var bayi = _bayiRepo.Getir(musteri.BayiId.Value);
                    if (bayi != null)
                        bayiler.Add(bayi);
                }
            }
            else if (currentKullanici != null)
            {
                // Admin girişi: Tüm bayiler
                bayiler = _bayiRepo.GetirList(x => x.Durumu == 1).ToList();
            }

            // Bayi listesini formatla - bayiTipi olmadan
            var bayiListesi = bayiler
                .Where(b => b.Durumu == 1)
                .OrderBy(b => b.Unvan)
                .Select(b => new
                {
                    value = b.Id,
                    text = $"{b.Kodu} - {b.Unvan}"
                    // bayiTipi kaldırıldı
                })
                .ToList();

            return Json(bayiListesi);
        }
        [HttpGet]
        public IActionResult GetSozlesmeDurumlari()
        {
        

            var durumlar = _durumRepo.GetirList(x => x.Durumu == 1)
                .Select(d => new { value = d.Id, text = d.Adi })
                .OrderBy(x => x.text)
                .ToList();
            return Json(durumlar);
        }

        [HttpGet]
        public IActionResult GetKriterTanimlari()
        {
        

            var kriterler = _kriterTanimRepo.GetirList(x => x.Durumu == 1)
                .Select(k => new { value = k.Id, text = $"{k.Adi} (%{k.Oran.ToString("0.##")})" })
                .OrderBy(x => x.text)
                .ToList();
            return Json(kriterler);
        }
    }
}