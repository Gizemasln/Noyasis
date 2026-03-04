using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminButtonController : AdminBaseController
    {
        private readonly ButtonPermissionRepository _repo;
        private readonly IWebHostEnvironment _env;

        public AdminButtonController(IWebHostEnvironment env)
        {
            _repo = new ButtonPermissionRepository();
            _env = env;
        }

        public IActionResult Index(string kullaniciTipi = "Admin")
        {
           

            // 1. ADIM: Seçili kullanıcı tipinin menü izinlerini getir
            var menuIzinRepo = new MenuIzinRepository();
            var kullaniciMenuIzinleri = menuIzinRepo.KullaniciTipineGoreGetir(kullaniciTipi);

            // Menü izni olan sayfaların URL'lerinden controller adlarını çıkar
            var izinliControllerlar = kullaniciMenuIzinleri
                .Select(m => MenuUrlToController(m.MenuUrl))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToHashSet();

            // 2. ADIM: Tüm butonları tara
            var detectedButtons = ScanAllButtons();

            // 3. ADIM: Sadece izinli controller'lara ait butonları filtrele
            var filtrelenmisButonlar = detectedButtons
                .Where(b => izinliControllerlar.Contains(b.Controller))
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .OrderBy(b => b.Controller)
                .ThenBy(b => b.Action)
                .ToList();

            var model = new AdminButtonViewModel
            {
                DetectedButtons = filtrelenmisButonlar, // SADECE İZİNLİ CONTROLLER'LARIN BUTONLARI
                SeciliKullaniciTipi = kullaniciTipi,
                GruplanmisButonlar = filtrelenmisButonlar
                    .GroupBy(b => b.Controller)
                    .ToDictionary(g => g.Key, g => g.ToList()), // SADECE İZİNLİ CONTROLLER'LAR
                IzinliControllerlar = izinliControllerlar.ToList() // View'e gönder (isteğe bağlı)
            };

            // Mevcut buton izinlerini getir (değişiklik yok)
            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);
            var izinDict = mevcutIzinler.ToDictionary(
                i => $"{i.SayfaAdi}|{i.ButonAksiyonu}",
                i => i.IzınVar
            );

            model.ButtonPermissions = new Dictionary<string, bool>();
            foreach (var button in filtrelenmisButonlar)
            {
                var key = $"{button.Controller}|{button.Action}";
                model.ButtonPermissions[key] = izinDict.TryGetValue(key, out var izin) ? izin : false;
            }

            var userInfo = GetCurrentUserInfo();
            ViewBag.CurrentUserType = userInfo.tip;
            ViewBag.CurrentUserId = userInfo.id;

            // İstatistik için ViewBag'e ekleyelim
            ViewBag.ToplamIzinliSayfa = izinliControllerlar.Count;
            ViewBag.ToplamButon = filtrelenmisButonlar.Count;

            return View(model);
        }

        // YARDIMCI METOD: MenuUrl'den Controller adını çıkar
        private string MenuUrlToController(string menuUrl)
        {
            if (string.IsNullOrEmpty(menuUrl)) return null;

            // # ile başlayanlar (grup başlıkları) controller değildir
            if (menuUrl.StartsWith("#")) return null;

            // /AdminMusteri gibi URL'lerden controller adını al
            // "/" işaretini kaldır ve varsa parametreleri temizle
            var controller = menuUrl.TrimStart('/');

            // Eğer slash varsa (örn: /Admin/Musteri) ilk parçayı al
            if (controller.Contains('/'))
            {
                controller = controller.Split('/')[0];
            }

            // Sadece harf ve rakam içeren kısmı al
            controller = Regex.Replace(controller, @"[^a-zA-Z0-9]", "");

            return controller;
        }

        [HttpPost]
        public IActionResult Kaydet(string kullaniciTipi, List<string> seciliIzinler)
        {
          

            // 1. ADIM: Seçili kullanıcı tipinin menü izinlerini getir
            var menuIzinRepo = new MenuIzinRepository();
            var kullaniciMenuIzinleri = menuIzinRepo.KullaniciTipineGoreGetir(kullaniciTipi);

            var izinliControllerlar = kullaniciMenuIzinleri
                .Select(m => MenuUrlToController(m.MenuUrl))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToHashSet();

            // 2. ADIM: Tüm butonları tara ve filtrele
            var detectedButtons = ScanAllButtons();
            var filtrelenmisButonlar = detectedButtons
                .Where(b => izinliControllerlar.Contains(b.Controller))
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            // 3. ADIM: Seçili izinleri kontrol et (sadece izinli controller'lar için)
            var yeniIzinler = filtrelenmisButonlar.Select(button => new ButtonPermission
            {
                KullaniciTipi = kullaniciTipi,
                SayfaAdi = button.Controller,
                ButonAksiyonu = button.Action,
                IzınVar = seciliIzinler?.Contains($"{button.Controller}|{button.Action}") ?? false,
                Aciklama = $"{GetSayfaAdi(button.Controller)} sayfasında {GetButtonAdi(button.Action)} işlemi"
            }).ToList();

            _repo.TemizleVeEkle(kullaniciTipi, yeniIzinler);

            TempData["Success"] = $"{kullaniciTipi} buton yetkileri kaydedildi. " ;
            return RedirectToAction("Index", new { kullaniciTipi });
        }

        [HttpPost]
        public IActionResult TopluYetkilendir(string kullaniciTipi, string aksiyonTipi, bool durum)
        {


            // 1. ADIM: Seçili kullanıcı tipinin menü izinlerini getir
            var menuIzinRepo = new MenuIzinRepository();
            var kullaniciMenuIzinleri = menuIzinRepo.KullaniciTipineGoreGetir(kullaniciTipi);

            var izinliControllerlar = kullaniciMenuIzinleri
                .Select(m => MenuUrlToController(m.MenuUrl))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToHashSet();

            // 2. ADIM: Tüm butonları tara ve filtrele
            var detectedButtons = ScanAllButtons();
            var filtrelenmisButonlar = detectedButtons
                .Where(b => izinliControllerlar.Contains(b.Controller))
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);
            var mevcutDict = mevcutIzinler.ToDictionary(
                i => $"{i.SayfaAdi}|{i.ButonAksiyonu}",
                i => i.IzınVar
            );

            var yeniIzinler = filtrelenmisButonlar.Select(button =>
            {
                var key = $"{button.Controller}|{button.Action}";
                bool izinVar = mevcutDict.TryGetValue(key, out var val) ? val : false;

                if (aksiyonTipi == "Tumu" || button.Action.Equals(aksiyonTipi, StringComparison.OrdinalIgnoreCase))
                {
                    izinVar = durum;
                }

                return new ButtonPermission
                {
                    KullaniciTipi = kullaniciTipi,
                    SayfaAdi = button.Controller,
                    ButonAksiyonu = button.Action,
                    IzınVar = izinVar,
                    Aciklama = $"{GetSayfaAdi(button.Controller)} sayfasında {GetButtonAdi(button.Action)} işlemi"
                };
            }).ToList();

            _repo.TemizleVeEkle(kullaniciTipi, yeniIzinler);

            var aksiyonAdi = aksiyonTipi == "Tumu" ? "Tüm butonlar" : GetButtonAdi(aksiyonTipi) + " butonları";
            var durumText = durum ? "yetkilendirildi" : "yetkisi kaldırıldı";
            TempData["Success"] = $"{kullaniciTipi} için {aksiyonAdi} {durumText}. " +
                                 $"Menü izni olan {yeniIzinler.Count} buton güncellendi.";

            return RedirectToAction("Index", new { kullaniciTipi });
        }

        [HttpGet]
        public IActionResult TumIzinleriGetirJson()
        {
            var tumIzinler = _repo.TumIzinleriGetir();

            if (tumIzinler == null)
            {
                return Json(new Dictionary<string, Dictionary<string, bool>>());
            }

            return Json(tumIzinler);
        }

        private List<DetectedButton> ScanAllButtons()
        {
            var buttons = new List<DetectedButton>();

            var allButtons = new Dictionary<string, List<string>>
            {
                ["AdminAnaSayfa"] = new List<string> {
    "bayi-duyuru-detay",      // Bayi duyuru detay butonu
    "web-duyuru-detay",       // Web duyuru detay butonu  
    "kampanya-detay",         // Kampanya detay butonu

},
                ["AdminAnaSayfaBannerResim"] = new List<string> {
            "ekle",
            "duzenle",
            "sil"

        },
                ["AdminAnaSayfaSlider"] = new List<string> {
            "ekle",
            "duzenle",
            "sil"

        },
                ["AdminARGEDurum"] = new List<string> {
            "ekle",
            "duzenle",
            "sil"

        },
                ["AdminBayi"] = new List<string> {
            "ekle",                    // Yeni bayi ekle butonu
            "duzenle",                  // Düzenle butonu
            "sil",                      // Sil butonu
            "teklifler",                     // Detay butonu
            "sozlesmeler",
            "detay"
        },
                ["AdminMusteri"] = new List<string> {
                    "ekle",
                    "detay",
                    "duzenle",
                    "sil",
                    "teklifler",
                     "sozlesmeler"

                },
                ["AdminBayiDuyuru"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",

                },
                ["AdminSozlesmeDurumu"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",

                },
                
                ["AdminDuyuru"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"


                },
                ["AdminKampanya"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "paketgoster",
                    "grupgoster"
                },
                ["AdminUrun"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminKategori"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                 
                },
                ["AdminTeklif"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "pdf",
                    "sozlesmeolustur",
                    "revize"
                },
                ["AdminPaket"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "oranlarigor",
                    "sabitcarpim",
                    "oranekle"
                },
                ["AdminSertifika"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "goruntule"
                },
                ["AdminMakale"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "detay",
                    "sil"
                },
                ["AdminIstekOneri"] = new List<string> {
                  "sil",
                    "detay",
                     "cevapduzenle",
                     "cevapyaz"

                },
            
                ["Arge"] = new List<string> {
                   
                    "duzenle",
                    "sil"
                  
                },
             
                ["AdminButton"] = new List<string> {
                    "yetki-ver",
                    "yetki-kaldir",
                    "toplu-yetkilendir",
                    "kaydet"
                },
                ["AdminGenelAydinlatma"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminHakkimizdaBilgileri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminHakkimizdaFotograf"] = new List<string> {
                    "ekle",

                    "sil"
                },
                ["AdminIK"] = new List<string> {
                      "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminIletisimBilgileri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminKVKK"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminLisansDurumu"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminLisansTip"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminLokasyon"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminMusteriSozlesme"] = new List<string> {
                    "detay",
                    "sil",
                    "pdfgor"
                },
                ["AdminMusteriTipi"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminPaketBaglama"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminPaketGrup"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminSSS"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminTeklifDurum"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminUrunGaleri"] = new List<string> {
                    "ekle",
                 
                    "sil"
                },
                ["AdminUYB"] = new List<string> {
                    "duzenle"
                },
                ["AdminYetki"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yetkilendir"
                },
                ["AdminArgeHata"] = new List<string> {

                    "sil",
                    "detay"

                },
                ["AdminBayiSozlesme"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"

                },
                ["AdminBayiSozlesmeKriteri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminBirim"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminEntegrator"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminIstekOneriDurum"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminKDV"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminDepartman"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminNedenler"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminNeredenDuydu"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil"
                },
                ["AdminTeklifler"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "onayla",
                    "onaykaldir"
                },
                ["IstekOneri"] = new List<string> {
                   
                    "duzenle",
                    "sil"
                },
                ["AdminTeklifVer"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                }
            };

            foreach (var controller in allButtons)
            {
                foreach (var action in controller.Value)
                {
                    buttons.Add(new DetectedButton
                    {
                        Controller = controller.Key,
                        Action = action
                    });
                }
            }

            return buttons;
        }

        private string GetSayfaAdi(string controller)
        {
            return controller switch
            {
                "AdminMusteri" => "Müşteriler",
                "AdminAnaSayfa" => "Ana Sayfa",
                "AdminAnaSayfaBannerResim" => "Ana Sayfa Banner",
                "AdminBayi" => "Bayiler",
                "AdminUrun" => "Ürünler",
                "AdminKategori" => "Kategoriler",
                "AdminTeklif" => "Teklifler",
                "AdminKampanya" => "Kampanyalar",
                "AdminDuyuru" => "Duyurular",
                "AdminMakale" => "Makaleler",
                "AdminSertifika" => "Sertifikalar",
                "AdminPaket" => "Modüller",
                "AdminIstekOneri" => "Admin İstek/Öneri",
                "AdminMenuIzin" => "Menü Yetkilendirme",
                "AdminButton" => "Buton Yetkilendirme",
                "AdminGenelAydinlatma" => "Genel Aydınlatma Metni",
                "AdminHakkimizdaBilgileri" => "Hakkımızda",
                "AdminHakkimizdaFotograf" => "Hakkımızda Görsel",
                "AdminIK" => "Başvuru Formları",
                "AdminIletisimBilgileri" => "İletişim Bilgileri",
                "AdminKVKK" => "KVKK Metni",
                "AdminLisansDurumu" => "Lisans Durumu",
                "AdminLisansTip" => "Lisans Tipleri",
                "AdminLokasyon" => "Lokasyonlar",
                "AdminMusteriSozlesme" => "Müşteri Sözleşmesi",
                "AdminMusteriTipi" => "Müşteri Tipleri",
                "AdminPaketBaglama" => "Paket Bağlama",
                "AdminPaketGrup" => "Paket Grupları",
                "AdminSSS" => "Sıkça Sorulan Sorular",
                "AdminTeklifDurum" => "Teklif Durumları",
                "AdminUrunGaleri" => "Ürün Görselleri",
                "AdminUYB" => "Genel Ayarlar",
                "AdminYetki" => "Yetkilendirme",
                "AdminBayiDuyuru" => "Bayi Duyurular",
                "AdminARGEDurum" => "Arge Durum",
                "AdminArgeHata" => "Admin ARGE/Hata",
                "AdminBayiSozlesme" => "Bayi Sözleşmesi",
                "AdminBayiSozlesmeKriteri" => "Bayi Sözleşme Kriteri",
                "AdminBirim" => "Birimler",
                "AdminEntegrator" => "Entegratörler",
                "AdminIstekOneriDurum" => "İstek Öneri Durum",
                "AdminKDV" => "KDV Oranları",
                "AdminDepartman" => "Departman Tipleri",
                "AdminNedenler" => "Teklif Kaybedilme Nedenleri",
                "AdminNeredenDuydu" => "Nereden Duydu",
                "AdminTeklifler" => "Ürün Teklifleri",
                "AdminTeklifVer" => "Teklif Oluştur",
                "Arge" => "ARGE/Hata",
                "IstekOneri" => "Istek Öneri",
                _ => controller
            };
        }

        private string GetButtonAdi(string action)
        {
            return action.ToLowerInvariant() switch
            {
                "ekle" or "create" or "add" or "yeni" => "Ekle",
                "duzenle" or "edit" or "guncelle" or "update" => "Düzenle",
                "sil" or "delete" or "remove" => "Sil",
                "detay" or "view" or "goruntule" or "görüntüle" => "Detay",
                "bayi-duyuru-detay" => "Bayi Duyuru Detay",
                "web-duyuru-detay" => "Web Duyuru Detay",
                "kampanya-detay" => "Kampanya Detay",
                "aktif-pasif" => "Aktif/Pasif",
                "sira-guncelle" => "Sıra Güncelle",
                "yayinla" => "Yayınla",
                "yayindan-kaldir" => "Yayından Kaldır",
                "onayla" => "Onayla",
                "reddet" => "Reddet",
                "pdf-indir" => "PDF İndir",
                "excel-aktar" => "Excel Aktar",
                "kopyala" => "Kopyala",
                "stok-guncelle" => "Stok Güncelle",
                "fiyat-guncelle" => "Fiyat Güncelle",
                "cevapla" => "Cevapla",
                "arsivle" => "Arşivle",
                "cozum-ekle" => "Çözüm Ekle",
                "yetkilendir" => "Yetkilendir",
                "lisansla" => "Lisansla",
                "yetki-ver" => "Yetki Ver",
                "yetki-kaldir" => "Yetki Kaldır",
                "toplu-yetkilendir" => "Toplu Yetkilendir",
                "kaydet" => "Kaydet",
                "anasayfa-yonet" => "Ana Sayfa Yönet",
                "slider-yonet" => "Slider Yönet",
                _ => action
            };
        }
    }
}