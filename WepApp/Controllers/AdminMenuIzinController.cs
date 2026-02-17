using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace WebApp.Controllers
{
    public class AdminMenuIzinController : Controller
    {
        private readonly MenuIzinRepository _repo = new MenuIzinRepository();

        public IActionResult Index()
        {
            var tumIzinler = _repo.Listele();
            var tumMenuler = GetTumMenuler();

            var model = new AdminMenuIzinViewModel
            {
                TumMenuler = tumMenuler
            };

            // Checkbox ID'leri üret
            var tipler = new[] { "Admin", "Musteri", "Bayi", "Distributor" };
            foreach (var tip in tipler)
            {
                foreach (var menu in tumMenuler)
                {
                    string key = $"{tip}|{menu.Url}";
                    string cleanUrl = menu.Url.Replace("/", "_").Replace("#", "hash_");
                    model.CheckboxIdMap[key] = $"check_{tip.ToLower()}_{cleanUrl}";
                }
            }

            // İzin listeleri
            model.AdminIzinler = tumIzinler.Where(x => x.KullaniciTipi == "Admin").Select(x => x.MenuUrl).ToList();
            model.MusteriIzinler = tumIzinler.Where(x => x.KullaniciTipi == "Musteri").Select(x => x.MenuUrl).ToList();
            model.BayiIzinler = tumIzinler.Where(x => x.KullaniciTipi == "Bayi").Select(x => x.MenuUrl).ToList();
            model.DistributorIzinler = tumIzinler.Where(x => x.KullaniciTipi == "Distributor").Select(x => x.MenuUrl).ToList();

            // Hiyerarşik yapıyı hazırla
            model.MenuHiyerarsisi = MenuHiyerarsisiOlustur(tumMenuler);

            return View(model);
        }

        [HttpPost]
        public IActionResult Kaydet(string tipi, List<string> seciliUrlLer)
        {
            var tumMenuler = GetTumMenuler();
            var yeniIzinler = new List<MenuIzin>();

            foreach (var url in seciliUrlLer ?? new List<string>())
            {
                var menu = tumMenuler.FirstOrDefault(m => m.Url == url);

                if (!EqualityComparer<(string Url, string Baslik, string? ParentUrl, string? Icon, int Siralama, int Seviye)>.Default.Equals(menu, default))
                {
                    yeniIzinler.Add(new MenuIzin
                    {
                        KullaniciTipi = tipi,
                        MenuUrl = menu.Url,
                        MenuBaslik = menu.Baslik,
                        ParentMenuUrl = menu.ParentUrl,
                        Icon = menu.Icon,
                        Siralama = menu.Siralama
                    });
                }
            }

            _repo.TemizleVeEkle(yeniIzinler);

            TempData["Success"] = $"{tipi} yetkileri kaydedildi.";
            return RedirectToAction("Index");
        }

        private List<(string Url, string Baslik, string? ParentUrl, string? Icon, int Siralama, int Seviye)> GetTumMenuler()
        {
            return new List<(string Url, string Baslik, string? ParentUrl, string? Icon, int Siralama, int Seviye)>
            {
                // 1. Seviye - Ana Menüler
                ("#contentManagement", "Web Yönetimi", null, "fas fa-file-alt", 1, 1),
                ("#offerManagement", "Bayi Kanalı Yönetimi", null, "fas fa-file-contract", 2, 1),
                ("/AdminMusteri", "Müşteriler", null, "fas fa-users", 30, 1),
                ("/AdminBayi", "Bayiler", null, "fas fa-store", 31, 1),
                ("/IstekOneri", "İstek/Öneri", null, "fas fa-lightbulb", 40, 1),
                ("/Arge", "ARGE/Hata", null, "fas fa-bug", 41, 1),
                ("/AdminMenuIzin", "Yetkilendirme", null, "fas fa-user-shield", 42, 1),
                ("/AdminPaketBaglama", "Paket Bağlama", null, "fas fa-cogs", 43, 1),

                // 2. Seviye - Alt Menüler (contentManagement altında)
                ("/AdminAnaSayfaBannerResim", "Ana Sayfa Banner", "#contentManagement", null, 1, 2),
                ("/AdminHakkimizdaBilgileri", "Hakkımızda", "#contentManagement", null, 2, 2),
                ("/AdminHakkimizdaFotograf", "Hakkımızda Görsel", "#contentManagement", null, 3, 2),
                ("/AdminIletisimBilgileri", "İletişim Bilgileri", "#contentManagement", null, 4, 2),
                ("/AdminMakale", "Makaleler", "#contentManagement", null, 5, 2),
                ("/AdminDuyuru", "Duyurular", "#contentManagement", null, 6, 2),
                ("/AdminSSS", "Sıkça Sorulan Sorular", "#contentManagement", null, 7, 2),
                ("/AdminUrun", "Ürünler", "#contentManagement", null, 8, 2),
                ("/AdminUrunGaleri", "Ürün Görselleri", "#contentManagement", null, 9, 2),
                ("/AdminSertifika", "Sertifikalar", "#contentManagement", null, 10, 2),
                ("/AdminKVKK", "KVKK Metni", "#contentManagement", null, 11, 2),
                ("/AdminGenelAydinlatma", "Genel Aydınlatma Metni", "#contentManagement", null, 12, 2),
                ("/AdminKategori", "Kategoriler", "#contentManagement", null, 13, 2),
                ("/AdminBirim", "Birimler", "#contentManagement", null, 14, 2),
                ("/AdminTeklifler", "Ürün Teklifleri", "#contentManagement", null, 15, 2),
                ("/AdminIK", "Başvuru Formları", "#contentManagement", null, 16, 2),
                ("/AdminLokasyon", "Lokasyonlar", "#contentManagement", null, 17, 2),

                // 2. Seviye - Alt Menüler (offerManagement altında)
                ("/AdminKampanya", "Kampanyalar", "#offerManagement", null, 1, 2),
                ("/AdminBayiDuyuru", "Bayi Duyurular", "#offerManagement", null, 2, 2),
                ("/AdminArgeHata", "ARGE/Hata Listesi", "#offerManagement", null, 3, 2),
                ("/AdminIstekOneri", "Istek Öneri Listesi", "#offerManagement", null, 4, 2),
                
                // 2. Seviye - Destek Tablosu (iç içe başlık)
                ("#destekSubmenu", "Destek Tablosu", "#offerManagement", null, 5, 2),
                
                // 2. Seviye - Sözleşme Yönetimi (iç içe başlık)
                ("#sozlesmeSubmenu", "Sözleşme Yönetimi", "#offerManagement", null, 6, 2),
                
                // 2. Seviye - Teklif (iç içe başlık)
                ("#teklifSubmenu", "Teklif", "#offerManagement", null, 7, 2),
                
                // 2. Seviye - Tek başına menüler
                ("/AdminPaket", "Modül", "#offerManagement", null, 8, 2),
    

                // 3. Seviye - Destek Tablosu alt menüleri
                ("/AdminUYB", "Genel Ayarlar", "#destekSubmenu", null, 1, 3),
                ("/AdminLisansTip", "Lisans Tipleri", "#destekSubmenu", null, 2, 3),
                ("/AdminSozlesmeDurumu", "Sözleşme Durumu", "#destekSubmenu", null, 3, 3),
                ("/AdminBayiSozlesmeKriteri", "Bayi Sözleşme Kriterleri", "#destekSubmenu", null, 4, 3),
                ("/AdminLisansDurumu", "Lisans Durumu", "#destekSubmenu", null, 5, 3),
                ("/AdminTeklifDurum", "Teklif Durumları", "#destekSubmenu", null, 6, 3),
                ("/AdminPaketGrup", "Paket Grupları", "#destekSubmenu", null, 7, 3),
                ("/AdminMusteriTipi", "Müşteri Tipleri", "#destekSubmenu", null, 8, 3),
                ("/AdminDepartman", "Departman Tipleri", "#destekSubmenu", null, 9, 3),
                ("/AdminKDV", "KDV Oranları", "#destekSubmenu", null, 10, 3),
                ("/AdminEntegrator", "Entegratörler", "#destekSubmenu", null, 11, 3),
                ("/AdminNedenler", "Teklif Kaybedilme Nedenleri", "#destekSubmenu", null, 12, 3),
                ("/AdminNeredenDuydu", "Nereden Duydu", "#destekSubmenu", null, 13, 3),
                ("/AdminARGEDurum", "ARGE Durum", "#destekSubmenu", null, 14, 3),
                ("/AdminIstekOneriDurum", "İstek Öneri Durum", "#destekSubmenu", null, 15, 3),

                // 3. Seviye - Sözleşme Yönetimi alt menüleri
                ("/AdminMusteriSozlesme", "Müşteri Sözleşmesi", "#sozlesmeSubmenu", null, 1, 3),
                ("/AdminBayiSozlesme", "Bayi Sözleşmesi", "#sozlesmeSubmenu", null, 2, 3),

                // 3. Seviye - Teklif alt menüleri
                ("/AdminTeklifVer", "Teklif Oluştur", "#teklifSubmenu", null, 1, 3),
                ("/AdminTeklif", "Teklif Listesi", "#teklifSubmenu", null, 42, 1),
                ("/AdminButton", "Buton Yetki", null, null, 44, 1)
            };
        }

        private Dictionary<int, List<MenuHiyerarsi>> MenuHiyerarsisiOlustur(List<(string Url, string Baslik, string? ParentUrl, string? Icon, int Siralama, int Seviye)> tumMenuler)
        {
            var hiyerarsi = new Dictionary<int, List<MenuHiyerarsi>>();

            foreach (var menu in tumMenuler.Where(m => m.Seviye == 1))
            {
                var menuItem = new MenuHiyerarsi
                {
                    Url = menu.Url,
                    Baslik = menu.Baslik,
                    Icon = menu.Icon,
                    Siralama = menu.Siralama,
                    Seviye = menu.Seviye
                };

                // Alt menüleri ekle
                menuItem.AltMenuler = tumMenuler
                    .Where(m => m.ParentUrl == menu.Url)
                    .Select(m => new MenuHiyerarsi
                    {
                        Url = m.Url,
                        Baslik = m.Baslik,
                        Icon = m.Icon,
                        Siralama = m.Siralama,
                        Seviye = m.Seviye,
                        AltMenuler = tumMenuler
                            .Where(sm => sm.ParentUrl == m.Url)
                            .Select(sm => new MenuHiyerarsi
                            {
                                Url = sm.Url,
                                Baslik = sm.Baslik,
                                Icon = sm.Icon,
                                Siralama = sm.Siralama,
                                Seviye = sm.Seviye
                            })
                            .OrderBy(sm => sm.Siralama)
                            .ToList()
                    })
                    .OrderBy(m => m.Siralama)
                    .ToList();

                if (!hiyerarsi.ContainsKey(menu.Seviye))
                    hiyerarsi[menu.Seviye] = new List<MenuHiyerarsi>();

                hiyerarsi[menu.Seviye].Add(menuItem);
            }

            return hiyerarsi;
        }
    }

    public class MenuHiyerarsi
    {
        public string Url { get; set; }
        public string Baslik { get; set; }
        public string Icon { get; set; }
        public int Siralama { get; set; }
        public int Seviye { get; set; }
        public List<MenuHiyerarsi> AltMenuler { get; set; } = new List<MenuHiyerarsi>();
    }
}