using System.Data.Entity;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class PaketBaglantiRepository : GenericRepository<PaketBaglanti>
    {
        Context _context = new Context();
        // Paketin bağlı olduğu tüm paketleri getir
        public List<Paket> GetBagliPaketler(int paketId, List<string> includes = null)
        {
            var query = _context.PaketBaglanti
                .Where(x => x.PaketId == paketId && x.Durumu == 1)
                .Select(x => x.BagliPaket);

            if (includes != null && includes.Any())
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query.ToList();
        }

        // Çoklu paket bağlantısı ekle - DÜZELTİLDİ
        public void CokluBaglantiEkle(int paketId, List<int> bagliPaketIds, int kullaniciId)
        {
            // Tüm mevcut bağlantıları al (aktif ve pasif dahil)
            var tumMevcutBaglantilar = _context.PaketBaglanti
                .Where(x => x.PaketId == paketId)
                .ToList();

            // Mevcut bağlantıların ID'lerini al
            var mevcutBagliPaketIds = tumMevcutBaglantilar
                .Select(x => x.BagliPaketId)
                .ToList();

            // Eklenecek yeni bağlantıları bul
            var yeniEklenecekIds = bagliPaketIds
                .Where(id => !mevcutBagliPaketIds.Contains(id) && id != paketId)
                .ToList();

            // Kaldırılacak bağlantıları bul (pasif yapılacak)
            var kaldirilacakIds = mevcutBagliPaketIds
                .Where(id => !bagliPaketIds.Contains(id))
                .ToList();

            // Yeni bağlantıları ekle
            foreach (var bagliPaketId in yeniEklenecekIds)
            {
                var yeniBaglanti = new PaketBaglanti
                {
                    PaketId = paketId,
                    BagliPaketId = bagliPaketId,
                    Durumu = 1,
                    EkleyenKullaniciId = kullaniciId,
                    GuncelleyenKullaniciId = kullaniciId,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };
                Ekle(yeniBaglanti);
            }

            // Aktifleştirilecek bağlantıları bul (zaten var ama pasif)
            var aktiflestirilecekler = tumMevcutBaglantilar
                .Where(x => bagliPaketIds.Contains(x.BagliPaketId) && x.Durumu == 0)
                .ToList();

            foreach (var baglanti in aktiflestirilecekler)
            {
                baglanti.Durumu = 1;
                baglanti.GuncelleyenKullaniciId = kullaniciId;
                baglanti.GuncellenmeTarihi = DateTime.Now;
                Guncelle(baglanti);
            }

            // Pasifleştirilecek bağlantıları bul (seçili değil ama aktif)
            var pasiflestirilecekler = tumMevcutBaglantilar
                .Where(x => kaldirilacakIds.Contains(x.BagliPaketId) && x.Durumu == 1)
                .ToList();

            foreach (var baglanti in pasiflestirilecekler)
            {
                baglanti.Durumu = 0;
                baglanti.GuncelleyenKullaniciId = kullaniciId;
                baglanti.GuncellenmeTarihi = DateTime.Now;
                Guncelle(baglanti);
            }

            _context.SaveChanges();
        }

        // Alternatif: Daha basit versiyon
        public void CokluBaglantiEkle2(int paketId, List<int> bagliPaketIds, int kullaniciId)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Önce tüm mevcut bağlantıları pasif yap
                var mevcutBaglantilar = _context.PaketBaglanti
                    .Where(x => x.PaketId == paketId && x.Durumu == 1)
                    .ToList();

                foreach (var baglanti in mevcutBaglantilar)
                {
                    baglanti.Durumu = 0;
                    baglanti.GuncelleyenKullaniciId = kullaniciId;
                    baglanti.GuncellenmeTarihi = DateTime.Now;
                }

                // Yeni bağlantıları ekle (tekrar eklenmeye çalışılanları kontrol et)
                foreach (var bagliPaketId in bagliPaketIds.Distinct())
                {
                    if (paketId == bagliPaketId) continue; // Kendi kendine bağlama

                    // Önce kontrol et, belki zaten var (pasif durumda)
                    var mevcut = _context.PaketBaglanti
                        .FirstOrDefault(x => x.PaketId == paketId && x.BagliPaketId == bagliPaketId);

                    if (mevcut != null)
                    {
                        // Zaten varsa aktif yap
                        mevcut.Durumu = 1;
                        mevcut.GuncelleyenKullaniciId = kullaniciId;
                        mevcut.GuncellenmeTarihi = DateTime.Now;
                    }
                    else
                    {
                        // Yoksa yeni ekle
                        var yeniBaglanti = new PaketBaglanti
                        {
                            PaketId = paketId,
                            BagliPaketId = bagliPaketId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullaniciId,
                            GuncelleyenKullaniciId = kullaniciId,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _context.PaketBaglanti.Add(yeniBaglanti);
                    }
                }

                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Çapraz bağlantı kontrolü
        public bool CaprazBaglantiKontrol(int paketId, int bagliPaketId)
        {
            return _context.PaketBaglanti
                .Any(x => x.PaketId == bagliPaketId &&
                         x.BagliPaketId == paketId &&
                         x.Durumu == 1);
        }

        public List<Paket> GetAnaPaketler(int bagliPaketId, List<string> includes = null)
        {
            var query = _context.PaketBaglanti
                .Where(x => x.BagliPaketId == bagliPaketId && x.Durumu == 1)
                .Select(x => x.Paket);

            if (includes != null && includes.Any())
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query.ToList();
        }
        public List<int> GetTumBagliPaketlerRecursive(int paketId)
        {
            var tumBagliPaketler = new List<int>();
            GetBagliPaketlerRecursive(paketId, tumBagliPaketler);
            return tumBagliPaketler.Distinct().ToList();
        }

        private void GetBagliPaketlerRecursive(int paketId, List<int> tumBagliPaketler)
        {
            var bagliPaketler = _context.PaketBaglanti
                .Where(x => x.PaketId == paketId && x.Durumu == 1)
                .Select(x => x.BagliPaketId)
                .ToList();

            foreach (var bagliPaket in bagliPaketler)
            {
                if (!tumBagliPaketler.Contains(bagliPaket))
                {
                    tumBagliPaketler.Add(bagliPaket);
                    GetBagliPaketlerRecursive(bagliPaket, tumBagliPaketler);
                }
            }
        }
    }
}