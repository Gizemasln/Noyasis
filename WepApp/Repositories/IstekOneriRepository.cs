// Repositories/IstekOneriRepository.cs
using WepApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Repositories;

namespace WepApp.Repositories
{
    public class IstekOneriRepository : GenericRepository<IstekOneriler>
    {
        private readonly Context _context;

        public IstekOneriRepository()
        {
            _context = new Context();
        }

        public IstekOneriler Getir(int id)
        {
            return _context.IstekOneriler
                .Include(x => x.Musteri)
                .Include(x => x.Bayi)
                .Include(x => x.LisansTip)
                .FirstOrDefault(x => x.Id == id && x.Durumu == 1);
        }

        public List<IstekOneriler> GetirList(Func<IstekOneriler, bool> where = null)
        {
            IQueryable<IstekOneriler> query = _context.IstekOneriler
                .Include(x => x.Musteri)
                .Include(x => x.Bayi)
                .Include(x => x.LisansTip)
                .Where(x => x.Durumu == 1);

            if (where != null)
                query = (IQueryable<IstekOneriler>)query.Where(where);

            return query.ToList();
        }

        public IQueryable<IstekOneriler> GetirQueryable()
        {
            return _context.IstekOneriler
                .Include(x => x.Musteri)
                .Include(x => x.Bayi)
                .Include(x => x.LisansTip)
                .Where(x => x.Durumu == 1);
        }

        public List<IstekOneriler> GetirBayiyeAitListesi(int bayiId, int page = 1, int pageSize = 10)
        {
            return _context.IstekOneriler
                .Include(x => x.LisansTip)
                .Include(x => x.Musteri)
                .Where(x => x.BayiId == bayiId && x.Durumu == 1)
                .OrderByDescending(x => x.EklenmeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<IstekOneriler> GetirMusteriyeAitListesi(int musteriId, int page = 1, int pageSize = 10)
        {
            return _context.IstekOneriler
                .Include(x => x.LisansTip)
                .Where(x => x.MusteriId == musteriId && x.Durumu == 1)
                .OrderByDescending(x => x.EklenmeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<IstekOneriler> GetirMusteriListesi(List<int> musteriIdler, int page = 1, int pageSize = 10)
        {
            return _context.IstekOneriler
                .Include(x => x.LisansTip)
                .Include(x => x.Musteri)
                .Where(x => x.MusteriId.HasValue && musteriIdler.Contains(x.MusteriId.Value) && x.Durumu == 1)
                .OrderByDescending(x => x.EklenmeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetirBayiyeAitToplamSayi(int bayiId)
        {
            return _context.IstekOneriler
                .Count(x => x.BayiId == bayiId && x.Durumu == 1);
        }

        public int GetirMusteriyeAitToplamSayi(int musteriId)
        {
            return _context.IstekOneriler
                .Count(x => x.MusteriId == musteriId && x.Durumu == 1);
        }

        public int GetirMusteriToplamSayi(List<int> musteriIdler)
        {
            return _context.IstekOneriler
                .Count(x => x.MusteriId.HasValue && musteriIdler.Contains(x.MusteriId.Value) && x.Durumu == 1);
        }

        public IstekOneriler GetirById(int id, int bayiId)
        {
            return _context.IstekOneriler
                .Include(x => x.LisansTip)
                .FirstOrDefault(x => x.Id == id && x.BayiId == bayiId && x.Durumu == 1);
        }

        public IstekOneriler GetirById(int id, int? musteriId, int? bayiId)
        {
            var query = _context.IstekOneriler
                .Include(x => x.LisansTip)
                .Include(x => x.Musteri)
                .Include(x => x.Bayi);

            if (musteriId.HasValue)
                return query.FirstOrDefault(x => x.Id == id && x.MusteriId == musteriId.Value && x.Durumu == 1);
            else if (bayiId.HasValue)
                return query.FirstOrDefault(x => x.Id == id && x.BayiId == bayiId.Value && x.Durumu == 1);

            return query.FirstOrDefault(x => x.Id == id && x.Durumu == 1);
        }

        public List<LisansTip> GetirAktifLisansTipleri()
        {
            return _context.LisansTip
                .Where(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();
        }

        public void Ekle(IstekOneriler entity)
        {
            entity.EklenmeTarihi = DateTime.Now;
            entity.GuncellenmeTarihi = DateTime.Now;
            entity.Durumu = 1;
            _context.IstekOneriler.Add(entity);
            _context.SaveChanges();
        }

        public void Guncelle(IstekOneriler entity)
        {
            entity.GuncellenmeTarihi = DateTime.Now;
            _context.IstekOneriler.Update(entity);
            _context.SaveChanges();
        }

        public void Sil(int id)
        {
            IstekOneriler entity = _context.IstekOneriler.Find(id);
            if (entity != null)
            {
                entity.Durumu = 0;
                entity.GuncellenmeTarihi = DateTime.Now;
                _context.SaveChanges();
            }
        }
    }
}