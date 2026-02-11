using WebApp.Models;

namespace WepApp.Models
{
    public class SSS
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public string Soru { get; set; }
        public string Cevap { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }
    }
}
