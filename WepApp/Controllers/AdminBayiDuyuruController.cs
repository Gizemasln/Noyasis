using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminBayiDuyuruController : AdminBaseController
    {
        BayiDuyuruRepository _repository = new BayiDuyuruRepository();

        public IActionResult Index()
        {
            LoadCommonData();
      List<BayiDuyuru> list = _repository.Listele()
    .Where(x => x.Durumu == 1)
    .OrderByDescending(x => x.Oncelik)
    .ThenByDescending(x => x.EklenmeTarihi)
    .ToList();

            ViewBag.DuyuruList = list;
            return View();
        }
        [HttpPost]
        public IActionResult Ekle(string Baslik, string Icerik, int Oncelik = 0, DateTime? YayinBaslangicTarihi = null, DateTime? YayinBitisTarihi = null, bool YayindaMi = false)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (!string.IsNullOrEmpty(Baslik))
            {
                BayiDuyuru model = new BayiDuyuru
                {
                    Baslik = Baslik,
                    Metin = Icerik,
                    Oncelik = Oncelik,
                    YayinBaslangicTarihi = YayinBaslangicTarihi,
                    YayinBitisTarihi = YayinBitisTarihi,
                    YayindaMi = YayindaMi ? 1 : 0,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId = kullanici.Id,
                    Durumu = 1
                };
                _repository.Ekle(model);
                TempData["Success"] = "Duyuru başarıyla eklendi.";
            }
            else
            {
                TempData["Error"] = "Lütfen başlık girin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Baslik, string Icerik, int Oncelik = 0, DateTime? YayinBaslangicTarihi = null, DateTime? YayinBitisTarihi = null, bool YayindaMi = false)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            BayiDuyuru existingEntity = _repository.Getir(Id);
            if (existingEntity != null)
            {
                existingEntity.Baslik = Baslik;
                existingEntity.Metin = Icerik;
                existingEntity.Oncelik = Oncelik;
                existingEntity.YayinBaslangicTarihi = YayinBaslangicTarihi;
                existingEntity.YayinBitisTarihi = YayinBitisTarihi;
                existingEntity.YayindaMi = YayindaMi ? 1 : 0;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId = kullanici.Id;
                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            BayiDuyuru duyuru = _repository.Getir(Id);
            if (duyuru != null)
            {
                duyuru.GuncellenmeTarihi = DateTime.Now;
                duyuru.KullanicilarId = kullanici.Id;
                duyuru.Durumu = 0;
                _repository.Guncelle(duyuru);
                TempData["Success"] = "Kayıt başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            BayiDuyuru item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                baslik = item.Baslik,
                icerik = item.Metin,
                oncelik = item.Oncelik,
                yayinbaslangictarihi = item.YayinBaslangicTarihi?.ToString("yyyy-MM-dd"),
                yayinbitistarihi = item.YayinBitisTarihi?.ToString("yyyy-MM-dd"),
                yayindami = item.YayindaMi
            });
        }
    }
}
