using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminTeklifDurumController : AdminBaseController
    {
        private readonly TeklifDurumRepository _teklifDurumRepository = new TeklifDurumRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<TeklifDurum> teklifDurumlari = _teklifDurumRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Sıra)          // Sıra'ya göre sırala
                .ThenBy(x => x.Adi)
                .ToList();

            ViewBag.TeklifDurumlari = teklifDurumlari;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string? Aciklama, int Sıra = 0)
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

                TeklifDurum existing = _teklifDurumRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir teklif durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                // Sıra belirtilmemişse en alta ekle (max sıra + 1)
                int yeniSira = Sıra > 0 ? Sıra :
                    (_teklifDurumRepository.GetirList(x => x.Durumu == 1).Max(x => (int?)x.Sıra) ?? 0) + 1;

                TeklifDurum yeniTeklifDurum = new TeklifDurum
                {
                    Adi = Adi ?? "",
                    Aciklama = Aciklama,
                    Sıra = yeniSira,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _teklifDurumRepository.Ekle(yeniTeklifDurum);
                TempData["Success"] = "Teklif durumu başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Teklif durumu eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string? Aciklama, int Sıra)
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

                TeklifDurum existing = _teklifDurumRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Teklif durumu bulunamadı.";
                    return RedirectToAction("Index");
                }

                TeklifDurum duplicate = _teklifDurumRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir teklif durumu zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.Aciklama = Aciklama;
                existing.Sıra = Sıra;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _teklifDurumRepository.Guncelle(existing);
                TempData["Success"] = "Teklif durumu başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Teklif durumu güncellenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            // Silme işlemi aynı kalıyor (soft delete)
            // ... (önceki kodunuz aynı)
            LoadCommonData();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                TeklifDurum existing = _teklifDurumRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Teklif durumu bulunamadı.";
                    return RedirectToAction("Index");
                }

                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _teklifDurumRepository.Guncelle(existing);

                TempData["Success"] = "Teklif durumu başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Teklif durumu silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            TeklifDurum item = _teklifDurumRepository.Getir(id);
            if (item == null || item.Durumu == 0)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                aciklama = item.Aciklama,
                sıra = item.Sıra               // Yeni alan eklendi
            });
        }
    }
}