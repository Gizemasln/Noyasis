
namespace WepApp.Models
{
    public class BayiSozlesme
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int  GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public int BayiId { get; set; }
        public string DokumanNo { get; set; }
        public DateTime YayinTarihi { get; set; }
        public DateTime RevizeTarihi { get; set; }
        public string RevizyonNo { get; set; }
        public string DosyaYolu { get; set; }
        public int SozlesmeDurumuId { get; set; }
        public decimal GecerlilikSuresi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public Bayi Bayi { get; set; }
        public SozlesmeDurumu SozlesmeDurumu { get; set; }
        public List<BayiSozlesmeBayiKriter> BayiSozlesmeBayiKriter { get;  set; }
    }
}
