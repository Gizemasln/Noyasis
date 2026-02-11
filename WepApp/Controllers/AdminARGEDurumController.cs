using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminARGEDurumController : AdminBaseController
    {

        ARGEDurumRepository _argeDurumRepository = new ARGEDurumRepository();
        public IActionResult Index()
        {
            LoadCommonData();
            List<ARGEDurum> list = _argeDurumRepository.GetirList(x => x.Durumu == 1).OrderBy(x => x.Sira).ToList();
            ViewBag.ARGEDurumlar = list;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Adi, int? Sira)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            int siraNo = Sira ?? 0;
            if (siraNo == 0)
            {
                int maxSira = _argeDurumRepository.GetirList(x => x.Durumu == 1).Max(x => (int?)x.Sira) ?? 0;
                siraNo = maxSira + 1;
            }

            ARGEDurum yeni = new ARGEDurum
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
                _argeDurumRepository.Ekle(yeni);
                TempData["Success"] = "ARGE durumu başarıyla eklendi.";
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


            ARGEDurum mevcut = _argeDurumRepository.Getir(Id);
            if (mevcut == null)
            {
                TempData["Error"] = "ARGE durumu bulunamadı.";
                return RedirectToAction("Index");
            }

            mevcut.Adi = Adi?.Trim() ?? "";
            mevcut.Sira = Sira;
            mevcut.GuncelleyenKullaniciId = kullanici.Id;
            mevcut.GuncellenmeTarihi = DateTime.Now;

            try
            {
                _argeDurumRepository.Guncelle(mevcut);
                TempData["Success"] = "ARGE durumu başarıyla güncellendi.";
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

            ARGEDurum argeDurum = _argeDurumRepository.Getir(Id);
            if (argeDurum == null)
            {
                TempData["Error"] = "ARGE durumu bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                argeDurum.Durumu = 0;
                argeDurum.GuncelleyenKullaniciId = kullanici.Id;
                argeDurum.GuncellenmeTarihi = DateTime.Now;
                _argeDurumRepository.Guncelle(argeDurum);
                TempData["Success"] = "ARGE durumu pasif hale getirildi.";
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
                    ARGEDurum argeDurum = _argeDurumRepository.Getir(item.Id);
                    if (argeDurum != null && argeDurum.Durumu == 1)
                    {
                        argeDurum.Sira = item.Sira;
                        argeDurum.GuncelleyenKullaniciId = kullanici.Id;
                        argeDurum.GuncellenmeTarihi = DateTime.Now;
                        _argeDurumRepository.Guncelle(argeDurum);
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
            ARGEDurum item = _argeDurumRepository.Getir(id);
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
        public IActionResult AdiKontrol(string adi, int? argeDurumId = null)
        {
            LoadCommonData();

            if (string.IsNullOrWhiteSpace(adi))
                return Json(new { success = false, message = "ARGE durum adı boş olamaz." });

            IQueryable<ARGEDurum> query = _argeDurumRepository.GetirQueryable(x => x.Durumu == 1 && x.Adi == adi.Trim());

            if (argeDurumId.HasValue)
                query = query.Where(x => x.Id != argeDurumId.Value);

            bool exists = query.Any();

            return Json(new
            {
                success = !exists,
                message = exists ? "Bu ARGE durum adı zaten mevcut." : "ARGE durum adı kullanılabilir."
            });
        }

        [HttpGet]
        public IActionResult SiraKontrol(int sira, int? argeDurumId = null)
        {
            LoadCommonData();

            if (sira <= 0)
                return Json(new { success = false, message = "Sıra numarası 0'dan büyük olmalıdır." });

            IQueryable<ARGEDurum> query = _argeDurumRepository.GetirQueryable(x => x.Durumu == 1 && x.Sira == sira);

            if (argeDurumId.HasValue)
                query = query.Where(x => x.Id != argeDurumId.Value);

            bool exists = query.Any();

            return Json(new
            {
                success = !exists,
                message = exists ? "Bu sıra numarası zaten kullanılıyor." : "Sıra numarası kullanılabilir."
            });
        }
    }

    public class SiraGuncelleModel
    {
        public int Id { get; set; }
        public int Sira { get; set; }
    }
}