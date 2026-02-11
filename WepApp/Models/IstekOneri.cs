// Models/IstekOneriler.cs
namespace WepApp.Models
{
    public class IstekOneriler
    {
        public int Id { get; set; }
        public int? MusteriId { get; set; }
        public int? BayiId { get; set; }
        public int LisansTipId { get; set; }
        public int IstekOneriDurumId { get; set; }
        public string Konu { get; set; }
        public string Metni { get; set; }
        public int Durumu { get; set; } // 1:Aktif, 0:Silinmiş
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        // Yeni alanlar - Distributor cevabı için
        public string? DistributorCevap { get; set; }
        public bool DistributorCevapVerdiMi { get; set; } = false;
        public DateTime? DistributorCevapTarihi { get; set; }
        public int? DistributorBayiId { get; set; }


        // Navigation properties
        public Musteri Musteri { get; set; }
        public Bayi Bayi { get; set; }
        public LisansTip LisansTip { get; set; }
        public IstekOneriDurum IstekOneriDurum { get; set; }
    }
}