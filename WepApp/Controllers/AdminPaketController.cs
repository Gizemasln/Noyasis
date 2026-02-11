using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Controllers;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class AdminPaketController : AdminBaseController
    {
        private readonly PaketRepository _paketRepository = new PaketRepository();
        private readonly KDVRepository _kdvRepository = new KDVRepository();
        private readonly LisansTipRepository _lisansTipRepository = new LisansTipRepository();
        private readonly BirimRepository _birimRepository = new BirimRepository();
        private readonly FiyatOranRepository _fiyatOranRepository = new FiyatOranRepository();

        public IActionResult Index()
        {
            LoadCommonData();

            // Paketleri KDV bilgisiyle birlikte getir
            List<string> join = new List<string> { "LisansTip", "Birim", "KDV" };
            List<Paket> paketler = _paketRepository.GetirList(x => x.Durumu == 1, join)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Adi)
                .ToList();

            // KDV listesini al
            List<KDV> kdvList = _kdvRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Oran)
                .ToList();

            ViewBag.KDV = kdvList;

            // Her paket için fiyat oranı sayısını hesapla
            Dictionary<int, int>  fiyatOranSayilari = _fiyatOranRepository.GetirList(x => x.Durumu == 1)
                .GroupBy(x => x.PaketId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.FiyatOranSayilari = fiyatOranSayilari;

            // Her paket için Sabit Çarpım (Oran=false) kontrolü
            Dictionary<int, bool> sabitCarpimVarMi = new Dictionary<int, bool>();
            Dictionary<int, int> sabitCarpimIdleri = new Dictionary<int, int>();

            foreach (Paket paket in paketler)
            {
                FiyatOran sabitCarpim = _fiyatOranRepository.GetirList(x => x.PaketId == paket.Id &&
                                                                     x.Durumu == 1 &&
                                                                     x.Oran == false)
                                                    .FirstOrDefault();

                sabitCarpimVarMi[paket.Id] = sabitCarpim != null;
                if (sabitCarpim != null)
                {
                    sabitCarpimIdleri[paket.Id] = sabitCarpim.Id;
                }
            }

            ViewBag.SabitCarpimVarMi = sabitCarpimVarMi;
            ViewBag.SabitCarpimIdleri = sabitCarpimIdleri;
            ViewBag.Paketler = paketler;

            ViewBag.LisansTipleri = _lisansTipRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi).ToList();

            ViewBag.Birimler = _birimRepository.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Adi).ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Ekle(Paket model)
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

                if (string.IsNullOrWhiteSpace(model.Adi))
                {
                    TempData["Error"] = "Paket adı gereklidir.";
                    return RedirectToAction("Index");
                }

                Paket existing = _paketRepository.GetirList(x => x.Adi == model.Adi.Trim() && x.Durumu == 1)
                    .FirstOrDefault();
                if (existing != null)
                {
                    TempData["Error"] = "Bu isimde bir paket zaten mevcut.";
                    return RedirectToAction("Index");
                }

                Paket yeniPaket = new Paket
                {
                    Adi = model.Adi.Trim(),
                    LisansTipId = model.LisansTipId,
                    BirimId = model.BirimId,
                    Fiyat = model.Fiyat,
                    KFiyat = model.Fiyat,
                    IndOran = model.IndOran,
                    KDVId = model.KDVId,  // KDVId olarak kaydet
                    Aktif = model.Aktif,
                    ModulKodu = model.ModulKodu?.Trim(),
                    EgitimSuresi = model.EgitimSuresi,
                    Sira = model.Sira,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _paketRepository.Ekle(yeniPaket);
                TempData["Success"] = "Paket başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Paket eklenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Guncelle(Paket model)
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

                Paket existing = _paketRepository.Getir(model.Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "Paket bulunamadı.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(model.Adi))
                {
                    TempData["Error"] = "Paket adı gereklidir.";
                    return RedirectToAction("Index");
                }

                Paket duplicate = _paketRepository.GetirList(x => x.Adi == model.Adi.Trim()
                                                            && x.Id != model.Id
                                                            && x.Durumu == 1)
                    .FirstOrDefault();
                if (duplicate != null)
                {
                    TempData["Error"] = "Bu isimde başka bir paket zaten mevcut.";
                    return RedirectToAction("Index");
                }

                existing.Adi = model.Adi.Trim();
                existing.LisansTipId = model.LisansTipId;
                existing.BirimId = model.BirimId;
                existing.Fiyat = model.Fiyat;
                existing.IndOran = model.IndOran;
                existing.KDVId = model.KDVId;  // KDVId olarak güncelle
                existing.Aktif = model.Aktif;
                existing.ModulKodu = model.ModulKodu?.Trim();
                existing.EgitimSuresi = model.EgitimSuresi;
                existing.Sira = model.Sira;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;

                _paketRepository.Guncelle(existing);
                TempData["Success"] = "Paket başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Paket güncellenirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DurumGuncelle(int Id, int aktif)
        {
            LoadCommonData();
            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                Paket existing = _paketRepository.Getir(Id);
                if (existing == null || existing.Durumu == 0)
                {
                    return Json(new { success = false, message = "Paket bulunamadı." });
                }

                existing.Aktif = aktif;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _paketRepository.Guncelle(existing);

                return Json(new { success = true, message = "Paket durumu güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
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

                Paket existing = _paketRepository.Getir(Id);
                if (existing == null || existing.Durumu == 0)
                {
                    TempData["Error"] = "Paket bulunamadı.";
                    return RedirectToAction("Index");
                }

                existing.Durumu = 0;
                existing.GuncelleyenKullaniciId = kullanici.Id;
                existing.GuncellenmeTarihi = DateTime.Now;
                _paketRepository.Guncelle(existing);

                TempData["Success"] = "Paket başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Paket silinirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Getir(int id)
        {
            LoadCommonData();

            Paket paket = _paketRepository.Getir(id);
            if (paket == null || paket.Durumu == 0) return NotFound();

            return Json(new
            {
                id = paket.Id,
                adi = paket.Adi,
                lisansTipiId = paket.LisansTipId,
                birimId = paket.BirimId,
                fiyat = paket.Fiyat,
                indOran = paket.IndOran,
                kdvId = paket.KDVId,  // KDVId döndür
                aktif = paket.Aktif,
                modulKodu = paket.ModulKodu,
                egitimSuresi = paket.EgitimSuresi,
                sira = paket.Sira
            });
        }

        [HttpPost]
        public IActionResult FiyatOraniEkle(int paketId, int min, int max, int oranYuzde, bool oranUygula = false)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                Paket paket = _paketRepository.Getir(paketId);
                if (paket == null || paket.Durumu == 0)
                {
                    return Json(new { success = false, message = "Paket bulunamadı." });
                }

                // YENİ KURAL: Aynı pakette hem sabit çarpım hem miktar bazlı oran OLAMAZ
                bool mevcutSabitCarpim = _fiyatOranRepository.GetirList(x => x.PaketId == paketId && x.Durumu == 1 && x.Oran == false).Any();
                bool mevcutMiktarBazli = _fiyatOranRepository.GetirList(x => x.PaketId == paketId && x.Durumu == 1 && x.Oran == true).Any();

                if (oranUygula && mevcutSabitCarpim)
                {
                    return Json(new { success = false, message = "Bu pakette zaten 'Sabit Çarpım' oranı tanımlı. Miktar bazlı oran ekleyemezsiniz." });
                }

                if (!oranUygula && mevcutMiktarBazli)
                {
                    return Json(new { success = false, message = "Bu pakette zaten miktar bazlı oranlar tanımlı. Sabit Çarpım ekleyemezsiniz." });
                }

                // Miktar bazlı için kontroller
                if (oranUygula)
                {
                    if (min >= max)
                    {
                        return Json(new { success = false, message = "Max değeri Min değerinden büyük olmalıdır." });
                    }

                    List<FiyatOran> mevcutOranlar = _fiyatOranRepository.GetirList(x => x.PaketId == paketId && x.Durumu == 1 && x.Oran == true)
                        .OrderBy(x => x.Min)
                        .ToList();

                    // Çakışma kontrolü
                    foreach (FiyatOran oran in mevcutOranlar)
                    {
                        if (min < oran.Max && max > oran.Min)
                        {
                            return Json(new { success = false, message = $"Bu aralık mevcut {oran.Min}-{oran.Max} aralığı ile çakışıyor." });
                        }
                    }

                    // Boşlukları kontrol et ve en uygun boşluğu öner
                    if (mevcutOranlar.Any())
                    {
                        // Boşlukları bul
                        List<(int Baslangic, int Bitis)> bosluklar = new List<(int Baslangic, int Bitis)>();
                        int oncekiMax = 0;

                        foreach (FiyatOran oran in mevcutOranlar)
                        {
                            if (oran.Min > oncekiMax + 1)
                            {
                                bosluklar.Add((oncekiMax + 1, oran.Min - 1));
                            }
                            oncekiMax = oran.Max;
                        }

                        // Eğer eklenen aralık bir boşluğa tam olarak uymuyorsa kontrol et
                        if (bosluklar.Any())
                        {
                            var uygunBosluk = bosluklar.FirstOrDefault(b => min >= b.Baslangic && max <= b.Bitis);

                            if (uygunBosluk == default)
                            {
                                // İlk boşluğu öner
                                var ilkBosluk = bosluklar.First();
                                return Json(new
                                {
                                    success = false,
                                    message = $"Girdiğiniz aralık mevcut boşluklara uymuyor. İlk boşluk: {ilkBosluk.Baslangic}-{ilkBosluk.Bitis}"
                                });
                            }
                        }
                        else
                        {
                            // Boşluk yoksa, son aralıktan sonraki değeri kontrol et
                            FiyatOran sonOran = mevcutOranlar.Last();
                            if (min != sonOran.Max + 1)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Yeni aralık {sonOran.Max + 1}'den başlamalıdır. Şu anki son aralık: {sonOran.Min}-{sonOran.Max}"
                                });
                            }
                        }
                    }
                }

                // Ekleme işlemi
                FiyatOran yeniFiyatOran = new FiyatOran
                {
                    PaketId = paketId,
                    Min = oranUygula ? min : 0,
                    Max = oranUygula ? max : 0,
                    OranYuzde = oranYuzde,
                    Oran = oranUygula,
                    Durumu = 1,
                    EkleyenKullaniciId = kullanici.Id,
                    GuncelleyenKullaniciId = kullanici.Id,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };

                _fiyatOranRepository.Ekle(yeniFiyatOran);
                return Json(new { success = true, message = "Fiyat oranı başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult FiyatOranlariGetir(int paketId)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null) return Unauthorized();

                List<FiyatOran> fiyatOranlari = _fiyatOranRepository.GetirList(x => x.PaketId == paketId && x.Durumu == 1)
                    .OrderBy(x => x.Oran) // Önce sabit çarpımlar
                    .ThenBy(x => x.Min)
                    .ToList();

                var result = fiyatOranlari.Select(x => new
                {
                    id = x.Id,
                    min = x.Min,
                    max = x.Max,
                    oranYuzde = x.OranYuzde,
                    aralik = $"{x.Min} - {x.Max}",
                    oran = $"%{x.OranYuzde}"
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult FiyatOraniSil(int id)
        {
            LoadCommonData();

            try
            {
                Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");
                if (kullanici == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                FiyatOran fiyatOran = _fiyatOranRepository.Getir(id);
                if (fiyatOran == null || fiyatOran.Durumu == 0)
                {
                    return Json(new { success = false, message = "Fiyat oranı bulunamadı." });
                }

                fiyatOran.Durumu = 0;
                fiyatOran.GuncelleyenKullaniciId = kullanici.Id;
                fiyatOran.GuncellenmeTarihi = DateTime.Now;
                _fiyatOranRepository.Guncelle(fiyatOran);

                return Json(new { success = true, message = "Fiyat oranı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }
    }
}