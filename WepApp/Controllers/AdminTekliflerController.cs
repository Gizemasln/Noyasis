using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using WebApp.Models;
using WepApp.Models;
using WepApp.Repositories; // Repository'nizin namespace'i

namespace WepApp.Controllers
{
    public class AdminTekliflerController : AdminBaseController
    {
        private readonly TekliflerRepository _repository;

        public AdminTekliflerController()
        {
            _repository = new TekliflerRepository(); // Dependency injection ile inject etmek daha iyi olur
        }

        public IActionResult Index()
        {
            LoadCommonData();

            List<Urun> urun = new List<Urun>();
            UrunRepository urunRepository = new UrunRepository();
            urun = urunRepository.GetirList(x => x.Durumu == 1);
            ViewBag.Urun = urun;
            List<string> join = new List<string>();
            join.Add("Urun");
            List<Teklifler> list = _repository.GetirList(x => x.Durumu == 1, join);
            ViewBag.TeklifList = list;
            return View();
        }

        [HttpPost]
        public IActionResult Ekle(string AdiSoyadi, string Telefon, string Eposta, string Aciklama, int UrunId)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(AdiSoyadi))
            {
                TempData["Error"] = "Lütfen ad soyad girin.";
                return RedirectToAction("Index");
            }

            try
            {
                int onay = 0;
                if (Request.Form.ContainsKey("Onay") && Request.Form["Onay"].ToString() == "on")
                {
                    onay = 1;
                }

                Teklifler model = new Teklifler
                {
                    AdiSoyadi = AdiSoyadi,
                    Telefon = Telefon,
                    Eposta = Eposta,
                    Aciklama = Aciklama,
                    UrunId = UrunId,
                    Onay = onay,
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
        public IActionResult Guncelle(int Id, string AdiSoyadi, string Telefon, string Eposta, string Aciklama, int UrunId)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            if (string.IsNullOrEmpty(AdiSoyadi))
            {
                TempData["Error"] = "Lütfen ad soyad girin.";
                return RedirectToAction("Index");
            }

            Teklifler existingEntity = _repository.Getir(Id);
            if (existingEntity == null)
            {
                TempData["Error"] = "Kayıt bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                int onay = 0;
                if (Request.Form.ContainsKey("Onay") && Request.Form["Onay"].ToString() == "on")
                {
                    onay = 1;
                }

                existingEntity.AdiSoyadi = AdiSoyadi;
                existingEntity.Telefon = Telefon;
                existingEntity.Eposta = Eposta;
                existingEntity.Aciklama = Aciklama;
                existingEntity.UrunId = UrunId;
                existingEntity.Onay = onay;
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
        public IActionResult OnayVer(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Teklifler teklif = _repository.Getir(Id);
            if (teklif != null)
            {
                try
                {
                    teklif.Onay = 1;
                    teklif.GuncellenmeTarihi = DateTime.Now;
                    teklif.KullanicilarId=kullanici.Id;
                    _repository.Guncelle(teklif);
                    TempData["Success"] = "Teklif başarıyla onaylandı.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Onaylama sırasında hata oluştu: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Kayıt bulunamadı.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult OnayKaldir(int Id)
        {
            LoadCommonData();

            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
            Teklifler teklif = _repository.Getir(Id);
            if (teklif != null)
            {
                try
                {
                    teklif.Onay = 0;
                    teklif.KullanicilarId=kullanici.Id;
                    teklif.GuncellenmeTarihi = DateTime.Now;
                    _repository.Guncelle(teklif);
                    TempData["Success"] = "Teklif onayı başarıyla kaldırıldı.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Onay kaldırma sırasında hata oluştu: " + ex.Message;
                }
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
            Teklifler teklif = _repository.Getir(Id);
            if (teklif != null)
            {
                try
                {
                    teklif.Durumu = 0;
                    teklif.GuncellenmeTarihi = DateTime.Now;
                    teklif.KullanicilarId=kullanici.Id;
                    _repository.Guncelle(teklif);
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
            Teklifler item = _repository.Getir(id);
            if (item == null)
            {
                return NotFound();
            }
            return Json(new
            {
                id = item.Id,
                adiSoyadi = item.AdiSoyadi,
                telefon = item.Telefon,
                eposta = item.Eposta,
                aciklama = item.Aciklama,
                urunId = item.UrunId ?? 0,
                onay = item.Onay ?? 0
            });
        }
    }
}