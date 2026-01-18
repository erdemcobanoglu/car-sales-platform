using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Presentation.Data;
using Presentation.Models;
using Presentation.ViewModels.Profile;

namespace Presentation.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        // ✅ Profil yoksa otomatik oluştur
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        var vm = new ProfileVm
        {
            UserId = userId,
            UserName = User.Identity?.Name ?? "",
            FullName = profile.FullName,
            Phone = profile.Phone,
            City = profile.City
        };

        return View(vm);
    }
}
