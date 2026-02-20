using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Linq;
using WepApp.Controllers;
using System.Collections.Generic;
using WebApp.Models;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace WebApp.Controllers
{
    public class AdminKampanyaController : AdminBaseController
    {
        private readonly KampanyaRepository _kampanyaRepo = new();
        private readonly PaketRepository _paketRepo = new();
        private readonly PaketGrupRepository _paketGrupRepo = new();
        private readonly KampanyaPaketRepository _kampanyaPaketRepo = new();
        private readonly PaketGrupDetayRepository _paketGrupDetayRepo = new();
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminKampanyaController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<Kampanya> kampanyalar = _kampanyaRepo.GetirList(x => x.Durumu == 1);

            foreach (Kampanya kampanya in kampanyalar)
            {
                kampanya.KampanyaPaketler = _kampanyaPaketRepo.GetirList(
                    x => x.KampanyaId == kampanya.Id && x.Durumu == 1,
                    new List<string> { "Paket", "PaketGrup" }
                ).ToList();
            }

            ViewBag.Kampanyalar = kampanyalar;
            ViewBag.Paketler = _paketRepo.GetirList(x => x.Durumu == 1 && x.Aktif == 1, new List<string> { "LisansTip", "Birim" });
            ViewBag.PaketGruplari = _paketGrupRepo.GetirList(x => x.Durumu == 1, new List<string> { "LisansTip" });

            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Baslik, string Metin, decimal IndirimYuzdesi,
            DateTime BaslangicTarihi, DateTime BitisTarihi, int[] seciliPaketler, int[] seciliPaketGruplari, IFormFile? Gorsel = null)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                Kampanya kampanya = new Kampanya
                {
                    Baslik = Baslik,
                    Metin = Metin,
                    IndirimYuzdesi = IndirimYuzdesi,
                    BaslangicTarihi = BaslangicTarihi,
                    BitisTarihi = BitisTarihi,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id
                };

                // Görsel yükleme işlemi
                if (Gorsel != null && Gorsel.Length > 0)
                {
                    kampanya.GorselYolu = GorselYukle(Gorsel);
                }

                _kampanyaRepo.Ekle(kampanya);

                decimal indirimOrani = 1 - (IndirimYuzdesi / 100m);

                // Seçili Paketler için KFiyat güncelle
                if (seciliPaketler != null && seciliPaketler.Length > 0)
                {
                    List<Paket> paketler = _paketRepo.GetirList(x => seciliPaketler.Contains(x.Id) && x.Durumu == 1);
                    foreach (Paket paket in paketler)
                    {
                        if (paket.Fiyat.HasValue)
                        {
                            paket.KFiyat = Math.Round(paket.Fiyat.Value * indirimOrani, 2);
                            paket.GuncellenmeTarihi = DateTime.Now;
                            paket.GuncelleyenKullaniciId = kullanici.Id;
                            _paketRepo.Guncelle(paket);
                        }

                        // KampanyaPaket ilişkisi ekle
                        KampanyaPaket kampanyaPaket = new KampanyaPaket
                        {
                            KampanyaId = kampanya.Id,
                            PaketId = paket.Id,
                            PaketGrupId = 0,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _kampanyaPaketRepo.Ekle(kampanyaPaket);
                    }
                }

                // Seçili Paket Grupları için (içindeki tüm paketlerin KFiyat'ı güncellenir)
                if (seciliPaketGruplari != null && seciliPaketGruplari.Length > 0)
                {
                    foreach (int grupId in seciliPaketGruplari)
                    {
                        KampanyaPaket kampanyaPaket = new KampanyaPaket
                        {
                            KampanyaId = kampanya.Id,
                            PaketId = 0,
                            PaketGrupId = grupId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _kampanyaPaketRepo.Ekle(kampanyaPaket);
                    }
                }

                TempData["Success"] = "Kampanya başarıyla eklendi ve seçili paketlerin kampanyalı fiyatları güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Baslik, string Metin, decimal IndirimYuzdesi,
            DateTime BaslangicTarihi, DateTime BitisTarihi, int[] seciliPaketler, int[] seciliPaketGruplari, IFormFile? Gorsel = null, string? MevcutGorsel = null)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Yetkiniz yok.";
                    return RedirectToAction("Index");
                }

                Kampanya k = _kampanyaRepo.Getir(Id);
                if (k == null)
                {
                    TempData["Error"] = "Kampanya bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Önce eski ilişkileri alalım (sonra KFiyat sıfırlamak için gerekirse)
                List<KampanyaPaket> eskiKampanyaPaketler = _kampanyaPaketRepo.GetirList(x => x.KampanyaId == Id && x.Durumu == 1);
                List<int> eskiPaketIds = eskiKampanyaPaketler.Where(x => x.PaketId > 0).Select(x => x.PaketId).ToList();

                // Kampanya bilgilerini güncelle
                k.Baslik = Baslik;
                k.Metin = Metin;
                k.IndirimYuzdesi = IndirimYuzdesi;
                k.BaslangicTarihi = BaslangicTarihi;
                k.BitisTarihi = BitisTarihi;
                k.GuncellenmeTarihi = DateTime.Now;
                k.GuncelleyenKullaniciId = kullanici.Id;

                // Görsel yükleme işlemi
                if (Gorsel != null && Gorsel.Length > 0)
                {
                    // Eski görseli sil
                    if (!string.IsNullOrEmpty(k.GorselYolu))
                    {
                        GorselSil(k.GorselYolu);
                    }
                    k.GorselYolu = GorselYukle(Gorsel);
                }

                _kampanyaRepo.Guncelle(k);

                decimal indirimOrani = 1 - (IndirimYuzdesi / 100m);

                // 1. Eski seçili ama artık seçilmeyen paketlerin KFiyat'ını eski haline getir (opsiyonel)
                // Eğer birden fazla kampanya varsa bu riskli olur. Bu yüzden şimdilik sadece yeni seçilenleri güncelliyoruz.

                // 2. Yeni seçilen paketlerin KFiyat'ını güncelle
                if (seciliPaketler != null && seciliPaketler.Length > 0)
                {
                    List<Paket> guncellenecekPaketler = _paketRepo.GetirList(x => seciliPaketler.Contains(x.Id));
                    foreach (Paket paket in guncellenecekPaketler)
                    {
                        if (paket.Fiyat.HasValue)
                        {
                            paket.KFiyat = Math.Round(paket.Fiyat.Value * indirimOrani, 2);
                            paket.GuncellenmeTarihi = DateTime.Now;
                            paket.GuncelleyenKullaniciId = kullanici.Id;
                            _paketRepo.Guncelle(paket);
                        }
                    }
                }

                // Mevcut ilişkileri sil, yenilerini ekle
                foreach (KampanyaPaket kp in eskiKampanyaPaketler)
                {
                    _kampanyaPaketRepo.Sil(kp);
                }

                // Yeni paket ilişkileri
                if (seciliPaketler != null)
                {
                    foreach (int paketId in seciliPaketler)
                    {
                        _kampanyaPaketRepo.Ekle(new KampanyaPaket
                        {
                            KampanyaId = Id,
                            PaketId = paketId,
                            PaketGrupId = 0,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        });
                    }
                }

                // Yeni grup ilişkileri
                if (seciliPaketGruplari != null)
                {
                    foreach (int grupId in seciliPaketGruplari)
                    {
                        _kampanyaPaketRepo.Ekle(new KampanyaPaket
                        {
                            KampanyaId = Id,
                            PaketId = 0,
                            PaketGrupId = grupId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        });
                    }
                }

                TempData["Success"] = "Kampanya güncellendi ve seçili paketlerin kampanyalı fiyatları yenilendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Güncelleme hatası: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Sil metodu (isteğe bağlı: silinen kampanyaya bağlı paketlerin KFiyat'ı normale dönebilir)
        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kampanya k = _kampanyaRepo.Getir(Id);
            if (k != null)
            {
                // Görseli sil
                if (!string.IsNullOrEmpty(k.GorselYolu))
                {
                    GorselSil(k.GorselYolu);
                }

                k.Durumu = 0;
                _kampanyaRepo.Guncelle(k);

                // İlişkileri pasif yap veya sil
                List<KampanyaPaket> ilişkiler = _kampanyaPaketRepo.GetirList(x => x.KampanyaId == Id);
                foreach (KampanyaPaket item in ilişkiler)
                {
                    item.Durumu = 0;
                    _kampanyaPaketRepo.Guncelle(item);
                }

                TempData["Success"] = "Kampanya pasif yapıldı.";
            }
            else
            {
                TempData["Error"] = "Kampanya bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();
            Kampanya k = _kampanyaRepo.Getir(id);
            if (k == null) return NotFound();

            List<KampanyaPaket> kampanyaPaketler = _kampanyaPaketRepo.GetirList(x => x.KampanyaId == id && x.Durumu == 1);
            List<int> seciliPaketIds = kampanyaPaketler.Where(x => x.PaketId > 0).Select(x => x.PaketId).ToList();
            List<int> seciliGrupIds = kampanyaPaketler.Where(x => x.PaketGrupId > 0).Select(x => x.PaketGrupId).ToList();

            List<Paket> tumPaketler = _paketRepo.GetirList(x => x.Durumu == 1 && x.Aktif == 1, new List<string> { "LisansTip", "Birim" });
            List<PaketGrup> tumGruplar = _paketGrupRepo.GetirList(x => x.Durumu == 1, new List<string> { "LisansTip" });

            StringBuilder sb = new StringBuilder();

            // Önce gruplar
            foreach (PaketGrup grup in tumGruplar)
            {
                string @checked = seciliGrupIds.Contains(grup.Id) ? "checked" : "";
                sb.Append($@"
            <tr data-tur=""grup"">
                <td>
                    <div class=""form-check"">
                        <input class=""form-check-input birlesik-checkbox"" type=""checkbox"" name=""seciliPaketGruplari"" value=""{grup.Id}"" {@checked}>
                        <label class=""form-check-label""></label>
                    </div>
                </td>
                <td><span class=""badge bg-primary tur-badge"">GRUP</span></td>
                <td><strong>{System.Web.HttpUtility.HtmlEncode(grup.Adi)}</strong></td>
                <td><small class=""text-muted"">{grup.LisansTip?.Adi}</small></td>
                <td class=""text-end text-success fw-bold"">{grup.Fiyat:N2} ₺</td>
            </tr>");
            }

            // Sonra paketler
            foreach (Paket paket in tumPaketler)
            {
                string @checked = seciliPaketIds.Contains(paket.Id) ? "checked" : "";
                sb.Append($@"
            <tr data-tur=""paket"">
                <td>
                    <div class=""form-check"">
                        <input class=""form-check-input birlesik-checkbox"" type=""checkbox"" name=""seciliPaketler"" value=""{paket.Id}"" {@checked}>
                        <label class=""form-check-label""></label>
                    </div>
                </td>
                <td><span class=""badge bg-info tur-badge"">PAKET</span></td>
                <td><strong>{System.Web.HttpUtility.HtmlEncode(paket.Adi)}</strong></td>
                <td><small class=""text-muted"">{paket.LisansTip?.Adi} • {paket.Birim?.Adi} • {paket.EgitimSuresi} gün</small></td>
                <td class=""text-end text-success fw-bold"">{paket.Fiyat:N2} ₺</td>
            </tr>");
            }

            return Json(new
            {
                id = k.Id,
                baslik = k.Baslik ?? "",
                metin = k.Metin ?? "",
                indirimYuzdesi = k.IndirimYuzdesi,
                baslangicTarihi = k.BaslangicTarihi.ToString("yyyy-MM-ddTHH:mm"),
                bitisTarihi = k.BitisTarihi.ToString("yyyy-MM-ddTHH:mm"),
                gorselyolu = k.GorselYolu,
                birlesikHtml = sb.ToString()   // tek alan
            });
        }

        [HttpGet]
        public IActionResult GetirKampanyaDetay(int id)
        {
            LoadCommonData();
            try
            {
                List<KampanyaPaket> kampanyaPaketler = _kampanyaPaketRepo.GetirList(
                    x => x.KampanyaId == id && x.Durumu == 1,
                    new List<string> { "Paket", "PaketGrup" }
                );

                var paketler = kampanyaPaketler
                    .Where(x => x.PaketId > 0 && x.Paket != null)
                    .Select(x => new {
                        id = x.PaketId,
                        adi = x.Paket.Adi,
                        tur = "Paket"
                    }).ToList();

                var paketGruplari = kampanyaPaketler
                    .Where(x => x.PaketGrupId > 0 && x.PaketGrup != null)
                    .Select(x => new {
                        id = x.PaketGrupId,
                        adi = x.PaketGrup.Adi,
                        tur = "Paket Grubu"
                    }).ToList();

                var tumIcerik = paketler.Concat(paketGruplari).ToList();

                return Json(new { success = true, data = tumIcerik });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Sadece kampanyaya direkt eklenen PAKETLER
        [HttpGet]
        public IActionResult GetirKampanyaPaketleri(int id)
        {
            LoadCommonData();

            try
            {
                Kampanya kampanya = _kampanyaRepo.Getir(id);
                if (kampanya == null) return Json(new { success = false, message = "Kampanya bulunamadı." });

                List<KampanyaPaket> direktPaketler = _kampanyaPaketRepo.GetirList(
                    x => x.KampanyaId == id && x.PaketId > 0 && x.Durumu == 1,
                    new List<string> { "Paket" }
                );

                var data = direktPaketler.Select(kp => new
                {
                    id = kp.Paket.Id,
                    adi = kp.Paket.Adi,
                    normalFiyat = kp.Paket.Fiyat.HasValue ? kp.Paket.Fiyat.Value : 0m,
                    kampanyaFiyat = kp.Paket.KFiyat.HasValue ? kp.Paket.KFiyat.Value : kp.Paket.Fiyat.Value,
                    lisansTip = kp.Paket.LisansTip?.Adi,
                    birim = kp.Paket.Birim?.Adi,
                    egitimSuresi = kp.Paket.EgitimSuresi
                }).ToList();

                return Json(new
                {
                    success = true,
                    kampanyaBaslik = kampanya.Baslik,
                    baslangic = kampanya.BaslangicTarihi.ToString("dd.MM.yyyy HH:mm"),
                    bitis = kampanya.BitisTarihi.ToString("dd.MM.yyyy HH:mm"),
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Sadece kampanyaya eklenen PAKET GRUPLARI
        [HttpGet]
        public IActionResult GetirKampanyaPaketGruplari(int id)
        {
            LoadCommonData();

            try
            {
                Kampanya kampanya = _kampanyaRepo.Getir(id);
                if (kampanya == null) return Json(new { success = false, message = "Kampanya bulunamadı." });

                List<KampanyaPaket> gruplar = _kampanyaPaketRepo.GetirList(
                    x => x.KampanyaId == id && x.PaketGrupId > 0 && x.Durumu == 1,
                    new List<string> { "PaketGrup" }
                );

                var data = gruplar.Select(kp => new
                {
                    id = kp.PaketGrup.Id,
                    adi = kp.PaketGrup.Adi,
                    fiyat = kp.PaketGrup.Fiyat,
                    lisansTip = kp.PaketGrup.LisansTip?.Adi,
                    icerikSayisi = _paketGrupDetayRepo.GetirList(x => x.PaketGrupId == kp.PaketGrupId && x.Durumu == 1).Count
                }).ToList();

                return Json(new
                {
                    success = true,
                    kampanyaBaslik = kampanya.Baslik,
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GorselSilAjax(int id)
        {
            try
            {
                Kampanya kampanya = _kampanyaRepo.Getir(id);
                if (kampanya != null && !string.IsNullOrEmpty(kampanya.GorselYolu))
                {
                    GorselSil(kampanya.GorselYolu);
                    kampanya.GorselYolu = null;
                    kampanya.GuncellenmeTarihi = DateTime.Now;
                    _kampanyaRepo.Guncelle(kampanya);
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
                string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", "Kampanya");

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
                return "/WebAdminTheme/Kampanya/" + uniqueFileName;
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
                    string filePath = Path.Combine(_hostEnvironment.WebRootPath, "WebAdminTheme", "Kampanya", fileName);

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