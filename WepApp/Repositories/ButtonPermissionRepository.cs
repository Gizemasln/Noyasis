using System;
using System.Collections.Generic;
using System.Linq;
using WepApp.Models;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace WepApp.Repositories
{
    public class ButtonPermissionRepository
    {
        private readonly string _connectionString;

        public ButtonPermissionRepository()
        {
            // HATA DÜZELTİLDİ - Sondaki fazla parantez kaldırıldı
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? "Server=193.35.155.81,1433;Database=Noyasis;User Id=sqluser;Password=5343212901Ga*;TrustServerCertificate=True;";
        }

        // Tüm buton izinlerini getir
        public List<ButtonPermission> Listele()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    string sql = "SELECT * FROM ButtonPermissions ORDER BY KullaniciTipi, SayfaAdi, ButonAksiyonu";
                    return db.Query<ButtonPermission>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Listele Hatası: " + ex.Message);
                return new List<ButtonPermission>();
            }
        }

        // Belirli kullanıcı tipinin izinlerini getir
        public List<ButtonPermission> KullaniciTipineGoreGetir(string kullaniciTipi)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    string sql = "SELECT * FROM ButtonPermissions WHERE KullaniciTipi = @KullaniciTipi ORDER BY SayfaAdi, ButonAksiyonu";
                    return db.Query<ButtonPermission>(sql, new { KullaniciTipi = kullaniciTipi }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("KullaniciTipineGoreGetir Hatası: " + ex.Message);
                return new List<ButtonPermission>();
            }
        }

        // Tüm kullanıcı tiplerinin izinlerini getir
        public Dictionary<string, Dictionary<string, bool>> TumIzinleriGetir()
        {
            var result = new Dictionary<string, Dictionary<string, bool>>();
            var tipler = new[] { "Admin", "Musteri", "Bayi", "Distributor" };

            foreach (var tip in tipler)
            {
                result[tip] = new Dictionary<string, bool>();
            }

            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    string sql = "SELECT KullaniciTipi, SayfaAdi, ButonAksiyonu, IzınVar FROM ButtonPermissions";

                    // Dynamic kullanarak hata riskini azalt
                    var izinler = db.Query(sql).ToList();

                    foreach (var izin in izinler)
                    {
                        string kullaniciTipi = izin.KullaniciTipi;
                        string sayfaAdi = izin.SayfaAdi;
                        string butonAksiyonu = izin.ButonAksiyonu;
                        bool izinVar = izin.IzınVar;

                        if (result.ContainsKey(kullaniciTipi))
                        {
                            string key = $"{sayfaAdi}|{butonAksiyonu}";
                            result[kullaniciTipi][key] = izinVar;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TumIzinleriGetir Hatası: " + ex.Message);
            }

            return result;
        }

        // İzinleri temizle ve yeni izinleri ekle
        public void TemizleVeEkle(string kullaniciTipi, List<ButtonPermission> yeniIzinler)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            // Önce bu kullanıcı tipine ait tüm izinleri sil
                            string deleteSql = "DELETE FROM ButtonPermissions WHERE KullaniciTipi = @KullaniciTipi";
                            db.Execute(deleteSql, new { KullaniciTipi = kullaniciTipi }, transaction);

                            // Yeni izinleri ekle
                            if (yeniIzinler.Any())
                            {
                                string insertSql = @"
                                    INSERT INTO ButtonPermissions (KullaniciTipi, SayfaAdi, ButonAksiyonu, IzınVar, Aciklama, CreatedDate)
                                    VALUES (@KullaniciTipi, @SayfaAdi, @ButonAksiyonu, @IzınVar, @Aciklama, GETDATE())";

                                db.Execute(insertSql, yeniIzinler, transaction);
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine("Transaction Hatası: " + ex.Message);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TemizleVeEkle Hatası: " + ex.Message);
                throw;
            }
        }
    }
}