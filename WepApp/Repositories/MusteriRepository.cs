using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class MusteriRepository : GenericRepository<Musteri>
    {
        private readonly Context _context;

        public MusteriRepository()
        {
            _context = new Context();
        }

        public MusteriRepository(Context context)
        {
            _context = context;
        }

        // Tüm müşterileri getir
        public List<Musteri> GetirList(Expression<Func<Musteri, bool>>? filter = null)
        {
            try
            {
                IQueryable<Musteri> query = _context.Musteri
                 
                     // Bayileri de dahil et
                    .AsQueryable();

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                return query.OrderBy(m => m.Ad).ThenBy(m => m.Soyad).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri listesi getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Tek bir müşteri getir
        public Musteri? Getir(int id)
        {
            try
            {
                return _context.Musteri
                
                
                      // Alt bayileri de dahil et
                    .FirstOrDefault(m => m.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteri ekle
        public bool Ekle(Musteri musteri)
        {
            try
            {
                // Kullanıcı adı kontrolü
                Musteri existingMusteri = _context.Musteri
                    .FirstOrDefault(m => m.KullaniciAdi == musteri.KullaniciAdi && m.Durum == 1);

                if (existingMusteri != null)
                {
                    throw new Exception("Bu kullanıcı adı zaten kullanılıyor.");
                }

                _context.Musteri.Add(musteri);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri eklenirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteri güncelle
        public bool Guncelle(Musteri musteri)
        {
            try
            {
                Musteri existing = _context.Musteri.Find(musteri.Id);
                if (existing == null)
                {
                    throw new Exception("Müşteri bulunamadı.");
                }

                // Kullanıcı adı kontrolü (kendisi hariç)
                Musteri duplicateMusteri = _context.Musteri
                    .FirstOrDefault(m => m.KullaniciAdi == musteri.KullaniciAdi
                                      && m.Id != musteri.Id
                                      && m.Durum == 1);

                if (duplicateMusteri != null)
                {
                    throw new Exception("Bu kullanıcı adı başka bir müşteri tarafından kullanılıyor.");
                }

                _context.Entry(existing).CurrentValues.SetValues(musteri);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri güncellenirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteri sil (soft delete)
        public bool Sil(int id)
        {
            try
            {
                Musteri musteri = _context.Musteri.Find(id);
                if (musteri == null)
                {
                    throw new Exception("Müşteri bulunamadı.");
                }

                // Bayi kontrolü
                int bayiSayisi = _context.Bayi
                    .Count(b =>  b.Durumu == 1);

                if (bayiSayisi > 0)
                {
                    throw new Exception("Bu müşteriye ait aktif bayiler var. Önce bayileri silmelisiniz.");
                }

                musteri.Durum = 0;
                musteri.GuncellenmeTarihi = DateTime.Now;
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri silinirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşterinin tüm bayilerini getir (hiyerarşi dahil)
        public List<Bayi> GetMusteriBayileri(int musteriId, bool sadecAnaBayiler = false)
        {
            try
            {
                if (sadecAnaBayiler)
                {
                    // Sadece ana bayiler
                    return _context.Bayi
                     
                        .Where(b => b.UstBayiId == null && b.Durumu == 1)
                        .OrderBy(b => b.Unvan)
                        .ToList();
                }
                else
                {
                    // Tüm bayiler (ana + alt)
                    List<Bayi> anaBayiler = _context.Bayi
                        .Where(b => b.UstBayiId == null && b.Durumu == 1)
                        .ToList();

                    List<Bayi> tumBayiler = new List<Bayi>();
                    BayiRepository bayiRepo = new BayiRepository(_context);

                    foreach (Bayi anaBayi in anaBayiler)
                    {
                        tumBayiler.AddRange(bayiRepo.GetBayiHiyerarsi(anaBayi.Id));
                    }

                    return tumBayiler.Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri bayileri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteri istatistikleri
        public Dictionary<string, object> GetMusteriIstatistikleri(int musteriId)
        {
            try
            {
                Musteri musteri = Getir(musteriId);
                if (musteri == null)
                {
                    throw new Exception("Müşteri bulunamadı.");
                }

                int anaBayiler = _context.Bayi
                    .Count(b =>b.UstBayiId == null && b.Durumu == 1);

                BayiRepository bayiRepo = new BayiRepository(_context);
                List<Bayi> tumBayiler = GetMusteriBayileri(musteriId, false);

                return new Dictionary<string, object>
                {
                    ["MusteriAd"] = $"{musteri.Ad} {musteri.Soyad}",
                    ["AnaBayiSayisi"] = anaBayiler,
                    ["ToplamBayiSayisi"] = tumBayiler.Count,
                    ["AltBayiSayisi"] = tumBayiler.Count - anaBayiler,
                    ["EnDerinSeviye"] = tumBayiler.Any() ? tumBayiler.Max(b => b.Seviye) : 0,
                    ["KayitTarihi"] = musteri.EklenmeTarihi
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri istatistikleri hesaplanırken hata oluştu: {ex.Message}", ex);
            }
        }

        // Kullanıcı adı ile müşteri getir
        public Musteri? GetByKullaniciAdi(string kullaniciAdi)
        {
            try
            {
                return _context.Musteri
                
                
                    .FirstOrDefault(m => m.KullaniciAdi == kullaniciAdi && m.Durum == 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri getirilirken hata oluştu: {ex.Message}", ex);
            }
        }

        // Müşteri login kontrolü
        public Musteri? Login(string kullaniciAdi, string sifre)
        {
            try
            {
                return _context.Musteri
             
                    .FirstOrDefault(m => m.KullaniciAdi == kullaniciAdi
                                      && m.Sifre == sifre
                                      && m.Durum == 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Login işlemi sırasında hata oluştu: {ex.Message}", ex);
            }
        }

        // MusteriRepository içine ekle
        public IQueryable<Musteri> GetirQueryable(Expression<Func<Musteri, bool>>? filter = null)
        {
            try
            {
                IQueryable<Musteri> query = _context.Musteri
           
                    
                    .AsQueryable();

                if (filter != null)
                    query = query.Where(filter);

                return query;
            }
            catch (Exception ex)
            {
                throw new Exception($"Queryable sorgu oluşturulurken hata: {ex.Message}", ex);
            }
        }
        // Tüm müşteri istatistikleri
        public Dictionary<string, int> GetTumMusteriIstatistikleri()
        {
            try
            {
                return new Dictionary<string, int>
                {
                    ["ToplamMusteri"] = _context.Musteri.Count(m => m.Durum == 1),
                    ["PasifMusteri"] = _context.Musteri.Count(m => m.Durum == 0),
                    ["BayiliMusteri"] = _context.Musteri
                        .Count(m => m.Durum == 1),
                    ["BayisizMusteri"] = _context.Musteri
                        .Count(m => m.Durum == 1 )
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Müşteri istatistikleri hesaplanırken hata oluştu: {ex.Message}", ex);
            }
        }

        // Dispose
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}