// WebApp.Models
using WepApp.Models;

public partial class Kullanicilar
{
    public int Id { get; set; }
    public int YetkiId { get; set; } // istersen bunu kaldırabilirsin, çünkü FK Yetki tablosunda

    public string? Adi { get; set; }
    public string? Sifre { get; set; }
    public string? Telefon { get; set; }
    public int? Durumu { get; set; }
    public DateTime? EklenmeTarihi { get; set; }
    public DateTime? GuncellenmeTarihi { get; set; }

    public Yetki Yetki { get; set; }
}
