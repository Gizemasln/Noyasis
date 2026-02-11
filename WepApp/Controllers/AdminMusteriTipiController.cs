using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminMusteriTipiController : AdminBaseController
    {
        MusteriTipiRepository _musteriTipiRepository = new MusteriTipiRepository();
        MusteriRepository _musteriRepository = new MusteriRepository();



        public IActionResult Index()
        {
            LoadCommonData();

            List<MusteriTipi> musteriTipleri = _musteriTipiRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();

            ViewBag.MusteriTipleri = musteriTipleri;

            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi)
        {
            LoadCommonData();
            try
            {
                // Oturum bilgilerini al
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                // Aynı isimde müşteri tipi var mı kontrol et
                MusteriTipi existing = _musteriTipiRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir müşteri tipi zaten mevcut.";
                    return RedirectToAction("Index");
                }

                MusteriTipi yeniMusteriTipi = new MusteriTipi
                {
                    Adi = Adi ?? "",
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _musteriTipiRepository.Ekle(yeniMusteriTipi);
                TempData["Success"] = "Müşteri tipi başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Müşteri tipi eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi)
        {
            LoadCommonData();
            try
            {
                // Oturum bilgilerini al
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                MusteriTipi existing = _musteriTipiRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Müşteri tipi bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Aynı isimde başka müşteri tipi var mı kontrol et
                MusteriTipi duplicate = _musteriTipiRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir müşteri tipi zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _musteriTipiRepository.Guncelle(existing);
                TempData["Success"] = "Müşteri tipi başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Müşteri tipi güncellenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            try
            {
                // Oturum bilgilerini al
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                MusteriTipi existing = _musteriTipiRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Müşteri tipi bulunamadı.";
                    return RedirectToAction("Index");
                }
                 List<Musteri> kullananMusteriler = _musteriRepository.GetirList(x => x.MusteriTipiId == Id && x.Durum == 1);
                if (kullananMusteriler.Any())
                {
                    TempData["Error"] = "Bu müşteri tipini kullanan müşteriler bulunmaktadır. Önce bu müşterileri silmelisiniz.";
                    return RedirectToAction("Index");
                }

                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _musteriTipiRepository.Guncelle(existing);
                TempData["Success"] = "Müşteri tipi başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Müşteri tipi silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();


            MusteriTipi item = _musteriTipiRepository.Getir(id);
            if (item == null || item.Durumu == 0)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                adi = item.Adi
            });
        }
    }
}