using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using WebApp.Models;
using System.IO;
using System.Collections.Generic;
using WebApp.Repositories;

namespace WepApp.Controllers
{
    public class AdminIstekOneriController : AdminBaseController
    {
        private readonly IstekOneriRepository _istekOneriRepository = new IstekOneriRepository();
        private readonly IstekOneriDurumRepository _istekOneriDurumRepository = new IstekOneriDurumRepository();
        private readonly BayiRepository _bayiRepository = new BayiRepository();
        private readonly KullanicilarRepository _kullaniciRepository = new KullanicilarRepository();
        private readonly MusteriRepository _musteriRepository = new MusteriRepository();

        public IActionResult Index()
        {
            List<string> join = new List<string>();
            join.Add("LisansTip");
            join.Add("Musteri");
            join.Add("Bayi");
            join.Add("IstekOneriDurum");

            // Temel sorgu - sadece aktif kayıtlar
            var query = _istekOneriRepository.GetirQueryable(x => x.Durumu == 1, join);

            // Kullanıcı bilgilerini al
            var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();

            // Kullanıcı tipine göre filtreleme yap
            if (kullaniciTipi == "Musteri")
            {
                // Müşteri: Sadece kendi kayıtları
                query = query.Where(x => x.MusteriId == kullaniciId);
            }
            else if (kullaniciTipi == "Bayi")
            {
                var altBayiIdleri = GetAllSubBayiIds(kullaniciId ?? 0);
                var tumBayiIdleri = new List<int> { kullaniciId ?? 0 };
                tumBayiIdleri.AddRange(altBayiIdleri);

                // Repository'deki yeni metodu kullan
                var musteriIdleri = _musteriRepository.GetMusteriIdleriByBayiIdleri(tumBayiIdleri);

                query = query.Where(x =>
                    (x.BayiId.HasValue && tumBayiIdleri.Contains(x.BayiId.Value)) ||
                    (x.MusteriId.HasValue && musteriIdleri.Contains(x.MusteriId.Value))
                );

                Bayi bayi = _bayiRepository.Getir(x => x.Id == kullaniciId && x.Durumu == 1);
                ViewBag.BayiInfo = bayi;
            }
            // Kullanıcı (Admin) için filtreleme yapma - tüm kayıtlar gelsin

            // Listeyi sırala ve çek
            List<IstekOneriler> liste = query
                .OrderByDescending(x => x.EklenmeTarihi)
                .ToList();

            // Durum listesini al (Id=1 olan hariç)
            List<IstekOneriDurum> durumListesi = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1 && x.Id != 1)
                .OrderBy(x => x.Sira)
                .ToList();

            ViewBag.DurumListesi = durumListesi;
            ViewBag.KullaniciTipi = kullaniciTipi;
            ViewBag.KullaniciId = kullaniciId;
            ViewBag.IstekOneriListesi = liste;

            return View();
        }

        // Alt bayileri recursive olarak bulan yardımcı metod
        private List<int> GetAllSubBayiIds(int bayiId)
        {
            var altBayiIdleri = new List<int>();

            // Bu bayinin direkt alt bayilerini bul
            var altBayiler = _bayiRepository.GetirList(x => x.UstBayiId == bayiId && x.Durumu == 1);

            foreach (var altBayi in altBayiler)
            {
                altBayiIdleri.Add(altBayi.Id);
                // Recursive olarak alt bayilerin alt bayilerini bul
                altBayiIdleri.AddRange(GetAllSubBayiIds(altBayi.Id));
            }

            return altBayiIdleri;
        }

        [HttpPost]
        public IActionResult CevapVer(int id, string aciklama)
        {
            

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();

                if (string.IsNullOrEmpty(kullaniciTipi) || kullaniciId == null)
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                var mevcut = _istekOneriRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı veya pasif." });
                }

                // --- Cevap güncelleme alanı ---
                mevcut.DistributorCevap = aciklama?.Trim();
                mevcut.DistributorCevapVerdiMi = true;
                mevcut.DistributorCevapTarihi = DateTime.Now;
                mevcut.DistributorBayiId = null;   // aşağıda doğru yere yazılacak
                mevcut.GuncelleyenKullaniciId = kullaniciId.Value;
                mevcut.GuncellenmeTarihi = DateTime.Now;
                mevcut.IstekOneriDurumId = 1;      // → bu değerin ne anlama geldiğini sabit yerine enum/tanımlı sabit yapmanızı öneririm

                // Kullanıcı tipine göre özel alanları doldur
                string cevapVerenAdi = null;

                if (kullaniciTipi == "Admin")
                {
                    var admin = _kullaniciRepository.Getir(x => x.Id == kullaniciId);
                    if (admin == null)
                    {
                        return Json(new { success = false, message = "Admin kullanıcısı bulunamadı." });
                    }

                    mevcut.AdminCevapVerdiMi = true;
                    mevcut.AdminCevapTarihi = DateTime.Now;
                    mevcut.AdminKullaniciId = kullaniciId;

                    cevapVerenAdi = admin.Adi;
                }
                else if (kullaniciTipi == "Bayi")
                {
                    var bayi = _bayiRepository.Getir(x => x.Id == kullaniciId);
                    if (bayi == null)
                    {
                        return Json(new { success = false, message = "Bayi kaydı bulunamadı." });
                    }

                    cevapVerenAdi = bayi.Unvan;

                    // Distributor yetkisi varsa
                    if (bayi.Distributor == true)
                    {
                        mevcut.DistributorCevapVerdiMi = true;
                        mevcut.DistributorCevapTarihi = DateTime.Now;
                        mevcut.DistributorBayiId = kullaniciId;
                    }
                    // else → normal bayi ise sadece genel cevap alanı güncellenir (DistributorCevap)
                }
                else
                {
                    // Beklenmeyen kullanıcı tipi
                    return Json(new { success = false, message = "Geçersiz kullanıcı tipi." });
                }

                _istekOneriRepository.Guncelle(mevcut);

                // Dönüş objesi
                return Json(new
                {
                    success = true,
                    message = "Cevabınız başarıyla kaydedildi.",
                    distributorCevap = aciklama,
                    cevapVerdiMi = true,
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    cevapVerenTip = kullaniciTipi,
                    cevapVerenAdi = cevapVerenAdi
                });
            }
            catch (Exception ex)
            {
                // Gerçek projede logger kullanın → _logger.LogError(ex, "CevapVer hatası - ID: {Id}", id);
                return Json(new
                {
                    success = false,
                    message = "İşlem sırasında beklenmeyen bir hata oluştu."
                    // production'da ex.Message dönmeyin!
                });
            }
        }

        [HttpPost]
        public IActionResult CevapGuncelle(int id, string aciklama)
        {
    

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler mevcut = _istekOneriRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Cevap güncelleme
                mevcut.DistributorCevap = aciklama;
                mevcut.DistributorCevapTarihi = DateTime.Now;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _istekOneriRepository.Guncelle(mevcut);

                return Json(new
                {
                    success = true,
                    message = "Cevabınız başarıyla güncellendi.",
                    distributorCevap = aciklama,
                    cevapTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem sırasında hata oluştu: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult GetirCevap(int id)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler kayit = _istekOneriRepository.Getir(id);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                // Kullanıcının bu cevabı düzenleyip düzenleyemeyeceğini kontrol et
                bool duzenleyebilir = false;
                if (kullaniciTipi == "Admin" && kayit.AdminCevapVerdiMi)
                {
                    duzenleyebilir = true; // Admin tüm admin cevaplarını düzenleyebilir
                }
                else if (kullaniciTipi == "Bayi" && kayit.DistributorCevapVerdiMi && kayit.DistributorBayiId == kullaniciId)
                {
                    duzenleyebilir = true; // Bayi sadece kendi cevaplarını düzenleyebilir
                }

                return Json(new
                {
                    success = true,
                    distributorCevap = kayit.DistributorCevap ?? "",
                    distributorCevapVerdiMi = kayit.DistributorCevapVerdiMi,
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    adminCevapVerdiMi = kayit.AdminCevapVerdiMi,
                    adminCevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    duzenleyebilir = duzenleyebilir
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Sil(int Id)
        {
           

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi) || kullaniciTipi != "Admin")
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler mevcut = _istekOneriRepository.Getir(Id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }

                // Soft delete
                mevcut.Durumu = 0;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _istekOneriRepository.Guncelle(mevcut);

                return Json(new { success = true, message = "Kayıt başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Silme işlemi sırasında hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult DurumGuncelle(int id, int durumId)
        {
           

            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi) || (kullaniciTipi != "Admin" && kullaniciTipi != "Bayi"))
                {
                    return Json(new { success = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
                }

                IstekOneriler mevcut = _istekOneriRepository.Getir(id);
                if (mevcut == null || mevcut.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }


                // Durum güncelleme
                mevcut.IstekOneriDurumId = durumId;
                mevcut.GuncelleyenKullaniciId = kullaniciId ?? 0;
                mevcut.GuncellenmeTarihi = DateTime.Now;

                _istekOneriRepository.Guncelle(mevcut);

                // Yeni durum adını getir
                IstekOneriDurum yeniDurum = _istekOneriDurumRepository.Getir(durumId);
                string durumAdi = yeniDurum?.Adi ?? "Belirtilmemiş";

                return Json(new
                {
                    success = true,
                    message = "Durum başarıyla güncellendi.",
                    durumAdi = durumAdi
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Durum güncelleme sırasında hata oluştu: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult DetayGetir(int id)
        {
            try
            {
                var (kullaniciTipi, kullaniciId) = GetCurrentUserInfo();
                if (string.IsNullOrEmpty(kullaniciTipi))
                {
                    return Json(new { success = false, message = "Yetkiniz yok" });
                }

                List<string> join = new List<string>();
                join.Add("LisansTip");
                join.Add("Musteri");
                join.Add("Bayi");
                join.Add("IstekOneriDurum");

                IstekOneriler kayit = _istekOneriRepository.Getir(x => x.Id == id, join);
                if (kayit == null || kayit.Durumu == 0)
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }

                // Cevap veren bilgilerini al
                string cevapVerenTip = "";
                string cevapVerenAdi = "";
                if (kayit.AdminCevapVerdiMi)
                {
                    cevapVerenTip = "Admin";
                    var admin = _kullaniciRepository.Getir(x => x.Id == kayit.AdminKullaniciId);
                    cevapVerenAdi = admin?.Adi;
                }
                else if (kayit.DistributorCevapVerdiMi)
                {
                    cevapVerenTip = "Bayi";
                    var bayi = _bayiRepository.Getir(x => x.Id == kayit.DistributorBayiId);
                    cevapVerenAdi = bayi?.Unvan;
                }

                // Müşteri adını al - Değişiklik burada
                string musteriAdi = "Belirtilmemiş";
                if (kayit.Musteri != null)
                {
                    musteriAdi = kayit.Musteri.AdSoyad ?? kayit.Musteri.TicariUnvan ?? "Belirtilmemiş";
                }

                return Json(new
                {
                    success = true,
                    id = kayit.Id,
                    konu = kayit.Konu,
                    metni = kayit.Metni,
                    musteriAdi = musteriAdi,  // Değişti
                    bayiAdi = kayit.Bayi?.Unvan ?? "Belirtilmemiş",
                    lisansTipAdi = kayit.LisansTip?.Adi ?? "Belirtilmemiş",
                    istekDurumId = kayit.IstekOneriDurumId,
                    istekDurumAdi = kayit.IstekOneriDurum?.Adi ?? "Belirtilmemiş",
                    distributorCevap = kayit.DistributorCevap ?? "",
                    distributorCevapTarihi = kayit.DistributorCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    adminCevapTarihi = kayit.AdminCevapTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    cevapVerenTip = cevapVerenTip,
                    cevapVerenAdi = cevapVerenAdi,
                    eklenmeTarihi = kayit.EklenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    guncellenmeTarihi = kayit.GuncellenmeTarihi.ToString("dd.MM.yyyy HH:mm"),
                    cevapDuzenleyebilir = (kullaniciTipi == "Admin") ||
                                         (kullaniciTipi == "Bayi" && kayit.BayiId == kullaniciId && !kayit.DistributorCevapVerdiMi && !kayit.AdminCevapVerdiMi),
                    durumDuzenleyebilir = (kullaniciTipi == "Admin") || (kullaniciTipi == "Bayi" && kayit.BayiId == kullaniciId),
                    silebilir = (kullaniciTipi == "Admin")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult GetirDurumlar()
        {
       

            try
            {
                var durumlar = _istekOneriDurumRepository.GetirList(x => x.Durumu == 1)
                    .OrderBy(x => x.Sira)
                    .Select(d => new {
                        id = d.Id,
                        adi = d.Adi,
                        sira = d.Sira
                    })
                    .ToList();

                return Json(new { success = true, durumlar = durumlar });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }
}