namespace WepApp.Models
{
    public class Kampanya
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public string? Baslik { get; set; }
        public string? Metin { get; set; }
        public decimal IndirimYuzdesi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        public List<KampanyaPaket> KampanyaPaketler { get; set; } = new List<KampanyaPaket>();
    }
}