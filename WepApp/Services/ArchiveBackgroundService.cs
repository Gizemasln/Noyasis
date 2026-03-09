using System;
using System.Linq;
using WepApp.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using WepApp.Models;

namespace WepApp.Services
{
    public class SozlesmeArsivlemeService : BackgroundService
    {
        private readonly ILogger<SozlesmeArsivlemeService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SozlesmeArsivlemeService(
            ILogger<SozlesmeArsivlemeService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sözleşme arşivleme servisi başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var sozlesmeRepo = scope.ServiceProvider
                        .GetRequiredService<MusteriSozlesmeRepository>();

                    var teklifRepo = scope.ServiceProvider
                        .GetRequiredService<TeklifRepository>();

                    var arsivRepo = scope.ServiceProvider
                        .GetRequiredService<ArsivRepository>();

                    Arsiv arsiv = arsivRepo.Getir(x => x.Durumu == 1);

                    if (arsiv == null)
                        continue;

                    var tarih = DateTime.Today.AddDays(-arsiv.Gun);

                    var arsivlenecekSozlesmeler = sozlesmeRepo
                        .GetirList(s => s.Durumu == 1 &&
                                        s.YayinTarihi.Date <= tarih)
                        .ToList();

                    foreach (var sozlesme in arsivlenecekSozlesmeler)
                    {
                        if (sozlesme.TeklifId != 0)
                        {
                            var teklif = teklifRepo.Getir(sozlesme.TeklifId);

                            if (teklif != null && teklif.TeklifDurumId != 12)
                            {
                                teklif.TeklifDurumId = 12;
                                teklif.GuncellenmeTarihi = DateTime.Now;

                                teklifRepo.Guncelle(teklif);

                                _logger.LogInformation(
                                    $"Teklif ID {teklif.Id} arşive alındı (Sözleşme: {sozlesme.DokumanNo})");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sözleşme arşivleme sırasında hata oluştu.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
