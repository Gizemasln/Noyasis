using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp.Models
{
    public class TeklifDetay
    {
     
            [Key]
            public int Id { get; set; }

            public int TeklifId { get; set; }
            public int Durumu { get; set; }

            [ForeignKey("TeklifId")]
            public virtual Teklif Teklif { get; set; } = null!;

            // "grup" → PaketGrup, "paket" → Paket
            [Required]
            [StringLength(10)]
            public string Tip { get; set; } = string.Empty; // "grup" | "paket"

            // Hangi nesneye ait?
            public int? PaketGrupId { get; set; }
            public int? PaketId { get; set; }

            [ForeignKey("PaketGrupId")]
            public virtual PaketGrup? PaketGrup { get; set; }

            [ForeignKey("PaketId")]
            public virtual Paket? Paket { get; set; }

            // Ekran üzerinde gösterilecek isimler (snapshot olduğu için saklıyoruz)
            public string ItemAdi { get; set; } = string.Empty;
            public string? PaketGrupAdi { get; set; } // Grup seçildiyse burası dolu olur

            // Fiyat & İndirim Bilgileri
            public decimal ListeFiyati { get; set; } = 0;           // Normal fiyat
            public decimal? KampanyaFiyati { get; set; }           // KFiyat varsa
            public int KampanyaIndirimYuzdesi { get; set; } = 0;
            public string? KampanyaBaslik { get; set; }

            public int BireyselIndirimYuzdesi { get; set; } = 0;    // Paketin kendi IndOran'ı
            public int GrupIndirimYuzdesi { get; set; } = 0;       // Teklifteki genel grup indirimi

            public int Miktar { get; set; } = 1;
            public decimal MiktarBazliEkOranYuzde { get; set; } = 0; // Miktar kademesinden gelen ek oran

            // Son hesaplanan değerler
            public decimal BirimFiyatNet { get; set; } = 0;
            public decimal SatirToplamNet { get; set; } = 0;

            // Paket bozuldu mu? (Grup seçiliyken bir modül kaldırıldıysa)
            public bool BagimsizModulMu { get; set; } = false;

            public int SiraNo { get; set; } = 0;

            public DateTime EklenmeTarihi { get; set; } 
            public DateTime GuncellenmeTarihi { get; set; } 
        }
    }