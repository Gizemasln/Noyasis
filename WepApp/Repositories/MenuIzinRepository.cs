using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class MenuIzinRepository
    {
        private readonly string _connectionString = "Server=193.35.155.81,1433;Database=Noyasis;User Id=sqluser;Password=5343212901Ga*;TrustServerCertificate=True;"; // appsettings.json'dan alabilirsin

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
                            Id = dr.GetInt32("Id"),
                            KullaniciTipi = dr.GetString("KullaniciTipi"),
                            MenuUrl = dr.GetString("MenuUrl"),
                            MenuBaslik = dr.GetString("MenuBaslik"),
                            ParentMenuUrl = dr.IsDBNull("ParentMenuUrl") ? null : dr.GetString("ParentMenuUrl"),
                            Icon = dr.IsDBNull("Icon") ? null : dr.GetString("Icon"),
                            Siralama = dr.GetInt32("Siralama")
                        });
                    }
                }
            }
            return list;
        }

        // İzin yönetiminde kullanmak için (opsiyonel, silme ve ekleme için)
        public void TemizleVeEkle(List<MenuIzin> yeniIzinler)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var tip = yeniIzinler.FirstOrDefault()?.KullaniciTipi ?? "";
                if (string.IsNullOrEmpty(tip)) return;

                // Önce sil
                new SqlCommand("DELETE FROM MenuIzinleri WHERE KullaniciTipi = @tip", con)
                {
                    Parameters = { new SqlParameter("@tip", tip) }
                }.ExecuteNonQuery();

                // Sonra ekle
                foreach (var izin in yeniIzinler)
                {
                    new SqlCommand(@"INSERT INTO MenuIzinleri 
                        (KullaniciTipi, MenuUrl, MenuBaslik, ParentMenuUrl, Icon, Siralama) 
                        VALUES (@tip, @url, @baslik, @parent, @icon, @sira)", con)
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
                    }.ExecuteNonQuery();
                }
            }
        }
    }
}