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
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

            var detectedButtons = ScanAllButtons();
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .OrderBy(b => b.Controller)
                .ThenBy(b => b.Action)
                .ToList();

            var model = new AdminButtonViewModel
            {
                DetectedButtons = uniqueButtons,
                SeciliKullaniciTipi = kullaniciTipi,
                GruplanmisButonlar = uniqueButtons
                    .GroupBy(b => b.Controller)
                    .ToDictionary(g => g.Key, g => g.ToList())
            };

            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);
            var izinDict = mevcutIzinler.ToDictionary(
                i => $"{i.SayfaAdi}|{i.ButonAksiyonu}",
                i => i.IzınVar
            );

            model.ButtonPermissions = new Dictionary<string, bool>();
            foreach (var button in uniqueButtons)
            {
                var key = $"{button.Controller}|{button.Action}";
                model.ButtonPermissions[key] = izinDict.TryGetValue(key, out var izin) ? izin : false;
            }

            var userInfo = GetCurrentUserInfo();
            ViewBag.CurrentUserType = userInfo.tip;
            ViewBag.CurrentUserId = userInfo.id;

            return View(model);
        }

        [HttpPost]
        public IActionResult Kaydet(string kullaniciTipi, List<string> seciliIzinler)
        {
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

            var detectedButtons = ScanAllButtons();
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            var yeniIzinler = uniqueButtons.Select(button => new ButtonPermission
            {
                KullaniciTipi = kullaniciTipi,
                SayfaAdi = button.Controller,
                ButonAksiyonu = button.Action,
                IzınVar = seciliIzinler?.Contains($"{button.Controller}|{button.Action}") ?? false,
                Aciklama = $"{GetSayfaAdi(button.Controller)} sayfasında {GetButtonAdi(button.Action)} işlemi"
            }).ToList();

            _repo.TemizleVeEkle(kullaniciTipi, yeniIzinler);

            TempData["Success"] = $"{kullaniciTipi} buton yetkileri kaydedildi. Toplam {yeniIzinler.Count} buton tespit edildi.";
            return RedirectToAction("Index", new { kullaniciTipi });
        }

        [HttpPost]
        public IActionResult TopluYetkilendir(string kullaniciTipi, string aksiyonTipi, bool durum)
        {
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

            var detectedButtons = ScanAllButtons();
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);
            var mevcutDict = mevcutIzinler.ToDictionary(
                i => $"{i.SayfaAdi}|{i.ButonAksiyonu}",
                i => i.IzınVar
            );

            var yeniIzinler = uniqueButtons.Select(button =>
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
            TempData["Success"] = $"{kullaniciTipi} için {aksiyonAdi} {durumText}. Toplam {yeniIzinler.Count} buton güncellendi.";

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
                ["AdminBayi"] = new List<string> {
            "ekle",                    // Yeni bayi ekle butonu
            "duzenle",                  // Düzenle butonu
            "sil",                      // Sil butonu
            "teklifler",                     // Detay butonu
            "sozlesmeler"
        },
                ["AdminBayiDuyuru"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yayinla",
                    "yayindan-kaldir"
                },
                ["AdminDuyuru"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yayinla",
                    "yayindan-kaldir"
                },
                ["AdminKampanya"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "aktiflestir",
                    "durdur"
                },
                ["AdminUrun"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "kopyala",
                    "stok-guncelle",
                    "fiyat-guncelle"
                },
                ["AdminKategori"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "sira-guncelle"
                },
                ["AdminTeklif"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "onayla",
                    "reddet",
                    "pdf-indir",
                    "excel-aktar"
                },
                ["AdminPaket"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "fiyat-guncelle",
                    "aktif-pasif"
                },
                ["AdminSertifika"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminMakale"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yayinla"
                },
                ["AdminIstekOneri"] = new List<string> {
                    "detay",
                    "cevapla",
                    "arsivle",
                    "durum-guncelle"
                },
                ["AdminArge"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "cozum-ekle",
                    "durum-guncelle"
                },
                ["AdminMenuIzin"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yetkilendir"
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
                    "sil",
                    "detay",
                    "yayinla"
                },
                ["AdminHakkimizdaBilgileri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminHakkimizdaFotograf"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "sira-guncelle"
                },
                ["AdminIK"] = new List<string> {
                    "detay",
                    "indir",
                    "arsivle",
                    "durum-guncelle"
                },
                ["AdminIletisimBilgileri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminKVKK"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yayinla"
                },
                ["AdminLisansDurumu"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminLisansTip"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminLokasyon"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminMusteriSozlesme"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "onayla",
                    "reddet"
                },
                ["AdminMusteriTipi"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminPaketBaglama"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminPaketGrup"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminSSS"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "sira-guncelle"
                },
                ["AdminTeklifDurum"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "sira-guncelle"
                },
                ["AdminUrunGaleri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "sira-guncelle"
                },
                ["AdminUYB"] = new List<string> {
                    "duzenle",
                    "detay",
                    "kaydet"
                },
                ["AdminYetki"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "yetkilendir"
                },
                ["AdminARGEDurum"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminArgeHata"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "cozum-ekle"
                },
                ["AdminBayiSozlesme"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay",
                    "onayla"
                },
                ["AdminBayiSozlesmeKriteri"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminBirim"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminEntegrator"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminIstekOneriDurum"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminKDV"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminDepartman"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminNedenler"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminNeredenDuydu"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
                },
                ["AdminTeklifler"] = new List<string> {
                    "ekle",
                    "duzenle",
                    "sil",
                    "detay"
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
                "AdminIstekOneri" => "İstek/Öneri",
                "AdminArge" => "ARGE/Hata",
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
                "AdminArgeHata" => "ARGE/Hata",
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