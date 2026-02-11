// Controllers/IstekOneriController.cs
using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebApp.Repositories;
using Microsoft.AspNetCore.Http;

namespace WepApp.Controllers
{
    public class IstekOneriController : AdminBaseController
    {
        private readonly IstekOneriRepository _repository = new IstekOneriRepository();
        private readonly GenericRepository<LisansTip> _lisansTipRepository = new GenericRepository<LisansTip>();
        private const int PageSize = 10;

        public IActionResult Index(int page = 1)
        {
            IActionResult redirectResult = LoadCommonData();
            if (redirectResult != null) return redirectResult;

            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;

            List<IstekOneriler> liste;
            int toplam;

            if (kullaniciTipi == "Musteri" && kullaniciId.HasValue)
            {
                liste = _repository.GetirMusteriyeAitListesi(kullaniciId.Value, page, PageSize);
                toplam = _repository.GetirMusteriyeAitToplamSayi(kullaniciId.Value);
            }
            else if (kullaniciTipi == "Bayi" && kullaniciId.HasValue)
            {
                liste = _repository.GetirBayiyeAitListesi(kullaniciId.Value, page, PageSize);
                toplam = _repository.GetirBayiyeAitToplamSayi(kullaniciId.Value);
            }
            else
            {
                liste = new List<IstekOneriler>();
                toplam = 0;
            }

            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1);

            ViewBag.LisansTipleri = lisansTipleri;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)toplam / PageSize);
            ViewBag.TotalCount = toplam;

            return View(liste);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(string Konu, string Metni, int LisansTipId)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası. Lütfen tekrar giriş yapın." });

            if (string.IsNullOrWhiteSpace(Konu) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Konu ve metin zorunludur." });

            try
            {
                IstekOneriler yeni = new IstekOneriler
                {
                    Konu = Konu.Trim(),
                    Metni = Metni.Trim(),
                    LisansTipId = LisansTipId,
                    Durumu = 1,
                    EkleyenKullaniciId = kullaniciId.Value,
                    GuncelleyenKullaniciId = kullaniciId.Value,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    IstekOneriDurumId=4
                };

                if (kullaniciTipi == "Musteri")
                    yeni.MusteriId = kullaniciId.Value;
                else if (kullaniciTipi == "Bayi")
                    yeni.BayiId = kullaniciId.Value;

                _repository.Ekle(yeni);
                return Json(new { success = true, message = "İstek/Öneri başarıyla gönderildi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ekleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirDuzenle(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler kayit = _repository.GetirById(id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            List<LisansTip> lisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1);

            var data = new
            {
                kayit.Id,
                kayit.Konu,
                kayit.Metni,
                kayit.LisansTipId,
                LisansTipleri = lisansTipleri.Select(g => new { g.Id, g.Adi })
            };

            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(int Id, string Konu, string Metni, int LisansTipId)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler mevcut = _repository.GetirById(Id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (mevcut == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            if (string.IsNullOrWhiteSpace(Konu) || string.IsNullOrWhiteSpace(Metni))
                return Json(new { success = false, message = "Konu ve metin zorunludur." });

            try
            {
                mevcut.Konu = Konu.Trim();
                mevcut.Metni = Metni.Trim();
                mevcut.LisansTipId = LisansTipId;
                mevcut.GuncelleyenKullaniciId = kullaniciId.Value;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _repository.Guncelle(mevcut);
                return Json(new { success = true, message = "Güncelleme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Güncelleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
            if (string.IsNullOrEmpty(kullaniciTipi) || !kullaniciId.HasValue)
                return Json(new { success = false, message = "Oturum hatası." });

            IstekOneriler kayit = _repository.GetirById(id,
                kullaniciTipi == "Musteri" ? kullaniciId : null,
                kullaniciTipi == "Bayi" ? kullaniciId : null);

            if (kayit == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            try
            {
                _repository.Sil(id);
                return Json(new { success = true, message = "Silme başarılı." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silme hatası: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}