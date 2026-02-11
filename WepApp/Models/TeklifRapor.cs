namespace WepApp.Models
{
    public class TeklifRapor
    {
        public string TeklifNo { get; set; } = "";
        public string LisansTipi { get; set; } = "";
        public string Tarih { get; set; } = "";
        public string MusteriAdi { get; set; } = "";
        public string MusteriTelefon { get; set; } = "";
        public string MusteriYetkili { get; set; } = "";
        public string GecerlilikTarihi { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public string Ek1 { get; set; } = "";
        public string Ek2 { get; set; } = "";
        public string Ek3 { get; set; } = "";
        public string Ek4 { get; set; } = "";

        // Tutarlar (her satırda aynı olacak)
        public string AraToplam { get; set; } = "0,00";
        public string Toplam { get; set; } = "0,00";

        public string IndirimToplam { get; set; } = "0,00";
        public string KdvTutar { get; set; } = "0,00";
        public string GenelToplam { get; set; } = "0,00";
        public string YaziIle { get; set; } = "";
        public bool AltSatirMi { get; set; }

        // Satır Detayları
        public string UrunAdi { get; set; } = "";           // Grup adı veya modül adı
        public string Miktar { get; set; } = "";            // "1 Lisans" veya boş
        public string IndirimYuzde { get; set; } = "";      // "80%" veya boş
        public string Tutar { get; set; } = "";             // "18.900,00 ₺" veya boş
        public string Girinti { get; set; } = "";
    }
}
