using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WepApp.Models;

namespace WepApp.Services
{
    public class ButtonScannerService
    {
        private readonly string _viewsPath;

        public ButtonScannerService(string webRootPath)
        {
            // Views klasörünün yolu (webRootPath'in bir üst dizini)
            var rootPath = Path.GetDirectoryName(webRootPath);
            _viewsPath = Path.Combine(rootPath, "Views");
        }

        public List<DetectedButton> ScanAllViews()
        {
            var allButtons = new List<DetectedButton>();

            if (!Directory.Exists(_viewsPath))
                return allButtons;

            // Tüm .cshtml dosyalarını tara
            var viewFiles = Directory.GetFiles(_viewsPath, "*.cshtml", SearchOption.AllDirectories);

            foreach (var viewFile in viewFiles)
            {
                var controllerName = GetControllerName(viewFile);
                var viewName = GetViewName(viewFile);
                var content = File.ReadAllText(viewFile);

                var buttons = ScanButtonsInContent(content, controllerName, viewName);
                allButtons.AddRange(buttons);
            }

            return allButtons;
        }

        private List<DetectedButton> ScanButtonsInContent(string content, string controller, string view)
        {
            var buttons = new List<DetectedButton>();

            // 1. <button> etiketlerini tara
            var buttonPattern = @"<button[^>]*class=[""'][^""']*(btn-danger|btn-warning|btn-primary|btn-success|btn-info|delete|sil|duzenle|edit|ekle|create|goster|view|kaydet|save)[^""']*[""'][^>]*>.*?</button>";
            var buttonMatches = Regex.Matches(content, buttonPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in buttonMatches)
            {
                var buttonType = DetectButtonType(match.Value);
                buttons.Add(new DetectedButton
                {
                    Controller = controller,
                    View = view,
                    Action = buttonType,
                    ButtonHtml = CleanHtml(match.Value),
                    FullPath = $"{controller}/{view}"
                });
            }

            // 2. <a> linklerini tara (icon'lu veya buton görünümlü)
            var linkPattern = @"<a[^>]*class=[""'][^""']*(btn|fa-edit|fa-trash|fa-eye|fa-plus|fa-download|fa-print)[^""']*[""'][^>]*>.*?</a>";
            var linkMatches = Regex.Matches(content, linkPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in linkMatches)
            {
                var buttonType = DetectButtonType(match.Value);
                buttons.Add(new DetectedButton
                {
                    Controller = controller,
                    View = view,
                    Action = buttonType,
                    ButtonHtml = CleanHtml(match.Value),
                    FullPath = $"{controller}/{view}"
                });
            }

            // 3. Icon'ları tara
            var iconPattern = @"<i[^>]*class=[""'][^""']*(fa-edit|fa-trash|fa-eye|fa-plus|fa-download|fa-print|fa-pencil|fa-times|fa-save|fa-check)[^""']*[""'][^>]*>";
            var iconMatches = Regex.Matches(content, iconPattern, RegexOptions.IgnoreCase);

            foreach (Match match in iconMatches)
            {
                var buttonType = DetectButtonType(match.Value);
                buttons.Add(new DetectedButton
                {
                    Controller = controller,
                    View = view,
                    Action = buttonType,
                    ButtonHtml = CleanHtml(match.Value),
                    FullPath = $"{controller}/{view}"
                });
            }

            // 4. Input submit butonları
            var inputPattern = @"<input[^>]*type=[""']submit[""'][^>]*class=[""'][^""']*btn[^""']*[""'][^>]*>";
            var inputMatches = Regex.Matches(content, inputPattern, RegexOptions.IgnoreCase);

            foreach (Match match in inputMatches)
            {
                var buttonType = DetectButtonType(match.Value);
                buttons.Add(new DetectedButton
                {
                    Controller = controller,
                    View = view,
                    Action = string.IsNullOrEmpty(buttonType) ? "submit" : buttonType,
                    ButtonHtml = CleanHtml(match.Value),
                    FullPath = $"{controller}/{view}"
                });
            }

            return buttons;
        }

        private string CleanHtml(string html)
        {
            // HTML'i temizle ve kısalt
            html = Regex.Replace(html, @"\s+", " ");
            if (html.Length > 100)
                html = html.Substring(0, 100) + "...";
            return html;
        }

        private string DetectButtonType(string html)
        {
            html = html.ToLower();

            if (html.Contains("sil") || html.Contains("delete") || html.Contains("fa-trash") || html.Contains("btn-danger") || html.Contains("times"))
                return "delete";

            if (html.Contains("düzenle") || html.Contains("duzenle") || html.Contains("edit") || html.Contains("fa-edit") || html.Contains("fa-pencil") || html.Contains("btn-warning"))
                return "edit";

            if (html.Contains("ekle") || html.Contains("yeni") || html.Contains("create") || html.Contains("fa-plus") || html.Contains("btn-success"))
                return "create";

            if (html.Contains("görüntüle") || html.Contains("goruntule") || html.Contains("view") || html.Contains("detay") || html.Contains("fa-eye") || html.Contains("btn-info"))
                return "view";

            if (html.Contains("indir") || html.Contains("download") || html.Contains("fa-download"))
                return "download";

            if (html.Contains("yazdır") || html.Contains("print") || html.Contains("fa-print"))
                return "print";

            if (html.Contains("export") || html.Contains("excel") || html.Contains("pdf") || html.Contains("fa-file-excel"))
                return "export";

            if (html.Contains("import") || html.Contains("içe aktar") || html.Contains("fa-file-import"))
                return "import";

            if (html.Contains("onayla") || html.Contains("approve") || html.Contains("fa-check"))
                return "approve";

            if (html.Contains("reddet") || html.Contains("reject") || html.Contains("fa-times"))
                return "reject";

            if (html.Contains("kaydet") || html.Contains("save") || html.Contains("fa-save"))
                return "save";

            return "view";
        }

        private string GetControllerName(string filePath)
        {
            var directory = Path.GetFileName(Path.GetDirectoryName(filePath));

            // Eğer doğrudan Views/Controller/View.cshtml şeklindeyse
            if (directory != "Views" && directory != "Shared" && !directory.StartsWith("_"))
                return directory;

            return Path.GetFileNameWithoutExtension(filePath);
        }

        private string GetViewName(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }
}