using System.Collections.Generic;
using WebApp.Controllers;

namespace WepApp.Models
{
    public class MenuIzin
    {
        public int Id { get; set; }
        public string KullaniciTipi { get; set; }
        public string MenuUrl { get; set; }
        public string MenuBaslik { get; set; }
        public string? ParentMenuUrl { get; set; }
        public string? Icon { get; set; }
        public int Siralama { get; set; }
    }

    public class AdminMenuIzinViewModel
    {
        public List<(string Url, string Baslik, string? ParentUrl, string? Icon, int Siralama, int Seviye)> TumMenuler { get; set; }
        public Dictionary<string, string> CheckboxIdMap { get; set; } = new Dictionary<string, string>();
        public List<string> AdminIzinler { get; set; } = new List<string>();
        public List<string> MusteriIzinler { get; set; } = new List<string>();
        public List<string> BayiIzinler { get; set; } = new List<string>();
        public List<string> DistributorIzinler { get; set; } = new List<string>();
        public Dictionary<int, List<MenuHiyerarsi>> MenuHiyerarsisi { get; set; } = new Dictionary<int, List<MenuHiyerarsi>>();
    }
}