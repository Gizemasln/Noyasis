using WebApp.Models;

namespace WepApp.Models
{
    public class UrunGaleri
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public string FotografBuyuk { get; set; }
        public string FotografKucuk { get; set; }
        public int UrunId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public Urun Urun { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }
    }
}
