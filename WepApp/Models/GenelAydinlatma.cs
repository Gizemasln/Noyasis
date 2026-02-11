using WebApp.Models;

namespace WepApp.Models
{
    public class GenelAydinlatma
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public string Metin { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }
    }
}
