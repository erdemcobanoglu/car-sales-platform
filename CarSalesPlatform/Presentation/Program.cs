using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Data.Seed;
using Presentation.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// ✅ Identity UI (Login/Register/Logout) Razor Pages olduğu için şart
builder.Services.AddRazorPages();

// DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ✅ Identity
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

// ✅ Cookie ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
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

// ✅ Identity UI sayfaları için şart
app.MapRazorPages();

// ===== MIGRATION + SEED =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, userManager);
}

app.Run();
