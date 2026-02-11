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
        private readonly IWebHostEnvironment _env;

        public AdminBayiSozlesmeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<BayiSozlesme> sozlesmeler = _sozlesmeRepo.GetirQueryable()
                .Where(x => x.Durumu == 1)
                .Include(s => s.Bayi)
                .Include(s => s.SozlesmeDurumu)
                .Include(s => s.BayiSozlesmeBayiKriter)
                    .ThenInclude(k => k.BayiSozlesmeKriteri)
                .OrderByDescending(s => s.EklenmeTarihi)
                .ToList();

            ViewBag.Sozlesmeler = sozlesmeler;
            return View();
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

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
                yayinTarihi = sozlesme.YayinTarihi.ToString("yyyy-MM-dd"),
                revizeTarihi = sozlesme.RevizeTarihi.ToString("yyyy-MM-dd") ?? "",
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
            LoadCommonData();

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
            LoadCommonData();

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
            LoadCommonData();

            return await KaydetAsync(Id, BayiId, DokumanNo, YayinTarihi, RevizeTarihi, RevizyonNo,
                SozlesmeDurumuId, GecerlilikSuresi, BitisTarihi, KriterIds ?? new List<int>(), Dosya, "güncellendi");
        }
        private async Task<IActionResult> KaydetAsync(int? Id, int BayiId, string DokumanNo, DateTime YayinTarihi,
             DateTime? RevizeTarihi, string RevizyonNo, int SozlesmeDurumuId, decimal GecerlilikSuresi,
             DateTime BitisTarihi, List<int> KriterIds, IFormFile? Dosya, string islem)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
                return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });

            if (string.IsNullOrWhiteSpace(DokumanNo))
                return Json(new { success = false, message = "Döküman No zorunludur." });

            try
            {
                bool benzersizKontrol = await BenzersizKontrolAsync(DokumanNo, RevizyonNo, Id);
                if (benzersizKontrol)
                    return Json(new { success = false, message = "Bu döküman no ve revizyon kombinasyonu zaten mevcut." });

                BayiSozlesme sozlesme;
                string? yeniDosyaYolu = null;

                if (Id.HasValue && Id > 0)
                {
                    sozlesme = _sozlesmeRepo.Getir(Id.Value);
                    if (sozlesme == null || sozlesme.Durumu == 0)
                        return Json(new { success = false, message = "Sözleşme bulunamadı." });
                    yeniDosyaYolu = sozlesme.DosyaYolu;
                }
                else
                {
                    sozlesme = new BayiSozlesme
                    {
                        EkleyenKullaniciId = kullanici.Id,
                        EklenmeTarihi = DateTime.Now,
                        Durumu = 1
                    };
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
                sozlesme.GuncelleyenKullaniciId = kullanici.Id;
                sozlesme.GuncellenmeTarihi = DateTime.Now;

                if (Id.HasValue && Id > 0)
                    _sozlesmeRepo.Guncelle(sozlesme);
                else
                    _sozlesmeRepo.Ekle(sozlesme);

                // KRİTERLERİ KAYDET (Artık KriterIds garanti geliyor)
                await KriterleriGuncelleAsync(sozlesme.Id, KriterIds, kullanici.Id);

                return Json(new { success = true, message = $"Bayi sözleşmesi başarıyla {islem}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem sırasında hata: {ex.Message}" });
            }
        }

        private async Task<bool> BenzersizKontrolAsync(string dokumanNo, string revizyonNo, int? excludeId = null)
        {
            LoadCommonData();

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
            LoadCommonData();

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
            LoadCommonData();

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
            LoadCommonData();

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
            LoadCommonData();

            var bayiler = _bayiRepo.GetirList(x => x.Durumu == 1)
                .Select(b => new { value = b.Id, text = $"{b.Kodu} - {b.Unvan}" })
                .OrderBy(x => x.text)
                .ToList();
            return Json(bayiler);
        }

        [HttpGet]
        public IActionResult GetSozlesmeDurumlari()
        {
            LoadCommonData();

            var durumlar = _durumRepo.GetirList(x => x.Durumu == 1)
                .Select(d => new { value = d.Id, text = d.Adi })
                .OrderBy(x => x.text)
                .ToList();
            return Json(durumlar);
        }

        [HttpGet]
        public IActionResult GetKriterTanimlari()
        {
            LoadCommonData();

            var kriterler = _kriterTanimRepo.GetirList(x => x.Durumu == 1)
                .Select(k => new { value = k.Id, text = $"{k.Adi} (%{k.Oran.ToString("0.##")})" })
                .OrderBy(x => x.text)
                .ToList();
            return Json(kriterler);
        }
    }
}