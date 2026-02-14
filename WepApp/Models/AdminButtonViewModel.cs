using System.Collections.Generic;

namespace WepApp.Models
{
    public class AdminButtonViewModel
    {
        public List<DetectedButton> DetectedButtons { get; set; } = new List<DetectedButton>();
        public Dictionary<string, List<DetectedButton>> GruplanmisButonlar { get; set; } = new Dictionary<string, List<DetectedButton>>();
        public Dictionary<string, bool> ButtonPermissions { get; set; } = new Dictionary<string, bool>();
        public string SeciliKullaniciTipi { get; set; } = "Admin";
    }

    public class DetectedButton
    {
        public string Controller { get; set; }
        public string View { get; set; }
        public string Action { get; set; }
        public string ButtonHtml { get; set; }
        public string FullPath { get; set; }
    }
}