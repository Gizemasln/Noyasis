using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Repository'nizin namespace'i

namespace WepApp.Controllers
{
    public class AdminSSSController : AdminBaseController
    {
        private readonly SSSRepository _repository;

        public AdminSSSController()
        {
            _repository = new SSSRepository(); // Dependency injection ile inject etmek daha iyi olur
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<SSS> list = _repository.GetirList(x => x.Durumu == 1);
            ViewBag.SSSList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string Soru, string Cevap)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            if (string.IsNullOrEmpty(Soru))
            {
                TempData["Error"] = "Lütfen soru girin.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(Cevap))
            {
                TempData["Error"] = "Lütfen cevap girin.";
                return RedirectToAction("Index");
            }

            try
            {
                SSS model = new SSS
                {
                    Soru = Soru,
                    Cevap = Cevap,
                    Durumu = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    KullanicilarId=kullanici.Id
                };
                _repository.Ekle(model);
                TempData["Success"] = "Bilgi başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Kayıt sırasında hata oluştu: " + ex.Message;
                // Log ekleyin: ILogger ile
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(int Id, string Soru, string Cevap)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            if (string.IsNullOrEmpty(Soru))
            {
                TempData["Error"] = "Lütfen soru girin.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(Cevap))
            {
                TempData["Error"] = "Lütfen cevap girin.";
                return RedirectToAction("Index");
            }

            SSS existingEntity = _repository.Getir(Id);
            if (existingEntity == null)
            {
                TempData["Error"] = "Kayıt bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                existingEntity.Soru = Soru;
                existingEntity.Cevap = Cevap;
                existingEntity.GuncellenmeTarihi = DateTime.Now;
                existingEntity.KullanicilarId=kullanici.Id;

                _repository.Guncelle(existingEntity);
                TempData["Success"] = "Kayıt başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Güncelleme sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            SSS sss = _repository.Getir(Id);
            if (sss != null)
            {
                try
                {
                    sss.Durumu = 0;
                    sss.GuncellenmeTarihi = DateTime.Now;
                    sss.KullanicilarId = kullanici.Id;
                    _repository.Guncelle(sss);
                    TempData["Success"] = "Kayıt başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Silme sırasında hata oluştu: " + ex.Message;
                }
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
            LoadCommonData();

            SSS item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                soru = item.Soru,
                cevap = item.Cevap
            });
        }
    }
}