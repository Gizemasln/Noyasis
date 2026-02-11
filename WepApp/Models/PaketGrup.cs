namespace WepApp.Models
{
    public class PaketGrup
    {
        public int Id { get; set; }
        public int LisansTipId { get; set; }
        public string Adi { get; set; }
        public decimal Fiyat { get; set; }
        public decimal KFiyat { get; set; }
        public decimal? EgitimSuresi { get; set; }
        public int IndOran { get; set; }
        public int Durumu { get; set; }
        public int Sira { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public int KayitYapanKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public LisansTip LisansTip { get; set; }
    }
}
