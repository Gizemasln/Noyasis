using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminUYBController : AdminBaseController
    {
        private readonly UYBRepository _uybRepository = new UYBRepository();
        private readonly ArsivRepository _arsivRepository = new ArsivRepository();

        public IActionResult Index()
        {
            UYB uyb = _uybRepository.Getir(x => x.Durumu == 1);
            ViewBag.UYB = uyb;

            // Aktif arşiv kaydını getir (Durumu=1 olan)
            Arsiv arsiv = _arsivRepository.Getir(x => x.Durumu == 1);
            ViewBag.Arsiv = arsiv;

            return View();
        }

        #region UYB İşlemleri

        [HttpPost]
        public IActionResult UYBEkle(decimal Oran)
        {
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "UYB oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                UYB mevcut = _uybRepository.Getir(x => x.Durumu == 1);
                if (mevcut != null)
                {
                    TempData["Error"] = "UYB oranı zaten tanımlı. Yeni ekleme yapılamaz, düzenleme yapabilirsiniz.";
                    return RedirectToAction("Index");
                }

                UYB yeniUYB = new UYB
                {
                    Oran = Oran,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _uybRepository.Ekle(yeniUYB);
                TempData["Success"] = "UYB oranı başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"UYB oranı eklenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UYBGuncelle(int Id, decimal Oran)
        {
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                if (Oran < 0 || Oran > 100)
                {
                    TempData["Error"] = "UYB oranı 0 ile 100 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                UYB mevcut = _uybRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "UYB oranı bulunamadı.";
                    return RedirectToAction("Index");
                }

                mevcut.Oran = Oran;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _uybRepository.Guncelle(mevcut);
                TempData["Success"] = "UYB oranı başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"UYB oranı güncellenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult UYBGetir(int id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
                return Unauthorized();

            UYB item = _uybRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new { id = item.Id, oran = item.Oran });
        }

        #endregion

        #region Arşiv İşlemleri

        [HttpPost]
        public IActionResult ArsivEkle(int Gun)
        {
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                if (Gun < 1 || Gun > 365)
                {
                    TempData["Error"] = "Arşiv gün sayısı 1 ile 365 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                // Zaten bir kayıt varsa eklemeye izin verme
                Arsiv mevcut = _arsivRepository.Getir(x => x.Durumu == 1);
                if (mevcut != null)
                {
                    TempData["Error"] = "Arşiv süresi zaten tanımlı. Yeni ekleme yapılamaz, düzenleme yapabilirsiniz.";
                    return RedirectToAction("Index");
                }

                Arsiv yeniArsiv = new Arsiv
                {
                    Gun = Gun,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _arsivRepository.Ekle(yeniArsiv);
                TempData["Success"] = "Arşiv süresi başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Arşiv süresi eklenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ArsivGuncelle(int Id, int Gun)
        {
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    TempData["Error"] = "Bu işlem için yetkiniz bulunmamaktadır.";
                    return RedirectToAction("Index");
                }

                if (Gun < 1 || Gun > 365)
                {
                    TempData["Error"] = "Arşiv gün sayısı 1 ile 365 arasında olmalıdır.";
                    return RedirectToAction("Index");
                }

                Arsiv mevcut = _arsivRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    TempData["Error"] = "Arşiv süresi bulunamadı.";
                    return RedirectToAction("Index");
                }

                mevcut.Gun = Gun;
                mevcut.GuncelleyenKullaniciId = kullanici.Id;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _arsivRepository.Guncelle(mevcut);
                TempData["Success"] = "Arşiv süresi başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Arşiv süresi güncellenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ArsivGetir(int id)
        {
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
                return Unauthorized();

            Arsiv item = _arsivRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new { id = item.Id, gun = item.Gun });
        }

        #endregion
    }
}