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

        public BayiSozlesme BayiSozlesme { get; set; }
        public BayiSozlesmeKriteri BayiSozlesmeKriteri { get; set; }
    }
}
