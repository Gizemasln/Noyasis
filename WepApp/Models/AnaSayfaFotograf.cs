using System;
using System.Collections.Generic;

namespace WebApp.Models;

public partial class AnaSayfaFotograf
{
    public int Id { get; set; }

    public string? FotografBuyuk { get; set; }

    public string? FotografKucuk { get; set; }

    public int? Durumu { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    public DateTime? GuncellenmeTarihi { get; set; }
}
