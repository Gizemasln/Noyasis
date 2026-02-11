namespace WepApp.Models
{
    public class BayiSozlesmeKriteri
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public string? Adi { get; set; }
        public string? Aciklama { get; set; }
        public string? UzunAciklama { get; set; }
        public string? Konu { get; set; }
        public decimal Oran { get; set; }
    }
}
