using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class AdminPaketBaglamaController : AdminBaseController
    {
      PaketRepository _paketRepository = new PaketRepository();
 PaketBaglantiRepository _paketBaglantiRepository = new PaketBaglantiRepository();
      KullanicilarRepository _kullaniciRepository = new KullanicilarRepository();

        // Ana sayfa - Paket listesi
        public IActionResult Index()
        {
            LoadCommonData();

            try
            {
                // Tüm aktif paketleri getir
                List<string> join = new List<string> { "LisansTip", "Birim", "KDV" };
                List<Paket> paketler = _paketRepository
                    .GetirList(x => x.Durumu == 1, join)
                    .OrderBy(x => x.Sira)
                    .ThenBy(x => x.Adi)
                    .ToList();

                // Her paket için bağlı paket sayısını hesapla
                Dictionary<int, int> bagliPaketSayilari = new Dictionary<int, int>();
                Dictionary<int, bool> caprazBaglantiVarMi = new Dictionary<int, bool>();

                foreach (var paket in paketler)
                {
                    // Bağlı paket sayısı
                    int bagliSayi = _paketBaglantiRepository
                        .GetirList(x => x.PaketId == paket.Id && x.Durumu == 1)
                        .Count();
                    bagliPaketSayilari[paket.Id] = bagliSayi;

                    // Çapraz bağlantı kontrolü
                    bool caprazVar = false;
                    var bagliPaketler = _paketBaglantiRepository
                        .GetirList(x => x.PaketId == paket.Id && x.Durumu == 1)
                        .Select(x => x.BagliPaketId)
                        .ToList();

                    foreach (var bagliId in bagliPaketler)
                    {
                        if (_paketBaglantiRepository.CaprazBaglantiKontrol(paket.Id, bagliId))
                        {
                            caprazVar = true;
                            break;
                        }
                    }
                    caprazBaglantiVarMi[paket.Id] = caprazVar;
                }

                ViewBag.Paketler = paketler;
                ViewBag.BagliPaketSayilari = bagliPaketSayilari;
                ViewBag.CaprazBaglantiVarMi = caprazBaglantiVarMi;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata oluştu: {ex.Message}";
                return View();
            }
        }

        // Paket bağlama sayfası
        [HttpGet]
        public IActionResult Baglama(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                Paket paket = _paketRepository.Getir(id);
                if (paket == null || paket.Durumu == 0)
                {
                    TempData["Error"] = "Paket bulunamadı.";
                    return RedirectToAction("Index");
                }

                ViewBag.Paket = paket;

                // Mevcut bağlı paketleri getir
                List<int> mevcutBagliPaketIds = _paketBaglantiRepository
                    .GetirList(x => x.PaketId == id && x.Durumu == 1)
                    .Select(x => x.BagliPaketId)
                    .ToList();

                // Tüm aktif paketleri getir (kendisi hariç)
                List<string> join = new List<string> { "LisansTip", "Birim", "KDV" };
                List<Paket> tumPaketler = _paketRepository
                    .GetirList(x => x.Durumu == 1 && x.Id != id, join)
                    .OrderBy(x => x.Sira)
                    .ThenBy(x => x.Adi)
                    .ToList();

                // Çapraz bağlantı kontrolü
                List<int> caprazBaglantiOlanlar = new List<int>();
                foreach (var bagliId in mevcutBagliPaketIds)
                {
                    if (_paketBaglantiRepository.CaprazBaglantiKontrol(id, bagliId))
                    {
                        caprazBaglantiOlanlar.Add(bagliId);
                    }
                }

                ViewBag.MevcutBagliPaketIds = mevcutBagliPaketIds;
                ViewBag.TumPaketler = tumPaketler;
                ViewBag.CaprazBaglantiOlanlar = caprazBaglantiOlanlar;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult BaglamaKaydet(int paketId, List<int> bagliPaketIds)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                Paket paket = _paketRepository.Getir(paketId);
                if (paket == null || paket.Durumu == 0)
                {
                    return Json(new { success = false, message = "Paket bulunamadı." });
                }

                // Null kontrolü
                bagliPaketIds ??= new List<int>();

                // Çapraz bağlantı kontrolü
                List<string> caprazHatalar = new List<string>();
                foreach (var bagliId in bagliPaketIds.Distinct())
                {
                    if (paketId == bagliId) continue; // Kendi kendine bağlama kontrolü

                    if (_paketBaglantiRepository.CaprazBaglantiKontrol(paketId, bagliId))
                    {
                        var caprazPaket = _paketRepository.Getir(bagliId);
                        caprazHatalar.Add($"{paket.Adi} <-> {caprazPaket?.Adi}");
                    }
                }

                if (caprazHatalar.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Çapraz bağlantı hatası: {string.Join(", ", caprazHatalar)} arasında zaten bağlantı var."
                    });
                }

                // Çoklu bağlantı ekle (yeni metod)
                _paketBaglantiRepository.CokluBaglantiEkle2(paketId, bagliPaketIds, kullanici.Id);

                return Json(new
                {
                    success = true,
                    message = "Paket bağlantıları başarıyla kaydedildi.",
                    redirectUrl = Url.Action("Index", "AdminPaketBaglama")
                });
            }
            catch (Exception ex)
            {
                // Inner exception'ı da göster
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;
                }

                return Json(new
                {
                    success = false,
                    message = $"Hata oluştu: {errorMessage}"
                });
            }
        }

        // Bağlı paketleri getir (JSON)
        [HttpGet]
        public IActionResult BagliPaketleriGetir(int paketId)
        {
            LoadCommonData();

            try
            {
                var bagliPaketler = _paketBaglantiRepository.GetBagliPaketler(paketId, new List<string> { "LisansTip", "Birim", "KDV" });

                var result = bagliPaketler.Select(x => new
                {
                    id = x.Id,
                    adi = x.Adi,
                    lisansTipi = x.LisansTip?.Adi,
                    birim = x.Birim?.Adi,
                    fiyat = x.Fiyat,
                    kdv = x.KDV?.Oran,
                    egitimSuresi = x.EgitimSuresi,
                    durum = x.Aktif == 1 ? "Aktif" : "Pasif"
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }

        // Zincirleme bağlı paketleri getir
        [HttpGet]
        public IActionResult TumBagliPaketleriGetir(int paketId)
        {
            LoadCommonData();

            try
            {
                var tumBagliPaketIds = _paketBaglantiRepository.GetTumBagliPaketlerRecursive(paketId);

                if (tumBagliPaketIds.Any())
                {
                    List<string> join = new List<string> { "LisansTip", "Birim", "KDV" };
                    var tumBagliPaketler = _paketRepository
                        .GetirList(x => tumBagliPaketIds.Contains(x.Id) && x.Durumu == 1, join)
                        .OrderBy(x => x.Sira)
                        .ThenBy(x => x.Adi)
                        .ToList();

                    var result = tumBagliPaketler.Select(x => new
                    {
                        id = x.Id,
                        adi = x.Adi,
                        lisansTipi = x.LisansTip?.Adi,
                        birim = x.Birim?.Adi,
                        fiyat = x.Fiyat,
                        kdv = x.KDV?.Oran,
                        egitimSuresi = x.EgitimSuresi,
                        seviye = GetBaglantiSeviyesi(paketId, x.Id)
                    }).ToList();

                    return Json(new { success = true, data = result });
                }

                return Json(new { success = true, data = new List<object>() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }

        // Bağlantı seviyesini hesapla
        private int GetBaglantiSeviyesi(int anaPaketId, int hedefPaketId)
        {
            var seviye = 0;
            var ziyaretEdilenler = new HashSet<int>();
            var kuyruk = new Queue<(int PaketId, int Seviye)>();

            kuyruk.Enqueue((anaPaketId, 0));
            ziyaretEdilenler.Add(anaPaketId);

            while (kuyruk.Count > 0)
            {
                var (currentId, currentSeviye) = kuyruk.Dequeue();

                if (currentId == hedefPaketId)
                {
                    return currentSeviye;
                }

                var bagliPaketler = _paketBaglantiRepository
                    .GetirList(x => x.PaketId == currentId && x.Durumu == 1)
                    .Select(x => x.BagliPaketId)
                    .ToList();

                foreach (var bagliId in bagliPaketler)
                {
                    if (!ziyaretEdilenler.Contains(bagliId))
                    {
                        ziyaretEdilenler.Add(bagliId);
                        kuyruk.Enqueue((bagliId, currentSeviye + 1));
                    }
                }
            }

            return -1; // Bağlantı yok
        }

        // Bağlantıyı kaldır
        [HttpPost]
        public IActionResult BaglantiyiKaldir(int baglantiId)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                PaketBaglanti baglanti = _paketBaglantiRepository.Getir(baglantiId);
                if (baglanti == null || baglanti.Durumu == 0)
                {
                    return Json(new { success = false, message = "Bağlantı bulunamadı." });
                }

                baglanti.Durumu = 0;
                baglanti.GuncelleyenKullaniciId = kullanici.Id;
                baglanti.GuncellenmeTarihi = DateTime.Now;
                _paketBaglantiRepository.Guncelle(baglanti);

                return Json(new { success = true, message = "Bağlantı başarıyla kaldırıldı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }

        // Bağlantı detayları
        [HttpGet]
        public IActionResult BaglantiDetay(int paketId)
        {
            LoadCommonData();

            try
            {
                var paket = _paketRepository.Getir(paketId);
                if (paket == null || paket.Durumu == 0)
                {
                    TempData["Error"] = "Paket bulunamadı.";
                    return RedirectToAction("Index");
                }

                ViewBag.Paket = paket;

                // Doğrudan bağlı paketler
                var bagliPaketler = _paketBaglantiRepository.GetBagliPaketler(paketId,
                    new List<string> { "LisansTip", "Birim", "KDV" });

                // Ana paketler (bu pakete bağlı olanlar)
                var anaPaketler = _paketBaglantiRepository.GetAnaPaketler(paketId,
                    new List<string> { "LisansTip", "Birim", "KDV" });

                // Zincirleme bağlı paketler
                var tumBagliPaketIds = _paketBaglantiRepository.GetTumBagliPaketlerRecursive(paketId);
                var tumBagliPaketler = tumBagliPaketIds.Any() ?
                    _paketRepository.GetirList(x => tumBagliPaketIds.Contains(x.Id) && x.Durumu == 1,
                        new List<string> { "LisansTip", "Birim", "KDV" }) : new List<Paket>();

                ViewBag.BagliPaketler = bagliPaketler;
                ViewBag.AnaPaketler = anaPaketler;
                ViewBag.TumBagliPaketler = tumBagliPaketler;
                ViewBag.TumBagliPaketSayisi = tumBagliPaketIds.Count;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Çapraz bağlantıları temizle
        [HttpPost]
        public IActionResult CaprazBaglantilariTemizle(int paketId)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                var baglantilar = _paketBaglantiRepository
                    .GetirList(x => x.PaketId == paketId && x.Durumu == 1)
                    .ToList();

                int silinenSayisi = 0;
                foreach (var baglanti in baglantilar)
                {
                    if (_paketBaglantiRepository.CaprazBaglantiKontrol(paketId, baglanti.BagliPaketId))
                    {
                        baglanti.Durumu = 0;
                        baglanti.GuncelleyenKullaniciId = kullanici.Id;
                        baglanti.GuncellenmeTarihi = DateTime.Now;
                        _paketBaglantiRepository.Guncelle(baglanti);
                        silinenSayisi++;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"{silinenSayisi} adet çapraz bağlantı temizlendi.",
                    silinenSayisi = silinenSayisi
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }
    }
}