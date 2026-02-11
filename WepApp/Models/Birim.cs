namespace WepApp.Models
{
    public class Birim
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public string Adi { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }

    }
}
