using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApp.Models;
namespace WebApp.Repositories
{
    public class GenericRepository<T> where T : class, new()
    {
        Context c = new Context();

        public List<T> Listele()
        {
            return c.Set<T>().ToList();
        }
        public void Ekle(T entity)
        {
            c.Set<T>().Add(entity);
            c.SaveChanges();
        }
        public void Sil(T entity)
        {
            c.Set<T>().Remove(entity);
            c.SaveChanges();
        }
        public void Guncelle(T entity)
        {
            c.Set<T>().Update(entity);
            c.SaveChanges();

        }
        public IQueryable<T> Query()
        {
            return c.Set<T>().AsQueryable().AsNoTracking();
        }

        public T Getir(int Id)
        {
            return c.Set<T>().Find(Id);

        }
        public T Getir(Expression<Func<T, bool>> predicate)
        {
            return c.Set<T>().Where(predicate).FirstOrDefault();
        }
        public List<T> GetirList(Expression<Func<T, bool>> predicate, List<string> SqlJoinTables)
        {
            IQueryable<T> query = c.Set<T>();

            if (SqlJoinTables != null && SqlJoinTables.Count > 0)
            {
                query = AddIncludeClauses(query, SqlJoinTables);
            }

            return query.Where(predicate).ToList();
        }
        public T Getir(Expression<Func<T, bool>> predicate, List<string> SqlJoinTables)
        {
            IQueryable<T> query = c.Set<T>();

            if (SqlJoinTables != null && SqlJoinTables.Count > 0)
            {
                query = AddIncludeClauses(query, SqlJoinTables);
            }

            return query.Where(predicate).FirstOrDefault();
        }
        public List<T> GetirList(Expression<Func<T, bool>> predicate)
        {
         return c.Set<T>().Where(predicate).ToList();
        }
        private IQueryable<T> AddIncludeClauses(IQueryable<T> query, List<string> SqlJoinTables)
        {
            foreach (string table in SqlJoinTables)
            {
                // Assuming table is a navigation property in your entities
                query = query.AsNoTracking().Include(table);
            }

            return query;
        }

        public IQueryable<T> GetirQueryable(
         Expression<Func<T, bool>>? filter = null,
         List<string>? includeTables = null)
        {
            try
            {
                IQueryable<T> query = c.Set<T>().AsQueryable();

                // Include tabloları ekle
                if (includeTables != null && includeTables.Any())
                {
                    foreach (string table in includeTables)
                    {
                        query = query.Include(table);
                    }
                }

                // Filter varsa uygula
                if (filter != null)
                    query = query.Where(filter);

                return query;
            }
            catch (Exception ex)
            {
                throw new Exception($"Queryable sorgu hatası: {ex.Message}", ex);
            }
        }







        public List<T> Listele(string p)
        {
            return c.Set<T>().Include(p).ToList();
        }
        public List<T> Listele(string p1,string p2)
        {
            return c.Set<T>().Include(p1).Include(p2).ToList();
        }
        public List<T> Listele(string p1, string p2,string p3)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3,string p4)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4,string p5)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4, string p5,string p6)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).Include(p6).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4, string p5, string p6,string p7)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).Include(p6).Include(p7).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4, string p5, string p6, string p7,string p8)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).Include(p6).Include(p7).Include(p8).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8,string p9)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).Include(p6).Include(p7).Include(p8).Include(p9).ToList();
        }
        public List<T> Listele(string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8, string p9,string p10)
        {
            return c.Set<T>().Include(p1).Include(p2).Include(p3).Include(p4).Include(p5).Include(p6).Include(p7).Include(p8).Include(p9).Include(p10).ToList();
        }
        public void Ekle(List<T> entity)
        {
            c.Set<T>().AddRange(entity);
            c.SaveChanges();
        }
        // Asenkron liste getir (filter ve include ile)
        public async Task<List<T>> GetirListAsync(
            Expression<Func<T, bool>>? filter = null,
            List<string>? includeTables = null)
        {
            IQueryable<T> query = c.Set<T>();

            if (includeTables != null && includeTables.Any())
            {
                foreach (string include in includeTables)
                {
                    query = query.Include(include);
                }
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }

        // Sadece filter ile asenkron
        public async Task<List<T>> GetirListAsync(Expression<Func<T, bool>> filter)
        {
            return await c.Set<T>().Where(filter).ToListAsync();
        }

        // Include'lu asenkron tek kayıt
        public async Task<T?> GetirAsync(Expression<Func<T, bool>> predicate, List<string> includeTables)
        {
            IQueryable<T> query = c.Set<T>();

            if (includeTables != null && includeTables.Any())
            {
                foreach (string include in includeTables)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        // Basit asenkron tek kayıt
        public async Task<T?> GetirAsync(Expression<Func<T, bool>> predicate)
        {
            return await c.Set<T>().FirstOrDefaultAsync(predicate);
        }

        // ID ile asenkron getir
        public async Task<T?> GetirAsync(int id)
        {
            return await c.Set<T>().FindAsync(id);
        }

    }
}
