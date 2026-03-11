using Hangfire; // YENÝ: Hangfire namespace'i
using Hangfire.Dashboard;
using Hangfire.SqlServer; // YENÝ: SQL Server storage için
using Microsoft.AspNetCore.Http.Features;
// Program.cs (.NET 6+)
using QuestPDF.Infrastructure;
using WepApp.Jobs;
using WepApp.Repositories;
using WepApp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// QuestPDF lisans ayarý
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddScoped<MusteriSozlesmeRepository>();
builder.Services.AddScoped<TeklifRepository>();

builder.Services.AddHostedService<SozlesmeArsivlemeService>();

// ============= YENÝ: HANGFIRE KURULUMU - DÜZELTÝLMÝÞ =============
// Hangfire için SQL Server storage ekleyin
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"))); // SADECE ÝSMÝNÝ YAZIN!

// Hangfire server'ý ekleyin
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Ayný anda çalýþacak iþçi sayýsý
    options.SchedulePollingInterval = TimeSpan.FromSeconds(30); // Zamanlanmýþ iþleri kontrol etme sýklýðý
});
// ====================================================

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Oturum süresi
    options.Cookie.HttpOnly = true; // Güvenlik için
    options.Cookie.IsEssential = true; // Cookie'nin gerekli olduðunu belirtir
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1073741824;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824;
});

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.MaxDepth = 2000000;
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession(); // Oturum middleware'ini buraya ekleyin
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ============= YENÝ: HANGFIRE DASHBOARD =============
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Müþteri Register Kontrol Ýþleri",
    StatsPollingInterval = 30000 // 30 saniye
});
// ====================================================

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "paketBaglama",
    pattern: "AdminPaketBaglama/{action}/{id?}",
    defaults: new { controller = "AdminPaketBaglama", action = "Index" }
);

// ============= YENÝ: JOB'LARI BAÞLAT =============
// Uygulama baþladýðýnda job'larý schedule et
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Her gece 03:00'te çalýþacak job
    recurringJobManager.AddOrUpdate<RegisterKontrolJob>(
        "register-kontrol-job",                    // Job ID
        job => job.Calistir(),                      // Çalýþacak metod
        "0 3 * * *",                                // Cron: Her gece 03:00
        TimeZoneInfo.Local);                         // Yerel saat dilimi

    Console.WriteLine("Hangfire job'larý baþlatýldý: Register kontrol job eklendi.");
}
// ==================================================

app.Run();

// ============= YENÝ: HANGFIRE YETKÝLENDÝRME FÝLTRESÝ =============
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Session'dan kullanýcý bilgisini kontrol et
        var kullanici = httpContext.Session.GetString("Kullanici");

        // Þimdilik herkese açýk, sonra düzenleyin
        return true;
    }
}
// ==============================================================