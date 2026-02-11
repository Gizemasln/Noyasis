using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using WebApp.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace WepApp.Controllers
{
    public class AdminPaketGrupController : AdminBaseController
    {
        private readonly PaketGrupRepository _paketGrupRepository = new PaketGrupRepository();
        private readonly LisansTipRepository _lisansTipRepository = new LisansTipRepository();
        private readonly PaketRepository _paketRepository = new PaketRepository();
        private readonly PaketGrupDetayRepository _paketGrupDetayRepository = new PaketGrupDetayRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<string> join = new List<string> { "LisansTip" };
            List<PaketGrup> gruplar = _paketGrupRepository.GetirList(x => x.Durumu == 1, join)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Adi)
                .ToList();
            ViewBag.PaketGruplari = gruplar;
            ViewBag.LisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(PaketGrup model, int[] seciliPaketler, string EgitimSuresiInput)
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

                if (string.IsNullOrWhiteSpace(model.Adi))
                {
                    TempData["Error"] = "Grup adı gereklidir.";
                    return RedirectToAction("Index");
                }

                PaketGrup existing = _paketGrupRepository.GetirList(x => x.Adi == model.Adi.Trim() && x.Durumu == 1)
                    .FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir grup zaten mevcut.";
                    return RedirectToAction("Index");
                }

                // Kullanıcının girdiği fiyatı kullan, paket toplamını kullanma
                decimal grupFiyati = model.Fiyat;

                // Eğitim süresini Türkçe kültürde parse et
                decimal egitimSuresi = 0;
                if (!string.IsNullOrEmpty(EgitimSuresiInput))
                {
                    // Türkçe kültürde parse et (virgül destekli)
                    if (decimal.TryParse(EgitimSuresiInput.Replace('.', ','), NumberStyles.Any, new CultureInfo("tr-TR"), out decimal parsedValue))
                    {
                        egitimSuresi = parsedValue;
                    }
                    else if (decimal.TryParse(EgitimSuresiInput, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue))
                    {
                        egitimSuresi = parsedValue;
                    }
                }

                PaketGrup yeniGrup = new PaketGrup
                {
                    Adi = model.Adi.Trim(),
                    LisansTipId = model.LisansTipId,
                    Fiyat = grupFiyati, // Kullanıcının girdiği fiyat
                    KFiyat = grupFiyati * (1 - (model.IndOran / 100)), // İndirimli fiyatı hesapla
                    EgitimSuresi = egitimSuresi, // Parse edilen eğitim süresi
                    IndOran = model.IndOran,
                    Sira = model.Sira,
                    Durumu = 1,
                    KayitYapanKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _paketGrupRepository.Ekle(yeniGrup);

                // Seçili paketleri grup detayına ekle
                if (seciliPaketler != null && seciliPaketler.Length > 0)
                {
                    foreach (int paketId in seciliPaketler)
                    {
                        PaketGrupDetay detay = new PaketGrupDetay
                        {
                            PaketGrupId = yeniGrup.Id,
                            PaketId = paketId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _paketGrupDetayRepository.Ekle(detay);
                    }
                }

                TempData["Success"] = "Paket grubu ve içeriği başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Grup eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(PaketGrup model, int[] seciliPaketler, string EgitimSuresiInput)
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

                PaketGrup existing = _paketGrupRepository.Getir(model.Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "Grup bulunamadı.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(model.Adi))
                {
                    TempData["Error"] = "Grup adı gereklidir.";
                    return RedirectToAction("Index");
                }

                PaketGrup duplicate = _paketGrupRepository.GetirList(x => x.Adi == model.Adi.Trim()
                                                            && x.Id != model.Id
                                                            && x.Durumu == 1)
                    .FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir grup zaten mevcut.";
                    return RedirectToAction("Index");
                }

                // Kullanıcının girdiği fiyatı kullan, paket toplamını kullanma
                decimal grupFiyati = model.Fiyat;

                // Eğitim süresini Türkçe kültürde parse et
                decimal egitimSuresi = existing.EgitimSuresi ?? 0; // Varsayılan olarak mevcut değer
                if (!string.IsNullOrEmpty(EgitimSuresiInput))
                {
                    // Türkçe kültürde parse et (virgül destekli)
                    if (decimal.TryParse(EgitimSuresiInput.Replace('.', ','), NumberStyles.Any, new CultureInfo("tr-TR"), out decimal parsedValue))
                    {
                        egitimSuresi = parsedValue;
                    }
                    else if (decimal.TryParse(EgitimSuresiInput, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue))
                    {
                        egitimSuresi = parsedValue;
                    }
                }

                existing.Adi = model.Adi.Trim();
                existing.LisansTipId = model.LisansTipId;
                existing.Fiyat = grupFiyati; // Kullanıcının girdiği fiyat
                existing.EgitimSuresi = egitimSuresi; // Parse edilen eğitim süresi
                existing.IndOran = model.IndOran;
                existing.KFiyat = grupFiyati * (1 - (model.IndOran / 100)); // İndirimli fiyatı hesapla
                existing.Sira = model.Sira;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _paketGrupRepository.Guncelle(existing);

                // Mevcut detayları sil ve yenilerini ekle
                List<PaketGrupDetay> mevcutDetaylar = _paketGrupDetayRepository.GetirList(x => x.PaketGrupId == model.Id);
                foreach (PaketGrupDetay detay in mevcutDetaylar)
                {
                    _paketGrupDetayRepository.Sil(detay);
                }

                // Yeni seçili paketleri ekle
                if (seciliPaketler != null && seciliPaketler.Length > 0)
                {
                    foreach (int paketId in seciliPaketler)
                    {
                        PaketGrupDetay yeniDetay = new PaketGrupDetay
                        {
                            PaketGrupId = model.Id,
                            PaketId = paketId,
                            Durumu = 1,
                            EkleyenKullaniciId = kullanici.Id,
                            GuncelleyenKullaniciId = kullanici.Id,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now
                        };
                        _paketGrupDetayRepository.Ekle(yeniDetay);
                    }
                }

                TempData["Success"] = "Paket grubu ve içeriği başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Grup güncellenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
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

                PaketGrup existing = _paketGrupRepository.Getir(Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "Grup bulunamadı.";
                    return RedirectToAction("Index");
                }

                // İlişkili detayları da sil
                List<PaketGrupDetay> detaylar = _paketGrupDetayRepository.GetirList(x => x.PaketGrupId == Id);
                foreach (PaketGrupDetay detay in detaylar)
                {
                    detay.Durumu = 0;
                    detay.GuncelleyenKullaniciId = kullanici.Id;
                    detay.GuncellenmeTarihi = DateTime.Now;
                    _paketGrupDetayRepository.Guncelle(detay);
                }

                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _paketGrupRepository.Guncelle(existing);

                TempData["Success"] = "Paket grubu ve içeriği başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Grup silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            PaketGrup grup = _paketGrupRepository.Getir(id);
            if (grup == null || grup.Durumu == 0) return NotFound();

            // Gruba ait paket ID'lerini al
            List<PaketGrupDetay> detaylar = _paketGrupDetayRepository.GetirList(x => x.PaketGrupId == id && x.Durumu == 1);
            List<int> paketIds = detaylar.Select(x => x.PaketId).ToList();

            return Json(new
            {
                id = grup.Id,
                adi = grup.Adi,
                lisansTipId = grup.LisansTipId,
                fiyat = grup.Fiyat,
                kFiyat = grup.KFiyat,
                egitimSuresi = grup.EgitimSuresi, // Eğitim süresi eklendi
                indOran = grup.IndOran,
                sira = grup.Sira,
                seciliPaketler = paketIds
            });
        }

        [HttpGet]
        public IActionResult GetirPaketler(int lisansTipId)
        {
            LoadCommonData();

            try
            {
                var paketler = _paketRepository.GetirList(
                        x => x.Durumu == 1 && x.LisansTipId == lisansTipId,
                        new List<string> { "LisansTip", "Birim" }
                    )
                    .Select(x => new
                    {
                        id = x.Id,
                        adi = x.Adi,
                        fiyat = x.Fiyat,
                        egitimSuresi = x.EgitimSuresi,
                        lisansTipAdi = x.LisansTip?.Adi,
                        birimAdi = x.Birim?.Adi,
                        sira = x.Sira        // ← sıra bilgisi burada alınmalı
                    })
                    .OrderBy(x => x.sira)   // ← küçükten büyüğe sıralama
                    .ToList();


                return Json(new { success = true, data = paketler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult HesaplaFiyat(int[] paketIds)
        {
            LoadCommonData();

            try
            {
                decimal toplamFiyat = 0;
                if (paketIds != null && paketIds.Length > 0)
                {
                    List<Paket> paketler = _paketRepository.GetirList(x => paketIds.Contains(x.Id) && x.Durumu == 1);
                    toplamFiyat = paketler.Sum(x => x.Fiyat ?? 0);
                }
                return Json(new { success = true, toplamFiyat = toplamFiyat });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}