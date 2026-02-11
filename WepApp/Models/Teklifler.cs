using WebApp.Models;

namespace WepApp.Models
{
    public class Teklifler
    {
        public int Id { get; set; }
        public int? UrunId { get; set; }
        public int? Durumu { get; set; }
        public int? Onay { get; set; }
        public string? AdiSoyadi { get; set; }
        public string? Telefon { get; set; }
        public string? Eposta { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public Urun Urun { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }
    }
}
