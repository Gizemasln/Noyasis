namespace WepApp.Models
{
    public class LisansDurumu
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public string Adi { get; set; }
    }
}
