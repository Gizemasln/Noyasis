using Microsoft.AspNetCore.Http.Features;
// Program.cs (.NET 6+)
using QuestPDF.Infrastructure;
using WepApp.Repositories;
using WepApp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// QuestPDF lisans ayarý
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddScoped<MusteriSozlesmeRepository>();
builder.Services.AddScoped<TeklifRepository>();


builder.Services.AddHostedService<SozlesmeArsivlemeService>();

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
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Oturum süresi
    options.Cookie.HttpOnly = true; // Güvenlik için
    options.Cookie.IsEssential = true; // Cookie'nin gerekli olduðunu belirtir
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824;
});
builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.MaxDepth = 2000000; });

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

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
// Program.cs veya Startup.cs'de
app.MapControllerRoute(
    name: "paketBaglama",
    pattern: "AdminPaketBaglama/{action}/{id?}",
    defaults: new { controller = "AdminPaketBaglama", action = "Index" }
);
app.Run();
