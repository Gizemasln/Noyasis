// WepApp.Controllers/AdminBirimController.cs
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminBirimController : AdminBaseController
    {
        private readonly BirimRepository _birimRepository;

        public AdminBirimController()
        {
            _birimRepository = new BirimRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();
            List<Birim> list = _birimRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Birimler = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            Birim yeni = new Birim
            {
                Adi = Adi?.Trim() ?? "",
                Durumu = 1,
                EkleyenKullaniciId = kullanici.Id,
                GuncelleyenKullaniciId = kullanici.Id,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now
            };

            try
            {
                _birimRepository.Ekle(yeni);
                TempData["Success"] = "Birim başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            Birim mevcut = _birimRepository.Getir(Id);
            if (mevcut == null)
            {
                TempData["Error"] = "Birim bulunamadı.";
                return RedirectToAction("Index");
            }

            mevcut.Adi = Adi?.Trim() ?? "";
            mevcut.GuncelleyenKullaniciId = kullanici.Id;
            mevcut.GuncellenmeTarihi = DateTime.Now;

            try
            {
                _birimRepository.Guncelle(mevcut);
                TempData["Success"] = "Birim başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Birim birim = _birimRepository.Getir(Id);
            if (kullanici == null)
            {
                TempData["Error"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            try
            {
                birim.Durumu = 0;
                birim.GuncelleyenKullaniciId=kullanici.Id;
                _birimRepository.Guncelle(birim);
                TempData["Success"] = "Birim pasif hale getirildi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            Birim item = _birimRepository.Getir(id);
            if (item == null || item.Durumu == 0)
                return NotFound();

            return Json(new
            {
                id = item.Id,
                adi = item.Adi
            });
        }

        [HttpGet]
        public IActionResult AdiKontrol(string adi, int? birimId = null)
        {
            LoadCommonData();

            if (string.IsNullOrWhiteSpace(adi))
                return Json(new { success = false, message = "Birim adı boş olamaz." });

            IQueryable<Birim> query = _birimRepository.GetirQueryable(x => x.Durumu == 1 && x.Adi == adi.Trim());

            if (birimId.HasValue)
                query = query.Where(x => x.Id != birimId.Value);

            bool exists = query.Any();

            return Json(new
            {
                success = !exists,
                message = exists ? "Bu birim adı zaten mevcut." : "Birim adı kullanılabilir."
            });
        }
    }
}