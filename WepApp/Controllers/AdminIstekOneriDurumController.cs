// WepApp.Controllers/AdminIstekOneriDurumController.cs
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminIstekOneriDurumController : AdminBaseController
    {
        IstekOneriDurumRepository _istekOneriDurumRepository = new IstekOneriDurumRepository();


        public IActionResult Index()
        {
            LoadCommonData();
            List<IstekOneriDurum> list = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1).OrderBy(x => x.Sira).ToList();
            ViewBag.IstekOneriDurumlar = list;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Adi, int? Sira)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            // Sıra numarası belirlenmesi
            int siraNo = Sira ?? 0;
            if (siraNo == 0)
            {
                int maxSira = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1).Max(x => (int?)x.Sira) ?? 0;
                siraNo = maxSira + 1;
            }

            IstekOneriDurum yeni = new IstekOneriDurum
            {
                Adi = Adi?.Trim() ?? "",
                Sira = siraNo,
                Durumu = 1,
                EkleyenKullaniciId = kullanici.Id,
                GuncelleyenKullaniciId = kullanici.Id,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now
            };

            try
            {
                _istekOneriDurumRepository.Ekle(yeni);
                TempData["Success"] = "İstek/Öneri durumu başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Guncelle(int Id, string Adi, int Sira)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            IstekOneriDurum mevcut = _istekOneriDurumRepository.Getir(Id);
            if (mevcut == null)
            {
                TempData["Error"] = "İstek/Öneri durumu bulunamadı.";
                return RedirectToAction("Index");
            }

            mevcut.Adi = Adi?.Trim() ?? "";
            mevcut.Sira = Sira;
            mevcut.GuncelleyenKullaniciId = kullanici.Id;
            mevcut.GuncellenmeTarihi = DateTime.Now;

            try
            {
                _istekOneriDurumRepository.Guncelle(mevcut);
                TempData["Success"] = "İstek/Öneri durumu başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            IstekOneriDurum istekOneriDurum = _istekOneriDurumRepository.Getir(Id);
            if (istekOneriDurum == null)
            {
                TempData["Error"] = "İstek/Öneri durumu bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                istekOneriDurum.Durumu = 0;
                istekOneriDurum.GuncelleyenKullaniciId = kullanici.Id;
                istekOneriDurum.GuncellenmeTarihi = DateTime.Now;
                _istekOneriDurumRepository.Guncelle(istekOneriDurum);
                TempData["Success"] = "İstek/Öneri durumu pasif hale getirildi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SiraGuncelle(List<SiraGuncelleModel> siralamalar)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                return Json(new { success = false, message = "Yetkisiz işlem." });
            }

            try
            {
                foreach (SiraGuncelleModel item in siralamalar)
                {
                    IstekOneriDurum istekOneriDurum = _istekOneriDurumRepository.Getir(item.Id);
                    if (istekOneriDurum != null && istekOneriDurum.Durumu == 1)
                    {
                        istekOneriDurum.Sira = item.Sira;
                        istekOneriDurum.GuncelleyenKullaniciId = kullanici.Id;
                        istekOneriDurum.GuncellenmeTarihi = DateTime.Now;
                        _istekOneriDurumRepository.Guncelle(istekOneriDurum);
                    }
                }
                return Json(new { success = true, message = "Sıralama başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            IstekOneriDurum item = _istekOneriDurumRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                sira = item.Sira
            });
        }

        [HttpGet]
        public IActionResult AdiKontrol(string adi, int? istekOneriDurumId = null)
        {
            LoadCommonData();

            if (string.IsNullOrWhiteSpace(adi))
                return Json(new { success = false, message = "İstek/Öneri durum adı boş olamaz." });

            IQueryable<IstekOneriDurum> query = _istekOneriDurumRepository.GetirQueryable(x => x.Durumu == 1 && x.Adi == adi.Trim());

            if (istekOneriDurumId.HasValue)
                query = query.Where(x => x.Id != istekOneriDurumId.Value);

            bool exists = query.Any();

            return Json(new
            {
                success = !exists,
                message = exists ? "Bu istek/öneri durum adı zaten mevcut." : "İstek/Öneri durum adı kullanılabilir."
            });
        }

        [HttpGet]
        public IActionResult SiraKontrol(int sira, int? istekOneriDurumId = null)
        {
            LoadCommonData();

            if (sira <= 0)
                return Json(new { success = false, message = "Sıra numarası 0'dan büyük olmalıdır." });

            IQueryable<IstekOneriDurum> query = _istekOneriDurumRepository.GetirQueryable(x => x.Durumu == 1 && x.Sira == sira);

            if (istekOneriDurumId.HasValue)
                query = query.Where(x => x.Id != istekOneriDurumId.Value);

            bool exists = query.Any();

            return Json(new
            {
                success = !exists,
                message = exists ? "Bu sıra numarası zaten kullanılıyor." : "Sıra numarası kullanılabilir."
            });
        }
    }


}