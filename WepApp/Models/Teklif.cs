using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp.Models
{
    public class Teklif
    {
        [Key]
        public int Id { get; set; }

        public string TeklifNo { get; set; } = string.Empty;

        public int MusteriId { get; set; }

        [ForeignKey("MusteriId")]
        public virtual Musteri Musteri { get; set; } 

        public int LisansTipId { get; set; }

        [ForeignKey("LisansTipId")]
        public virtual LisansTip LisansTip { get; set; } 

        public string? Aciklama { get; set; }

        // Genel grup indirim oranı (% cinsinden)
        public int GrupIndirimOrani { get; set; } = 0;

        // Hesaplanan toplam değerler (PDF ve rapor için cache)
        public decimal ToplamListeFiyat { get; set; } = 0;
        public decimal EgitimSuresi { get; set; }
        public decimal ToplamIndirim { get; set; } = 0;
        public decimal MiktarBazliEkTutar { get; set; } = 0;
        public decimal KdvTutari { get; set; } = 0;           // %20 KDV
        public decimal AraToplam { get; set; } = 0;
        public decimal NetToplam { get; set; } = 0;
        public string? ErtelenmeNedeni { get; set; }

        public int OlusturanKullaniciId { get; set; }
        public int? TeklifDurumId { get; set; }
        public int? NedenlerId { get; set; }

        public bool Aktif { get; set; } = true;
        public bool OnaylandiMi { get; set; } = false;
        public DateTime? OnayTarihi { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public DateTime? GecerlilikTarihi { get; set; }

        // Navigation Properties
        public virtual ICollection<TeklifDetay> Detaylar { get; set; } = new List<TeklifDetay>();

        public TeklifDurum TeklifDurum { get; set; }
        public Nedenler Nedenler { get; set; }
    }
}