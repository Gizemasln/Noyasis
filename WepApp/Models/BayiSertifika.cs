

using WebApp.Models;

namespace WepApp.Models
{
    public class BayiSertifika
    {

        public int Id { get; set; }


        public int BayiId { get; set; } // Sertifikanın ait olduğu bayi
        public string SertifikaAdi { get; set; } // Sertifika ismi (örneğin: "Teknik Eğitim Sertifikası")

        public string Aciklama { get; set; } // Sertifika hakkında kısa açıklama

        public DateTime VerilisTarihi { get; set; } // Sertifikanın verildiği tarih

        public DateTime? GecerlilikTarihi { get; set; } // Sertifikanın geçerli olduğu son tarih

        public string DosyaYolu { get; set; } // PDF veya resim dosya yolu (örneğin: /uploads/sertifikalar/sertifika1.pdf)

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public DateTime GuncellenmeTarihi { get; set; } = DateTime.Now;
        public int Durumu { get; set; }
        public int KullanicilarId { get; set; }
        public Kullanicilar Kullanicilar { get; set; }
        public Bayi Bayi { get; set; }

    }
}
