using System;
using System.Collections.Generic;

namespace WepApp.Models
{
    public class Bayi
    {
        public int Id { get; set; }
        public bool Distributor { get; set; } = false;
        public string? Unvan { get; set; } = string.Empty;
        public string? KullaniciAdi { get; set; } = string.Empty;
        public string? Sifre { get; set; } = string.Empty;

        // Üst bayi (null ise Ana Bayi'dir)
        public int? UstBayiId { get; set; }
        public virtual Bayi? UstBayi { get; set; }
        
        // Alt bayiler
        public virtual ICollection<Bayi> AltBayiler { get; set; } = new List<Bayi>();

        public string? Email { get; set; } = string.Empty;
        public string? Telefon { get; set; } = string.Empty;
        public string? Adres { get; set; } = string.Empty;
        public string? Kodu { get; set; } = string.Empty;
        public string? Bolge { get; set; } = string.Empty;
        public string? Il { get; set; } = string.Empty;
        public string? Ilce { get; set; } = string.Empty;
        public string? Belde { get; set; } = string.Empty;
        public string? TCVNo { get; set; } = string.Empty;
        public string? VergiDairesi { get; set; } = string.Empty;
        public string? KepAdresi { get; set; } = string.Empty;
        public string? WebAdresi { get; set; } = string.Empty;
        public string? Aciklama { get; set; } = string.Empty;
        public string? AlpemixFirmaAdi { get; set; } = string.Empty;
        public string? AlpemixGrupAdi { get; set; } = string.Empty;
        public string? AlpemixSifre { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }
        public string? LogoUzanti { get; set; } = string.Empty;
        public byte[]? Imza { get; set; }
        public string? ImzaUzanti { get; set; } = string.Empty;

        public int? KayitYapanId { get; set; }
        public int? DegisiklikYapanId { get; set; }

        public int? Seviye { get; set; } = 0;
        public int? Durumu { get; set; } = 1;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public DateTime GuncellenmeTarihi { get; set; } = DateTime.Now;
    }
}