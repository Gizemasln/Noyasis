namespace WepApp.Models
{
    public class BayiSozlesmeBayiKriter
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        public int BayiSozlesmeId { get; set; }
        public int BayiSozlesmeKriteriId { get; set; }
        // BayiSozlesmeBayiKriter modeline ekleyin
        public int? EkleyenBayiId { get; set; }
        public int? EkleyenMusteriId { get; set; }
        public int? GuncelleyenBayiId { get; set; }
        public int? GuncelleyenMusteriId { get; set; }
        public BayiSozlesme BayiSozlesme { get; set; }
        public BayiSozlesmeKriteri BayiSozlesmeKriteri { get; set; }
    }
}
