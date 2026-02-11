using WebApp.Controllers;

namespace WepApp.Models
{
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