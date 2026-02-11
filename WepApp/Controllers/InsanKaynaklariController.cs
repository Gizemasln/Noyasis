using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Repositories;
using System.IO;
using WebApp.Models;
using WebApp.Repositories;
using WepApp.Models;
using WepApp.Repositories;

namespace WebApp.Controllers
{
    public class InsanKaynaklariController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public InsanKaynaklariController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            LoadCommonData();

            // Sadece başarılı gönderimden sonra modal gösterilsin
            bool basarili = TempData["Basarili"] as bool? == true;
            if (basarili)
            {
                ViewData["Basarili"] = true;
            }

            return View();
        }

        [HttpPost]
        public IActionResult Kaydet(IKFormu ikFormu, IFormFile? DosyaYolu)
        {
            LoadCommonData();

            IKFormuRepository ikFormuRepository = new IKFormuRepository();

            try
            {
                string dosyaYolu = "";
                if (DosyaYolu != null && DosyaYolu.Length > 0)
                {
                    string uploadsPath = Path.Combine(_environment.WebRootPath, "WebAdminTheme", "IK");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + DosyaYolu.FileName;
                    string filePath = Path.Combine(uploadsPath, uniqueFileName);
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        DosyaYolu.CopyTo(fileStream);
                    }

                    dosyaYolu = "/WebAdminTheme/IK/" + uniqueFileName;
                }

                IKFormu ikFormu1 = new IKFormu
                {
                    AdiSoyadi = ikFormu.AdiSoyadi ?? "",
                    Telefon = ikFormu.Telefon ?? "",
                    Eposta = ikFormu.Eposta ?? "",
                    TC = ikFormu.TC ?? "",
                    DosyaYolu = dosyaYolu,
                    Mesaj = ikFormu.Mesaj ?? "",
                    EklenmeTarihi = DateTime.Now,
                    GuncellenmeTarihi = DateTime.Now,
                    Durumu = 1,
                    KullanicilarId=0
                };

                ikFormuRepository.Ekle(ikFormu1);

                // Başarı durumunu TempData ile gönder ve redirect yap
                TempData["Basarili"] = true;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Kayıt sırasında hata oluştu: " + ex.Message;
                return View("Index", ikFormu);
            }
        }
    }
}