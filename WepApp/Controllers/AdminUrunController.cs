using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using System.Linq;
using WebApp.Models;
using WepApp.Controllers;

namespace WebApp.Controllers
{
    public class AdminUrunController : AdminBaseController
    {
        private readonly UrunRepository _repository;
        private readonly KategoriRepository _kategoriRepository;

        public AdminUrunController()
        {
            _repository = new UrunRepository();
            _kategoriRepository = new KategoriRepository();
        }

        public IActionResult Index()
        {
            LoadCommonData();

            // Sadece Kategori join'i kaldı, Kampanyalar tamamen kaldırıldı
            List<string> join = new List<string> { "Kategori" };
            List<Urun> urunList = _repository.GetirList(x => x.Durumu == 1, join);

            foreach (Urun urun in urunList)
            {
                decimal suresizIndirim = urun.SureSizIndirimYuzdesi ?? 0;
                urun.SonFiyat = Math.Round(urun.Fiyat * (1 - suresizIndirim / 100m), 2);
            }

            ViewBag.UrunList = urunList;
            ViewBag.Kategoriler = _kategoriRepository.GetirList(x => x.Durumu == 1);
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Adi, string Aciklama, int KategoriId, int? Stok, string UrunKodu,
            decimal Fiyat, decimal? SureSizIndirimYuzdesi)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            Urun model = new Urun
            {
                Adi = Adi ?? "",
                Aciklama = Aciklama ?? "",
                KategoriId = KategoriId,
                Stok = Stok ?? 0,
                UrunKodu = UrunKodu ?? "",
                Fiyat = Fiyat,
                SureSizIndirimYuzdesi = SureSizIndirimYuzdesi,
                Durumu = 1,
                EklenmeTarihi = DateTime.Now,
                GuncellenmeTarihi = DateTime.Now,
                KullanicilarId = kullanici.Id
            };

            _repository.Ekle(model);
            TempData["Success"] = "Ürün başarıyla eklendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Adi, string Aciklama, int KategoriId, int? Stok,
            string UrunKodu, decimal Fiyat, decimal? SureSizIndirimYuzdesi)
        {
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Urun existing = _repository.Getir(Id);

            if (existing != null)
            {
                existing.Adi = Adi;
                existing.Aciklama = Aciklama;
                existing.KategoriId = KategoriId;
                existing.Stok = Stok ?? 0;
                existing.UrunKodu = UrunKodu;
                existing.Fiyat = Fiyat;
                existing.SureSizIndirimYuzdesi = SureSizIndirimYuzdesi;
                existing.GuncellenmeTarihi = DateTime.Now;
                existing.KullanicilarId = kullanici.Id;

                _repository.Guncelle(existing);
                TempData["Success"] = "Ürün güncellendi.";
            }
            else
            {
                TempData["Error"] = "Ürün bulunamadı.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();
            Urun urun = _repository.Getir(Id);
            if (urun != null)
            {
                urun.Durumu = 0;
                urun.GuncellenmeTarihi = DateTime.Now;
                _repository.Guncelle(urun);
                TempData["Success"] = "Ürün silindi (pasif).";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            Urun item = _repository.Getir(id);
            if (item == null) return NotFound();

            return Json(new
            {
                id = item.Id,
                adi = item.Adi,
                aciklama = item.Aciklama,
                kategoriId = item.KategoriId,
                stok = item.Stok,
                urunKodu = item.UrunKodu,
                fiyat = item.Fiyat,
                sureSizIndirimYuzdesi = item.SureSizIndirimYuzdesi
            });
        }
    }
}