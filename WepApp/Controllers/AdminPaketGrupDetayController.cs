using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System;
using System.Linq;
using System.Collections.Generic;
using WebApp.Models;

namespace WepApp.Controllers
{
    public class AdminPaketGrupDetayController : AdminBaseController
    {
        private readonly PaketGrupRepository _paketGrupRepo = new PaketGrupRepository();
        private readonly PaketRepository _paketRepo = new PaketRepository();
        private readonly PaketGrupDetayRepository _detayRepo = new PaketGrupDetayRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            var gruplar = _paketGrupRepo.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Adi)
                .Select(x => new
                {
                    x.Id,
                    x.Adi,
                    x.Fiyat,
                    LisansTipAdi = x.LisansTip?.Adi
                })
                .ToList();
            ViewBag.PaketGruplari = gruplar;
            return View();
        }

        [HttpGet]
        public IActionResult GetirDetaylar(int grupId)
        {
            LoadCommonData();

            try
            {
                PaketGrup grup = _paketGrupRepo.Getir(grupId);
                if (grup == null || grup.Durumu == 0)
                    return Json(new { success = false, message = "Grup bulunamadı." });

                var detaylar = _detayRepo.GetirList(x => x.PaketGrupId == grupId && x.Durumu == 1, new List<string> { "Paket" })
                    .Select(x => new
                    {
                        id = x.Id,
                        paketAdi = x.Paket?.Adi ?? "-",
                        fiyat = x.Paket?.Fiyat ?? 0,
                        egitimSuresi = x.Paket?.EgitimSuresi ?? 0,
                        aktif = x.Paket?.Durumu == 1
                    }).ToList();

                return Json(new
                {
                    detaylar,
                    grupFiyat = grup.Fiyat,
                    paketSayisi = detaylar.Count,
                    ortalamaSure = detaylar.Any() ? Math.Round(detaylar.Average(x => x.egitimSuresi), 0) : 0
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Hata oluştu." });
            }
        }

        [HttpGet]
        public IActionResult GetirPaketler(int grupId)
        {
            LoadCommonData();

            try
            {
                PaketGrup grup = _paketGrupRepo.Getir(grupId);
                if (grup == null || grup.Durumu == 0)
                    return Json(new { success = false, message = "Grup bulunamadı." });

                List<int> mevcutPaketIds = _detayRepo.GetirList(x => x.PaketGrupId == grupId && x.Durumu == 1)
                    .Select(x => x.PaketId).ToList();

                var paketler = _paketRepo.GetirList(x => x.Durumu == 1 && x.LisansTipId == grup.LisansTipId, new List<string> { "LisansTip", "Birim" })
                    .Select(x => new
                    {
                        id = x.Id,
                        adi = x.Adi,
                        fiyat = x.Fiyat,
                        egitimSuresi = x.EgitimSuresi,
                        secili = mevcutPaketIds.Contains(x.Id)
                    })
                    .OrderBy(x => x.adi)
                    .ToList();

                return Json(paketler);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Kaydet(int grupId, int[] paketIds)
        {
            LoadCommonData();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                    return Json(new { success = false, message = "Oturum süreniz dolmuş." });

                PaketGrup grup = _paketGrupRepo.Getir(grupId);
                if (grup == null || grup.Durumu == 0)
                    return Json(new { success = false, message = "Grup bulunamadı." });

                if (paketIds == null || !paketIds.Any())
                    return Json(new { success = false, message = "En az bir paket seçin." });

                List<Paket> secilenPaketler = _paketRepo.GetirList(x => paketIds.Contains(x.Id) && x.Durumu == 1).ToList();
                List<Paket> farkliLisans = secilenPaketler.Where(x => x.LisansTipId != grup.LisansTipId).ToList();
                if (farkliLisans.Any())
                {
                    string isimler = string.Join(", ", farkliLisans.Select(x => x.Adi));
                    return Json(new
                    {
                        success = false,
                        message = $"Uyumsuz paketler: {isimler}. Sadece {grup.LisansTip?.Adi} lisanslı paketler eklenebilir."
                    });
                }

                List<PaketGrupDetay> mevcutDetaylar = _detayRepo.GetirList(x => x.PaketGrupId == grupId && x.Durumu == 1).ToList();
                List<PaketGrupDetay> silinecekler = mevcutDetaylar.Where(x => !paketIds.Contains(x.PaketId)).ToList();
                foreach (PaketGrupDetay item in silinecekler)
                {
                    item.Durumu = 0;
                    item.GuncelleyenKullaniciId = kullanici.Id;
                    item.GuncellenmeTarihi = DateTime.Now;
                    _detayRepo.Guncelle(item);
                }

                List<int> mevcutIds = mevcutDetaylar.Select(x => x.PaketId).ToList();
                List<int> eklenecekIds = paketIds.Where(id => !mevcutIds.Contains(id)).ToList();
                foreach (int paketId in eklenecekIds)
                {
                    Paket paket = _paketRepo.Getir(paketId);
                    if (paket != null && paket.Durumu == 1 && paket.LisansTipId == grup.LisansTipId)
                    {
                        if (paket.PaketDurumu != 1)
                        {
                            paket.PaketDurumu = 1;
                            paket.GuncelleyenKullaniciId = kullanici.Id;
                            paket.GuncellenmeTarihi = DateTime.Now;
                            _paketRepo.Guncelle(paket);
                        }

                        PaketGrupDetay yeniDetay = new PaketGrupDetay
                        {
                            PaketGrupId = grupId,
                            PaketId = paketId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _detayRepo.Ekle(yeniDetay);
                    }
                }

                // Update PaketGrup.Fiyat with the sum of selected packages' prices
                decimal? toplamFiyat = secilenPaketler.Sum(x => x.Fiyat);
                grup.Fiyat = toplamFiyat ??0;
                grup.GuncelleyenKullaniciId = kullanici.Id;
                grup.GuncellenmeTarihi = DateTime.Now;
                _paketGrupRepo.Guncelle(grup);

                return Json(new
                {
                    success = true,
                    message = $"{grup.Adi} güncellendi. {eklenecekIds.Count} eklendi, {silinecekler.Count} kaldırıldı. Grup fiyatı: {toplamFiyat:C}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }
    }
}