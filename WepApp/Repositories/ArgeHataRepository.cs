using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class ArgeHataRepository : GenericRepository<ArgeHata>
    {
        private readonly Context _context;

        public ArgeHataRepository()
        {
            _context = new Context();
        }

        public ArgeHataRepository(Context context)
        {
            _context = context;
        }
        // Kullanıcı tipine göre filtreleme ile listeleme (include yok)
        public List<ArgeHata> GetirListe(string kullaniciTipi, int? kullaniciId, int page = 1, int pageSize = 10)
        {
            try
            {
                Console.WriteLine($"GetirListe: KullanıcıTipi={kullaniciTipi}, KullanıcıId={kullaniciId}");

                Expression<Func<ArgeHata, bool>> filter = null;

                if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
                    filter = x => x.MusteriId == kullaniciId;
                else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
                    filter = x => x.BayiId == kullaniciId;

                IQueryable<ArgeHata> query = GetirQueryable(filter); // include olmadan

                int count = query.Count();
                Console.WriteLine($"Sorgu sonucu öncesi toplam kayıt: {count}");

                List<ArgeHata> result = query
                    .OrderByDescending(x => x.EklenmeTarihi)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                Console.WriteLine($"GetirListe: {result.Count} kayıt döndü");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetirListe hatası: {ex.Message}");
                throw new Exception($"Listeleme hatası: {ex.Message}", ex);
            }
        }
        // ArgeHataRepository sınıfınıza ekleyin
        public IQueryable<ArgeHata> GetirQueryable(Expression<Func<ArgeHata, bool>> filter = null, List<string> includePaths = null)
        {
            IQueryable<ArgeHata> query = _context.ArgeHata.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includePaths != null)
            {
                foreach (var path in includePaths)
                {
                    query = query.Include(path);
                }
            }

            return query;
        }
        public int GetirToplamSayi(string kullaniciTipi, int? kullaniciId)
        {
            try
            {
                Expression<Func<ArgeHata, bool>> filter = null;
                if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
                    filter = x => x.MusteriId == kullaniciId;
                else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
                    filter = x => x.BayiId == kullaniciId;

                int count = filter != null ? GetirQueryable(filter).Count() : GetirQueryable().Count();
                Console.WriteLine($"GetirToplamSayi: {count} kayıt");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetirToplamSayi hatası: {ex.Message}");
                throw;
            }
        }




        // Aktif lisans tipleri
        public new List<LisansTip> GetirAktifLisansTipleri()
        {
            try
            {
                GenericRepository<LisansTip> lisansTipRepo = new GenericRepository<LisansTip>();
                return lisansTipRepo.GetirList(x => x.Durumu == 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetirAktifLisansTipleri hatası: {ex.Message}");
                throw new Exception($"Lisans tipleri getirme hatası: {ex.Message}", ex);
            }
        }
    }
}