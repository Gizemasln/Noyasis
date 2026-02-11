namespace WepApp.Models
{
    public class FiyatOran
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public bool Oran { get; set; }
        public int? OranYuzde { get; set; }
        public int PaketId { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        public Paket Paket { get; set; }
    }
}
