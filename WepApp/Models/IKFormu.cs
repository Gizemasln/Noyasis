using WebApp.Models;

namespace WepApp.Models
{
    public class IKFormu
    {
        public int Id { get; set; }
        public int? Durumu { get; set; }
        public string? AdiSoyadi { get; set; }
        public string? Telefon { get; set; }
        public string? Eposta { get; set; }
        public string? TC { get; set; }
        public string? DosyaYolu { get; set; }
        public string? Mesaj { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }
    }
}
