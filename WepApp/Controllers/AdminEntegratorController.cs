using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminEntegratorController : AdminBaseController
    {
        private readonly EntegratorRepository _entegratorRepository = new EntegratorRepository();

        public IActionResult Index()
        {
            LoadCommonData();

            List<Entegrator> entegratorler = _entegratorRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();

            ViewBag.Entegratorler = entegratorler;

            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string Kodu, string Aciklama)
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

                // Adı kontrolü
                Entegrator existingAdi = _entegratorRepository.GetirList(x => x.Adi == Adi && x.Durumu == 1).FirstOrDefault();
                if (existingAdi != null)
                {
                    TempData["Error"] = "Bu isimde bir entegratör zaten mevcut.";
                    return RedirectToAction("Index");
                }

                // Kodu kontrolü
                Entegrator existingKodu = _entegratorRepository.GetirList(x => x.Kodu == Kodu && x.Durumu == 1).FirstOrDefault();
                if (existingKodu != null)
                {
                    TempData["Error"] = "Bu kodda bir entegratör zaten mevcut.";
                    return RedirectToAction("Index");
                }

                Entegrator yeniEntegrator = new Entegrator
                {
                    Adi = Adi ?? "",
                    Kodu = Kodu ?? "",
                    Aciklama = Aciklama ?? "",
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _entegratorRepository.Ekle(yeniEntegrator);
                TempData["Success"] = "Entegratör başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Entegratör eklenirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string Kodu, string Aciklama)
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

                Entegrator existing = _entegratorRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Entegratör bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Adı benzersizlik kontrolü
                Entegrator duplicateAdi = _entegratorRepository.GetirList(x => x.Adi == Adi && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicateAdi != null)
                {
                    TempData["Error"] = "Bu isimde başka bir entegratör zaten mevcut.";
                    return RedirectToAction("Index");
                }

                // Kodu benzersizlik kontrolü
                Entegrator duplicateKodu = _entegratorRepository.GetirList(x => x.Kodu == Kodu && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicateKodu != null)
                {
                    TempData["Error"] = "Bu kodda başka bir entegratör zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = Adi ?? "";
                existing.Kodu = Kodu ?? "";
                existing.Aciklama = Aciklama ?? "";
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _entegratorRepository.Guncelle(existing);
                TempData["Success"] = "Entegratör başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Entegratör güncellenirken hata oluştu: {ex.Message}";
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

                Entegrator existing = _entegratorRepository.Getir(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Entegratör bulunamadı.";
                    return RedirectToAction("Index");
                }
                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _entegratorRepository.Guncelle(existing);
                TempData["Success"] = "Entegratör başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Entegratör silinirken hata oluştu: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();


            Entegrator item = _entegratorRepository.Getir(id);
            if (item == null || item.Durumu == 0)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                kodu = item.Kodu,
                aciklama = item.Aciklama
            });
        }
    }
}