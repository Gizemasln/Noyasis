// Models/Lokasyon.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Lokasyon
    {
        public int Id { get; set; }


        public string SehirAdi { get; set; } // e.g., "İstanbul"

        public string Adres { get; set; }


        public string Tip { get; set; } // e.g., "Bölge Müdürlüğü"

        public string Telefon { get; set; }


        public string Email { get; set; }

        public int Durumu { get; set; } = 1; // 1: Aktif, 0: Pasif

        public DateTime? EklenmeTarihi { get; set; }

        public DateTime? GuncellenmeTarihi { get; set; }

        public int? KullanicilarId { get; set; } // Ekleyen/Güncelleyeni işaret et
    }
}