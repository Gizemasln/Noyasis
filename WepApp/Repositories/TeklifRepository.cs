using WebApp.Repositories;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class TeklifRepository : GenericRepository<Teklif>
    {
        // TeklifRepository.cs'ye eklenecek metod

        public void MusterininSonTeklifTarihiniGuncelle(int musteriId)
        {
            var musteriRepo = new MusteriRepository();
            var musteri = musteriRepo.Getir(musteriId);

            if (musteri != null)
            {
                // Bu müşteri için en son teklifi bul
                var sonTeklif = GetirList(x => x.MusteriId == musteriId && x.Aktif == true)
                                .OrderByDescending(x => x.EklenmeTarihi)
                                .FirstOrDefault();

                if (sonTeklif != null)
                {
                    musteri.SonTeklifTarihi = sonTeklif.EklenmeTarihi ?? DateTime.Now;
                    musteri.GuncellenmeTarihi = DateTime.Now;
                    musteriRepo.Guncelle(musteri);
                }
            }
        }
    }
}
