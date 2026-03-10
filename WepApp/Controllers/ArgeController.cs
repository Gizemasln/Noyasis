using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebApp.Repositories;
using Microsoft.AspNetCore.Http;

namespace WepApp.Controllers
{
    public class ArgeController : AdminBaseController
    {
        private readonly ArgeHataRepository _repository = new ArgeHataRepository();
        private readonly GenericRepository<LisansTip> _lisansTipRepository = new GenericRepository<LisansTip>();
        private readonly GenericRepository<Musteri> _musteriRepository = new GenericRepository<Musteri>();
        private readonly GenericRepository<Bayi> _bayiRepository = new GenericRepository<Bayi>();
        private readonly MusteriSozlesmeRepository _sozlesmeRepository = new MusteriSozlesmeRepository();
        private const int PageSize = 10;

        public IActionResult Index(int page = 1, string tip = null, int? durum = 1, string filtre = "tumu")
        {
   

            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;
            ViewBag.SeciliFiltre = filtre;

            // Bayi müşterilerini ve bayi bilgisini yükle
            List<Musteri> bayiMusterileri = null;
            Bayi bayiBilgi = null;
            if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                bayiMusterileri = _musteriRepository
                    .GetirList(x => x.BayiId == kullaniciId.Value && x.Durum == 1)
                    .OrderBy(x => x.AdSoyad)
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

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                query = query.Where(x => x.MusteriId == kullaniciId.Value);
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                if (filtre == "bayi")
                {
                    query = query.Where(x => x.BayiId == kullaniciId.Value);
                }
                else if (filtre == "musteri")
                {
                    query = query.Where(x => x.Musteri != null && x.Musteri.BayiId == kullaniciId.Value);
                }
                else // "tumu"
                {
                    query = query.Where(x => x.BayiId == kullaniciId.Value ||
                                            (x.Musteri != null && x.Musteri.BayiId == kullaniciId.Value));
                }
            }

            if (!string.IsNullOrEmpty(tip))
            {
                query = query.Where(x => x.Tipi == tip);
            }
            if (durum.HasValue)
            {
                query = query.Where(x => x.Durumu == durum.Value);
            }

            int toplam = query.Count();
            List<ArgeHata> liste = query
                .OrderByDescending(x => x.EklenmeTarihi)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Bayi için ayrı listeler (filtre = "tumu" ise)
            List<ArgeHata> bayiListe = new List<ArgeHata>();
            List<ArgeHata> musteriListe = new List<ArgeHata>();

            if (kullaniciTipi == "Bayi" && kullaniciId.HasValue && filtre == "tumu")
            {
                bayiListe = _repository.GetirQueryable()
                    .Include(x => x.LisansTip)
                    .Include(x => x.Musteri)
                    .Include(x => x.Bayi)
                    .Include(x => x.ARGEDurum)
                    .Where(x => x.BayiId == kullaniciId.Value && x.Durumu == 1)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .ToList();

                musteriListe = _repository.GetirQueryable()
                    .Include(x => x.LisansTip)
                    .Include(x => x.Musteri)
                    .Include(x => x.Bayi)
                    .Include(x => x.ARGEDurum)
                    .Where(x => x.Musteri != null && x.Musteri.BayiId == kullaniciId.Value && x.Durumu == 1)
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .ToList();
            }

            // LİSANS TİPLERİ VE LİSANS NUMARALARI - SADECE LİSANS NUMARASI OLAN SÖZLEŞMELER
            List<LisansTip> lisansTipleri = new List<LisansTip>();
            Dictionary<int, List<string>> lisansNumaralari = new Dictionary<int, List<string>>();

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                var musteriSozlesmeler = _sozlesmeRepository.GetirList(
                    x => x.Durumu == 1 &&
                         x.MusteriId == kullaniciId.Value &&
                         x.SozlesmeDurumuId == 11 &&
                         !string.IsNullOrEmpty(x.LisansNo), // SADECE LİSANS NUMARASI OLANLAR
                    new List<string> { "Teklif.LisansTip" }
                );

                // Lisans tiplerini ve numaralarını grupla
                foreach (var sozlesme in musteriSozlesmeler.Where(s => s.Teklif?.LisansTip != null && s.Teklif.LisansTip.Durumu == 1))
                {
                    var lisansTip = sozlesme.Teklif.LisansTip;
                    if (!lisansTipleri.Any(lt => lt.Id == lisansTip.Id))
                    {
                        lisansTipleri.Add(lisansTip);
                    }

                    if (!lisansNumaralari.ContainsKey(lisansTip.Id))
                    {
                        lisansNumaralari[lisansTip.Id] = new List<string>();
                    }

                    if (!lisansNumaralari[lisansTip.Id].Contains(sozlesme.LisansNo))
                    {
                        lisansNumaralari[lisansTip.Id].Add(sozlesme.LisansNo);
                    }
                }
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                var bayiMusteriSozlesmeler = _sozlesmeRepository.GetirQueryable()
                    .Where(x => x.Durumu == 1 &&
                                x.SozlesmeDurumuId == 11 &&
                                !string.IsNullOrEmpty(x.LisansNo) && // SADECE LİSANS NUMARASI OLANLAR
                                x.Musteri != null &&
                                x.Musteri.BayiId == kullaniciId.Value)
                    .Include(s => s.Teklif)
                    .ThenInclude(t => t.LisansTip)
                    .ToList();

                // Lisans tiplerini ve numaralarını grupla
                foreach (var sozlesme in bayiMusteriSozlesmeler.Where(s => s.Teklif?.LisansTip != null && s.Teklif.LisansTip.Durumu == 1))
                {
                    var lisansTip = sozlesme.Teklif.LisansTip;
                    if (!lisansTipleri.Any(lt => lt.Id == lisansTip.Id))
                    {
                        lisansTipleri.Add(lisansTip);
                    }

                    if (!lisansNumaralari.ContainsKey(lisansTip.Id))
                    {
                        lisansNumaralari[lisansTip.Id] = new List<string>();
                    }

                    if (!lisansNumaralari[lisansTip.Id].Contains(sozlesme.LisansNo))
                    {
                        lisansNumaralari[lisansTip.Id].Add(sozlesme.LisansNo);
                    }
                }
            }

            ViewBag.LisansTipleri = lisansTipleri;
            ViewBag.LisansNumaralari = lisansNumaralari;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)toplam / PageSize);
            ViewBag.TotalCount = toplam;
            ViewBag.TipiListesi = new[] { "ARGE", "Hata" };
            ViewBag.SelectedTip = tip;
            ViewBag.SelectedDurum = durum;

            // Bayi için ayrı listeleri View'a gönder
            ViewBag.BayiListe = bayiListe;
            ViewBag.MusteriListe = musteriListe;

            return View(liste);
        }

        [HttpGet]
        public IActionResult GetLisansTipleriVeNumaralariByMusteri(int musteriId)
        {
            if (musteriId <= 0)
            {
                return Json(new List<object>());
            }

            var sozlesmeler = _sozlesmeRepository.GetirList(
                x => x.Durumu == 1 &&
                     x.MusteriId == musteriId &&
                     x.SozlesmeDurumuId == 11 &&
                     !string.IsNullOrEmpty(x.LisansNo), // SADECE LİSANS NUMARASI OLANLAR
                new List<string> { "Teklif.LisansTip" }
            );

            var lisansBilgileri = sozlesmeler
                .Where(s => s.Teklif?.LisansTip != null && s.Teklif.LisansTip.Durumu == 1)
                .Select(s => new {
                    lisansTipId = s.Teklif.LisansTip.Id,
                    lisansTipAdi = s.Teklif.LisansTip.Adi,
                    lisansNo = s.LisansNo,
                    sozlesmeId = s.Id
                })
                .GroupBy(x => new { x.lisansTipId, x.lisansTipAdi })
                .Select(g => new {
                    id = g.Key.lisansTipId,
                    adi = g.Key.lisansTipAdi,
                    lisansNumaralari = g.Select(x => new {
                        lisansNo = x.lisansNo,
                        sozlesmeId = x.sozlesmeId
                    }).ToList()
                })
                .ToList();

            return Json(lisansBilgileri);
        }

        [HttpGet]
        public IActionResult GetLisansNumaralariByMusteriVeTip(int musteriId, int lisansTipId)
        {
            if (musteriId <= 0 || lisansTipId <= 0)
            {
                return Json(new List<object>());
            }

            var sozlesmeler = _sozlesmeRepository.GetirList(
                x => x.Durumu == 1 &&
                     x.MusteriId == musteriId &&
                     x.SozlesmeDurumuId == 11 &&
                     !string.IsNullOrEmpty(x.LisansNo) &&
                     x.Teklif.LisansTipId == lisansTipId,
                new List<string> { "Teklif.LisansTip" }
            );

            var lisansNumaralari = sozlesmeler
                .Select(s => new {
                    lisansNo = s.LisansNo,
                    sozlesmeId = s.Id
                })
                .ToList();

            return Json(lisansNumaralari);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Tipi, string Adi, string Soyadi, int? LisansTipId, string LisansNo,
                                 string Metni, IFormFile Dosya = null, string bildirimSahibi = "bayi",
                                 int? SecilenMusteriId = null, int? SecilenSozlesmeId = null)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası. Lütfen tekrar giriş yapın." });

            if (kullaniciTipi == "Bayi" && bildirimSahibi == "musteri" && !SecilenMusteriId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen bir müşteri seçiniz." });
            }

            if (string.IsNullOrWhiteSpace(Tipi) || string.IsNullOrWhiteSpace(Adi) ||
                string.IsNullOrWhiteSpace(Soyadi) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Zorunlu alanları doldurunuz." });

            // Lisans numarası kontrolü
            if (!LisansTipId.HasValue || string.IsNullOrWhiteSpace(LisansNo))
            {
                return Json(new { success = false, message = "Lütfen lisans tipi ve lisans numarası seçiniz." });
            }

            // Lisans numarasının geçerli olduğunu kontrol et
            bool lisansGecerli = false;
            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                lisansGecerli = _sozlesmeRepository.GetirQueryable()
                    .Any(x => x.Durumu == 1 &&
                              x.MusteriId == kullaniciId.Value &&
                              x.SozlesmeDurumuId == 11 &&
                              !string.IsNullOrEmpty(x.LisansNo) &&
                              x.Teklif.LisansTipId == LisansTipId &&
                              x.LisansNo == LisansNo);
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                int musteriId = bildirimSahibi == "musteri" && SecilenMusteriId.HasValue
                    ? SecilenMusteriId.Value
                    : (bildirimSahibi == "bayi" ? 0 : 0);

                if (bildirimSahibi == "musteri" && SecilenMusteriId.HasValue)
                {
                    lisansGecerli = _sozlesmeRepository.GetirQueryable()
                        .Any(x => x.Durumu == 1 &&
                                  x.MusteriId == SecilenMusteriId.Value &&
                                  x.SozlesmeDurumuId == 11 &&
                                  !string.IsNullOrEmpty(x.LisansNo) &&
                                  x.Teklif.LisansTipId == LisansTipId &&
                                  x.LisansNo == LisansNo);
                }
                else if (bildirimSahibi == "bayi")
                {
                    // Bayi adına kayıt yapılıyorsa, bayiye ait lisans numarası kontrolü
                    // (Bayilerin kendi lisansları varsa buraya eklenebilir)
                    lisansGecerli = true; // Şimdilik true, gerekiyorsa düzenlenebilir
                }
            }

            if (!lisansGecerli)
            {
                return Json(new { success = false, message = "Geçersiz lisans numarası veya bu lisansa sahip aktif sözleşme bulunamadı." });
            }

            try
            {
                string dosyaYolu = null;
                if (Dosya != null && Dosya.Length > 0)
                {
                    if (Dosya.Length > 10 * 1024 * 1024)
                        return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalıdır." });

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
                    ARGEDurumId = 1,
                    LisansNo = LisansNo // Lisans numarasını kaydet
                };

                if (LisansTipId.HasValue)
                    yeni.LisansTipId = LisansTipId.Value;

                if (SecilenSozlesmeId.HasValue)
                    yeni.MusteriSozlesmeId = SecilenSozlesmeId.Value;

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

            // Sadece lisans numarası olan sözleşmeleri getir
            List<LisansTip> lisansTipleri = new List<LisansTip>();
            Dictionary<int, List<string>> lisansNumaralari = new Dictionary<int, List<string>>();

            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                var sozlesmeler = _sozlesmeRepository.GetirList(
                    x => x.Durumu == 1 &&
                         x.MusteriId == kullaniciId.Value &&
                         x.SozlesmeDurumuId == 11 &&
                         !string.IsNullOrEmpty(x.LisansNo),
                    new List<string> { "Teklif.LisansTip" }
                );

                foreach (var sozlesme in sozlesmeler.Where(s => s.Teklif?.LisansTip != null && s.Teklif.LisansTip.Durumu == 1))
                {
                    var lisansTip = sozlesme.Teklif.LisansTip;
                    if (!lisansTipleri.Any(lt => lt.Id == lisansTip.Id))
                    {
                        lisansTipleri.Add(lisansTip);
                    }

                    if (!lisansNumaralari.ContainsKey(lisansTip.Id))
                    {
                        lisansNumaralari[lisansTip.Id] = new List<string>();
                    }

                    if (!lisansNumaralari[lisansTip.Id].Contains(sozlesme.LisansNo))
                    {
                        lisansNumaralari[lisansTip.Id].Add(sozlesme.LisansNo);
                    }
                }
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue && kayit.MusteriId.HasValue)
            {
                var sozlesmeler = _sozlesmeRepository.GetirList(
                    x => x.Durumu == 1 &&
                         x.MusteriId == kayit.MusteriId.Value &&
                         x.SozlesmeDurumuId == 11 &&
                         !string.IsNullOrEmpty(x.LisansNo),
                    new List<string> { "Teklif.LisansTip" }
                );

                foreach (var sozlesme in sozlesmeler.Where(s => s.Teklif?.LisansTip != null && s.Teklif.LisansTip.Durumu == 1))
                {
                    var lisansTip = sozlesme.Teklif.LisansTip;
                    if (!lisansTipleri.Any(lt => lt.Id == lisansTip.Id))
                    {
                        lisansTipleri.Add(lisansTip);
                    }

                    if (!lisansNumaralari.ContainsKey(lisansTip.Id))
                    {
                        lisansNumaralari[lisansTip.Id] = new List<string>();
                    }

                    if (!lisansNumaralari[lisansTip.Id].Contains(sozlesme.LisansNo))
                    {
                        lisansNumaralari[lisansTip.Id].Add(sozlesme.LisansNo);
                    }
                }
            }

            var data = new
            {
                kayit.Id,
                kayit.Tipi,
                kayit.Adi,
                kayit.Soyadi,
                kayit.LisansTipId,
                kayit.LisansNo,
                kayit.Metni,
                kayit.DosyaYolu,
                kayit.MusteriId,
                kayit.BayiId,
                kayit.DistributorCevap,
                kayit.DistributorCevapVerdiMi,
                kayit.DistributorCevapTarihi,
                kayit.ARGEDurumId,
                ArgeDurumAdi = kayit.ARGEDurum?.Adi ?? "Beklemede",
                MusteriAdi = kayit.Musteri != null ? kayit.Musteri.AdSoyad : null,
                BayiAdi = kayit.Bayi != null ? kayit.Bayi.Unvan : null,
                LisansTipleri = lisansTipleri.Select(g => new { g.Id, g.Adi }),
                LisansNumaralari = lisansNumaralari
            };
            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(int Id, string Tipi, string Adi, string Soyadi,
                                         int? LisansTipId, string LisansNo, string Metni, IFormFile Dosya = null,
                                         string duzenleBildirimSahibi = "bayi", int? SecilenMusteriId = null,
                                         int? SecilenSozlesmeId = null)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            ArgeHata mevcut = _repository.Getir(x => x.Id == Id);
            if (mevcut == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            if (mevcut.Durumu != 1)
            {
                return Json(new { success = false, message = "Bu kayıt incelenmeye alındığı için düzenlenemez. Yalnızca 'İncelenecek' durumundaki kayıtlar düzenlenebilir." });
            }

            if (kullaniciTipi == "Musteri" && mevcut.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı düzenleme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && mevcut.BayiId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu kaydı düzenleme yetkiniz yok." });
            }

            if (kullaniciTipi == "Bayi" && duzenleBildirimSahibi == "musteri" && !SecilenMusteriId.HasValue)
            {
                return Json(new { success = false, message = "Lütfen bir müşteri seçiniz." });
            }

            if (string.IsNullOrWhiteSpace(Tipi) || string.IsNullOrWhiteSpace(Adi) ||
                string.IsNullOrWhiteSpace(Soyadi) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Zorunlu alanları doldurunuz." });

            // Lisans numarası kontrolü
            if (!LisansTipId.HasValue || string.IsNullOrWhiteSpace(LisansNo))
            {
                return Json(new { success = false, message = "Lütfen lisans tipi ve lisans numarası seçiniz." });
            }

            try
            {
                string dosyaYolu = mevcut.DosyaYolu;
                if (Dosya != null && Dosya.Length > 0)
                {
                    if (Dosya.Length > 10 * 1024 * 1024)
                        return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalıdır." });

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
                mevcut.LisansNo = LisansNo;
                mevcut.GuncelleyenKullaniciId = kullaniciId.Value;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                if (SecilenSozlesmeId.HasValue)
                    mevcut.MusteriSozlesmeId = SecilenSozlesmeId.Value;

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

            if (kullaniciTipi == "Musteri" && kayit.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu cevabı görüntüleme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && kayit.BayiId != kullaniciId.Value)
            {
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