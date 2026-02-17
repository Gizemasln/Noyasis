using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebApp.Repositories;
using Microsoft.AspNetCore.Http;

namespace WepApp.Controllers
{
    public class IstekOneriController : AdminBaseController
    {
        private readonly IstekOneriRepository _repository = new IstekOneriRepository();
        private readonly GenericRepository<LisansTip> _lisansTipRepository = new GenericRepository<LisansTip>();
        private readonly BayiRepository _bayiRepository = new BayiRepository();
        private readonly MusteriRepository _musteriRepository = new MusteriRepository();
        private const int PageSize = 10;

        public IActionResult Index(int page = 1, string filtre = "tumu")
        {
            IActionResult redirectResult = LoadCommonData();
            if (redirectResult != null) return redirectResult;

            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;
            ViewBag.SeciliFiltre = filtre;

            List<IstekOneriler> liste = new List<IstekOneriler>();
            List<IstekOneriler> bayiListe = new List<IstekOneriler>();
            List<IstekOneriler> musteriListe = new List<IstekOneriler>();
            int toplam = 0;

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                // Müşteri sadece kendi kayıtlarını görür
                liste = _repository.GetirMusteriyeAitListesi(kullaniciId.Value, page, PageSize);
                toplam = _repository.GetirMusteriyeAitToplamSayi(kullaniciId.Value);
                ViewBag.ToplamKendi = toplam;
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                // Bayi için iki ayrı liste
                if (filtre == "tumu" || filtre == "bayi")
                {
                    bayiListe = _repository.GetirBayiyeAitListesi(kullaniciId.Value, page, PageSize);
                    ViewBag.ToplamBayi = _repository.GetirBayiyeAitToplamSayi(kullaniciId.Value);
                }

                if (filtre == "tumu" || filtre == "musteri")
                {
                    // Bayiye bağlı müşterilerin ID'lerini bul
                    var musteriIdler = _musteriRepository.GetirList(x => x.BayiId == kullaniciId.Value && x.Durum == 1)
                        .Select(m => m.Id)
                        .ToList();

                    musteriListe = _repository.GetirMusteriListesi(musteriIdler, page, PageSize);
                    ViewBag.ToplamMusteri = _repository.GetirMusteriToplamSayi(musteriIdler);
                }

                // Filtreye göre ana listeyi belirle
                if (filtre == "bayi")
                    liste = bayiListe;
                else if (filtre == "musteri")
                    liste = musteriListe;
                else
                {
                    // "tumu" seçildiğinde iki listeyi birleştir (sayfalama için ayrı hesapla)
                    // Sayfalama yapmadan tümünü göster veya ayrı kartlarda göster - View'de ayrı kartlarda göstereceğiz
                }
            }

            // Lisans Tipleri kısmı
            MusteriSozlesmeRepository sozlesmeRepository = new MusteriSozlesmeRepository();
            List<LisansTip> kullanilanLisansTipleri;
            List<string> join = new List<string>();
            join.Add("Teklif.LisansTip");

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                var musteriSozlesmes = sozlesmeRepository.GetirList(
                    x => x.Durumu == 1 &&
                         x.MusteriId == kullaniciId.Value &&
                         x.SozlesmeDurumuId == 11,
                 join);

                kullanilanLisansTipleri = musteriSozlesmes
                    .Select(s => s.Teklif?.LisansTip)
                    .Where(lt => lt != null && lt.Durumu == 1)
                    .GroupBy(lt => lt.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.Value > 0)
            {
                var musteriSozlesmes = sozlesmeRepository.GetirQueryable()
                    .Where(x => x.Durumu == 1 &&
                                x.SozlesmeDurumuId == 11 &&
                                x.Musteri != null &&
                                x.Musteri.BayiId == kullaniciId.Value)
                    .Include(s => s.Teklif)
                    .ThenInclude(t => t.LisansTip)
                    .ToList();

                kullanilanLisansTipleri = musteriSozlesmes
                    .Select(s => s.Teklif?.LisansTip)
                    .Where(lt => lt != null && lt.Durumu == 1)
                    .GroupBy(lt => lt.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                kullanilanLisansTipleri = new List<LisansTip>();
            }

            ViewBag.LisansTipleri = kullanilanLisansTipleri;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)toplam / PageSize);
            ViewBag.TotalCount = toplam;

            // Bayi için ayrı listeleri View'a gönder
            ViewBag.BayiListe = bayiListe;
            ViewBag.MusteriListe = musteriListe;

            return View(liste);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Konu, string Metni, int LisansTipId, string bildirimSahibi = "bayi")
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası. Lütfen tekrar giriş yapın." });

            if (string.IsNullOrWhiteSpace(Konu) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Konu ve metin zorunludur." });

            try
            {
                IstekOneriler yeni = new IstekOneriler
                {
                    Konu = Konu.Trim(),
                    Metni = Metni.Trim(),
                    LisansTipId = LisansTipId,
                    Durumu = 1,
                    EkleyenKullaniciId = kullaniciId.Value,
                    GuncelleyenKullaniciId = kullaniciId.Value,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    IstekOneriDurumId = 1
                };

                if (kullaniciTipi == "Musteri")
                {
                    yeni.MusteriId = kullaniciId.Value;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    if (bildirimSahibi == "bayi")
                    {
                        yeni.BayiId = kullaniciId.Value;
                    }
                    else if (bildirimSahibi == "musteri")
                    {
                        // Müşteri adına ekleme için ayrı bir parametre gerekebilir
                        // Şimdilik sadece bayi adına ekleme yapılsın
                        yeni.BayiId = kullaniciId.Value;
                    }
                }

                _repository.Ekle(yeni);
                return Json(new { success = true, message = "İstek/Öneri başarıyla gönderildi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ekleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirDuzenle(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler kayit = _repository.GetirById(id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1);

            var data = new
            {
                kayit.Id,
                kayit.Konu,
                kayit.Metni,
                kayit.LisansTipId,
                kayit.MusteriId,
                kayit.BayiId,
                LisansTipleri = lisansTipleri.Select(g => new { g.Id, g.Adi })
            };

            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(int Id, string Konu, string Metni, int LisansTipId)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler mevcut = _repository.GetirById(Id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (mevcut == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            if (string.IsNullOrWhiteSpace(Konu) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Konu ve metin zorunludur." });

            try
            {
                mevcut.Konu = Konu.Trim();
                mevcut.Metni = Metni.Trim();
                mevcut.LisansTipId = LisansTipId;
                mevcut.GuncelleyenKullaniciId = kullaniciId.Value;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _repository.Guncelle(mevcut);
                return Json(new { success = true, message = "Güncelleme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Güncelleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler kayit = _repository.GetirById(id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            try
            {
                _repository.Sil(id);
                return Json(new { success = true, message = "Silme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CevapDetayGetir(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler kayit = _repository.Getir(x => x.Id == id,
                new List<string> { "IstekOneriDurum" });

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            // Yetki kontrolü
            if (kullaniciTipi == "Musteri" && kayit.MusteriId != kullaniciId.Value)
            {
                return Json(new { success = false, message = "Bu cevabı görüntüleme yetkiniz yok." });
            }
            else if (kullaniciTipi == "Bayi" && kayit.BayiId != kullaniciId.Value)
            {
                if (kayit.Musteri != null && kayit.Musteri.BayiId != kullaniciId.Value)
                {
                    return Json(new { success = false, message = "Bu cevabı görüntüleme yetkiniz yok." });
                }
            }

            var data = new
            {
                success = true,
                distributorCevap = kayit.DistributorCevap ?? "",
                distributorCevapVerdiMi = kayit.DistributorCevapVerdiMi,
                distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                istekDurumAdi = kayit.IstekOneriDurum?.Adi ?? "Beklemede"
            };
            return Json(data);
        }
    }
}