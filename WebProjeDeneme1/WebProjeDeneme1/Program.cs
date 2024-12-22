using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL bağlantısını ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// MVC yapılandırmasını ekle
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ortam kontrolü: Development mı, Production mı?
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Geliştirme ortamı için detaylı hata sayfası
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Üretim ortamında hata sayfası
    app.UseHsts(); // HTTPS Strict Transport Security
}

// HTTPS yönlendirmesi ve statik dosyalar
app.UseHttpsRedirection();
app.UseStaticFiles();

// Routing işlemleri
app.UseRouting();
app.UseAuthorization(); // Yetkilendirme middleware'i

// Varsayılan rota yapılandırması
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
