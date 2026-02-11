using Microsoft.AspNetCore.Mvc;
using WepApp.Models;
using WepApp.Repositories;
using AspNetCore.Reporting;
using WebApp.Models;
using System.Text;
using System.Globalization;

namespace WepApp.Controllers
{
    public class AdminTeklifVerController : AdminBaseController
    {
        private readonly IWebHostEnvironment _webHostEnviroment;
        private readonly PaketGrupRepository _paketGrupRepo = new PaketGrupRepository();
        private readonly PaketRepository _paketRepo = new PaketRepository();
        private readonly PaketGrupDetayRepository _paketGrupDetayRepo = new PaketGrupDetayRepository();
        private readonly PaketGrupDetayRepository _detayRepo = new PaketGrupDetayRepository();
        private readonly MusteriRepository _musteriRepo = new MusteriRepository();
        private readonly LisansTipRepository _lisansTipRepo = new LisansTipRepository();
        private readonly TeklifRepository _teklifRepo = new TeklifRepository();
        private readonly TeklifDetayRepository _teklifDetayRepo = new TeklifDetayRepository();
        private readonly KampanyaRepository _kampanyaRepo = new KampanyaRepository();
        private readonly SayacRepository _sayacRepo = new SayacRepository();
        private readonly FiyatOranRepository _fiyatOranRepo = new FiyatOranRepository();
        private readonly MusteriTipiRepository _musteriTipiRepository = new MusteriTipiRepository();
        private readonly MusteriDurumuRepository _musteriDurumuRepository = new MusteriDurumuRepository();
        private readonly BayiRepository _bayiRepository = new BayiRepository();
        private readonly PaketBaglantiRepository _paketBaglantiRepo = new PaketBaglantiRepository();

        public AdminTeklifVerController(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnviroment = webHostEnvironment;
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public IActionResult Index(int teklifId)
        {
            Teklif mevcutTeklif = null;
            if (teklifId > 0)
            {
                mevcutTeklif = _teklifRepo.Getir(x => x.Id == teklifId,
                    new List<string> { "Musteri", "LisansTip", "Detaylar" });

                if (mevcutTeklif != null)
                {
                    // Teklif numarasını güncelle (-1, -2 ekle)
                    string yeniTeklifNo = TeklifNumarasiniGuncelle(mevcutTeklif.TeklifNo);

                    // Mevcut teklif numarasını da ViewBag'e ekleyin
                    ViewBag.MevcutTeklifNo = mevcutTeklif.TeklifNo; // YENİ SATIR

                    ViewBag.MevcutTeklif = mevcutTeklif;
                    ViewBag.YeniTeklifNo = yeniTeklifNo;
                    ViewBag.TeklifId = teklifId;

                    // Geçerlilik tarihini mevcut tekliften al veya 30 gün ekle
                    if (mevcutTeklif.GecerlilikTarihi.HasValue)
                    {
                        ViewBag.GecerlilikTarihi = mevcutTeklif.GecerlilikTarihi.Value.ToString("yyyy-MM-dd");
                    }
                }
            }

            Bayi currentBayi = SessionHelper.GetObjectFromJson<Bayi>(HttpContext.Session, "Bayi");
            List<Bayi> bayiList;
            if (currentBayi != null)
                bayiList = _bayiRepository.GetBayiVeAltBayiler(currentBayi.Id) ?? new List<Bayi>();
            else
                bayiList = _bayiRepository.GetirList(x => x.Durumu == 1)?.ToList() ?? new List<Bayi>();

            foreach (Bayi b in bayiList)
                b.Seviye = b.Seviye ?? 0;

            ViewBag.TumBayiler = bayiList;
            ViewBag.MusteriDurumu = _musteriDurumuRepository.Listele() ?? new List<MusteriDurumu>();
            ViewBag.MusteriTipleri = _musteriTipiRepository.GetirList(x => x.Durumu == 1)?.OrderBy(x => x.Adi).ToList() ?? new List<MusteriTipi>();

            LoadCommonData();
            ViewBag.Musteriler = _musteriRepo.GetirList(x => x.Durum == 1)
                .OrderBy(x => x.Ad).ThenBy(x => x.Soyad).ToList();
            ViewBag.LisansTipleri = _lisansTipRepo.GetirList(x => x.Durumu == 1)
                .OrderBy(x => x.Sayi).ToList();

            LisansTip ilkLisans = _lisansTipRepo
                .GetirList(x => x.Durumu == 1)
                ?.OrderBy(x => x.Sayi)
                .FirstOrDefault();
            int ilk = ilkLisans?.Id ?? 0;
            ViewBag.IlkLisansTipId = ilk;

            KDVRepository kDVRepository = new KDVRepository();
            KDV kdv = kDVRepository.Getir(x => x.Durumu == 1);
            ViewBag.KDV = kdv.Oran;

            return View();
        }
        private string TeklifNumarasiniGuncelle(string mevcutTeklifNo)
        {
            if (string.IsNullOrEmpty(mevcutTeklifNo))
                return YeniTeklifNumarasiOlustur();

            int lastDashIndex = mevcutTeklifNo.LastIndexOf('-');

            // Eğer hiç '-' yoksa: 29 -> 29-1
            if (lastDashIndex == -1)
            {
                return mevcutTeklifNo + "-1";
            }

            string lastPart = mevcutTeklifNo.Substring(lastDashIndex + 1);

            // Son parça sayı mı bakalım
            if (int.TryParse(lastPart, out int sonEk))
            {
                // Eğer son parçanın uzunluğu küçükse (kısa ek: 1,2,10 vs.)
                // o zaman ek kabul edip arttır.
                // Uzunsa (ör. "0029", "202412") bunu ana numara kabul edip "-1" ekle.
                if (lastPart.Length <= 3)
                {
                    string anaNumara = mevcutTeklifNo.Substring(0, lastDashIndex);
                    return anaNumara + "-" + (sonEk + 1);
                }
                else
                {
                    // Uzun sayılar base sayı olabilir -> yeni ek ekle
                    return mevcutTeklifNo + "-1";
                }
            }
            else
            {
                // Son kısım sayı değilse, ek ekle
                return mevcutTeklifNo + "-1";
            }
        }


        // Controller'a eklenmesi gereken yeni action
        [HttpGet]
        public IActionResult GetirTeklifDetaylari(int teklifId)
        {
            try
            {
                Teklif teklif = _teklifRepo.Getir(x => x.Id == teklifId,
                    new List<string> { "Detaylar"});

                if (teklif == null)
                    return Json(new { success = false, message = "Teklif bulunamadı." });

                var detaylar = teklif.Detaylar
                    .Where(d => d.Durumu == 1)
                    .OrderBy(d => d.SiraNo)
                    .Select(d => new
                    {
                        id = d.PaketId ?? d.PaketGrupId ?? 0,
                        tip = d.Tip,
                        paketMi = d.BagimsizModulMu == false,
                        parentId = d.PaketGrupId ?? 0,
                        adi = d.ItemAdi,
                        indirimYuzdesi = d.BireyselIndirimYuzdesi,
                        miktar = d.Miktar,
                        fiyatOrani = d.MiktarBazliEkOranYuzde
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    detaylar = detaylar,
                    grupIndirimOrani = teklif.GrupIndirimOrani
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private string YeniTeklifNumarasiOlustur()
        {
            Sayac sayac = _sayacRepo.Getir(x => x.Ad == "TeklifNo");

            if (sayac == null)
            {
                sayac = new Sayac
                {
                    Ad = "TeklifNo",
                    SonDeger = 1,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now
                };
                _sayacRepo.Ekle(sayac);
            }
            else
            {
                sayac.SonDeger++;
                sayac.GuncellenmeTarihi = DateTime.Now;
                _sayacRepo.Guncelle(sayac);
            }

            return $"TK-{DateTime.Now:yyyyMM}-{sayac.SonDeger.ToString().PadLeft(4, '0')}";
        }

        [HttpGet]
        public IActionResult GetirYeniTeklifNo()
        {
            try
            {
                string teklifNo = YeniTeklifNumarasiOlustur();
                return Json(new { success = true, teklifNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirGruplarVePaketler(int lisansTipId)
        {
            try
            {
                List<Kampanya> aktifKampanyalar = _kampanyaRepo.GetirList(
                    x => x.Durumu == 1 &&
                         x.BaslangicTarihi <= DateTime.Now &&
                         x.BitisTarihi >= DateTime.Now,
                    new List<string>
                    {
                "KampanyaPaketler",
                "KampanyaPaketler.Paket",
                "KampanyaPaketler.PaketGrup"
                    })
                    .ToList();

                Dictionary<int, Kampanya> kampanyaDict = new Dictionary<int, Kampanya>();
                foreach (Kampanya k in aktifKampanyalar)
                {
                    foreach (KampanyaPaket kp in k.KampanyaPaketler.Where(x => x.Durumu == 1))
                    {
                        if (kp.PaketId > 0 && !kampanyaDict.ContainsKey(kp.PaketId))
                            kampanyaDict.Add(kp.PaketId, k);
                        if (kp.PaketGrupId > 0 && !kampanyaDict.ContainsKey(-kp.PaketGrupId))
                            kampanyaDict.Add(-kp.PaketGrupId, k);
                    }
                }

                List<object> tumVeriler = new List<object>();

                // 1. Paket Gruplarını Getir - ID'leri benzersiz yap
                var gruplar = _paketGrupRepo.GetirList(
                    x => x.Durumu == 1 && x.LisansTipId == lisansTipId,
                    new List<string> { "LisansTip" })
                    .OrderBy(x => x.Sira)
                    .ThenBy(x => x.Adi)
                    .Select(g => new
                    {
                        id = g.Id,
                        originalId = g.Id,
                        adi = g.Adi,
                        lisansTip = g.LisansTip?.Adi ?? "-",
                        birim = "-",
                        fiyat = g.Fiyat,
                        KFiyat = g.KFiyat,
                        indOran = g.IndOran,
                        tip = "grup",
                        parentId = (string?)null,
                        grupId = 0,
                        modulKodu = "",
                        sira = g.Sira,
                        // EĞİTİM SÜRESİ EKLENDİ - DÜZGÜN FORMATTA
                        egitimSuresi = g.EgitimSuresi.HasValue ?
                            (g.EgitimSuresi.Value % 1 == 0 ?
                             g.EgitimSuresi.Value.ToString("0") :
                             g.EgitimSuresi.Value.ToString("0.0")) :
                            "0",
                        kampanyaVar = kampanyaDict.ContainsKey(-g.Id),
                        kampanyaYuzde = kampanyaDict.ContainsKey(-g.Id) ? kampanyaDict[-g.Id].IndirimYuzdesi : 0,
                        kampanyaBaslik = kampanyaDict.ContainsKey(-g.Id) ? kampanyaDict[-g.Id].Baslik : ""
                    })
                    .ToList();

                // Gruplar için ID'leri offset ile benzersiz yap (örn: 10000+)
                int grupOffset = 10000;
                foreach (var grup in gruplar)
                {
                    var newGrup = new
                    {
                        id = grup.originalId + grupOffset, // Benzersiz ID
                        originalId = grup.originalId,
                        adi = grup.adi,
                        lisansTip = grup.lisansTip,
                        birim = grup.birim,
                        fiyat = grup.fiyat,
                        KFiyat = grup.KFiyat,
                        indOran = grup.indOran,
                        tip = "grup",
                        parentId = (string?)null,
                        grupId = 0,
                        modulKodu = grup.modulKodu,
                        sira = grup.sira,
                        // EĞİTİM SÜRESİ EKLENDİ
                        egitimSuresi = grup.egitimSuresi,
                        kampanyaVar = grup.kampanyaVar,
                        kampanyaYuzde = grup.kampanyaYuzde,
                        kampanyaBaslik = grup.kampanyaBaslik
                    };
                    tumVeriler.Add(newGrup);
                }

                // 2. GRUP İÇİNDEKİ PAKETLERİ - HER GRUPTA GÖRÜNDÜĞÜ KADAR TEKRARLI GETİR
                var grupPaketleriTekrarli = _detayRepo.GetirList(
                        x => x.Durumu == 1 &&
                             x.Paket != null && x.Paket.Durumu == 1 &&
                             x.PaketGrup != null && x.PaketGrup.LisansTipId == lisansTipId,
                        new List<string> { "Paket", "PaketGrup", "Paket.LisansTip", "Paket.Birim" })
                    .OrderBy(x => x.PaketGrup.Sira)
                    .ThenBy(x => x.Paket.Sira)
                    .ThenBy(x => x.Paket.Adi)
                    .Select(x => new
                    {
                        id = x.Paket.Id,
                        originalId = x.Paket.Id,
                        adi = x.Paket.Adi,
                        lisansTip = x.Paket.LisansTip?.Adi ?? "-",
                        birim = x.Paket.Birim?.Adi ?? "-",
                        fiyat = x.Paket.Fiyat ?? 0m,
                        KFiyat = x.Paket.KFiyat ?? 0m,
                        indOran = x.Paket.IndOran ?? 0,
                        tip = "paket",
                        parentId = (x.PaketGrupId + grupOffset).ToString(), // Offset eklenmiş grup ID
                        grupId = x.PaketGrupId,
                        originalParentId = x.PaketGrupId, // Original grup ID'yi de sakla
                        modulKodu = x.Paket.ModulKodu ?? "",
                        sira = x.PaketGrup.Sira * 1000 + (x.Paket.Sira ?? 0),
                        // EĞİTİM SÜRESİ EKLENDİ
                        egitimSuresi = x.Paket.EgitimSuresi.HasValue ?
                            (x.Paket.EgitimSuresi.Value % 1 == 0 ?
                             x.Paket.EgitimSuresi.Value.ToString("0") :
                             x.Paket.EgitimSuresi.Value.ToString("0.0")) :
                            "0",
                        kampanyaVar = kampanyaDict.ContainsKey(x.Paket.Id),
                        miktarKademesiVar = _fiyatOranRepo.GetirList(fo => fo.PaketId == x.Paket.Id && fo.Durumu == 1).Any(),
                        kampanyaYuzde = kampanyaDict.ContainsKey(x.Paket.Id) ? kampanyaDict[x.Paket.Id].IndirimYuzdesi : 0,
                        kampanyaBaslik = kampanyaDict.ContainsKey(x.Paket.Id) ? kampanyaDict[x.Paket.Id].Baslik : ""
                    })
                    .ToList();

                tumVeriler.AddRange(grupPaketleriTekrarli);

                // 3. Bağımsız Paketler
                var bagimsizPaketler = _paketRepo.GetirList(
                    x => x.Durumu == 1 && x.LisansTipId == lisansTipId,
                    new List<string> { "LisansTip", "Birim" })
                    .Where(x => !_detayRepo.GetirList(d => d.PaketId == x.Id && d.Durumu == 1).Any())
                    .OrderBy(x => x.Sira)
                    .ThenBy(x => x.Adi)
                    .Select(x => new
                    {
                        id = x.Id,
                        originalId = x.Id,
                        adi = x.Adi,
                        lisansTip = x.LisansTip?.Adi ?? "-",
                        birim = x.Birim?.Adi ?? "-",
                        fiyat = x.Fiyat ?? 0m,
                        KFiyat = x.KFiyat ?? 0m,
                        indOran = x.IndOran ?? 0,
                        tip = "paket",
                        parentId = (string?)null,
                        grupId = 0,
                        originalParentId = 0,
                        modulKodu = x.ModulKodu ?? "",
                        sira = 100000 + (x.Sira ?? 0),
                        // EĞİTİM SÜRESİ EKLENDİ
                        egitimSuresi = x.EgitimSuresi.HasValue ?
                            (x.EgitimSuresi.Value % 1 == 0 ?
                             x.EgitimSuresi.Value.ToString("0") :
                             x.EgitimSuresi.Value.ToString("0.0")) :
                            "0",
                        miktarKademesiVar = _fiyatOranRepo.GetirList(fo => fo.PaketId == x.Id && fo.Durumu == 1).Any(),
                        kampanyaVar = kampanyaDict.ContainsKey(x.Id),
                        kampanyaYuzde = kampanyaDict.ContainsKey(x.Id) ? kampanyaDict[x.Id].IndirimYuzdesi : 0,
                        kampanyaBaslik = kampanyaDict.ContainsKey(x.Id) ? kampanyaDict[x.Id].Baslik : ""
                    })
                    .ToList();

                tumVeriler.AddRange(bagimsizPaketler);

                // DEBUG: Konsola verileri yazdır
                Console.WriteLine("=== DEBUG: GetirGruplarVePaketler ===");
                foreach (var grup in gruplar)
                {
                    Console.WriteLine($"Grup: {grup.adi}, Eğitim Süresi: {grup.egitimSuresi}");
                }

                return Json(new { success = true, veriler = tumVeriler });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult HesaplaTeklif([FromBody] TeklifHesaplaModel model)
        {
            try
            {
                if (model?.SeciliItemler == null || !model.SeciliItemler.Any())
                    return Json(new { success = false, message = "Hiçbir öğe seçilmedi." });

                // Bağlı paketleri kontrol et ve ekle
                var tumSeciliItemler = new List<SeciliItem>();
                var islenmisItemler = new HashSet<int>(); // ID'leri takip et

                foreach (var item in model.SeciliItemler)
                {
                    // Öğeyi ekle
                    if (!islenmisItemler.Contains(item.Id))
                    {
                        tumSeciliItemler.Add(item);
                        islenmisItemler.Add(item.Id);
                    }

                    // Eğer paket tipindeyse, bağlı paketleri kontrol et
                 
                        var bagliPaketler = _paketBaglantiRepo.GetBagliPaketler(item.Id);

                        foreach (var bagliPaket in bagliPaketler.Where(p => p.Durumu == 1))
                        {
                            if (!islenmisItemler.Contains(bagliPaket.Id))
                            {
                                // Bağlı paketi de seçili listeye ekle
                                tumSeciliItemler.Add(new SeciliItem
                                {
                                    Id = bagliPaket.Id,
                                    Tip = "modul",
                                    Adi = bagliPaket.Adi,
                                    IndirimYuzdesi = item.IndirimYuzdesi, // Ana paketin indirimini miras al
                                    Miktar = item.Miktar,
                                    GrupId = item.GrupId,
                                    PaketMi = item.PaketMi // Bağlı paketler de modül olarak kabul edilir
                                });

                                islenmisItemler.Add(bagliPaket.Id);
                            }
                        
                    }
                }

                // Eski hesaplama işlemlerini yeni liste ile devam ettir
                decimal araToplam = 0m;
                decimal toplamIndirim = 0m;
                decimal listeFiyatiToplam = 0m;

                List<Kampanya> aktifKampanyalar = _kampanyaRepo.GetirList(
                    x => x.Durumu == 1 && x.BaslangicTarihi <= DateTime.Now && x.BitisTarihi >= DateTime.Now,
                    new List<string> { "KampanyaPaketler" }).ToList();

                Dictionary<int, decimal> kampanyaYuzdeDict = new Dictionary<int, decimal>();
                Dictionary<int, string> kampanyaBaslikDict = new Dictionary<int, string>();

                foreach (Kampanya k in aktifKampanyalar)
                {
                    foreach (KampanyaPaket kp in k.KampanyaPaketler.Where(x => x.Durumu == 1))
                    {
                        if (kp.PaketId > 0)
                        {
                            if (!kampanyaYuzdeDict.ContainsKey(kp.PaketId))
                            {
                                kampanyaYuzdeDict.Add(kp.PaketId, k.IndirimYuzdesi);
                                kampanyaBaslikDict.Add(kp.PaketId, k.Baslik);
                            }
                        }
                        else if (kp.PaketGrupId > 0)
                        {
                            if (!kampanyaYuzdeDict.ContainsKey(-kp.PaketGrupId))
                            {
                                kampanyaYuzdeDict.Add(-kp.PaketGrupId, k.IndirimYuzdesi);
                                kampanyaBaslikDict.Add(-kp.PaketGrupId, k.Baslik);
                            }
                        }
                    }
                }

                List<object> hesaplanmisItemler = new List<object>();

                // BU KISIMDA tumSeciliItemler KULLANILACAK
                foreach (SeciliItem item in tumSeciliItemler)
                {
                    // Geri kalan hesaplama kodu aynı kalacak...
                    // Sadece tumSeciliItemler kullanıldığından emin olun
                    if (item.Tip == "grup")
                    {
                        // OFFSET'İ TEMİZLE
                        int temizId = item.Id;
                        if (item.Id > 10000)
                        {
                            temizId = item.Id - 10000;
                        }

                        PaketGrup grup = _paketGrupRepo.Getir(x => x.Id == temizId && x.Durumu == 1);
                        if (grup == null) continue;

                        decimal grupFiyat = grup.Fiyat;
                        decimal grupListeFiyati = grup.KFiyat;

                        bool grupKampanyaVar = kampanyaYuzdeDict.ContainsKey(-temizId);
                        decimal grupKampanyaYuzde = grupKampanyaVar ? kampanyaYuzdeDict[-temizId] : 0m;
                        string grupKampanyaBaslik = grupKampanyaVar ? kampanyaBaslikDict[-temizId] : "";

                        decimal indirimliGrupFiyat = grupFiyat;
                        decimal toplamGrupIndirim = 0m;

                        if (item.IndirimYuzdesi > 0)
                        {
                            decimal manuelIndirim = grupFiyat * item.IndirimYuzdesi / 100m;
                            indirimliGrupFiyat -= manuelIndirim;
                            toplamGrupIndirim += manuelIndirim;
                        }

                        if (grupKampanyaYuzde > 0)
                        {
                            decimal kampanyaIndirim = indirimliGrupFiyat * grupKampanyaYuzde / 100m;
                            indirimliGrupFiyat -= kampanyaIndirim;
                            toplamGrupIndirim += kampanyaIndirim;
                        }

                        araToplam += indirimliGrupFiyat;
                        toplamIndirim += toplamGrupIndirim;
                        listeFiyatiToplam += grupListeFiyati;

                        hesaplanmisItemler.Add(new
                        {
                            id = item.Id,
                            tip = item.Tip,
                            adi = grup.Adi,
                            birimFiyat = grupFiyat,
                            listeFiyati = grupListeFiyati,
                            indirimliFiyat = indirimliGrupFiyat,
                            toplamTutar = indirimliGrupFiyat,
                            kampanyaVar = grupKampanyaVar,
                            kampanyaYuzde = grupKampanyaYuzde,
                            kampanyaBaslik = grupKampanyaBaslik,
                            manuelIndirimYuzde = item.IndirimYuzdesi,
                            toplamIndirimTutar = toplamGrupIndirim,
                            paketMi = true,
                            otomatikEklendi = false // Kullanıcının seçtiği
                        });
                    }
                    else if (item.Tip == "paket")
                    {
                        Paket paket = _paketRepo.Getir(x => x.Id == item.Id && x.Durumu == 1);
                        if (paket == null) continue;

                        decimal birimFiyat = paket.Fiyat ?? 0m;
                        decimal listeFiyati = paket.KFiyat ?? birimFiyat;

                        if (item.PaketMi)
                        {
                            birimFiyat = 0m;
                            listeFiyati = 0m;
                        }
                        else
                        {
                            if (item.FiyatOrani != null && item.FiyatOrani != 0)
                            {
                                decimal oran = 1 + (decimal)item.FiyatOrani / 100m;
                                birimFiyat *= oran;
                                listeFiyati *= oran;
                            }
                        }

                        bool paketKampanyaVar = kampanyaYuzdeDict.ContainsKey(item.Id);
                        decimal paketKampanyaYuzde = paketKampanyaVar ? kampanyaYuzdeDict[item.Id] : 0m;
                        string paketKampanyaBaslik = paketKampanyaVar ? kampanyaBaslikDict[item.Id] : "";

                        decimal indirimliFiyat = birimFiyat;
                        decimal toplamItemIndirim = 0m;

                        if (item.IndirimYuzdesi > 0)
                        {
                            decimal manuelIndirim = birimFiyat * item.IndirimYuzdesi / 100m;
                            indirimliFiyat -= manuelIndirim;
                            toplamItemIndirim += manuelIndirim;
                        }

                        if (paketKampanyaYuzde > 0)
                        {
                            decimal kampanyaIndirim = indirimliFiyat * paketKampanyaYuzde / 100m;
                            indirimliFiyat -= kampanyaIndirim;
                            toplamItemIndirim += kampanyaIndirim;
                        }

                        araToplam += indirimliFiyat;
                        toplamIndirim += toplamItemIndirim;
                        listeFiyatiToplam += listeFiyati;

                        hesaplanmisItemler.Add(new
                        {
                            id = item.Id,
                            tip = item.Tip,
                            adi = paket.Adi,
                            birimFiyat = birimFiyat,
                            listeFiyati = listeFiyati,
                            indirimliFiyat = indirimliFiyat,
                            toplamTutar = indirimliFiyat,
                            kampanyaVar = paketKampanyaVar,
                            kampanyaYuzde = paketKampanyaYuzde,
                            kampanyaBaslik = paketKampanyaBaslik,
                            manuelIndirimYuzde = item.IndirimYuzdesi,
                            toplamIndirimTutar = toplamItemIndirim,
                            paketMi = item.PaketMi,
                            otomatikEklendi = !model.SeciliItemler.Any(x => x.Id == item.Id) // Otomatik eklenip eklenmediği
                        });
                    }
                }

                decimal genelGrupIndirimTutar = model.GrupIndirimOrani > 0 && model.GrupIndirimOrani <= 100
                    ? araToplam * model.GrupIndirimOrani / 100m
                    : 0m;

                decimal araTutar = araToplam - genelGrupIndirimTutar;

                decimal miktarEkTutar = 0m;
                if (model.ToplamMiktarOrani > 0)
                {
                    miktarEkTutar = araToplam * (model.ToplamMiktarOrani / 100m);
                    araTutar += miktarEkTutar;
                }

                decimal kdv = araTutar * 0.20m;
                decimal genelToplam = araTutar + kdv;

                toplamIndirim += genelGrupIndirimTutar;

                return Json(new
                {
                    success = true,
                    paketSayisi = tumSeciliItemler.Count(x => x.Tip == "paket"),
                    grupSayisi = tumSeciliItemler.Count(x => x.Tip == "grup"),
                    listeFiyatiToplam = Math.Round(listeFiyatiToplam, 2),
                    araToplam = Math.Round(araToplam, 2),
                    toplamIndirim = Math.Round(toplamIndirim, 2),
                    genelGrupIndirim = Math.Round(genelGrupIndirimTutar, 2),
                    araTutar = Math.Round(araTutar, 2),
                    kdv = Math.Round(kdv, 2),
                    toplamFiyat = Math.Round(genelToplam, 2),
                    miktarEkTutar = Math.Round(miktarEkTutar, 2),
                    hesaplanmisItemler = hesaplanmisItemler,
                    orijinalSecilenler = tumSeciliItemler.Select(x => x.Id).ToList(),
                    tumSecilenler = tumSeciliItemler.Select(x => x.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetirFiyatOrani(int paketId, int miktar)
        {
            try
            {
                FiyatOran fiyatOrani = _fiyatOranRepo.GetirList(
                    x => x.PaketId == paketId && x.Durumu == 1 && miktar >= x.Min && miktar <= x.Max)
                    .OrderBy(x => x.Min)
                    .FirstOrDefault();

                if (fiyatOrani != null)
                {
                    // Eğer Oran = false ise fiyat artışı yok, sadece miktar ile çarpma yapılacak
                    // Bu durumda oranYuzde = 0 dönüyoruz
                    if (fiyatOrani.Oran == false)
                    {
                        return Json(new { success = true, oranYuzde = 0, isOran = false });
                    }
                    else
                    {
                        return Json(new { success = true, oranYuzde = fiyatOrani.OranYuzde, isOran = true });
                    }
                }
                return Json(new { success = false, oranYuzde = 0, isOran = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // AdminTeklifVerController.cs içine ekleyin

        [HttpPost]
        public IActionResult GeciciMusteriKaydet([FromBody] GeciciMusteriModel model)
        {
            try
            {
                // Session'a geçici müşteri kaydet
                var geciciMusteri = new
                {
                    TicariUnvan = model.TicariUnvan ?? "",
                    Ad = model.Ad ?? "",
                    Soyad = model.Soyad ?? "",
                    KullaniciAdi = model.KullaniciAdi ?? "",
                    Sifre = model.Sifre ?? "",
                    Email = model.Email ?? "",
                    Telefon = model.Telefon ?? "",
                    Adres = model.Adres ?? "",
                    Il = model.Il ?? "",
                    Ilce = model.Ilce ?? "",
                    Belde = model.Belde ?? "",
                    Bolge = model.Bolge ?? "",
                    TCVNo = model.TCVNo ?? "",
                    VergiDairesi = model.VergiDairesi ?? "",
                    KepAdresi = model.KepAdresi ?? "",
                    WebAdresi = model.WebAdresi ?? "",
                    Aciklama = model.Aciklama ?? "",
                    AlpemixFirmaAdi = model.AlpemixFirmaAdi ?? "",
                    AlpemixGrupAdi = model.AlpemixGrupAdi ?? "",
                    AlpemixSifre = model.AlpemixSifre ?? "",
                    MusteriTipiId = model.MusteriTipiId,
                    MusteriDurumuId = model.MusteriDurumuId,
                    BayiId = model.BayiId,
                    Diger = model.Diger ?? "",
                    Logo = model.Logo,
                    Imza = model.Imza
                };

                HttpContext.Session.SetString("GeciciMusteri",
                    Newtonsoft.Json.JsonConvert.SerializeObject(geciciMusteri));

                return Json(new
                {
                    success = true,
                    message = "Müşteri bilgileri geçici olarak kaydedildi.",
                    geciciMusteriId = -1, // Geçici ID
                    musteriAdi = $"{model.Ad} {model.Soyad}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetGeciciMusteri()
        {
            try
            {
                string geciciMusteriJson = HttpContext.Session.GetString("GeciciMusteri");
                if (string.IsNullOrEmpty(geciciMusteriJson))
                {
                    return Json(new { success = false, message = "Geçici müşteri bulunamadı." });
                }

                dynamic geciciMusteri = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(geciciMusteriJson);
                return Json(new { success = true, data = geciciMusteri });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Model class'ları Controller'ın altına ekleyin
        public class GeciciMusteriModel
        {
            public string TicariUnvan { get; set; } = "";
            public string Ad { get; set; } = "";
            public string Soyad { get; set; } = "";
            public string KullaniciAdi { get; set; } = "";
            public string Sifre { get; set; } = "";
            public string Email { get; set; } = "";
            public string Telefon { get; set; } = "";
            public string Adres { get; set; } = "";
            public string Il { get; set; } = "";
            public string Ilce { get; set; } = "";
            public string Belde { get; set; } = "";
            public string Bolge { get; set; } = "";
            public string TCVNo { get; set; } = "";
            public string VergiDairesi { get; set; } = "";
            public string KepAdresi { get; set; } = "";
            public string WebAdresi { get; set; } = "";
            public string Aciklama { get; set; } = "";
            public string AlpemixFirmaAdi { get; set; } = "";
            public string AlpemixGrupAdi { get; set; } = "";
            public string AlpemixSifre { get; set; } = "";
            public int? MusteriTipiId { get; set; }
            public int MusteriDurumuId { get; set; }
            public int? BayiId { get; set; }
            public string Diger { get; set; } = "";
            public string Logo { get; set; } = "";
            public string Imza { get; set; } = "";
        }
        // Controller'a ekleyin
        [HttpGet]
        public IActionResult GetirBagliPaketler(int paketId)
        {
            try
            {
                var bagliPaketler = _paketBaglantiRepo.GetBagliPaketler(paketId);



                var sonuc = bagliPaketler
                    .Where(p => p.Durumu == 1)
                    .Select(p => new
                    {
                        id = p.Id,
                        adi = p.Adi,
                        modulKodu = p.ModulKodu ?? "",
                        fiyat = p.Fiyat ?? 0,
                        kfiyat = p.KFiyat ?? 0,
                        egitimSuresi = p.EgitimSuresi,
                        bagliOlduguModulId = paketId // Hangi modüle bağlı olduğunu belirt
                    })
                    .ToList();

                return Json(new { success = true, bagliPaketler = sonuc });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult OlusturTeklif([FromBody] TeklifOlusturModel model, int? Id = null)
        {
            int kullanilacakMusteriId = model.MusteriId;
            LoadCommonData();
            Kullanicilar kullanici = SessionHelper.GetObjectFromJson<Kullanicilar>(HttpContext.Session, "Kullanici");

            try
            {
                if (model == null) return Json(new { success = false, message = "Geçersiz veri." });
                if (model.LisansTipId <= 0) return Json(new { success = false, message = "Lisans tipi seçimi zorunludur." });
                if (model.SeciliItemler == null || !model.SeciliItemler.Any())
                    return Json(new { success = false, message = "En az bir ürün/paket seçmelisiniz." });

                // BAĞLI PAKETLERİ EKLE
                var tumSeciliItemler = new List<SeciliItemModel>();
                var islenmisItemler = new HashSet<int>();

                foreach (var item in model.SeciliItemler)
                {
                    // Öğeyi ekle
                    if (!islenmisItemler.Contains(item.Id))
                    {
                        tumSeciliItemler.Add(item);
                        islenmisItemler.Add(item.Id);
                    }

                    // Eğer paket tipindeyse, bağlı paketleri kontrol et
                    if (item.Tip == "paket")
                    {
                        var bagliPaketler = _paketBaglantiRepo.GetBagliPaketler(item.Id);

                        foreach (var bagliPaket in bagliPaketler.Where(p => p.Durumu == 1))
                        {
                            if (!islenmisItemler.Contains(bagliPaket.Id))
                            {
                                // Bağlı paketin bilgilerini al
                                var bagliPaketModel = new SeciliItemModel
                                {
                                    Id = bagliPaket.Id,
                                    Tip = "paket",
                                    Adi = bagliPaket.Adi,
                                    IndirimYuzdesi = item.IndirimYuzdesi,
                                    Miktar = item.Miktar,
                                    FiyatOrani = item.FiyatOrani,
                                    PaketMi = item.PaketMi,
                                    ListeFiyati = item.ListeFiyati,
                                    BirimFiyatNet = item.BirimFiyatNet,
                                    SatirToplamNet = item.SatirToplamNet,
                                    KampanyaYuzde = item.KampanyaYuzde,
                                    KampanyaVar = item.KampanyaVar,
                                    KampanyaBaslik = item.KampanyaBaslik
                                };

                                tumSeciliItemler.Add(bagliPaketModel);
                                islenmisItemler.Add(bagliPaket.Id);
                            }
                        }
                    }
                }

                string yeniTeklifNo = YeniTeklifNumarasiOlustur();
                if (model.YeniMusteri == null)
                {
                    Musteri musteri = _musteriRepo.Getir(x => x.Id == model.MusteriId && x.Durum == 1);
                    if (musteri == null) return Json(new { success = false, message = "Müşteri bulunamadı." });
                }

                LisansTip lisansTip = _lisansTipRepo.Getir(x => x.Id == model.LisansTipId && x.Durumu == 1);
                if (lisansTip == null) return Json(new { success = false, message = "Lisans tipi bulunamadı." });

                // TOPLAM EĞİTİM SÜRESİ HESAPLA - PAKET EĞİTİM SÜRESİ + MODÜLLERİN EĞİTİM SÜRESİ
                decimal toplamEgitimSuresi = 0m;
                HashSet<int> islenmisPaketIdleri = new HashSet<int>();
                HashSet<int> islenmisModulIdleri = new HashSet<int>();

                Console.WriteLine($"=== EĞİTİM SÜRESİ HESAPLAMASI BAŞLADI ===");

                // ÖNCE PAKETLERİ İŞLE
                foreach (SeciliItemModel item in model.SeciliItemler.Where(x => x.Tip == "grup"))
                {
                    Console.WriteLine($"Paket işleniyor: ID={item.Id}, Ad={item.Adi}");
                    int temizId = item.Id;
                    if (item.Id > 10000) // offset kontrolü
                    {
                        temizId = item.Id - 10000;
                    }

                    PaketGrup grup = _paketGrupRepo.Getir(x => x.Id == temizId && x.Durumu == 1);

                    if (grup != null)
                    {
                        Console.WriteLine($"Paket bulundu: {grup.Adi}, Eğitim Süresi: {grup.EgitimSuresi?.ToString() ?? "NULL"}");

                        // 1. PAKETİN KENDİ EĞİTİM SÜRESİ (EK OLARAK)
                        if (grup.EgitimSuresi.HasValue)
                        {
                            toplamEgitimSuresi += grup.EgitimSuresi.Value;
                            Console.WriteLine($"✓ PAKET EĞİTİM SÜRESİ EKLENDİ: {grup.Adi} paketi için {grup.EgitimSuresi.Value} saat eklendi");
                            Console.WriteLine($"  Toplam şu anda: {toplamEgitimSuresi} saat");
                        }
                        else
                        {
                            Console.WriteLine($"✗ PAKET EĞİTİM SÜRESİ YOK: {grup.Adi} paketi için eğitim süresi NULL veya 0");
                        }

                        // 2. BU PAKETTEKİ MODÜLLERİN EĞİTİM SÜRELERİ
                        List<PaketGrupDetay> paketModulleri = _paketGrupDetayRepo.GetirList(
                            x => x.Durumu == 1 && x.PaketGrupId == temizId,
                            new List<string> { "Paket" });

                        Console.WriteLine($"  Pakette {paketModulleri.Count()} modül bulundu");

                        foreach (PaketGrupDetay modulDetay in paketModulleri)
                        {
                            if (modulDetay.Paket != null)
                            {
                                Console.WriteLine($"  Modül: {modulDetay.Paket.Adi}, Eğitim Süresi: {modulDetay.Paket.EgitimSuresi?.ToString() ?? "NULL"}");

                                if (modulDetay.Paket.EgitimSuresi.HasValue)
                                {
                                    toplamEgitimSuresi += modulDetay.Paket.EgitimSuresi.Value;
                                    islenmisModulIdleri.Add(modulDetay.PaketId);
                                    Console.WriteLine($"  ✓ PAKET MODÜLÜ EĞİTİM SÜRESİ EKLENDİ: {modulDetay.Paket.Adi} modülü için {modulDetay.Paket.EgitimSuresi.Value} saat eklendi");
                                    Console.WriteLine($"    Toplam şu anda: {toplamEgitimSuresi} saat");
                                }
                            }
                        }

                        islenmisPaketIdleri.Add(temizId);
                    }
                    else
                    {
                        Console.WriteLine($"✗ Paket bulunamadı: ID={temizId}");
                    }
                }

                // SONRA BAĞIMSIZ MODÜLLERİ İŞLE
                foreach (SeciliItemModel item in model.SeciliItemler.Where(x => x.Tip == "paket"))
                {

                    Paket paket = _paketRepo.Getir(x => x.Id == item.Id && x.Durumu == 1);
                    if (paket != null)
                    {
                        Console.WriteLine($"Modül bulundu: {paket.Adi}, Eğitim Süresi: {paket.EgitimSuresi?.ToString() ?? "NULL"}");

                        // Bu modülün bir pakete ait olup olmadığını kontrol et
                        PaketGrupDetay modulDetay = _paketGrupDetayRepo.Getir(x =>
                            x.Durumu == 1 && x.PaketId == item.Id);

                        if (modulDetay == null)
                        {
                            // BAĞIMSIZ MODÜL - direkt ekle (eğer eğitim süresi varsa)
                            if (paket.EgitimSuresi.HasValue)
                            {
                                toplamEgitimSuresi += paket.EgitimSuresi.Value;
                                Console.WriteLine($"✓ BAĞIMSIZ MODÜL EĞİTİM SÜRESİ EKLENDİ: {paket.Adi} modülü için {paket.EgitimSuresi.Value} saat eklendi");
                                Console.WriteLine($"  Toplam şu anda: {toplamEgitimSuresi} saat");
                            }
                        }
                        else if (!islenmisModulIdleri.Contains(item.Id))
                        {
                            // PAKETE AİT MODÜL ama henüz işlenmemiş - ekle
                            if (paket.EgitimSuresi.HasValue)
                            {
                                toplamEgitimSuresi += paket.EgitimSuresi.Value;
                                islenmisModulIdleri.Add(item.Id);
                                Console.WriteLine($"✓ PAKETE AİT MODÜL EĞİTİM SÜRESİ EKLENDİ: {paket.Adi} modülü için {paket.EgitimSuresi.Value} saat eklendi");
                                Console.WriteLine($"  Toplam şu anda: {toplamEgitimSuresi} saat");
                            }
                        }
                        else
                        {
                            // Modül zaten paket içinde işlendi
                            Console.WriteLine($"✗ MODÜL ZATEN EKLENDİ: {paket.Adi} modülü zaten paket içinde eklendi, tekrar eklenmedi");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✗ Modül bulunamadı: ID={item.Id}");
                    }
                }

                Console.WriteLine($"=== TOPLAM EĞİTİM SÜRESİ: {toplamEgitimSuresi} saat ===");

                // FİYAT PROBLEMİ ÇÖZÜMÜ: Client'tan gelen fiyatları direkt kullan
                decimal araToplam = model.SeciliItemler.Sum(x => x.SatirToplamNet);
                decimal genelIndirim = araToplam * model.GrupIndirimOrani / 100m;
                decimal araTutar = araToplam - genelIndirim;
                decimal kdv = araTutar * 0.20m;
                decimal netToplam = araTutar + kdv;

                // YENİ MÜŞTERİ VAR MI?
                if (model.YeniMusteri != null)
                {
                    Musteri yeniMusteri = new Musteri
                    {
                        TicariUnvan = model.YeniMusteri.TicariUnvan ?? "",
                        Ad = model.YeniMusteri.Ad ?? "",
                        Soyad = model.YeniMusteri.Soyad ?? "",
                        KullaniciAdi = model.YeniMusteri.KullaniciAdi ?? "",
                        Sifre = model.YeniMusteri.Sifre ?? "", // Hash'le! (gerçek projede)
                        Email = model.YeniMusteri.Email ?? "",
                        Telefon = model.YeniMusteri.Telefon ?? "",
                        MusteriDurumuId = model.YeniMusteri.MusteriDurumuId ?? 1,
                        MusteriTipiId = model.YeniMusteri.MusteriTipiId,
                        Diger = model.YeniMusteri.Diger ?? "",
                        BayiId = model.YeniMusteri.BayiId,
                        Bolge = model.YeniMusteri.Bolge ?? "",
                        Il = model.YeniMusteri.Il ?? "",
                        Ilce = model.YeniMusteri.Ilce ?? "",
                        Belde = model.YeniMusteri.Belde ?? "",
                        Adres = model.YeniMusteri.Adres ?? "",
                        TCVNo = model.YeniMusteri.TCVNo ?? "",
                        VergiDairesi = model.YeniMusteri.VergiDairesi ?? "",
                        KepAdresi = model.YeniMusteri.KepAdresi ?? "",
                        WebAdresi = model.YeniMusteri.WebAdresi ?? "",
                        Aciklama = model.YeniMusteri.Aciklama ?? "",
                        AlpemixFirmaAdi = model.YeniMusteri.AlpemixFirmaAdi ?? "",
                        AlpemixGrupAdi = model.YeniMusteri.AlpemixGrupAdi ?? "",
                        AlpemixSifre = model.YeniMusteri.AlpemixSifre ?? "",
                        Durum = 1,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now,
                        EkleyenKullaniciId = kullanici?.Id ?? 0
                    };

                    _musteriRepo.Ekle(yeniMusteri);

                    // Logo ve İmza varsa kaydet
                    if (model.YeniMusteri.Logo != null)
                    {
                        string logoAdi = Guid.NewGuid() + Path.GetExtension(model.YeniMusteri.Logo.FileName);
                        string logoYolu = Path.Combine(_webHostEnviroment.WebRootPath, "WebAdminTheme", "logo", logoAdi);
                        using (FileStream stream = new FileStream(logoYolu, FileMode.Create))
                            model.YeniMusteri.Logo.CopyTo(stream);
                        yeniMusteri.LogoUzanti = logoAdi;
                    }

                    if (model.YeniMusteri.Imza != null)
                    {
                        string imzaAdi = Guid.NewGuid() + Path.GetExtension(model.YeniMusteri.Imza.FileName);
                        string imzaYolu = Path.Combine(_webHostEnviroment.WebRootPath, "WebAdminTheme", "imza", imzaAdi);
                        using (FileStream stream = new FileStream(imzaYolu, FileMode.Create))
                            model.YeniMusteri.Imza.CopyTo(stream);
                        yeniMusteri.ImzaUzanti = imzaAdi;
                    }

                    _musteriRepo.Guncelle(yeniMusteri); // ID almak için
                    kullanilacakMusteriId = yeniMusteri.Id;
                }

                Teklif yeniTeklif = new Teklif
                {
                    TeklifNo = (Id != null ? model.TeklifNo : yeniTeklifNo),
                    GecerlilikTarihi = model.GecerlilikTarihi,
                    MusteriId = kullanilacakMusteriId,
                    LisansTipId = model.LisansTipId,
                    Aciklama = model.Aciklama?.Trim(),
                    GrupIndirimOrani = model.GrupIndirimOrani,
                    ToplamListeFiyat = model.FiyatOzete.Liste.AraToplam + model.FiyatOzete.Liste.Indirim,
                    AraToplam = model.FiyatOzete.Liste.AraToplam,
                    ToplamIndirim = model.FiyatOzete.Liste.Indirim,
                    KdvTutari = model.FiyatOzete.Liste.KDVToplam,
                    NetToplam = model.FiyatOzete.Liste.NetToplam,

                    // EĞİTİM SÜRESİ KAYDI
                    EgitimSuresi = toplamEgitimSuresi,

                    OlusturanKullaniciId = kullanici?.Id ?? 0,
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    Aktif = true,
                    OnaylandiMi = false,
                    TeklifDurumId = 1
                };

                _teklifRepo.Ekle(yeniTeklif);

                if (Id != null)
                {
                    Teklif teklif = _teklifRepo.Getir(x => x.Id == Id);
                    teklif.TeklifDurumId = 10;
                    teklif.GuncellenmeTarihi = DateTime.Now;
                    _teklifRepo.Guncelle(teklif);
                }

                if (!KaydetTeklifDetaylariDirekt(yeniTeklif.Id, model.SeciliItemler))
                {
                    _teklifRepo.Sil(yeniTeklif);
                    return Json(new { success = false, message = "Detaylar kaydedilemedi." });
                }

                return Json(new
                {
                    success = true,
                    message = "Teklif başarıyla oluşturuldu.",
                    teklifId = yeniTeklif.Id,
                    teklifNo = yeniTeklifNo,
                    toplamEgitimSuresi = toplamEgitimSuresi, // İstemciye de gönder
                    redirectTo = "AdminTeklif/Index"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        private bool KaydetTeklifDetaylariDirekt(int teklifId, List<SeciliItemModel> seciliItemler)
        {
            try
            {
                List<TeklifDetay> detaylar = new List<TeklifDetay>();
                int siraNo = 1;

                foreach (SeciliItemModel item in seciliItemler)
                {

                    if (item.Tip == "grup")
                    {
                        // OFFSET'İ TEMİZLE
                        int temizId = item.Id;
                        if (item.Id > 10000)
                        {
                            temizId = item.Id - 10000;
                        }

                        // Grup başlık satırı
                        detaylar.Add(new TeklifDetay
                        {
                            TeklifId = teklifId,
                            Tip = "grup",
                            PaketGrupId = temizId, // temizId kullan
                            ItemAdi = item.Adi,
                            PaketGrupAdi = item.Adi,
                            ListeFiyati = item.BirimFiyatNet,
                            BirimFiyatNet = item.BirimFiyatNet,
                            SatirToplamNet = item.SatirToplamNet,
                            KampanyaFiyati = item.KampanyaVar ? item.BirimFiyatNet : null,
                            KampanyaIndirimYuzdesi = (int)item.KampanyaYuzde,
                            KampanyaBaslik = item.KampanyaBaslik,
                            BireyselIndirimYuzdesi = (int)item.IndirimYuzdesi,
                            Miktar = 1,
                            SiraNo = siraNo++,
                            EklenmeTarihi = DateTime.Now,
                            GuncellenmeTarihi = DateTime.Now,
                            Durumu = 1
                        });

                        continue; // ⛔ grup olan item tekrar aşağıda eklenmesin
                    }

                    // Normal item satırı
                    detaylar.Add(new TeklifDetay
                    {
                        TeklifId = teklifId,
                        Tip = item.Tip,
                        PaketGrupId = null,
                        PaketId = item.Id,
                        ItemAdi = item.Adi,
                        ListeFiyati = item.BirimFiyatNet,
                        BirimFiyatNet = item.BirimFiyatNet,
                        SatirToplamNet = item.SatirToplamNet,
                        KampanyaFiyati = item.KampanyaVar ? item.BirimFiyatNet : null,
                        KampanyaIndirimYuzdesi = (int)item.KampanyaYuzde,
                        KampanyaBaslik = item.KampanyaBaslik ?? "",
                        BireyselIndirimYuzdesi = (int)item.IndirimYuzdesi,
                        Miktar = item.Miktar,
                        MiktarBazliEkOranYuzde = item.FiyatOrani,
                        BagimsizModulMu = !item.PaketMi,
                        SiraNo = siraNo++,
                        EklenmeTarihi = DateTime.Now,
                        GuncellenmeTarihi = DateTime.Now,
                        Durumu = 1
                    });
                }

                foreach (TeklifDetay d in detaylar)
                    _teklifDetayRepo.Ekle(d);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Detay kaydetme hatası: " + ex.Message);
                return false;
            }
        }

        private string SayiyiYaziyaCevir(decimal tutar)
        {
            if (tutar == 0) return "Sıfır Türk Lirası";

            string[] birler = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
            string[] onlar = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan" };

            long tl = (long)Math.Floor(tutar);
            int kurus = (int)Math.Round((tutar - tl) * 100);

            string sonuc = "";

            long milyon = tl / 1000000;
            if (milyon > 0)
            {
                sonuc += Oku(milyon) + " Milyon ";
                tl %= 1000000;
            }

            long bin = tl / 1000;
            if (bin > 0)
            {
                if (bin == 1)
                    sonuc += "Bin ";
                else
                    sonuc += Oku(bin) + " Bin ";
                tl %= 1000;
            }

            if (tl > 0)
                sonuc += Oku(tl);

            sonuc = sonuc.Trim();
            if (string.IsNullOrEmpty(sonuc)) sonuc = "Sıfır";

            sonuc += " TL";

            if (kurus > 0)
                sonuc += " " + kurus.ToString().PadLeft(2, '0') + " Kr";

            return sonuc;
        }

        private string Oku(long sayi)
        {
            if (sayi == 0) return "";
            if (sayi == 1) return "Bir";

            string[] birler = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
            string[] onlar = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan" };

            long yuz = sayi / 100;
            long on = (sayi % 100) / 10;
            long bir = sayi % 10;

            string yazi = "";

            if (yuz > 0)
            {
                if (yuz == 1)
                    yazi += "Yüz";
                else
                    yazi += birler[yuz] + "Yüz";
            }

            if (on > 0)
                yazi += onlar[on];

            if (bir > 0)
                yazi += birler[bir];

            return yazi.Trim();
        }
        [HttpGet]
        public IActionResult TeklifPdf(int teklifId)
        {
            Teklif teklif = _teklifRepo.Getir(x => x.Id == teklifId,
                new List<string> { "Musteri", "LisansTip", "Detaylar" });
            if (teklif == null)
                return NotFound();

            Musteri musteri = teklif.Musteri;
            decimal genelToplam = teklif.NetToplam;
            string yaziIle = SayiyiYaziyaCevir(genelToplam);
            string tarihStr = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("tr-TR"));
            string gecerlilik = DateTime.Now.AddDays(15).ToString("dd.MM.yyyy");

            var baslik = new
            {
                teklif.TeklifNo,
                Tarih = tarihStr,
                MusteriAdi = $"{musteri.Ad} {musteri.Soyad}",
                MusteriTelefon = musteri.Telefon ?? "",
                MusteriYetkili = musteri.Ad ?? "Noyasis İş Ortağı",
                GecerlilikTarihi = gecerlilik,
                Aciklama = string.IsNullOrWhiteSpace(teklif.Aciklama) ? "" : "* " + teklif.Aciklama.Trim(),
                AraToplam = teklif.AraToplam.ToString("N2"),
                IndirimToplam = teklif.ToplamIndirim.ToString("N2"),
                KdvTutar = teklif.KdvTutari.ToString("N2"),
                GenelToplam = genelToplam.ToString("N2"),
                YaziIle = yaziIle,
                LisansTipi = teklif.LisansTip?.Adi ?? "",
                Toplam=teklif.ToplamListeFiyat.ToString("N2"),
                EgitimSuresi = teklif.EgitimSuresi.ToString("N0") // Eğitim süresi eklendi


            };

            List<TeklifRapor> satirlar = new List<TeklifRapor>();
            List<TeklifDetay> detaylar = teklif.Detaylar.OrderBy(x => x.SiraNo).ToList();

            // 1) ÖNCE TÜM GRUP SATIRLARINI BUL VE İŞLE
            List<TeklifDetay> grupSatirlari = detaylar
                .Where(x => x.Tip == "grup" && x.PaketGrupId.HasValue)
                .OrderBy(x => x.SiraNo)
                .ToList();

            // Her grup için işlem yap
            foreach (TeklifDetay grup in grupSatirlari)
            {
                string modulListesi = "";

                // 1. YOL: PaketGrupDetay tablosundan modülleri al
                List<PaketGrupDetay> paketGrupDetaylari = _paketGrupDetayRepo.GetirList(
                    x => x.Durumu == 1 && x.PaketGrupId == grup.PaketGrupId.Value,
                    new List<string> { "Paket" }
                );

                if (paketGrupDetaylari.Any())
                {
                    // Modül isimlerini virgülle birleştir
                    modulListesi = string.Join(", ", paketGrupDetaylari
                        .Where(pgd => pgd.Paket != null)
                        .Select(pgd => pgd.Paket.Adi?.Trim() ?? "Bilinmeyen Modül"));
                }
                else
                {
                    int grupSirasi = grup.SiraNo;

                    // Sonraki satırları kontrol et (bir sonraki grup satırına kadar)
                    int sonrakiGrupSirasi = detaylar
                        .Where(x => x.Id != grup.Id && x.Tip == "grup" && x.SiraNo > grupSirasi)
                        .OrderBy(x => x.SiraNo)
                        .FirstOrDefault()?.SiraNo ?? int.MaxValue;

                    // Bu grup ile sonraki grup arasındaki modülleri al
                    List<TeklifDetay> grupModulleri = detaylar
                        .Where(x => x.Tip == "modul" &&
                               x.SiraNo > grupSirasi &&
                               x.SiraNo < sonrakiGrupSirasi)
                        .OrderBy(x => x.SiraNo)
                        .ToList();

                    if (grupModulleri.Any())
                    {
                        modulListesi = string.Join(", ", grupModulleri
                            .Select(m => m.ItemAdi?.Trim() ?? m.Paket?.Adi?.Trim() ?? "Modül"));
                    }
                }

                // Grup toplam fiyatı
                decimal grupToplam = grup.BirimFiyatNet > 0 ? grup.BirimFiyatNet : grup.ListeFiyati;

                // Grup satırını EKLE (MODÜLLERİ AYRI SATIR EKLEME)
                satirlar.Add(new TeklifRapor
                {
                    TeklifNo = baslik.TeklifNo,
                    Tarih = baslik.Tarih,
                    MusteriAdi = baslik.MusteriAdi,
                    MusteriTelefon = baslik.MusteriTelefon,
                    MusteriYetkili = baslik.MusteriYetkili,
                    GecerlilikTarihi = baslik.GecerlilikTarihi,
                    Aciklama = baslik.Aciklama,
                    AraToplam = baslik.AraToplam,
                    Toplam=baslik.Toplam,
                    IndirimToplam = baslik.IndirimToplam,
                    KdvTutar = baslik.KdvTutar,
                    GenelToplam = baslik.GenelToplam,
                    YaziIle = baslik.YaziIle,
                    UrunAdi = !string.IsNullOrEmpty(grup.PaketGrupAdi) ? grup.PaketGrupAdi.Trim() :
                              (!string.IsNullOrEmpty(grup.ItemAdi) ? grup.ItemAdi.Trim() : "Paket Grubu"),
                    Miktar = "1",
                    IndirimYuzde = (grup.KampanyaIndirimYuzdesi + grup.BireyselIndirimYuzdesi) > 0 ?
                                  (grup.KampanyaIndirimYuzdesi + grup.BireyselIndirimYuzdesi) + "%" : "",
                    Tutar = grupToplam.ToString("N2") + " ₺",
                    AltSatirMi = false,
                    LisansTipi=baslik.LisansTipi,
                    // MODÜLLER SADECE GİRİNTİ ALANINDA
                    Girinti = string.IsNullOrEmpty(modulListesi) ? "" : "(" + modulListesi + ")"
                });
            }

            // 2) BAĞIMSIZ MODÜLLERİ BUL (SADECE BagimsizModulMu = true OLANLAR)
            // NOT: Normal modülleri AYRI SATIR OLARAK EKLEMEYECEĞİZ
            List<TeklifDetay> bagimsizModuller = detaylar
                .Where(x => x.Tip == "modul" && x.BagimsizModulMu == true)
                .OrderBy(x => x.SiraNo)
                .ToList();

            foreach (TeklifDetay detay in bagimsizModuller)
            {
                // BAĞIMSIZ MODÜLLERİ AYRI SATIR OLARAK EKLE
                satirlar.Add(new TeklifRapor
                {
                    TeklifNo = baslik.TeklifNo,
                    Tarih = baslik.Tarih,
                    MusteriAdi = baslik.MusteriAdi,
                    MusteriTelefon = baslik.MusteriTelefon,
                    MusteriYetkili = baslik.MusteriYetkili,
                    GecerlilikTarihi = baslik.GecerlilikTarihi,
                    Aciklama = baslik.Aciklama,
                    AraToplam = baslik.AraToplam,
                    Toplam = baslik.Toplam,
                    IndirimToplam = baslik.IndirimToplam,
                    LisansTipi = baslik.LisansTipi,
                    KdvTutar = baslik.KdvTutar,
                    GenelToplam = baslik.GenelToplam,
                    YaziIle = baslik.YaziIle,
                    UrunAdi = !string.IsNullOrEmpty(detay.ItemAdi) ? detay.ItemAdi.Trim() :
                              (detay.Paket != null ? detay.Paket.Adi?.Trim() : "Modül"),
                    Miktar = detay.Miktar.ToString(),
                    IndirimYuzde = (detay.KampanyaIndirimYuzdesi + detay.BireyselIndirimYuzdesi) > 0 ?
                                  (detay.KampanyaIndirimYuzdesi + detay.BireyselIndirimYuzdesi) + "%" : "",
                    Tutar = detay.BirimFiyatNet.ToString("N2") + " ₺",
                    AltSatirMi = false,
                    Girinti = "" // Bağımsız modüllerin girintisi yok
                });
            }

            // 3) EĞER HİÇ SATIR YOKSA (SADECE MODÜLLER VARSA) - FALLBACK
            if (!satirlar.Any())
            {
                // Sadece grup olmayan ve BagimsizModulMu = true olanları ekle
                foreach (TeklifDetay detay in detaylar.Where(x => x.BagimsizModulMu == true).OrderBy(x => x.SiraNo))
                {
                    satirlar.Add(new TeklifRapor
                    {
                        TeklifNo = baslik.TeklifNo,
                        Tarih = baslik.Tarih,
                        MusteriAdi = baslik.MusteriAdi,
                        MusteriTelefon = baslik.MusteriTelefon,
                        MusteriYetkili = baslik.MusteriYetkili,
                        GecerlilikTarihi = baslik.GecerlilikTarihi,
                        Aciklama = baslik.Aciklama,
                        AraToplam = baslik.AraToplam,
                        Toplam = baslik.Toplam,
                        IndirimToplam = baslik.IndirimToplam,
                        LisansTipi = baslik.LisansTipi,

                        KdvTutar = baslik.KdvTutar,
                        GenelToplam = baslik.GenelToplam,
                        YaziIle = baslik.YaziIle,
                        UrunAdi = !string.IsNullOrEmpty(detay.ItemAdi) ? detay.ItemAdi.Trim() :
                                  (detay.Paket != null ? detay.Paket.Adi?.Trim() : "Modül"),
                        Miktar = detay.Miktar.ToString(),
                        IndirimYuzde = (detay.KampanyaIndirimYuzdesi + detay.BireyselIndirimYuzdesi) > 0 ?
                                      (detay.KampanyaIndirimYuzdesi + detay.BireyselIndirimYuzdesi) + "%" : "",
                        Tutar = detay.BirimFiyatNet.ToString("N2") + " ₺",
                        AltSatirMi = false,
                        Girinti = ""
                    });
                }
            }

            // DEBUG: Oluşan satırları logla
            foreach (TeklifRapor satir in satirlar)
            {
                Console.WriteLine($"Satır: {satir.UrunAdi}, Girinti: {satir.Girinti}");
            }

            // RDLC PDF ÜRET
            string rdlcPath = Path.Combine(_webHostEnviroment.WebRootPath, "Raporlar", "Teklif.rdlc");

            if (!System.IO.File.Exists(rdlcPath))
            {
                return BadRequest($"RDLC dosyası bulunamadı: {rdlcPath}");
            }

            LocalReport localReport = new LocalReport(rdlcPath);
            localReport.AddDataSource("DataSetTeklif", satirlar);

            ReportResult result = localReport.Execute(RenderType.Pdf);
            Response.Headers["Content-Disposition"] = $"inline; filename={baslik.TeklifNo}.pdf";
            return File(result.MainStream, "application/pdf");
        }
    }

    public class TeklifOlusturModel
    {
        // Temel Bilgiler
        public string TeklifNo { get; set; } = "";
        public int MusteriId { get; set; }
        public int LisansTipId { get; set; }
        public string? Aciklama { get; set; }
        public int GrupIndirimOrani { get; set; }
        public DateTime GecerlilikTarihi { get; set; }
        public List<SeciliItemModel> SeciliItemler { get; set; } = new List<SeciliItemModel>();
        public YeniMusteriModel YeniMusteri { get; set; }

        // Fiyat Özet Tablosu Bilgileri
        public FiyatOzeteModel FiyatOzete { get; set; } = new FiyatOzeteModel();
    }
    public class YeniMusteriModel
    {
        // Temel Bilgiler
        public string TicariUnvan { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;

        // İletişim Bilgileri
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Bolge { get; set; } = string.Empty;
        public string Il { get; set; } = string.Empty;
        public string Ilce { get; set; } = string.Empty;
        public string Belde { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;

        // Vergi ve Resmi Bilgiler
        public string TCVNo { get; set; } = string.Empty;
        public string VergiDairesi { get; set; } = string.Empty;
        public string KepAdresi { get; set; } = string.Empty;
        public string WebAdresi { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;

        // Alpemix Bilgileri
        public string AlpemixFirmaAdi { get; set; } = string.Empty;
        public string AlpemixGrupAdi { get; set; } = string.Empty;
        public string AlpemixSifre { get; set; } = string.Empty;

        // İlişkili Alanlar
        public int? MusteriDurumuId { get; set; }
        public int? MusteriTipiId { get; set; }
        public string Diger { get; set; } = string.Empty; // "Diğer" tipi seçildiğinde buraya yazılır
        public int? BayiId { get; set; }

        // Dosyalar
        public IFormFile? Logo { get; set; }
        public IFormFile? Imza { get; set; }
    }
    public class FiyatOzeteModel
    {
        public FiyatOzeteTablosu Liste { get; set; } = new FiyatOzeteTablosu();
        public FiyatOzeteTablosu Kampanya { get; set; } = new FiyatOzeteTablosu();
    }

    public class FiyatOzeteTablosu
    {
        public decimal Indirim { get; set; }
        public decimal KDVToplam { get; set; }
        public decimal AraToplam { get; set; }
        public decimal NetToplam { get; set; }
    }

    public class SeciliItemModel
    {
        public int Id { get; set; }
        public string Tip { get; set; } = "";
        public string Adi { get; set; } = "";
        public decimal IndirimYuzdesi { get; set; }
        public int Miktar { get; set; } = 1;
        public decimal FiyatOrani { get; set; }
        public string? KampanyaBaslik { get; set; }
        public bool PaketMi { get; set; }

        // FİYAT ALANLARI - BU ALANLAR ARTIK DOLU GELECEK
        public decimal ListeFiyati { get; set; }
        public decimal BirimFiyatNet { get; set; }
        public decimal SatirToplamNet { get; set; }
        public decimal KampanyaYuzde { get; set; }
        public bool KampanyaVar { get; set; }
    }

    public class TeklifHesaplaModel
    {
        public int MusteriId { get; set; }
        public decimal GrupIndirimOrani { get; set; }
        public List<SeciliItem> SeciliItemler { get; set; } = new List<SeciliItem>();
        public decimal ToplamMiktarOrani { get; set; } = 0;
    }

    public class SeciliItem
    {
        public int Id { get; set; }
        public string Tip { get; set; } = "";
        public string Adi { get; set; } = "";
        public decimal IndirimYuzdesi { get; set; }
        public int Miktar { get; set; } = 0;
        public string? KampanyaAdi { get; set; }
        public decimal? FiyatOrani { get; set; } = 0;
        public string? KampanyaBaslik { get; set; }
        public int GrupId { get; set; }
        public bool PaketMi { get; set; }
    }

}