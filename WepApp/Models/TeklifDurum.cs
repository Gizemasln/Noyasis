namespace WepApp.Models
{
    public class TeklifDurum
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int Sıra { get; set; }
        public string Adi { get; set; }
        public string? Aciklama { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
    }
}
