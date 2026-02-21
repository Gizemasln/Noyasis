using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Repositories
{
    public class SliderRepository : GenericRepository<Slider>
    {
        private readonly Context _context;

        public SliderRepository()
        {
            _context = new Context();
        }

        // Slider'a özel metotlar

        // Yayındaki sliderları sıralı getir
        public List<Slider> GetirAktifSliderlar()
        {
            return _context.Set<Slider>()
                .Where(x => x.Durumu == 1 && x.YayindaMi == 1)
                .OrderBy(x => x.SlaytSiraNo)
                .ThenByDescending(x => x.EklenmeTarihi)
                .ToList();
        }

        // Görseli olan sliderları getir
        public List<Slider> GetirGorselliSliderlar()
        {
            return _context.Set<Slider>()
                .Where(x => x.Durumu == 1 && !string.IsNullOrEmpty(x.GorselYolu))
                .OrderBy(x => x.SlaytSiraNo)
                .ToList();
        }

        // Videosu olan sliderları getir
        public List<Slider> GetirVideoSliderlar()
        {
            return _context.Set<Slider>()
                .Where(x => x.Durumu == 1 && !string.IsNullOrEmpty(x.VideoYolu))
                .OrderBy(x => x.SlaytSiraNo)
                .ToList();
        }

        // En yüksek sıra numarasını getir
        public int GetirMaxSiraNo()
        {
            var maxSira = _context.Set<Slider>()
                .Where(x => x.Durumu == 1)
                .Max(x => (int?)x.SlaytSiraNo);

            return maxSira ?? 0;
        }

        // Sıra numaralarını yeniden düzenle
        public void SiraNumaralariniYenidenDuzenle()
        {
            var sliderlar = _context.Set<Slider>()
                .Where(x => x.Durumu == 1)
                .OrderBy(x => x.SlaytSiraNo)
                .ToList();

            int yeniSira = 1;
            foreach (var slider in sliderlar)
            {
                if (slider.SlaytSiraNo != yeniSira)
                {
                    slider.SlaytSiraNo = yeniSira;
                    slider.GuncellenmeTarihi = DateTime.Now;
                }
                yeniSira++;
            }
            _context.SaveChanges();
        }
    }
}