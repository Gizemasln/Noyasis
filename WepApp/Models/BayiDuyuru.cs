namespace WepApp.Models
{
    public class BayiDuyuru
    {   
        public int Id { get; set; }
        public int? Durumu { get; set; }
        public string? Baslik { get; set; }
        public string? Metin { get; set; }
        public DateTime? EklenmeTarihi { get; set; } 
        public DateTime? GuncellenmeTarihi { get; set; }
        public int? YayindaMi { get; set; }
        public int? Oncelik { get; set; } = 0;
        public DateTime? YayinBaslangicTarihi { get; set; }
        public DateTime? YayinBitisTarihi { get; set; }
        public int? KullanicilarId { get; set; }
    }
}
