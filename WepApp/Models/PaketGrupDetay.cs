namespace WepApp.Models
{
    public class PaketGrupDetay
    {
        public int Id { get; set; }
        public int Durumu { get; set; }
        public int PaketGrupId { get; set; }
        public int PaketId { get; set; }
        public int EkleyenKullaniciId { get; set; }
        public int GuncelleyenKullaniciId { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public DateTime GuncellenmeTarihi { get; set; }
        public Paket Paket { get; set; }    
        public PaketGrup PaketGrup { get; set; }
    }
}
