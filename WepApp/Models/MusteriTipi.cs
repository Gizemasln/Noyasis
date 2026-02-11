namespace WepApp.Models
{
    public class MusteriTipi
    {
        public int Id { get; set; }
        public string Adi { get; set; }
        public int Durumu { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
    }
}
