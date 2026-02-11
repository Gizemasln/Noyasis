
namespace WepApp.Models
{
    public class PaketBaglanti
    {
        public int Id { get; set; }
        public int PaketId { get; set; }
        public int BagliPaketId { get; set; }
        public int Durumu { get; set; }
        public int? EkleyenKullaniciId { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        // Navigation Properties
        public Paket Paket { get; set; }
        public Paket BagliPaket { get; set; }
    }
}