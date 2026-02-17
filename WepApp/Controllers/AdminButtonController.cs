using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using System.IO;

namespace WebApp.Controllers
{
    public class AdminButtonController : Controller
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
            // 1. View'leri tara ve gerçek butonları bul
            var detectedButtons = ScanViewsForButtons();

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
            var detectedButtons = ScanViewsForButtons();
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
        public IActionResult TumIzinleriGetirJson()
        {
            var izinler = _repo.TumIzinleriGetir();
            return Json(izinler);
        }

        private List<DetectedButton> ScanViewsForButtons()
        {
            var buttons = new List<DetectedButton>();
            var viewsPath = Path.Combine(_env.ContentRootPath, "Views");

            if (!Directory.Exists(viewsPath))
                return buttons;

            // Tüm cshtml dosyalarını tara
            var viewFiles = Directory.GetFiles(viewsPath, "*.cshtml", SearchOption.AllDirectories);

            foreach (var viewFile in viewFiles)
            {
                var content = System.IO.File.ReadAllText(viewFile);
                var fileName = Path.GetFileNameWithoutExtension(viewFile);
                var folderName = Path.GetFileName(Path.GetDirectoryName(viewFile));

                // Controller adını belirle
                var controllerName = folderName;

                // Gerçek butonları bul
                FindButtonsInContent(content, controllerName, fileName, buttons);
            }

            return buttons;
        }

        private void FindButtonsInContent(string content, string controllerName, string viewName, List<DetectedButton> buttons)
        {
            // SADECE EKLE BUTONLARI VE TABLO İŞLEM BUTONLARINI BUL
            var patterns = new[]
            {
                // 1. Ana ekle butonları (fa-plus icon ile)
                @"<button[^>]*class=[""'][^""']*\bbtn-primary\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-plus[^""']*[""']>.*?</i>.*?</button>",
                @"<a[^>]*class=[""'][^""']*\bbtn-primary\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-plus[^""']*[""']>.*?</i>.*?</a>",
                
                // 2. Tablo içindeki işlem butonları (düzenle ve sil)
                @"<td[^>]*>\s*<button[^>]*class=[""'][^""']*btn-warning[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-edit[^""']*[""']>.*?</i>.*?</button>\s*<button[^>]*class=[""'][^""']*btn-danger[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-trash[^""']*[""']>.*?</i>.*?</button>\s*</td>",
                
                // 3. Düzenle butonları (fa-edit icon)
                @"<button[^>]*class=[""'][^""']*\bbtn-warning\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-edit[^""']*[""']>.*?</i>.*?</button>",
                @"<a[^>]*class=[""'][^""']*\bbtn-warning\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-edit[^""']*[""']>.*?</i>.*?</a>",
                
                // 4. Sil butonları (fa-trash icon)
                @"<button[^>]*class=[""'][^""']*\bbtn-danger\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-trash[^""']*[""']>.*?</i>.*?</button>",
                @"<a[^>]*class=[""'][^""']*\bbtn-danger\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-trash[^""']*[""']>.*?</i>.*?</a>",
                
                // 5. Detay butonları (fa-eye icon) - eğer varsa
                @"<button[^>]*class=[""'][^""']*\bbtn-info\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-eye[^""']*[""']>.*?</i>.*?</button>",
                @"<a[^>]*class=[""'][^""']*\bbtn-info\b[^""']*[""'][^>]*>.*?<i[^>]*class=[""'][^""']*fa-eye[^""']*[""']>.*?</i>.*?</a>",
                
                // 6. Genel buton desenleri (metin bazlı)
                @"<button[^>]*>(.*?)(?:Ekle|ekle|Yeni|yeni|Düzenle|düzenle|Sil|sil|Detay|detay|Görüntüle|görüntüle).*?</button>",
                @"<a[^>]*>(.*?)(?:Ekle|ekle|Yeni|yeni|Düzenle|düzenle|Sil|sil|Detay|detay|Görüntüle|görüntüle).*?</a>"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    var buttonHtml = match.Value;
                    var action = DetectActionFromButton(buttonHtml);

                    if (!string.IsNullOrEmpty(action) && IsValidAction(action))
                    {
                        // Aynı controller+action kombinasyonu varsa ekleme
                        var existingButton = buttons.FirstOrDefault(b =>
                            b.Controller == controllerName &&
                            b.Action == action);

                        if (existingButton == null)
                        {
                            buttons.Add(new DetectedButton
                            {
                                Controller = controllerName,
                                Action = action
                            });
                        }
                    }
                }
            }
        }

        private bool IsValidAction(string action)
        {
            // SADECE EKLE, DÜZENLE, SİL, DETAY butonları geçerli
            var validActions = new[] { "create", "edit", "delete", "view" };
            return validActions.Contains(action.ToLower());
        }

        private string DetectActionFromButton(string buttonHtml)
        {
            buttonHtml = buttonHtml.ToLower();

            // İPTAL BUTONLARINI ELE - hiçbir şekilde listelenmesin
            if (buttonHtml.Contains("iptal") ||
                buttonHtml.Contains("cancel") ||
                buttonHtml.Contains("data-bs-dismiss=\"modal\"") ||
                buttonHtml.Contains("btn-secondary") ||
                buttonHtml.Contains("btn-close"))
            {
                return null;
            }

            // YAZDIR, EXCEL, PDF BUTONLARINI ELE
            if (buttonHtml.Contains("yazdır") ||
                buttonHtml.Contains("yazdir") ||
                buttonHtml.Contains("excel") ||
                buttonHtml.Contains("pdf") ||
                buttonHtml.Contains("print") ||
                buttonHtml.Contains("export") ||
                buttonHtml.Contains("rocket") ||
                buttonHtml.Contains("dışa aktar") ||
                buttonHtml.Contains("disa aktar"))
            {
                return null;
            }

            // 1. Data-permission attribute'u
            var permissionMatch = Regex.Match(buttonHtml, @"data-button-permission=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (permissionMatch.Success)
            {
                var action = MapToStandardAction(permissionMatch.Groups[1].Value);
                if (IsValidAction(action)) return action;
            }

            // 2. Data-action attribute'u
            var actionMatch = Regex.Match(buttonHtml, @"data-action=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (actionMatch.Success)
            {
                var action = MapToStandardAction(actionMatch.Groups[1].Value);
                if (IsValidAction(action)) return action;
            }

            // 3. Asp-action attribute'u
            var aspActionMatch = Regex.Match(buttonHtml, @"asp-action=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (aspActionMatch.Success)
            {
                var action = MapToStandardAction(aspActionMatch.Groups[1].Value);
                if (IsValidAction(action)) return action;
            }

            // 4. onclick fonksiyonlarına göre kontrol
            if (buttonHtml.Contains("duzenlegetir") || buttonHtml.Contains("edit") || buttonHtml.Contains("düzenle")) return "edit";
            if (buttonHtml.Contains("silOnay") || buttonHtml.Contains("delete") || buttonHtml.Contains("sil")) return "delete";
            if (buttonHtml.Contains("detaygetir") || buttonHtml.Contains("view") || buttonHtml.Contains("görüntüle")) return "view";

            // 5. Icon sınıflarına göre kontrol
            if (buttonHtml.Contains("fa-plus") || buttonHtml.Contains("fa-add") || buttonHtml.Contains("fa-hammer")) return "create";
            if (buttonHtml.Contains("fa-edit") || buttonHtml.Contains("fa-pencil") || buttonHtml.Contains("fa-wrench")) return "edit";
            if (buttonHtml.Contains("fa-trash") || buttonHtml.Contains("fa-times") || buttonHtml.Contains("fa-eraser")) return "delete";
            if (buttonHtml.Contains("fa-eye") || buttonHtml.Contains("fa-search") || buttonHtml.Contains("fa-binoculars")) return "view";

            // 6. Class sınıflarına göre kontrol
            if (buttonHtml.Contains("btn-primary") && (buttonHtml.Contains("ekle") || buttonHtml.Contains("plus") || buttonHtml.Contains("add"))) return "create";
            if (buttonHtml.Contains("btn-warning") || buttonHtml.Contains("btn-edit")) return "edit";
            if (buttonHtml.Contains("btn-danger") || buttonHtml.Contains("btn-delete")) return "delete";
            if (buttonHtml.Contains("btn-info") || buttonHtml.Contains("btn-view") || buttonHtml.Contains("btn-detail")) return "view";

            // 7. Buton metnini çıkar
            var text = ExtractButtonText(buttonHtml).ToLower();

            // 8. Metin bazlı kontrol
            if (text.Contains("ekle") || text.Contains("yeni") || text == "add" || text == "create" || text.Contains("plus")) return "create";
            if (text.Contains("düzenle") || text.Contains("duzenle") || text == "edit" || text == "update" || text.Contains("güncelle") || text.Contains("guncelle")) return "edit";
            if (text.Contains("sil") || text == "delete" || text == "remove" || text.Contains("kaldır") || text.Contains("kaldir")) return "delete";
            if (text.Contains("detay") || text.Contains("görüntüle") || text.Contains("goruntule") || text == "view" || text == "details" || text.Contains("incele")) return "view";

            return null; // Tespit edilemedi
        }

        private string ExtractButtonText(string buttonHtml)
        {
            // Button etiketi içindeki metni çıkar
            var textMatch = Regex.Match(buttonHtml, @">(.*?)</", RegexOptions.Singleline);
            if (textMatch.Success)
            {
                // HTML tag'lerini temizle
                var text = Regex.Replace(textMatch.Groups[1].Value, @"<[^>]+>", "");
                // Icon ve boşlukları temizle
                text = Regex.Replace(text, @"<i[^>]*>.*?</i>", "");
                text = Regex.Replace(text, @"&nbsp;", " ");
                return text.Trim();
            }
            return "";
        }

        private string MapToStandardAction(string action)
        {
            action = action.ToLower();

            if (action == "create" || action == "ekle" || action == "add" || action == "new") return "create";
            if (action == "edit" || action == "duzenle" || action == "düzenle" || action == "update" || action == "guncelle" || action == "güncelle") return "edit";
            if (action == "delete" || action == "sil" || action == "remove") return "delete";
            if (action == "view" || action == "detay" || action == "details" || action == "goruntule" || action == "görüntüle") return "view";

            return action;
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
                "view" => "Detay",
                _ => action
            };
        }
    }
}