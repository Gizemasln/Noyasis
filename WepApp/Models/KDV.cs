namespace WepApp.Models
{
    public class KDV
    {
        public int Id { get; set; }
        public int? Durumu { get; set; }
        public int? EkleyenKullaniciId { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }
        public decimal? Oran { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
