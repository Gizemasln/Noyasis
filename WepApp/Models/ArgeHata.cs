namespace WepApp.Models
{
    public class ArgeHata
    {
        public int Id { get; set; }
        public int? MusteriId { get; set; }
        public int? BayiId { get; set; }
        public int? LisansTipId { get; set; }
        public string Tipi { get; set; } // ARGE veya Hata
        public string Adi { get; set; } // Formu dolduran adı 
        public string Soyadi { get; set; } // Formu dolduran soyadı
        public string? DosyaYolu { get; set; } // wwwroot/WebAdminTheme/ARGE klasörüne
        public string Metni { get; set; }
        public int Durumu { get; set; }
        public int? ARGEDurumId { get; set; }

        // Distributor cevabı için
        public string? DistributorCevap { get; set; }
        public bool DistributorCevapVerdiMi { get; set; } = false;
        public DateTime? DistributorCevapTarihi { get; set; }
        public int? DistributorBayiId { get; set; }

        // Admin cevabı için
        public bool AdminCevapVerdiMi { get; set; } = false;
        public DateTime? AdminCevapTarihi { get; set; }
        public int? AdminKullaniciId { get; set; }

        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }

        public Musteri Musteri { get; set; }
        public ARGEDurum ARGEDurum { get; set; }
        public Bayi Bayi { get; set; }
        public LisansTip LisansTip { get; set; }
    }
}