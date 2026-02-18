using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class MenuIzinRepository
    {
        private readonly string _connectionString = "Server=193.35.155.81,1433;Database=Noyasis;User Id=sqluser;Password=5343212901Ga*;TrustServerCertificate=True;";

        public List<MenuIzin> Listele()
        {
            var list = new List<MenuIzin>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT * FROM MenuIzinleri ORDER BY Siralama", con))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new MenuIzin
                        {
                            Id = dr.GetInt32(dr.GetOrdinal("Id")),
                            KullaniciTipi = dr.GetString(dr.GetOrdinal("KullaniciTipi")),
                            MenuUrl = dr.GetString(dr.GetOrdinal("MenuUrl")),
                            MenuBaslik = dr.GetString(dr.GetOrdinal("MenuBaslik")),
                            ParentMenuUrl = dr.IsDBNull(dr.GetOrdinal("ParentMenuUrl")) ? null : dr.GetString(dr.GetOrdinal("ParentMenuUrl")),
                            Icon = dr.IsDBNull(dr.GetOrdinal("Icon")) ? null : dr.GetString(dr.GetOrdinal("Icon")),
                            Siralama = dr.GetInt32(dr.GetOrdinal("Siralama"))
                        });
                    }
                }
            }
            return list;
        }

        public void TemizleVeEkle(string kullaniciTipi, List<MenuIzin> yeniIzinler)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Önce mevcut yetkileri sil
                        using (var cmd = new SqlCommand("DELETE FROM MenuIzinleri WHERE KullaniciTipi = @tip", con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@tip", kullaniciTipi);
                            cmd.ExecuteNonQuery();
                        }

                        // Yeni yetkileri ekle
                        foreach (var izin in yeniIzinler)
                        {
                            using (var cmd = new SqlCommand(@"
                                INSERT INTO MenuIzinleri 
                                (KullaniciTipi, MenuUrl, MenuBaslik, ParentMenuUrl, Icon, Siralama) 
                                VALUES (@tip, @url, @baslik, @parent, @icon, @sira)", con, transaction)
                            {
                                Parameters =
                                {
                                    new SqlParameter("@tip", izin.KullaniciTipi),
                                    new SqlParameter("@url", izin.MenuUrl),
                                    new SqlParameter("@baslik", izin.MenuBaslik),
                                    new SqlParameter("@parent", (object?)izin.ParentMenuUrl ?? DBNull.Value),
                                    new SqlParameter("@icon", (object?)izin.Icon ?? DBNull.Value),
                                    new SqlParameter("@sira", izin.Siralama)
                                }
                            })
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<MenuIzin> KullaniciTipineGoreListele(string kullaniciTipi)
        {
            return Listele().Where(x => x.KullaniciTipi == kullaniciTipi).OrderBy(x => x.Siralama).ToList();
        }
    }
}