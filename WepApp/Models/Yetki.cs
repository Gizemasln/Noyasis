// WepApp.Models
public class Yetki
{
    public int Id { get; set; }
    public int Durumu { get; set; }
    public int KullanicilarId { get; set; }  // FK burası
    public DateTime EklenmeTarihi { get; set; }
    public DateTime GuncellenmeTarihi { get; set; }
    public string Adi { get; set; }
    public Kullanicilar Kullanicilar { get; set; }
}
