using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminBayiSozlesmeKriteriController : AdminBaseController
    {
        private readonly BayiSozlesmeKriteriRepository _repository = new BayiSozlesmeKriteriRepository();

        public IActionResult Index()
        {
            LoadCommonData();
            List<BayiSozlesmeKriteri> kriterler = _repository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi)
                .ToList();
            ViewBag.Kriterler = kriterler;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string Aciklama, string UzunAciklama, string Konu, decimal Oran)
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

                if (string.IsNullOrWhiteSpace(Adi))
                {
                    TempData["Error"] = "Kriter adı zorunludur.";
                    return RedirectToAction("Index");
                }

                BayiSozlesmeKriteri existing = _repository.GetirList(x => x.Adi.Trim().ToLower() == Adi.Trim().ToLower() && x.Durumu == 1).FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir kriter zaten mevcut.";
                    return RedirectToAction("Index");
                }

                BayiSozlesmeKriteri yeni = new BayiSozlesmeKriteri
                {
                    Adi = Adi.Trim(),
                    Aciklama = Aciklama?.Trim(),
                    UzunAciklama = UzunAciklama?.Trim(),
                    Konu = Konu?.Trim(),
                    Oran = Oran,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _repository.Ekle(yeni);
                TempData["Success"] = "Bayi sözleşme kriteri başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ekleme sırasında hata: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string Aciklama, string UzunAciklama, string Konu, decimal Oran)
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

                BayiSozlesmeKriteri mevcut = _repository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "Kriter bulunamadı.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(Adi))
                {
                    TempData["Error"] = "Kriter adı zorunludur.";
                    return RedirectToAction("Index");
                }

                BayiSozlesmeKriteri duplicate = _repository.GetirList(x => x.Adi.Trim().ToLower() == Adi.Trim().ToLower() && x.Id != Id && x.Durumu == 1).FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir kriter zaten mevcut.";
                    return RedirectToAction("Index");
                }

                mevcut.Adi = Adi.Trim();
                mevcut.Aciklama = Aciklama?.Trim();
                mevcut.UzunAciklama = UzunAciklama?.Trim();
                mevcut.Konu = Konu?.Trim();
                mevcut.Oran = Oran;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _repository.Guncelle(mevcut);
                TempData["Success"] = "Kriter başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Güncelleme sırasında hata: {ex.Message}";
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

                BayiSozlesmeKriteri mevcut = _repository.Getir(Id);
                if (mevcut == null)
                {
                    TempData["Error"] = "Kriter bulunamadı.";
                    return RedirectToAction("Index");
                }
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;
                _repository.Guncelle(mevcut);

                TempData["Success"] = "Kriter başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Silme sırasında hata: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

       
            BayiSozlesmeKriteri item = _repository.Getir(id);
            if (item == null || item.Durumu == 0) return NotFound();

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                aciklama = item.Aciklama,
                uzunAciklama = item.UzunAciklama,
                konu = item.Konu,
                oran = item.Oran
            });
        }
    }
}