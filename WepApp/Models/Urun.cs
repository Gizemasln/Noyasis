// WepApp.Models/Urun.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp.Models
{
    public class Urun
    {
        public int Id { get; set; }

        [Required]
        public string Adi { get; set; }

        public string Aciklama { get; set; }

        public int KategoriId { get; set; }

        public int Stok { get; set; } // 1 = Var, 0 = Yok

        public string UrunKodu { get; set; }

        public int Durumu { get; set; } = 1; // 1 = Aktif

        public DateTime EklenmeTarihi { get; set; }

        public DateTime GuncellenmeTarihi { get; set; }

        // FİYAT
        [Required]
        public decimal Fiyat { get; set; }
        public decimal SonFiyat { get; set; }

        // SÜRESİZ İNDİRİM
        public decimal? SureSizIndirimYuzdesi { get; set; } // %20, %50 vs. null = yok

        // Hesaplanan indirimli fiyat (süresiz)
        [NotMapped]
        public decimal SureSizIndirimliFiyat =>
            SureSizIndirimYuzdesi.HasValue
                ? Math.Round(Fiyat * (1 - SureSizIndirimYuzdesi.Value / 100), 2)
                : Fiyat;

        // Navigasyon
        public Kategori Kategori { get; set; }
        public List<UrunGaleri> UrunGaleri { get; set; }
        public int KullanicilarId { get; set; }
        public Kullanicilar Kullanicilar { get; set; }

    }
}