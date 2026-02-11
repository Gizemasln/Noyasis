namespace WepApp.Models
{
    public class Paket
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int LisansTipId  { get; set; }
        public int? IndOran  { get; set; }
        public int? KDVId  { get; set; }
        public int? Aktif  { get; set; }
        public string? ModulKodu  { get; set; }
        public string? Adi { get; set; }
        public decimal? Fiyat { get; set; }
        public decimal? KFiyat { get; set; }
        public decimal? EgitimSuresi { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }
        public int? EkleyenKullaniciId { get; set; }
        public int? PaketDurumu { get; set; }

        public int? Sira { get; set; }
        public int? BirimId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        public LisansTip LisansTip { get; set; }
        public Birim Birim { get; set; }
        public KDV KDV { get; set; }

    }
}
