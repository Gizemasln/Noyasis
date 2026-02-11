using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WepApp.Models
{
    public class MusteriSozlesme
    {
        [Key]
        public int Id { get; set; }

        public int TeklifId { get; set; }
        public int MusteriId { get; set; }
        public string LisansNo { get; set; } = string.Empty;
        public decimal YillikBakim { get; set; }

        public string SozlesmeTipi { get; set; } = string.Empty; // YeniKayit, Yenileme, Guncelleme
        public bool OdemeBekleme { get; set; }

        // Bilgilendirme tercihleri
        public bool SmsBilgilendirme { get; set; }
        public bool EmailBilgilendirme { get; set; }
        public bool TelefonBilgilendirme { get; set; }
        public bool HaberPaylasimi { get; set; }

        // YENİ: Doküman checkbox'ları
        public bool VergiKimlikLevhasıVar { get; set; }
        public bool TicariSicilGazetesiVar { get; set; }
        public bool KimlikOnYuzuVar { get; set; }
        public bool ImzaSirkusuVar { get; set; }

        // YENİ: Dosya adları
        public string? VergiKimlikLevhasıDosyaAdi { get; set; }
        public string? TicariSicilGazetesiDosyaAdi { get; set; }
        public string? KimlikOnYuzuDosyaAdi { get; set; }
        public string? ImzaSirkusuDosyaAdi { get; set; }

        // Müşteri bilgileri
        public string TicariUnvan { get; set; } = string.Empty;
        public string Adi { get; set; } = string.Empty;
        public string Soyadi { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string CepTelefon { get; set; } = string.Empty;
        public string Adres1 { get; set; } = string.Empty;
        public string Adres2 { get; set; } = string.Empty;
        public string Ulke { get; set; } = string.Empty;
        public string Il { get; set; } = string.Empty;
        public string Ilce { get; set; } = string.Empty;
        public string VergiDairesi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WebSitesi { get; set; } = string.Empty;
        public string Referans { get; set; } = string.Empty;
        public int? NeredenDuyduId { get; set; }
        public int? EntegratorId { get; set; }

        // Sistem alanları
        public int Durumu { get; set; } = 1;
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public DateTime? UYBSuresi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public string DokumanNo { get; set; } = string.Empty;
        public DateTime YayinTarihi { get; set; }
        public DateTime RevizeTarihi { get; set; }
        public string RevizyonNo { get; set; } = string.Empty;
        public string? DosyaAdi { get; set; }
        public string? IptalNeden { get; set; }
        public int SozlesmeDurumuId { get; set; } = 1;

        [ForeignKey("SozlesmeDurumuId")]
        public SozlesmeDurumu? SozlesmeDurumu { get; set; }

        [ForeignKey("TeklifId")]
        public virtual Teklif Teklif { get; set; } = null!;

        [ForeignKey("MusteriId")]
        public Musteri Musteri { get; set; } = null!;

        [ForeignKey("EntegratorId")]
        public Entegrator Entegrator { get; set; } = null!;
    }
}