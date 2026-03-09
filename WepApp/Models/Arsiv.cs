using System;

namespace WepApp.Models
{
    public class Arsiv
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int Gun { get; set; }
        public int? EkleyenKullaniciId { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
    }
}