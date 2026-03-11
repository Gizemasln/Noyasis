using Hangfire;
using WepApp.Repositories;
using WepApp.Models;

namespace WepApp.Jobs
{
    public class RegisterKontrolJob
    {
        private readonly MusteriRepository _musteriRepo;
        private readonly KilitRepository _kilitRepo;

        public RegisterKontrolJob()
        {
            _musteriRepo = new MusteriRepository();
            _kilitRepo = new KilitRepository();
        }

        // Bu metod her gece 03:00'te çalışacak
        [AutomaticRetry(Attempts = 3)] // Hata durumunda 3 kez dene
        public void Calistir()
        {
            try
            {
                // Kilit ayarlarını al
                var kilitAyari = _kilitRepo.Getir(x => x.Durumu == 1);

                bool kilitAktif = kilitAyari?.Aktif ?? true;
                int kilitGun = kilitAyari?.Gun ?? 15;

                // Kilit sistemi kapalıysa işlem yapma
                if (!kilitAktif)
                {
                    Console.WriteLine($"{DateTime.Now}: Kilit sistemi kapalı. İşlem yapılmadı.");
                    return;
                }

                Console.WriteLine($"{DateTime.Now}: Register kontrolü başladı. Süre: {kilitGun} gün");

                // Süresi geçen müşterileri bul
                var sinirTarih = DateTime.Now.AddDays(-kilitGun);
                var suresiGecenMusteriler = _musteriRepo.GetirList(x =>
                    x.Register == true &&
                    x.RegisterYapanBayiId != null &&
                    (x.SonTeklifTarihi == null || x.SonTeklifTarihi < sinirTarih));

                Console.WriteLine($"Bulunan müşteri sayısı: {suresiGecenMusteriler.Count}");

                // Her bir müşterinin register durumunu 0 yap
                int islemSayisi = 0;
                foreach (var musteri in suresiGecenMusteriler)
                {
                    musteri.Register = false;
                    musteri.RegisterYapanBayiId = null;
                    musteri.GuncellenmeTarihi = DateTime.Now;

                    _musteriRepo.Guncelle(musteri);

                    Console.WriteLine($"Müşteri serbest bırakıldı: ID={musteri.Id}, Adı={musteri.AdSoyad}");
                    islemSayisi++;
                }

                Console.WriteLine($"{DateTime.Now}: Register kontrolü tamamlandı. {islemSayisi} müşteri serbest bırakıldı.");

                // Job sonucunu Hangfire'a bildir
                if (islemSayisi > 0)
                {
                    Console.WriteLine($"Başarılı: {islemSayisi} müşteri güncellendi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register kontrolü sırasında hata: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Hatayı fırlat ki Hangfire tekrar denesin
                throw new InvalidOperationException($"Register kontrol hatası: {ex.Message}", ex);
            }
        }
    }
}