using System;
using System.Collections.Generic;

namespace WebApp.Models;

public partial class IletisimBilgileri
{
    public int Id { get; set; }

    public string? Adi { get; set; }

    public string? Telefon1 { get; set; }

    public string? Telefon2 { get; set; }

    public string? Telefon3 { get; set; }

    public string? Telefon4 { get; set; }

    public string? Email1 { get; set; }

    public string? Faks { get; set; }

    public string? Email2 { get; set; }

    public string? IbanNo { get; set; }

    public string? BankaAdi { get; set; }

    public string? GoogleMapsBaglanti { get; set; }

    public string? WhatsApp { get; set; }

    public string? Twitter { get; set; }

    public string? Facebook { get; set; }

    public string? Instagram { get; set; }

    public string? Linkedin { get; set; }

    public string? YouTube { get; set; }
    public string? Adres { get; set; }
    public int Durumu { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    public DateTime? GuncellenmeTarihi { get; set; }
    public int KullanicilarId { get; set; }

    public Kullanicilar Kullanicilar { get; set; }
}
