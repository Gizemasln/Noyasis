

using WebApp.Models;

namespace WepApp.Models;

public partial class HakkimizdaFotograf
{
    public int Id { get; set; }

    public string? FotografBuyuk { get; set; }

    public string? FotografKucuk { get; set; }

    public int? Durumu { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    public DateTime? GuncellenmeTarihi { get; set; }
    public int KullanicilarId { get; set; }

    public Kullanicilar Kullanicilar { get; set; }
}
