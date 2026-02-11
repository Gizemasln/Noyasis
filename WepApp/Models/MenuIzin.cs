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
}