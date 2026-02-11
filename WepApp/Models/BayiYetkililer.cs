namespace WepApp.Models
{
    public class BayiYetkililer
    {
        public int Id { get; set; }
        public int? Aktif { get; set; }
        public int? DepartmanId { get; set; }
        public int? BayiId { get; set; }
        public int? EkleyenKullaniciId { get; set; }
        public int? GuncelleyenKullaniciId { get; set; }
        public string? Kodu { get; set; }
        public string? Adi { get; set; }
        public string? Soyadi { get; set; }
        public string? Cinsiyet { get; set; }
        public string? Gorevi { get; set; }
        public string? DahiliNo { get; set; }
        public string? Cep { get; set; }
        public string? Email { get; set; }
        public int? Durumu { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public Departman Departman { get; set; }
        public Bayi Bayi { get; set; }

    }
}
