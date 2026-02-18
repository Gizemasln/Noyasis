using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using AspNetCore.Reporting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace WepApp.Controllers
{
    public class AdminTeklifController : AdminBaseController
    {
        private readonly IWebHostEnvironment _webHostEnviroment;

        private readonly TeklifRepository _teklifRepo = new TeklifRepository();
        private readonly TeklifDurumRepository _teklifDurumRepo = new TeklifDurumRepository();
        private readonly MusteriRepository _musteriRepo = new MusteriRepository();
        private readonly LisansTipRepository _lisansTipRepo = new LisansTipRepository();
        private readonly TeklifDetayRepository _teklifDetayRepo = new TeklifDetayRepository();
        private readonly NedenlerRepository _nedenlerRepo = new NedenlerRepository();
        private readonly NeredenDuyduRepository _neredenDuyduRepo = new NeredenDuyduRepository();
        private readonly EntegratorRepository _entegratorRepo = new EntegratorRepository();
        private readonly MusteriSozlesmeRepository _sozlesmeRepo = new MusteriSozlesmeRepository();
        private readonly UYBRepository _uybRepo = new UYBRepository();

        public AdminTeklifController(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnviroment = webHostEnvironment;
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        // GetCurrentUserId methodunu ekleyin
        private int GetCurrentUserId()
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            return kullanici?.Id ?? 0;
        }

        public IActionResult Index()
        {
            LoadCommonData();

            try
            {
                List<Teklif> teklifler = _teklifRepo.GetirList(
                    x => x.Aktif == true,
                    new List<string> { "Musteri", "LisansTip", "Musteri.Bayi", "TeklifDurum", "Nedenler" })
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .ToList();

                // Sözleşmeleri de getir
                List<MusteriSozlesme> sozlesmeler = _sozlesmeRepo.GetirList(x => x.Durumu == 1)
                    .ToList();

                // Hangi teklifin aktif sözleşmesi var?
                HashSet<int> teklifIdAktifSozlesmeVar = sozlesmeler
                    .Select(s => s.TeklifId)
                    .Distinct()
                    .ToHashSet();

                ViewBag.Teklifler = teklifler;
                ViewBag.TeklifIdAktifSozlesmeVar = teklifIdAktifSozlesmeVar;

                // TÜM LİSANS TİPLERİNİ GETİR (direkt veritabanından)
                var tumLisansTipleri = _lisansTipRepo.GetirList(x => x.Durumu == 1)
                    .OrderBy(lt => lt.Adi)
                    .ToList();

                ViewBag.TumLisansTipleri = tumLisansTipleri;

                ViewBag.TeklifDurumlari = _teklifDurumRepo
                    .GetirList(x => x.Durumu == 1)
                    .OrderBy(x => x.Sıra)
                    .ToList();

                ViewBag.Nedenler = _nedenlerRepo.GetirList(x => x.Durumu == 1).ToList();

                LoadCommonData();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Teklifler yüklenirken hata oluştu: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetirSozlesmeBilgileri(int teklifId)
        {
            LoadCommonData();

            try
            {
                // 1. Önce bu teklife ait aktif bir sözleşme var mı kontrol et
                MusteriSozlesme mevcutSozlesme = _sozlesmeRepo.Getir(x => x.TeklifId == teklifId && x.Durumu == 1);

                if (mevcutSozlesme != null)
                {
                    return Json(new
                    {
                        success = false,
                        sozlesmeVar = true,
                        sozlesmeId = mevcutSozlesme.Id,
                        message = "Bu teklife ait aktif bir sözleşme zaten mevcut. " +
                                  "Yeni sözleşme oluşturabilmek için mevcut sözleşmeyi silin veya pasif hale getirin."
                    });
                }

                // 2. Teklif bilgilerini getir
                Teklif teklif = _teklifRepo.Getir(
                    x => x.Id == teklifId && x.Aktif == true,
                    new List<string> { "Musteri", "LisansTip", "Detaylar", "Musteri.MusteriTipi", "Detaylar.PaketGrup" });

                if (teklif == null)
                {
                    return Json(new { success = false, message = "Teklif bulunamadı." });
                }

                // 3. Kampanya bilgisi (ilk detaydan al)
                string kampanyaBilgisi = "Kampanya yok";
                TeklifDetay ilkDetay = teklif.Detaylar?.FirstOrDefault();
                if (ilkDetay != null)
                {
                    if (!string.IsNullOrEmpty(ilkDetay.KampanyaBaslik))
                    {
                        kampanyaBilgisi = ilkDetay.KampanyaBaslik;
                    }
                    else if (ilkDetay.KampanyaIndirimYuzdesi > 0)
                    {
                        kampanyaBilgisi = $"%{ilkDetay.KampanyaIndirimYuzdesi} Kampanya İndirimi";
                    }
                }

                // 4. Paket adı (ilk detaydan al)
                string paketAdi = "Belirtilmemiş";
                if (ilkDetay != null)
                {
                    paketAdi = ilkDetay.ItemAdi ?? (ilkDetay.PaketGrupAdi ?? "Belirtilmemiş");
                }

                // 5. Nereden duydu listesi
                var neredenDuyduList = _neredenDuyduRepo.GetirList(x => x.Durumu == 1)
                    .Select(n => new { n.Id, n.Adi })
                    .ToList();

                // 6. Entegratör listesi
                var entegratorList = _entegratorRepo.GetirList(x => x.Durumu == 1)
                    .Select(e => new { e.Id, e.Adi, e.Kodu })
                    .ToList();

                // 7. Aktif UYB oranını al
                UYB uybOrani = _uybRepo.Getir(x=> x.Durumu==1);
                decimal tutarKdvsiz = teklif.NetToplam / 1.20m; // KDV'siz tutar
                decimal uybTutari = tutarKdvsiz * (uybOrani.Oran / 100m);

                // 8. Başarılı yanıt
                var result = new
                {
                    success = true,
                    data = new
                    {
                        teklifId = teklif.Id,
                        teklifNo = teklif.TeklifNo,
                        lisansTipi = teklif.LisansTip?.Adi,
                        paketAdi = paketAdi,
                        kampanyaBilgisi = kampanyaBilgisi,
                        tutarKdvsiz = tutarKdvsiz,
                        uybOrani = uybOrani, // UYB oranını da gönder
                        uybTutari = uybTutari, // Hesaplanan UYB tutarı
                        musteri = new
                        {
                            id = teklif.Musteri?.Id ?? 0,
                            ticariUnvan = teklif.Musteri?.TicariUnvan ?? "",
                            adi = teklif.Musteri?.Ad,
                            soyadi = teklif.Musteri?.Soyad,
                            telefon = teklif.Musteri?.Telefon ?? "",
                            adres1 = teklif.Musteri?.Adres ?? "",
                            adres2 = "",
                            il = teklif.Musteri?.Il ?? "",
                            ilce = teklif.Musteri?.Ilce ?? "",
                            vergiDairesi = teklif.Musteri?.VergiDairesi ?? "",
                            vergiNo = teklif.Musteri?.TCVNo ?? "",
                            email = teklif.Musteri?.Email ?? "",
                            webAdresi = teklif.Musteri?.WebAdresi ?? "",
                            musteriTipi = teklif.Musteri?.MusteriTipi?.Adi ?? "",
                            neredenDuyduId = 1, // Varsayılan değer
                            entegratorId = teklif.Musteri?.BayiId // Bayi ID'sini entegratör olarak kullan
                        },
                        neredenDuyduList = neredenDuyduList,
                        entegratorList = entegratorList
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> KaydetSozlesme([FromForm] IFormCollection form, [FromForm] IFormFile VergiKimlikLevhasıDosya,
       [FromForm] IFormFile TicariSicilGazetesiDosya, [FromForm] IFormFile KimlikOnYuzuDosya, [FromForm] IFormFile ImzaSirkusuDosya)
        {
            try
            {
                int kullaniciId = GetCurrentUserId();
                if (kullaniciId == 0)
                    return Json(new { success = false, message = "Oturum süresi doldu." });

                // Form verilerini güvenli şekilde al
                if (!int.TryParse(form["TeklifId"], out int teklifId))
                    return Json(new { success = false, message = "Geçersiz teklif ID." });

                if (!int.TryParse(form["MusteriId"], out int musteriId))
                    return Json(new { success = false, message = "Geçersiz müşteri ID." });

                // LisansNo zorunlu değil, null olabilir
                string lisansNo = form["LisansNo"].ToString()?.Trim() ?? "";
                decimal yillikBakim;
                // Yıllık Bakım ücreti
                string yillikBakimStr = form["YillikBakim"].ToString()?.Trim() ?? "0";

                // Boşsa 0 kabul et
                if (string.IsNullOrWhiteSpace(yillikBakimStr))
                {
                    yillikBakimStr = "0";
                }

                // Türkçe formatını decimal'a çevir
                try
                {
                    // 1. Önce ₺, TL, boşluk gibi karakterleri temizle
                    yillikBakimStr = yillikBakimStr
                        .Replace("₺", "")
                        .Replace("TL", "")
                        .Replace(" ", "")
                        .Trim();

                    // 2. Binlik ayraçlarını (nokta) kaldır
                    // Örnek: "1.652,00" → "1652,00"
                    yillikBakimStr = yillikBakimStr.Replace(".", "");

                    // 3. Ondalık ayracını nokta yap (virgülü noktaya çevir)
                    // Örnek: "1652,00" → "1652.00"
                    yillikBakimStr = yillikBakimStr.Replace(",", ".");

                    // 4. Decimal'a çevir
                    if (!decimal.TryParse(yillikBakimStr, NumberStyles.Any, CultureInfo.InvariantCulture, out yillikBakim))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Geçersiz yıllık bakım ücreti formatı. Lütfen sayısal bir değer girin. Örnek: 1.652,00"
                        });
                    }

                    // Negatif olamaz
                    if (yillikBakim < 0)
                    {
                        return Json(new { success = false, message = "Yıllık bakım ücreti negatif olamaz." });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Yıllık bakım ücreti hesaplanırken hata: {ex.Message}"
                    });
                }


                // Entegratör ID'si (zorunlu)
                if (!int.TryParse(form["EntegratorId"], out int entegratorId) || entegratorId <= 0)
                    return Json(new { success = false, message = "Geçerli bir entegratör seçmelisiniz." });

                // Tarih alanlarını al ve parse et
                DateTime yayinTarihi;
                DateTime uybSuresi;

                // Yayın Tarihi - varsayılan bugün, formdan gelirse onu kullan
                if (!string.IsNullOrWhiteSpace(form["YayinTarihi"]))
                {
                    // Türkçe tarih formatından (dd.MM.yyyy) DateTime'a çevir
                    string tarihStr = form["YayinTarihi"].ToString().Trim();
                    if (DateTime.TryParseExact(tarihStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out yayinTarihi))
                    {
                        // Başarılı
                    }
                    else if (DateTime.TryParse(tarihStr, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out yayinTarihi))
                    {
                        // Alternatif parse denemesi
                    }
                    else
                    {
                        yayinTarihi = DateTime.Today;
                    }
                }
                else
                {
                    yayinTarihi = DateTime.Today;
                }

                // UYB Süresi - varsayılan yayın tarihinden 1 yıl sonra, formdan gelirse onu kullan
                if (!string.IsNullOrWhiteSpace(form["UYBSuresi"]))
                {
                    // Türkçe tarih formatından (dd.MM.yyyy) DateTime'a çevir
                    string tarihStr = form["UYBSuresi"].ToString().Trim();
                    if (DateTime.TryParseExact(tarihStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out uybSuresi))
                    {
                        // Başarılı
                    }
                    else if (DateTime.TryParse(tarihStr, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out uybSuresi))
                    {
                        // Alternatif parse denemesi
                    }
                    else
                    {
                        uybSuresi = yayinTarihi.AddYears(1);
                    }
                }
                else
                {
                    uybSuresi = yayinTarihi.AddYears(1);
                }

                // Doküman checkbox değerleri - daha güvenli kontrol
                bool vergiKimlikLevhasıVar = form.ContainsKey("VergiKimlikLevhasıVar") &&
                                              form["VergiKimlikLevhasıVar"].ToString() == "on";
                bool ticariSicilGazetesiVar = form.ContainsKey("TicariSicilGazetesiVar") &&
                                              form["TicariSicilGazetesiVar"].ToString() == "on";
                bool kimlikOnYuzuVar = form.ContainsKey("KimlikOnYuzuVar") &&
                                       form["KimlikOnYuzuVar"].ToString() == "on";
                bool imzaSirkusuVar = form.ContainsKey("ImzaSirkusuVar") &&
                                      form["ImzaSirkusuVar"].ToString() == "on";

                // Müşteri bilgilerini kontrol et
                string ticariUnvan = form["TicariUnvan"].ToString()?.Trim() ?? "";
                string adi = form["Adi"].ToString()?.Trim() ?? "";
                string soyadi = form["Soyadi"].ToString()?.Trim() ?? "";
                string telefon = form["Telefon"].ToString()?.Trim() ?? "";
                string adres1 = form["Adres1"].ToString()?.Trim() ?? "";
                string il = form["Il"].ToString()?.Trim() ?? "";
                string ilce = form["Ilce"].ToString()?.Trim() ?? "";
                string vergiDairesi = form["VergiDairesi"].ToString()?.Trim() ?? "";
                string vergiNo = form["VergiNo"].ToString()?.Trim() ?? "";
                string email = form["Email"].ToString()?.Trim() ?? "";
                string referans = form["Referans"].ToString()?.Trim() ?? "";

                // Zorunlu alan kontrolleri
                List<string> hatalar = new List<string>();

                if (string.IsNullOrWhiteSpace(ticariUnvan))
                    hatalar.Add("Ticari Ünvan");
                if (string.IsNullOrWhiteSpace(adi))
                    hatalar.Add("Adı");
                if (string.IsNullOrWhiteSpace(soyadi))
                    hatalar.Add("Soyadı");
                if (string.IsNullOrWhiteSpace(telefon))
                    hatalar.Add("Telefon");
                if (string.IsNullOrWhiteSpace(adres1))
                    hatalar.Add("Adres - 1");
                if (string.IsNullOrWhiteSpace(il))
                    hatalar.Add("İl");
                if (string.IsNullOrWhiteSpace(ilce))
                    hatalar.Add("İlçe");
                if (string.IsNullOrWhiteSpace(vergiDairesi))
                    hatalar.Add("Vergi Dairesi");
                if (string.IsNullOrWhiteSpace(vergiNo))
                    hatalar.Add("Vergi No");
                if (string.IsNullOrWhiteSpace(email))
                    hatalar.Add("E-posta");
                if (string.IsNullOrWhiteSpace(referans))
                    hatalar.Add("Referans Sektör");
                if (entegratorId <= 0)
                    hatalar.Add("Entegratör");

                // Tarih alanları zorunlu değil ama kontrol edelim
                if (yayinTarihi == DateTime.MinValue)
                    hatalar.Add("Yayın Tarihi (geçersiz format)");
                if (uybSuresi == DateTime.MinValue)
                    hatalar.Add("UYB Süresi (geçersiz format)");

                if (hatalar.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Eksik zorunlu alanlar: {string.Join(", ", hatalar)}"
                    });
                }

                // Nereden Duydu ID (zorunlu değil)
                int? neredenDuyduId = null;
                if (int.TryParse(form["NeredenDuyduId"], out int tempNeredenId) && tempNeredenId > 0)
                {
                    neredenDuyduId = tempNeredenId;
                }

                MusteriSozlesme sozlesme = new MusteriSozlesme
                {
                    TeklifId = teklifId,
                    MusteriId = musteriId,
                    LisansNo = lisansNo,
                    YillikBakim = yillikBakim,

                    // Yeni eklenen tarih alanları
                    YayinTarihi = yayinTarihi,
                    UYBSuresi = uybSuresi,

                    // Sözleşme Tipi
                    SozlesmeTipi = form["SozlesmeTipi"].ToString() ?? "YeniKayit",

                    // Ödeme Bekleme (checkbox "on" değeri olarak gelir)
                    OdemeBekleme = form.ContainsKey("OdemeBekleme") &&
                                  form["OdemeBekleme"].ToString() == "on",

                    // Bilgilendirme tercihleri
                    SmsBilgilendirme = form.ContainsKey("SmsBilgilendirme") &&
                                      form["SmsBilgilendirme"].ToString() == "on",
                    EmailBilgilendirme = form.ContainsKey("EmailBilgilendirme") &&
                                        form["EmailBilgilendirme"].ToString() == "on",
                    TelefonBilgilendirme = form.ContainsKey("TelefonBilgilendirme") &&
                                          form["TelefonBilgilendirme"].ToString() == "on",
                    HaberPaylasimi = form.ContainsKey("HaberPaylasimi") &&
                                    form["HaberPaylasimi"].ToString() == "on",

                    // Doküman checkbox'ları
                    VergiKimlikLevhasıVar = vergiKimlikLevhasıVar,
                    TicariSicilGazetesiVar = ticariSicilGazetesiVar,
                    KimlikOnYuzuVar = kimlikOnYuzuVar,
                    ImzaSirkusuVar = imzaSirkusuVar,

                    // Müşteri bilgileri
                    TicariUnvan = ticariUnvan,
                    Adi = adi,
                    Soyadi = soyadi,
                    Telefon = telefon,
                    CepTelefon = form["CepTelefon"].ToString()?.Trim() ?? "",
                    Adres1 = adres1,
                    Adres2 = form["Adres2"].ToString()?.Trim() ?? "",
                    Ulke = form["Ulke"].ToString()?.Trim() ?? "Türkiye",
                    Il = il,
                    Ilce = ilce,
                    VergiDairesi = vergiDairesi,
                    VergiNo = vergiNo,
                    Email = email,
                    WebSitesi = form["WebSitesi"].ToString()?.Trim() ?? "",
                    Referans = referans,
                    NeredenDuyduId = neredenDuyduId,
                    EntegratorId = entegratorId,

                    // Sistem alanları
                    Durumu = 1,
                    EkleyenKullaniciId = kullaniciId,
                    GuncelleyenKullaniciId = kullaniciId,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    RevizeTarihi = DateTime.Now,
                    RevizyonNo = "1.0",
                    SozlesmeDurumuId = 6,
                    DokumanNo = GenerateDokumanNo(),

                    // Navigation property'leri NULL bırak
                    Teklif = null!,
                    Musteri = null,
                    SozlesmeDurumu = null,
                    Entegrator = null
                };

                // DOSYALARI KAYDET - MusteriSozlesme KLASÖRÜNE
                string uploadsFolder = Path.Combine(_webHostEnviroment.WebRootPath, "WebAdminTheme", "MusteriSozlesme");

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Benzersiz dosya adı için
                string dosyaPrefix = $"SOZ_{teklifId}_{DateTime.Now:yyyyMMddHHmmss}";

                // Dosya yükleme yardımcı fonksiyonu
                async Task<string?> DosyaKaydet(IFormFile dosya, string dosyaTipi)
                {
                    if (dosya != null && dosya.Length > 0)
                    {
                        // Dosya boyutu kontrolü (10MB limit)
                        if (dosya.Length > 10 * 1024 * 1024)
                        {
                            throw new Exception($"{dosyaTipi} dosyası 10MB'dan büyük olamaz.");
                        }

                        // Dosya uzantısı kontrolü
                        var izinliUzantilar = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                        string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();

                        if (!izinliUzantilar.Contains(uzanti))
                        {
                            throw new Exception($"{dosyaTipi} için geçersiz dosya formatı. İzinli formatlar: {string.Join(", ", izinliUzantilar)}");
                        }

                        // Güvenli dosya adı oluştur
                        string guvenliDosyaAdi = $"{dosyaTipi.Replace(" ", "_").ToLowerInvariant()}_{dosyaPrefix}{uzanti}";
                        string filePath = Path.Combine(uploadsFolder, guvenliDosyaAdi);

                        // Dosyayı kaydet
                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            await dosya.CopyToAsync(stream);
                        }

                        return guvenliDosyaAdi;
                    }
                    return null;
                }

                // Her bir dosyayı kaydet
                try
                {
                    sozlesme.VergiKimlikLevhasıDosyaAdi = await DosyaKaydet(VergiKimlikLevhasıDosya, "VergiKimlikLevhası");
                    sozlesme.TicariSicilGazetesiDosyaAdi = await DosyaKaydet(TicariSicilGazetesiDosya, "TicariSicilGazetesi");
                    sozlesme.KimlikOnYuzuDosyaAdi = await DosyaKaydet(KimlikOnYuzuDosya, "KimlikOnYuzu");
                    sozlesme.ImzaSirkusuDosyaAdi = await DosyaKaydet(ImzaSirkusuDosya, "ImzaSirkusu");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Dosya yükleme hatası: " + ex.Message });
                }

                // DB'ye ekle
                _sozlesmeRepo.Ekle(sozlesme);

                // Teklif durumunu "Sözleşmede" yap
                Teklif teklif = _teklifRepo.Getir(t => t.Id == teklifId);
                if (teklif != null)
                {
                    teklif.TeklifDurumId = 9; // Sözleşmede
                    teklif.GuncellenmeTarihi = DateTime.Now;
                    _teklifRepo.Guncelle(teklif);
                }

                return Json(new
                {
                    success = true,
                    message = "Sözleşme başarıyla kaydedildi.",
                    dokumanNo = sozlesme.DokumanNo,
                    sozlesmeId = sozlesme.Id
                });
            }
            catch (Exception ex)
            {
                // Hata mesajını daha detaylı logla
                Console.WriteLine($"Sözleşme kaydetme hatası: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = "Kayıt sırasında hata oluştu: " + ex.Message });
            }
        }

        private string GenerateDokumanNo()
        {
            return $"SOZ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        public IActionResult SozlesmePdf(int sozlesmeId)
        {
            UYBRepository uYBRepository = new UYBRepository();
            UYB uYB = uYBRepository.Getir(x => x.Durumu == 1);

            List<string> join = new List<string>();
            join.Add("SozlesmeDurumu");
            join.Add("Teklif");
            join.Add("Teklif.LisansTip");
            join.Add("Teklif.Detaylar");
            join.Add("Musteri");
            join.Add("Musteri.MusteriTipi");
            join.Add("Entegrator");
            MusteriSozlesme musteriSozlesme = _sozlesmeRepo.Getir(x => x.Id == sozlesmeId, join);

            SozlesmeRapor sozlesmeRapor = new SozlesmeRapor();
            sozlesmeRapor.Adi = musteriSozlesme.Adi;
            sozlesmeRapor.PaketAdi = musteriSozlesme.Teklif.Detaylar.FirstOrDefault()?.PaketGrupAdi;
            sozlesmeRapor.PaketIcerigi = string.Join(", ",
                musteriSozlesme.Teklif.Detaylar.Select(x => x.ItemAdi));
            sozlesmeRapor.Kampanya = musteriSozlesme.Teklif.Detaylar.FirstOrDefault()?.KampanyaBaslik ?? "Kampanya yok";
            sozlesmeRapor.Soyadi = musteriSozlesme.Soyadi;
            sozlesmeRapor.Unvan = musteriSozlesme.TicariUnvan;
            sozlesmeRapor.Adres = musteriSozlesme.Adres1 + " " + musteriSozlesme.Adres2;
            sozlesmeRapor.IletisimNo = musteriSozlesme.Telefon;
            sozlesmeRapor.CepTelefonu = musteriSozlesme.CepTelefon;
            sozlesmeRapor.Email = musteriSozlesme.Email;
            sozlesmeRapor.VergiDairesi = musteriSozlesme.VergiDairesi;
            sozlesmeRapor.VergiNo = musteriSozlesme.VergiNo;
            sozlesmeRapor.MusteriTipi = musteriSozlesme.Musteri.MusteriTipi.Adi;
            sozlesmeRapor.Sms = musteriSozlesme.SmsBilgilendirme ? "X" : "";
            sozlesmeRapor.Eposta = musteriSozlesme.EmailBilgilendirme ? "X" : "";
            sozlesmeRapor.Telefon = musteriSozlesme.TelefonBilgilendirme ? "X" : "";
            sozlesmeRapor.Ek1 = musteriSozlesme.YayinTarihi.ToString("dd.MM.yyyy");
            sozlesmeRapor.PaketFiyati = musteriSozlesme.Teklif.ToplamListeFiyat.ToString("N2");
            sozlesmeRapor.Ek2 = musteriSozlesme.Teklif.TeklifNo;
            sozlesmeRapor.Toplam = musteriSozlesme.Teklif.NetToplam.ToString("N2");
            sozlesmeRapor.LisansTipi = musteriSozlesme.Teklif.LisansTip?.Adi;
            sozlesmeRapor.YaziIle = ParaYaziyaCevir(musteriSozlesme.Teklif.NetToplam);
            sozlesmeRapor.Ek3 = musteriSozlesme.Entegrator.Adi;
            sozlesmeRapor.UYBOran = uYB.Oran.ToString();
            sozlesmeRapor.UYBTutar = musteriSozlesme.YillikBakim.ToString("N2");
            string rdlcPath = Path.Combine(_webHostEnviroment.WebRootPath, "Raporlar", "Sozlesme.rdlc");
            if (!System.IO.File.Exists(rdlcPath))
            {
                return BadRequest($"RDLC dosyası bulunamadı: {rdlcPath}");
            }
            LocalReport localReport = new LocalReport(rdlcPath);
            localReport.AddDataSource("DataSetSozlesme", new List<SozlesmeRapor> { sozlesmeRapor });
            ReportResult result = localReport.Execute(RenderType.Pdf);
            Response.Headers["Content-Disposition"] = $"inline; filename={musteriSozlesme.Teklif.TeklifNo}.pdf";
            return File(result.MainStream, "application/pdf");
        }

        public static string ParaYaziyaCevir(decimal tutar)
        {
            if (tutar == 0)
                return "Sıfır Lira";

            decimal tamKisim = Math.Floor(tutar);
            int kuruş = (int)((tutar - tamKisim) * 100);

            string yazi = SayiyiYaziyaCevir((long)tamKisim) + " Lira";

            if (kuruş > 0)
                yazi += " " + SayiyiYaziyaCevir(kuruş) + " Kuruş";

            return yazi;
        }

        private static string SayiyiYaziyaCevir(long sayi)
        {
            string[] birler = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
            string[] onlar = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan" };
            string[] binler = { "", "Bin", "Milyon", "Milyar", "Trilyon" };

            if (sayi == 0)
                return "Sıfır";

            int grupIndex = 0;
            string yazi = "";

            while (sayi > 0)
            {
                int grup = (int)(sayi % 1000);

                if (grup > 0)
                {
                    string grupYazi = "";

                    int yüzler = grup / 100;
                    int onlarBas = (grup % 100) / 10;
                    int birlerBas = grup % 10;

                    if (yüzler == 1)
                        grupYazi += "Yüz";
                    else if (yüzler > 1)
                        grupYazi += birler[yüzler] + " Yüz";

                    if (onlarBas > 0)
                        grupYazi += " " + onlar[onlarBas];

                    if (birlerBas > 0)
                        grupYazi += " " + birler[birlerBas];

                    if (grupIndex > 0)
                    {
                        if (grup == 1 && grupIndex == 1)
                            grupYazi = "Bin";
                        else
                            grupYazi += " " + binler[grupIndex];
                    }

                    yazi = grupYazi.Trim() + " " + yazi.Trim();
                }

                sayi /= 1000;
                grupIndex++;
            }

            return yazi.Trim();
        }

        [HttpPost]
        public IActionResult DurumDegistir(int id, int durumId, int? nedenId = null, string? ertelenmeNedeni = null)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu. Lütfen tekrar giriş yapın." });

                Teklif teklif = _teklifRepo.Getir(x => x.Id == id && x.Aktif == true);
                if (teklif == null)
                    return Json(new { success = false, message = "Teklif bulunamadı." });

                // Eski durumu kaydet
                int eskiDurumId = teklif.TeklifDurumId ?? 0;

                // Durum ID kontrolü - 0 (Belirtilmemiş) değerini kabul etme
                if (durumId == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Lütfen geçerli bir durum seçin."
                    });
                }

                // Durum var mı kontrol et
                TeklifDurum durum = _teklifDurumRepo.Getir(x => x.Id == durumId && x.Durumu == 1);
                if (durum == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Seçilen durum bulunamadı veya aktif değil."
                    });
                }

                // Durum 5 (Kaybedildi/Reddedildi) ise neden kontrolü
                if (durumId == 5)
                {
                    if (!nedenId.HasValue || nedenId.Value == 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Reddetme durumu için neden seçmelisiniz."
                        });
                    }

                    Nedenler neden = _nedenlerRepo.Getir(x => x.Id == nedenId.Value && x.Durumu == 1);
                    if (neden == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Seçilen neden bulunamadı veya aktif değil."
                        });
                    }

                    teklif.NedenlerId = nedenId;
                    teklif.ErtelenmeNedeni = null; // Ertelenme nedenini temizle
                }
                // Durum 7 (Ertelendi) ise ertelenme nedeni kontrolü
                else if (durumId == 7)
                {
                    if (string.IsNullOrWhiteSpace(ertelenmeNedeni))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Ertelenme durumu için neden girmelisiniz."
                        });
                    }

                    teklif.ErtelenmeNedeni = ertelenmeNedeni.Trim();
                    teklif.NedenlerId = null; // Reddetme nedenini temizle
                }
                else
                {
                    // Diğer durumlarda neden ve ertelenme nedenini temizle
                    teklif.NedenlerId = null;
                    teklif.ErtelenmeNedeni = null;
                }

                // Durum ID'sini güncelle
                teklif.TeklifDurumId = durumId;

                // Onay durumları için onay tarihi güncelle
                if ( durumId == 6) // Onaylandı durumları
                {
                    MusteriSozlesme sozles = _sozlesmeRepo.Getir(x => x.TeklifId == id);
                    if(sozles != null)
                    {
                        sozles.SozlesmeDurumuId = 7;
                        _sozlesmeRepo.Guncelle(sozles);

                    }
              
                    teklif.OnaylandiMi = true;
                    teklif.OnayTarihi = DateTime.Now;
                }
                else
                {
                    teklif.OnaylandiMi = false;
                    teklif.OnayTarihi = null;
                }

                teklif.GuncellenmeTarihi = DateTime.Now;

                // Güncelle
                _teklifRepo.Guncelle(teklif);

                string yeniDurum = durum.Adi;
                string nedenAdi = nedenId.HasValue && nedenId.Value > 0
                    ? _nedenlerRepo.Getir(x => x.Id == nedenId.Value)?.Adi ?? ""
                    : "";
                string ertelenmeNedeniText = teklif.ErtelenmeNedeni ?? "";

                return Json(new
                {
                    success = true,
                    message = $"Durum '{yeniDurum}' olarak güncellendi.",
                    yeniDurum = yeniDurum,
                    nedenAdi = nedenAdi,
                    ertelenmeNedeni = ertelenmeNedeniText,
                    durumId = durumId
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Hata oluştu: " + ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetirNedenler()
        {
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Yetkiniz bulunmamaktadır." });
                }

                var nedenler = _nedenlerRepo.GetirList(x => x.Durumu == 1)
                    .OrderBy(x => x.Adi)
                    .Select(n => new
                    {
                        id = n.Id,
                        adi = n.Adi,
                        eklenmeTarihi = n.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm")
                    })
                    .ToList();

                return Json(new { success = true, nedenler = nedenler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirTeklifDetay(int id)
        {
            try
            {
                Teklif teklif = _teklifRepo.Getir(
                    x => x.Id == id && x.Aktif == true,
                    new List<string> { "Musteri", "LisansTip", "Detaylar", "TeklifDurum", "Nedenler" });

                if (teklif == null)
                    return Json(new { success = false, message = "Teklif bulunamadı." });

                var detaylar = teklif.Detaylar?
                    .Where(x => x.Durumu == 1)
                    .OrderBy(x => x.SiraNo)
                    .Select(d => new
                    {
                        id = d.Id,
                        siraNo = d.SiraNo,
                        tip = d.Tip,
                        itemAdi = d.ItemAdi,
                        paketGrupAdi = d.PaketGrupAdi,
                        listeFiyati = d.ListeFiyati,
                        kampanyaFiyati = d.KampanyaFiyati,
                        kampanyaIndirimYuzdesi = d.KampanyaIndirimYuzdesi,
                        kampanyaBaslik = d.KampanyaBaslik,
                        bireyselIndirimYuzdesi = d.BireyselIndirimYuzdesi,
                        grupIndirimYuzdesi = d.GrupIndirimYuzdesi,
                        miktar = d.Miktar,
                        miktarBazliEkOranYuzde = d.MiktarBazliEkOranYuzde,
                        birimFiyatNet = d.BirimFiyatNet,
                        satirToplamNet = d.SatirToplamNet,
                        bagimsizModulMu = d.BagimsizModulMu
                    })
                    .ToList();

                var teklifBilgileri = new
                {
                    id = teklif.Id,
                    teklifNo = teklif.TeklifNo,
                    musteriAdi = (teklif.Musteri?.Ad + " " + teklif.Musteri?.Soyad).Trim(),
                    musteriTelefon = teklif.Musteri?.Telefon,
                    musteriEmail = teklif.Musteri?.Email,
                    lisansTipi = teklif.LisansTip?.Adi,
                    aciklama = teklif.Aciklama,
                    grupIndirimOrani = teklif.GrupIndirimOrani,
                    toplamListeFiyat = teklif.ToplamListeFiyat,
                    toplamIndirim = teklif.ToplamIndirim,
                    miktarBazliEkTutar = teklif.MiktarBazliEkTutar,
                    kdvTutari = teklif.KdvTutari,
                    araToplam = teklif.AraToplam,
                    netToplam = teklif.NetToplam,
                    olusturmaTarihi = teklif.EklenmeTarihi?.ToString("dd.MM.yyyy HH:mm"),
                    gecerlilikTarihi = teklif.GecerlilikTarihi?.ToString("dd.MM.yyyy HH:mm"),
                    durumId = teklif.TeklifDurumId ?? 0,
                    durumAdi = teklif.TeklifDurum?.Adi ?? "Belirtilmemiş",
                    nedenId = teklif.NedenlerId ?? 0,
                    nedenAdi = teklif.Nedenler?.Adi ?? "",
                    ertelenmeNedeni = teklif.ErtelenmeNedeni ?? ""
                };

                return Json(new
                {
                    success = true,
                    teklif = teklifBilgileri,
                    detaylar = detaylar
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SilTeklif(int id)
        {
            MusteriSozlesmeRepository _musteriSozlesmeRepo = new MusteriSozlesmeRepository();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süresi doldu. Lütfen tekrar giriş yapın." });

                Teklif teklif = _teklifRepo.Getir(x => x.Id == id && x.Aktif == true);
                if (teklif == null)
                    return Json(new { success = false, message = "Teklif bulunamadı." });

                // YENİ KONTROL: Bu teklife ait aktif sözleşme var mı?
                MusteriSozlesme aktifSozlesme = _musteriSozlesmeRepo.Getir(x => x.TeklifId == id && x.Durumu == 1);
                if (aktifSozlesme != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bu teklife ait aktif bir sözleşme bulunduğu için teklif silinemez."
                    });
                }

                // Aktif sözleşme yoksa silme işlemine devam et
                teklif.Aktif = false;
                teklif.GuncellenmeTarihi = DateTime.Now;

                // Detayları da pasif yap
                List<TeklifDetay> detaylar = _teklifDetayRepo.GetirList(x => x.TeklifId == id && x.Durumu == 1);
                foreach (TeklifDetay detay in detaylar)
                {
                    detay.Durumu = 0;
                    detay.GuncellenmeTarihi = DateTime.Now;
                    _teklifDetayRepo.Guncelle(detay);
                }

                _teklifRepo.Guncelle(teklif);

                return Json(new
                {
                    success = true,
                    message = $"{teklif.TeklifNo} numaralı teklif başarıyla silindi."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında hata: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult CheckAktifSozlesme(int teklifId)
        {
            try
            {
                // Teklife ait aktif sözleşme var mı?
                MusteriSozlesme aktifSozlesme = _sozlesmeRepo.Getir(x => x.TeklifId == teklifId && x.Durumu == 1);

                return Json(new
                {
                    success = true,
                    aktifSozlesmeVar = aktifSozlesme != null,
                    message = aktifSozlesme != null
                        ? "Bu teklife ait aktif bir sözleşme zaten mevcut."
                        : "Bu teklif için sözleşme oluşturulabilir."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    aktifSozlesmeVar = false,
                    message = "Kontrol sırasında hata oluştu: " + ex.Message
                });
            }
        }
        [HttpGet]
        public IActionResult TeklifDetay(int id)
        {
            try
            {
                Teklif teklif = _teklifRepo.Getir(
                    x => x.Id == id && x.Aktif == true,
                    new List<string> { "Musteri", "LisansTip", "Detaylar", "TeklifDurum", "Nedenler" });

                if (teklif == null)
                {
                    TempData["Hata"] = "Teklif bulunamadı.";
                    return RedirectToAction("Index");
                }

                ViewBag.Teklif = teklif;
                ViewBag.TeklifDurumlari = _teklifDurumRepo.GetirList(x => x.Durumu == 1).ToList();
                LoadCommonData();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Teklif detayları yüklenirken hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}