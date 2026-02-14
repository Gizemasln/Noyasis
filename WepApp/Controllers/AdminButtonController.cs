using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using WepApp.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace WebApp.Controllers
{
    public class AdminButtonController : Controller
    {
        private readonly ButtonPermissionRepository _repo;
        private readonly ButtonScannerService _scanner;

        public AdminButtonController(IWebHostEnvironment env)
        {
            _repo = new ButtonPermissionRepository();
            _scanner = new ButtonScannerService(env.WebRootPath);
        }

        public IActionResult Index(string kullaniciTipi = "Admin")
        {
            // 1. Mevcut view'lerdeki butonları tara
            var detectedButtons = _scanner.ScanAllViews();

            // 2. Benzersiz controller+action kombinasyonlarını al
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .OrderBy(b => b.Controller)
                .ThenBy(b => b.Action)
                .ToList();

            // 3. ViewModel'i hazırla
            var model = new AdminButtonViewModel
            {
                DetectedButtons = uniqueButtons,
                SeciliKullaniciTipi = kullaniciTipi,
                GruplanmisButonlar = uniqueButtons
                    .GroupBy(b => b.Controller)
                    .ToDictionary(g => g.Key, g => g.ToList())
            };

            // 4. Mevcut izinleri yükle
            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);

            // 5. Her buton için izin durumunu belirle
            model.ButtonPermissions = new Dictionary<string, bool>();
            foreach (var button in uniqueButtons)
            {
                var key = $"{button.Controller}|{button.Action}";
                var izin = mevcutIzinler.FirstOrDefault(i =>
                    i.SayfaAdi == button.Controller &&
                    i.ButonAksiyonu == button.Action);

                model.ButtonPermissions[key] = izin?.IzınVar ?? false;
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Kaydet(string kullaniciTipi, List<string> seciliIzinler)
        {
            // Tüm butonları tara
            var detectedButtons = _scanner.ScanAllViews();
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            var yeniIzinler = new List<ButtonPermission>();

            foreach (var button in uniqueButtons)
            {
                string key = $"{button.Controller}|{button.Action}";
                bool izinVar = seciliIzinler?.Contains(key) ?? false;

                yeniIzinler.Add(new ButtonPermission
                {
                    KullaniciTipi = kullaniciTipi,
                    SayfaAdi = button.Controller,
                    ButonAksiyonu = button.Action,
                    IzınVar = izinVar,
                    Aciklama = $"{GetSayfaAdi(button.Controller)} sayfasında {GetButtonAdi(button.Action)} işlemi"
                });
            }

            _repo.TemizleVeEkle(kullaniciTipi, yeniIzinler);

            TempData["Success"] = $"{kullaniciTipi} buton yetkileri kaydedildi. Toplam {yeniIzinler.Count} buton tespit edildi.";
            return RedirectToAction("Index", new { kullaniciTipi });
        }

        [HttpGet]
        public IActionResult TaraVeGoster()
        {
            var buttons = _scanner.ScanAllViews();
            return View(buttons);
        }

        [HttpGet]
        public IActionResult TumIzinleriGetirJson()
        {
            var izinler = _repo.TumIzinleriGetir();
            return Json(izinler);
        }

        // Sayfa adlarını Türkçeleştir
        private string GetSayfaAdi(string controller)
        {
            return controller switch
            {
                "AdminMusteri" => "Müşteriler",
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
                "AdminButtonController" => "Buton Yetkilendirme",
                "AdminAnaSayfaBannerResim" => "Ana Sayfa Banner",
                "AdminHakkimizdaBilgileri" => "Hakkımızda",
                "AdminHakkimizdaFotograf" => "Hakkımızda Görsel",
                "AdminIletisimBilgileri" => "İletişim",
                "AdminSSS" => "SSS",
                "AdminUrunGaleri" => "Ürün Görselleri",
                "AdminKVKK" => "KVKK",
                "AdminGenelAydinlatma" => "Aydınlatma Metni",
                "AdminBirim" => "Birimler",
                "AdminTeklifler" => "Ürün Teklifleri",
                "AdminIK" => "İK Başvuruları",
                "AdminLokasyon" => "Lokasyonlar",
                "AdminBayiDuyuru" => "Bayi Duyurular",
                "AdminArgeHata" => "ARGE Hata",
                "AdminUYB" => "Genel Ayarlar",
                "AdminLisansTip" => "Lisans Tipleri",
                "AdminSozlesmeDurumu" => "Sözleşme Durumu",
                "AdminBayiSozlesmeKriteri" => "Bayi Sözleşme Kriterleri",
                "AdminLisansDurumu" => "Lisans Durumu",
                "AdminTeklifDurum" => "Teklif Durumları",
                "AdminPaketGrup" => "Paket Grupları",
                "AdminMusteriTipi" => "Müşteri Tipleri",
                "AdminDepartman" => "Departmanlar",
                "AdminKDV" => "KDV Oranları",
                "AdminEntegrator" => "Entegratörler",
                "AdminNedenler" => "Teklif Kaybedilme Nedenleri",
                "AdminNeredenDuydu" => "Nereden Duydu",
                "AdminARGEDurum" => "ARGE Durum",
                "AdminIstekOneriDurum" => "İstek/Öneri Durum",
                "AdminMusteriSozlesme" => "Müşteri Sözleşmesi",
                "AdminBayiSozlesme" => "Bayi Sözleşmesi",
                "AdminTeklifVer" => "Teklif Oluştur",
                _ => controller
            };
        }

        // Buton aksiyonlarını Türkçeleştir
        private string GetButtonAdi(string action)
        {
            return action.ToLower() switch
            {
                "create" => "Ekle",
                "edit" => "Düzenle",
                "delete" => "Sil",
                "view" => "Görüntüle",
                "export" => "Dışa Aktar",
                "import" => "İçe Aktar",
                "download" => "İndir",
                "print" => "Yazdır",
                "approve" => "Onayla",
                "reject" => "Reddet",
                "save" => "Kaydet",
                "copy" => "Kopyala",
                _ => action
            };
        }
    }
}