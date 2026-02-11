namespace WepApp.Models
{
    public class MusteriYetkililer
    {
        public int Id { get; set; }
        public int? Aktif { get; set; } = 1; // 1 = Aktif, 0 = Pasif
        public int? DepartmanId { get; set; }
        public int? MusteriId { get; set; }
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
        public Departman? Departman { get; set; }
        public Musteri? Musteri { get; set; }

    }
}
