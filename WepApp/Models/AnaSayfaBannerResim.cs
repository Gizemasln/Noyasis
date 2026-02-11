
namespace WebApp.Models
{
    public class AnaSayfaBannerResim
    {
        public int Id { get; set; }
        public string? Baslik { get; set; }
        public string? Metin { get; set; }
        public string? FotografBuyuk { get; set; }
        public string? FotografKucuk { get; set; }
        public int? Durumu { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public int KullanicilarId { get; set; }

        public Kullanicilar Kullanicilar { get; set; }



    }
}
