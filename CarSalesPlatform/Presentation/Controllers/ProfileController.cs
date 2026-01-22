using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Models;
using Presentation.ViewModels.Profile;

namespace Presentation.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // GET: /Profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        // ✅ Profil yoksa otomatik oluştur
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
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

        var completeness = await CalculateProfileCompletenessAsync(profile);
        ViewBag.ProfileCompleteness = completeness;

        return View(vm);
    }

    // POST: /Profile/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileVm vm)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        // readonly alanları tekrar dolduralım
        vm.UserId = userId;
        vm.UserName = User.Identity?.Name ?? "";

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.UserProfiles.Add(profile);
        }

        if (!ModelState.IsValid)
        {
            var completenessInvalid = await CalculateProfileCompletenessAsync(profile);
            ViewBag.ProfileCompleteness = completenessInvalid;
            return View("Index", vm);
        }

        // ✅ Sadece kullanıcı bilgileri (Id hariç)
        profile.FullName = vm.FullName?.Trim();
        profile.Phone = vm.Phone?.Trim();
        profile.City = vm.City?.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // CHANGE EMAIL
    // =========================
    [HttpGet]
    public IActionResult ChangeEmail()
    {
        return View(new ChangeEmailVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(ChangeEmailVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.Email = vm.NewEmail;
        user.UserName = vm.NewEmail; // genelde email=username

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);

            return View(vm);
        }

        // ✅ cookie/claims refresh
        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Email updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // CHANGE PASSWORD
    // =========================
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(
            user,
            vm.CurrentPassword,
            vm.NewPassword
        );

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);

            return View(vm);
        }

        // ✅ cookie/claims refresh
        await _signInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // PROFILE COMPLETENESS
    // =========================
    private async Task<int> CalculateProfileCompletenessAsync(UserProfile profile)
    {
        var user = await _userManager.GetUserAsync(User);

        int score = 0;
        if (!string.IsNullOrWhiteSpace(profile.FullName)) score += 25;
        if (!string.IsNullOrWhiteSpace(profile.Phone)) score += 25;
        if (!string.IsNullOrWhiteSpace(profile.City)) score += 25;
        if (user?.EmailConfirmed == true) score += 25;

        return score;
    }
}
