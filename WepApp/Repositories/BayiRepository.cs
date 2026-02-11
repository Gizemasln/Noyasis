using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class BayiRepository
    {
        private readonly Context _context;

        public BayiRepository()
        {
            _context = new Context();
        }

        public BayiRepository(Context context)
        {
            _context = context;
        }

        // Tüm bayileri getir (include ile)
        public List<Bayi> GetirList(Expression<Func<Bayi, bool>>? filter = null)
        {
            try
            {
                IQueryable<Bayi> query = _context.Bayi
                   
                    .Include(b => b.UstBayi)
                    .AsQueryable();

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi listesi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Tek bir bayi getir (include ile)
        public Bayi? Getir(int id)
        {
            try
            {
                return _context.Bayi
                    
                    .Include(b => b.UstBayi)
                    .Include(b => b.AltBayiler)
                    .FirstOrDefault(b => b.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Bayi ekle
        public bool Ekle(Bayi bayi)
        {
            try
            {
                // Kullanıcı adı kontrolü
                Bayi existingBayi = _context.Bayi
                    .FirstOrDefault(b => b.KullaniciAdi == bayi.KullaniciAdi && b.Durumu == 1);

                if (existingBayi != null)
                {
                    throw new Exception("Bu kullanıcı adı zaten kullanılıyor.");
                }

                _context.Bayi.Add(bayi);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi eklenirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Bayi güncelle
        public bool Guncelle(Bayi bayi)
        {
            try
            {
                Bayi existing = _context.Bayi.Find(bayi.Id);
                if (existing == null)
                {
                    throw new Exception("Bayi bulunamadı.");
                }

                // Kullanıcı adı kontrolü (kendisi hariç)
                Bayi duplicateBayi = _context.Bayi
                    .FirstOrDefault(b => b.KullaniciAdi == bayi.KullaniciAdi
                                      && b.Id != bayi.Id
                                      && b.Durumu == 1);

                if (duplicateBayi != null)
                {
                    throw new Exception("Bu kullanıcı adı başka bir bayi tarafından kullanılıyor.");
                }

                _context.Entry(existing).CurrentValues.SetValues(bayi);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi güncellenirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Bayi sil (soft delete)
        public bool Sil(int id)
        {
            try
            {
                Bayi bayi = _context.Bayi.Find(id);
                if (bayi == null)
                {
                    throw new Exception("Bayi bulunamadı.");
                }

                // Alt bayi kontrolü
                int altBayiler = _context.Bayi
                    .Count(b => b.UstBayiId == id && b.Durumu == 1);

                if (altBayiler > 0)
                {
                    throw new Exception("Bu bayinin aktif alt bayileri var. Önce alt bayileri silmelisiniz.");
                }

                bayi.Durumu = 0;
                bayi.GuncellenmeTarihi = DateTime.Now;
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi silinirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Bayi hiyerarşisini getir (tüm alt bayiler dahil)
        public List<Bayi> GetBayiHiyerarsi(int bayiId)
        {
            try
            {
                List<Bayi> hiyerarsi = new List<Bayi>();
                Bayi bayi = Getir(bayiId);

                if (bayi == null) return hiyerarsi;

                hiyerarsi.Add(bayi);
                GetAltBayilerRecursive(bayiId, hiyerarsi);

                return hiyerarsi;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi hiyerarşisi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Rekürsif olarak alt bayileri getir
        private void GetAltBayilerRecursive(int ustBayiId, List<Bayi> hiyerarsi)
        {
            List<Bayi> altBayiler = _context.Bayi
              
                .Include(b => b.UstBayi)
                .Where(b => b.UstBayiId == ustBayiId && b.Durumu == 1)
                .ToList();

            foreach (var altBayi in altBayiler)
            {
                hiyerarsi.Add(altBayi);
                GetAltBayilerRecursive(altBayi.Id, hiyerarsi);
            }
        }

        // Ana bayileri getir (üst bayisi olmayan)
        public List<Bayi> GetAnaBayiler(int? musteriId = null)
        {
            try
            {
                IQueryable<Bayi> query = _context.Bayi
                    
                    .Where(b => b.UstBayiId == null && b.Durumu == 1);


                return query.OrderBy(b => b.Unvan).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ana bayiler getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Belirli seviyedeki bayileri getir
        public List<Bayi> GetBayilerBySeviye(int seviye)
        {
            try
            {
                return _context.Bayi
                   
                    .Include(b => b.UstBayi)
                    .Where(b => b.Seviye == seviye && b.Durumu == 1)
                    .OrderBy(b => b.Unvan)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Seviye {seviye} bayileri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteriye ait tüm bayileri getir (ana ve alt bayiler dahil)
        public List<Bayi> GetBayilerByMusteri(int musteriId)
        {
            try
            {
                // Önce ana bayileri al
                List<Bayi> anaBayiler = _context.Bayi
                    .Where(b =>  b.Durumu == 1)
                    .ToList();

                List<Bayi> tumBayiler = new List<Bayi>();
                foreach (var anaBayi in anaBayiler)
                {
                    tumBayiler.AddRange(GetBayiHiyerarsi(anaBayi.Id));
                }

                return tumBayiler.Distinct().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri bayileri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Üst bayileri getir (ana bayiye kadar)
        public List<Bayi> GetUstBayiler(int bayiId)
        {
            try
            {
                List<Bayi> ustBayiler = new List<Bayi>();
                Bayi bayi = Getir(bayiId);

                while (bayi?.UstBayiId != null)
                {
                    bayi = Getir(bayi.UstBayiId.Value);
                    if (bayi != null)
                    {
                        ustBayiler.Add(bayi);
                    }
                }

                return ustBayiler;
            }
            catch (Exception ex)
            {
                throw new Exception($"Üst bayiler getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Ana bayiyi getir (herhangi bir bayiden)
        public Bayi? GetAnaBayi(int bayiId)
        {
            try
            {
                Bayi bayi = Getir(bayiId);
                if (bayi == null) return null;

                while (bayi.UstBayiId != null)
                {
                    Bayi ustBayi = Getir(bayi.UstBayiId.Value);
                    if (ustBayi == null) break;
                    bayi = ustBayi;
                }

                return bayi;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ana bayi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Alt bayi sayısını getir
        public int GetAltBayiSayisi(int bayiId, bool recursive = false)
        {
            try
            {
                if (recursive)
                {
                    // Tüm alt bayiler (rekürsif)
                    List<Bayi> hiyerarsi = GetBayiHiyerarsi(bayiId);
                    return hiyerarsi.Count - 1; // Kendisi hariç
                }
                else
                {
                    // Sadece direkt alt bayiler
                    return _context.Bayi
                        .Count(b => b.UstBayiId == bayiId && b.Durumu == 1);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Alt bayi sayısı hesaplanırken hata oluştu: {ex.Message}", ex);
            }
        }

        // Bayi istatistikleri
        public Dictionary<string, int> GetBayiIstatistikleri()
        {
            try
            {
                return new Dictionary<string, int>
                {
                    ["ToplamBayi"] = _context.Bayi.Count(b => b.Durumu == 1),
                    ["AnaBayiSayisi"] = _context.Bayi.Count(b => b.UstBayiId == null && b.Durumu == 1),
                    ["AltBayiSayisi"] = _context.Bayi.Count(b => b.UstBayiId != null && b.Durumu == 1),
                    ["PasifBayiSayisi"] = _context.Bayi.Count(b => b.Durumu == 0),
                    ["EnDerinSeviye"] = _context.Bayi.Any() ? _context.Bayi.Max(b => b.Seviye ?? 0) : 0
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi istatistikleri hesaplanırken hata oluştu: {ex.Message}", ex);
            }
        }

        // Kullanıcı adı ile bayi getir
        public Bayi? GetByKullaniciAdi(string kullaniciAdi)
        {
            try
            {
                return _context.Bayi
                  
                    .Include(b => b.UstBayi)
                    .FirstOrDefault(b => b.KullaniciAdi == kullaniciAdi && b.Durumu == 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }
        public Bayi? Getir(
    Expression<Func<Bayi, bool>> filter,
    List<string>? includes = null)
        {
            try
            {
                IQueryable<Bayi> query = _context.Bayi.AsQueryable();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                return query.FirstOrDefault(filter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Giriş yapan bayinin kendisi ve tüm alt bayilerini getirir (hiyerarşik)
        public List<Bayi> GetBayiVeAltBayiler(int bayiId)
        {
            try
            {
                List<Bayi> result = new List<Bayi>();

                // Önce kendisini ekle
                Bayi currentBayi = Getir(bayiId);
                if (currentBayi != null && currentBayi.Durumu == 1)
                {
                    result.Add(currentBayi);
                }

                // Sonra tüm alt bayileri rekürsif olarak ekle
                List<Bayi> altBayiler = GetBayiHiyerarsi(bayiId);
                result.AddRange(altBayiler.Where(b => b.Id != bayiId)); // Kendisini tekrar eklememek için

                return result.Distinct().OrderBy(b => b.Seviye).ThenBy(b => b.Unvan).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Bayi ve alt bayileri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }
        // Dispose
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}