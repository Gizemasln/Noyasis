using System;
using System.Collections.Generic;

namespace WebApp.Models;

public partial class AnaSayfaRakamlari
{
    public int Id { get; set; }

    public string? Baslik { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    public DateTime? GuncellenmeTarihi { get; set; }

    public int? Durumu { get; set; }
    public string? Sayi { get; set; }
}
