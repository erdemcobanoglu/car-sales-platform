using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Data.Seed;
using Presentation.Models;

// ✅ Queue/Worker namespace'ini kendi yapına göre düzelt
using Presentation.BackgroundJobs;
using Presentation.BackgroundJobs.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext (SQL Server) + ✅ Lazy Loading Proxies
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .UseLazyLoadingProxies()
);

// ✅ Identity (MVC AccountController kullanıyorsun)
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ✅ Cookie ayarları (Authorize -> MVC /Account/Login’e yönlendir)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
});


// ===============================
// ✅ PHOTO UPLOAD QUEUE + WORKER
// ===============================
builder.Services.AddSingleton<IPhotoJobQueue, PhotoJobQueue>();
builder.Services.AddHostedService<PhotoProcessingWorker>();


// ===============================
// ✅ UPLOAD LIMITLERI
// (Mobil çoklu foto upload için önemli)
// ===============================
const long maxUploadBytes = 200_000_000; // 200MB örnek

// Multipart form limit
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxUploadBytes;
});

// Kestrel limit (self-host için)
builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = maxUploadBytes;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ===== MIGRATION + SEED =====
// Not: Prod'da seed istemiyorsan burayı env'e bağla.
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

db.Database.Migrate();

if (app.Environment.IsDevelopment())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await DbSeeder.SeedAsync(db, userManager);
}

app.Run();
