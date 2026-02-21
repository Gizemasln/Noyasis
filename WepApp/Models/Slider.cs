using System;
using WepApp.Models;

namespace WebApp.Models
{
    public class Slider
    {
        public int Id { get; set; }
        public int Durumu { get; set; } // 1:Aktif, 0:Pasif
        public int SlaytSiraNo { get; set; }
        public string? SlaytBaslik { get; set; }
        public string? SlaytUrl { get; set; }
        public string? SlaytButonAdi { get; set; }
        public int YeniSekmeMi { get; set; } // 1:Evet, 0:Hayır
        public string? GorselYolu { get; set; }
        public string? VideoYolu { get; set; } // Video dosya yolu (VideoUrl yerine)
        public int YayindaMi { get; set; } // 1:Yayında, 0:Yayında Değil
        public string? Aciklama { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }

    }
}