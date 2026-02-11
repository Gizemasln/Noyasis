using System;

namespace WepApp.Models
{
    public class Musteri
    {
        public int Id { get; set; }
        public int? MusteriTipiId { get; set; }
        public int? MusteriDurumuId { get; set; }
        public int? BayiId { get; set; }

        // Temel bilgiler
        public string Ad { get; set; } = string.Empty;
        public string? TicariUnvan { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty; // Sisteme giriş için
        public string Sifre { get; set; } = string.Empty; // Sisteme giriş için

        // İletişim ve adres bilgileri
        public string Email { get; set; } = string.Empty;
        public string Diger { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string Il { get; set; } = string.Empty;
        public string Ilce { get; set; } = string.Empty;
        public string Belde { get; set; } = string.Empty;
        public string Bolge { get; set; } = string.Empty;

        // Vergi ve resmi bilgiler
        public string TCVNo { get; set; } = string.Empty;
        public string VergiDairesi { get; set; } = string.Empty;
        public string KepAdresi { get; set; } = string.Empty;
        public string WebAdresi { get; set; } = string.Empty;

        // Özel alanlar / açıklamalar
        public string Aciklama { get; set; } = string.Empty;
        public string AlpemixFirmaAdi { get; set; } = string.Empty;
        public string AlpemixGrupAdi { get; set; } = string.Empty;
        public string AlpemixSifre { get; set; } = string.Empty;

        // Logo ve imza
        public byte[]? Logo { get; set; }
        public string? LogoUzanti { get; set; } = string.Empty;
        public byte[]? Imza { get; set; }
        public string? ImzaUzanti { get; set; } = string.Empty;

     
        public int Durum { get; set; } = 1; // Aktif/pasif
        public int? EkleyenKullaniciId { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }

        // Tarihler
        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public DateTime GuncellenmeTarihi { get; set; } = DateTime.Now;

        // Navigasyon
        public MusteriTipi MusteriTipi { get; set; }
        public MusteriDurumu MusteriDurumu { get; set; }
        public Bayi Bayi { get; set; }


    }
}
