using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using System.IO;
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
            // Kullanıcı kontrolü yap
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

            var detectedButtons = ScanViewsForButtons();

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

            model.ButtonPermissions = new Dictionary<string, bool>();
            var izinDict = mevcutIzinler.ToDictionary(
                i => $"{i.SayfaAdi}|{i.ButonAksiyonu}",
                i => i.IzınVar
            );

            foreach (var button in uniqueButtons)
            {
                var key = $"{button.Controller}|{button.Action}";
                model.ButtonPermissions[key] = izinDict.ContainsKey(key) ? izinDict[key] : false;
            }

            // Kullanıcı bilgilerini ViewBag'e ekle - DÜZELTİLDİ!
            var userInfo = GetCurrentUserInfo();
            ViewBag.CurrentUserType = userInfo.tip; // Artık "Admin", "Musteri" veya "Bayi" dönecek
            ViewBag.CurrentUserId = userInfo.id;

            return View(model);
        }

        [HttpPost]
        public IActionResult Kaydet(string kullaniciTipi, List<string> seciliIzinler)
        {
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

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

        [HttpPost]
        public IActionResult TopluYetkilendir(string kullaniciTipi, string aksiyonTipi, bool durum)
        {
            var redirect = LoadCommonData();
            if (redirect != null) return redirect;

            var detectedButtons = ScanViewsForButtons();
            var uniqueButtons = detectedButtons
                .GroupBy(b => new { b.Controller, b.Action })
                .Select(g => g.First())
                .ToList();

            var yeniIzinler = new List<ButtonPermission>();
            var mevcutIzinler = _repo.KullaniciTipineGoreGetir(kullaniciTipi);
            var mevcutDict = mevcutIzinler.ToDictionary(i => $"{i.SayfaAdi}|{i.ButonAksiyonu}", i => i.IzınVar);

            foreach (var button in uniqueButtons)
            {
                string key = $"{button.Controller}|{button.Action}";
                bool izinVar = mevcutDict.ContainsKey(key) ? mevcutDict[key] : false;

                if (aksiyonTipi == "Tumu" || button.Action.ToLower() == aksiyonTipi.ToLower())
                {
                    izinVar = durum;
                }

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
            var aksiyonAdi = aksiyonTipi == "Tumu" ? "Tüm butonlar" : GetButtonAdi(aksiyonTipi) + " butonları";
            var durumText = durum ? "yetkilendirildi" : "yetkisi kaldırıldı";
            TempData["Success"] = $"{kullaniciTipi} için {aksiyonAdi} {durumText}. Toplam {yeniIzinler.Count} buton güncellendi.";
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

            var viewsPath = Path.Combine(_env.WebRootPath, "Views");
            if (!Directory.Exists(viewsPath))
            {
                viewsPath = Path.Combine(_env.ContentRootPath, "Views");
            }
            if (!Directory.Exists(viewsPath))
            {
                viewsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views");
            }

            if (!Directory.Exists(viewsPath))
            {
                var allViews = Directory.GetDirectories(_env.ContentRootPath, "Views", SearchOption.AllDirectories);
                if (allViews.Any())
                {
                    viewsPath = allViews.First();
                }
            }

            string logPath = Path.Combine(_env.ContentRootPath, "buton_yolu_log.txt");
            System.IO.File.WriteAllText(logPath, $"Kullanılan views yolu: {viewsPath} - Var mı: {Directory.Exists(viewsPath)}");

            var possiblePaths = new List<string>
            {
                Path.Combine(_env.ContentRootPath, "Views"),
                !string.IsNullOrEmpty(_env.WebRootPath) ? Path.Combine(_env.WebRootPath, "Views") : null,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views"),
                Path.Combine(Directory.GetCurrentDirectory(), "Views"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Views"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Views")
            };

            var allDirs = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.AllDirectories);
            foreach (var dir in allDirs.Where(d => d.EndsWith("Views")))
            {
                possiblePaths.Add(dir);
            }

            using (StreamWriter log = new StreamWriter(logPath, false))
            {
                log.WriteLine($"=== BUTON TARAMA LOGU - {DateTime.Now} ===");
                log.WriteLine($"ContentRootPath: {_env.ContentRootPath}");
                log.WriteLine($"WebRootPath: {_env.WebRootPath}");
                log.WriteLine($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                log.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
                log.WriteLine("");
                log.WriteLine("ARANAN YOLLAR:");

                foreach (var path in possiblePaths.Distinct().Where(p => p != null))
                {
                    string fullPath = Path.GetFullPath(path);
                    log.WriteLine($"- {fullPath} : {(Directory.Exists(fullPath) ? "VAR" : "YOK")}");
                    if (Directory.Exists(fullPath))
                    {
                        viewsPath = fullPath;
                        log.WriteLine($" >>> BULUNDU! Bu yol kullanılacak: {fullPath}");
                        break;
                    }
                }

                if (viewsPath == null)
                {
                    log.WriteLine("");
                    log.WriteLine("Hiçbir views klasörü bulunamadı! Tüm dizinler taranıyor...");
                    try
                    {
                        var allFolders = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory, "Views", SearchOption.AllDirectories);
                        if (allFolders.Any())
                        {
                            viewsPath = allFolders.First();
                            log.WriteLine($"Derin taramada bulundu: {viewsPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"Derin tarama hatası: {ex.Message}");
                    }
                }

                if (viewsPath != null && Directory.Exists(viewsPath))
                {
                    log.WriteLine("");
                    log.WriteLine($"VIEWS KLASÖRÜ BULUNDU: {viewsPath}");
                    var viewFiles = Directory.GetFiles(viewsPath, "*.cshtml", SearchOption.AllDirectories);
                    log.WriteLine($"Toplam {viewFiles.Length} .cshtml dosyası bulundu:");
                    foreach (var file in viewFiles.Take(20))
                    {
                        log.WriteLine($"- {file}");
                    }
                    if (viewFiles.Length > 20)
                        log.WriteLine($"... ve {viewFiles.Length - 20} dosya daha");

                    foreach (var viewFile in viewFiles)
                    {
                        try
                        {
                            var content = System.IO.File.ReadAllText(viewFile);
                            var fileName = Path.GetFileNameWithoutExtension(viewFile);
                            var folderName = Path.GetFileName(Path.GetDirectoryName(viewFile));
                            var controllerName = folderName;

                            FindButtonsInContent(content, controllerName, fileName, buttons);
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"Dosya okuma hatası {viewFile}: {ex.Message}");
                        }
                    }

                    log.WriteLine("");
                    log.WriteLine($"Toplam {buttons.Count} buton bulundu:");
                    foreach (var btn in buttons)
                    {
                        log.WriteLine($"- {btn.Controller} / {btn.Action}");
                    }
                }
                else
                {
                    log.WriteLine("");
                    log.WriteLine("HATA: Views klasörü bulunamadı!");
                    log.WriteLine("");
                    log.WriteLine($"BaseDirectory içindeki klasörler ({AppDomain.CurrentDomain.BaseDirectory}):");
                    try
                    {
                        var dirs = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory);
                        foreach (var dir in dirs)
                        {
                            log.WriteLine($"- {Path.GetFileName(dir)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"Klasör listeleme hatası: {ex.Message}");
                    }
                }
            }

            return buttons;
        }

        private void FindButtonsInContent(string content, string controllerName, string viewName, List<DetectedButton> buttons)
        {
            var patterns = new[]
            {
                // Ekle
                @"<(button|a)[^>]*class=[""'][^""']*\bbtn-primary\b[^""']*[""'][^>]*>[\s\S]*?<i[^>]*fa-plus[\s\S]*?</\1>",
                // Düzenle + Sil kombinasyonu
                @"<td[^>]*>[\s\S]*?<button[^>]*btn-warning[^>]*>[\s\S]*?fa-edit[\s\S]*?</button>[\s\S]*?<button[^>]*btn-danger[^>]*>[\s\S]*?fa-trash[\s\S]*?</button>[\s\S]*?</td>",
                // Düzenle
                @"<(button|a)[^>]*btn-warning[^>]*>[\s\S]*?fa-edit[\s\S]*?</\1>",
                // Sil
                @"<(button|a)[^>]*btn-danger[^>]*>[\s\S]*?fa-trash[\s\S]*?</\1>",
                // Detay / Göz (hem button hem a)
                @"<(button|a)[^>]*>[\s\S]*?fa-eye[\s\S]*?</\1>",
                @"<(button|a)[^>]*class=[""'][^""']*(btn-info|detay|goruntule|detay-goster)[^""']*[""'][^>]*>[\s\S]*?fa-eye[\s\S]*?</\1>",
                // Genel metin
                @"<(button|a)[^>]*>(.*?)(Ekle|ekle|Yeni|yeni|Düzenle|düzenle|Sil|sil|Detay|detay|Görüntüle|görüntüle).*?</\1>"
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
            var validActions = new[]
            {
                "create", "edit", "delete", "view",
                "ekle", "yeni", "add",
                "duzenle", "guncelle", "update",
                "sil", "delete", "remove",
                "detay", "detay-goster", "goruntule", "sozlesme-detay",
                "lisansla", "muhasebelendi-yap", "lisans-iptal", "muhasebe-geri-al"
            };

            return validActions.Contains(action.ToLowerInvariant());
        }

        private string DetectActionFromButton(string buttonHtml)
        {
            buttonHtml = buttonHtml.ToLowerInvariant();

            // 1. Öncelik: data-button-permission (en güvenilir yöntem)
            var permissionMatch = Regex.Match(buttonHtml, @"data-button-permission\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (permissionMatch.Success)
            {
                var action = permissionMatch.Groups[1].Value.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(action))
                {
                    return action;
                }
            }

            // Diğer tespitler (geriye uyumluluk)
            if (buttonHtml.Contains("iptal") || buttonHtml.Contains("cancel") ||
                buttonHtml.Contains("data-bs-dismiss=\"modal\"") || buttonHtml.Contains("btn-close"))
                return null;

            if (buttonHtml.Contains("yazdır") || buttonHtml.Contains("excel") || buttonHtml.Contains("pdf") ||
                buttonHtml.Contains("print") || buttonHtml.Contains("export"))
                return null;

            var actionMatch = Regex.Match(buttonHtml, @"data-action=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (actionMatch.Success)
                return MapToStandardAction(actionMatch.Groups[1].Value);

            var aspActionMatch = Regex.Match(buttonHtml, @"asp-action=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (aspActionMatch.Success)
                return MapToStandardAction(aspActionMatch.Groups[1].Value);

            if (buttonHtml.Contains("duzenlegetir") || buttonHtml.Contains("düzenle")) return "duzenle";
            if (buttonHtml.Contains("silonay") || buttonHtml.Contains("sil")) return "sil";
            if (buttonHtml.Contains("detaygetir") || buttonHtml.Contains("görüntüle")) return "detay";

            if (buttonHtml.Contains("fa-plus")) return "ekle";
            if (buttonHtml.Contains("fa-edit")) return "duzenle";
            if (buttonHtml.Contains("fa-trash")) return "sil";
            if (buttonHtml.Contains("fa-eye")) return "detay";

            if (buttonHtml.Contains("btn-primary") && (buttonHtml.Contains("ekle") || buttonHtml.Contains("plus"))) return "ekle";
            if (buttonHtml.Contains("btn-warning")) return "duzenle";
            if (buttonHtml.Contains("btn-danger")) return "sil";
            if (buttonHtml.Contains("btn-info") && buttonHtml.Contains("fa-eye")) return "detay";

            var text = ExtractButtonText(buttonHtml).ToLowerInvariant();
            if (text.Contains("ekle") || text.Contains("yeni")) return "ekle";
            if (text.Contains("düzenle") || text.Contains("güncelle")) return "duzenle";
            if (text.Contains("sil")) return "sil";
            if (text.Contains("detay") || text.Contains("görüntüle")) return "detay";

            return null;
        }

        private string ExtractButtonText(string buttonHtml)
        {
            var textMatch = Regex.Match(buttonHtml, @">(.*?)</", RegexOptions.Singleline);
            if (textMatch.Success)
            {
                var text = Regex.Replace(textMatch.Groups[1].Value, @"<[^>]+>", "");
                text = Regex.Replace(text, @"<i[^>]*>.*?</i>", "");
                text = Regex.Replace(text, @"&nbsp;", " ");
                return text.Trim();
            }
            return "";
        }

        private string MapToStandardAction(string action)
        {
            action = action.ToLowerInvariant();
            if (action == "create" || action == "ekle" || action == "add" || action == "new") return "ekle";
            if (action == "edit" || action == "duzenle" || action == "düzenle" || action == "update" || action == "guncelle") return "duzenle";
            if (action == "delete" || action == "sil" || action == "remove") return "sil";
            if (action == "view" || action == "detay" || action == "goruntule") return "detay";
            return action;
        }

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
                _ => controller
            };
        }

        private string GetButtonAdi(string action)
        {
            return action.ToLowerInvariant() switch
            {
                "create" or "ekle" or "add" or "yeni" => "Ekle",
                "edit" or "duzenle" or "guncelle" => "Düzenle",
                "delete" or "sil" or "remove" => "Sil",
                "view" or "detay" or "goruntule" or "detay-goster" => "Detay",
                _ => action
            };
        }
    }
}