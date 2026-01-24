using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Models;
using Presentation.ViewModels.Vehicles;

// ✅ ImageSharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

// ✅ Çakışmayı önlemek için alias
using ImageSharpImage = SixLabors.ImageSharp.Image;
using VehicleTrim = Presentation.Models.Trim;

namespace Presentation.Controllers;

[Authorize]
public class VehiclesController : Controller
{
    private readonly AppDbContext _db;

    public VehiclesController(AppDbContext db)
    {
        _db = db;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ✅ Normalize: trim + UPPER (Audi/audi/AUDI tek olsun)
    private static string Norm(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

    // =========================
    // LIST PAGE
    // =========================
    [HttpGet]
    public IActionResult Index() => View();

    // DataTables server-side endpoint
    [HttpPost]
    public async Task<IActionResult> Datatable()
    {
        var userId = CurrentUserId;

        var draw = Request.Form["draw"].FirstOrDefault();
        var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var s) ? s : 0;
        var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var l) ? l : 10;
        var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

        var sortColIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDir = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColName = Request.Form[$"columns[{sortColIndex}][data]"].FirstOrDefault();

        // ✅ Include YOK: projection zaten join yaptırır. Lazy loading burada istenmez.
        var query = _db.Vehicles
            .AsNoTracking()
            .Where(v => v.OwnerId == userId)
            .Select(v => new VehicleListItemVm
            {
                Id = v.Id,
                Make = v.Make.Name,
                Model = v.Model.Name,
                Trim = v.Trim != null ? v.Trim.Name : null,
                Year = v.Year,
                Mileage = v.Mileage,
                MileageUnit = v.MileageUnit.ToString(),
                FuelType = v.FuelType.ToString(),
                Transmission = v.Transmission.ToString(),
                BodyType = v.BodyType.ToString(),
                IsPublished = v.IsPublished,
                CoverPhotoUrl = v.Photos
                    .OrderBy(p => p.SortOrder)
                    .Where(p => p.IsCover)
                    .Select(p => p.Url)
                    .FirstOrDefault()
                    ?? v.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()
            });

        var recordsTotal = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            var sLike = $"%{searchValue}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Make, sLike) ||
                EF.Functions.Like(x.Model, sLike) ||
                (x.Trim != null && EF.Functions.Like(x.Trim, sLike)) ||
                EF.Functions.Like(x.FuelType, sLike) ||
                EF.Functions.Like(x.Transmission, sLike) ||
                EF.Functions.Like(x.BodyType, sLike) ||
                EF.Functions.Like(x.Year.ToString(), sLike) ||
                EF.Functions.Like(x.Mileage.ToString(), sLike)
            );
        }

        var recordsFiltered = await query.CountAsync();

        query = (sortColName, sortDir?.ToLowerInvariant()) switch
        {
            ("make", "desc") => query.OrderByDescending(x => x.Make),
            ("make", _) => query.OrderBy(x => x.Make),

            ("model", "desc") => query.OrderByDescending(x => x.Model),
            ("model", _) => query.OrderBy(x => x.Model),

            ("year", "desc") => query.OrderByDescending(x => x.Year),
            ("year", _) => query.OrderBy(x => x.Year),

            ("mileage", "desc") => query.OrderByDescending(x => x.Mileage),
            ("mileage", _) => query.OrderBy(x => x.Mileage),

            _ => query.OrderByDescending(x => x.Id)
        };

        var data = await query.Skip(start).Take(length).ToListAsync();

        return Json(new { draw, recordsTotal, recordsFiltered, data });
    }

    // =========================
    // DETAILS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId;

        // ✅ Lazy loading için: AsNoTracking YOK, Include YOK
        var v = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);

        if (v == null) return NotFound();

        // ✅ Photos sıralaması garanti olsun
        await _db.Entry(v)
            .Collection(x => x.Photos)
            .Query()
            .OrderBy(p => p.SortOrder)
            .LoadAsync();

        // Make/Model/Trim gibi nav'lar View içinde erişilince lazy-load olur.
        return View(v);
    }

    // =========================
    // MODAL CREATE/EDIT
    // =========================

    [HttpGet]
    public IActionResult CreateModal()
        => PartialView("_VehicleFormModal", new VehicleFormVm());

    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var userId = CurrentUserId;

        // ✅ Lazy loading için Include yok
        var v = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);

        if (v == null) return NotFound();

        // ✅ Photos sıralı yüklensin (VM dolduracağız)
        await _db.Entry(v)
            .Collection(x => x.Photos)
            .Query()
            .OrderBy(p => p.SortOrder)
            .LoadAsync();

        var vm = new VehicleFormVm
        {
            Id = v.Id,

            // ✅ free text fields
            MakeName = v.Make?.Name ?? "",
            ModelName = v.Model?.Name ?? "",
            TrimName = v.Trim?.Name,

            Year = v.Year,
            Mileage = v.Mileage,
            MileageUnit = v.MileageUnit,
            EngineLiters = v.EngineLiters,
            FuelType = v.FuelType,
            Transmission = v.Transmission,
            BodyType = v.BodyType,
            Seats = v.Seats,
            Doors = v.Doors,
            Colour = v.Colour,
            TotalOwners = v.TotalOwners,
            NctExpiry = v.NctExpiry,
            IsPublished = v.IsPublished,

            Photos = v.Photos
                .OrderBy(p => p.SortOrder)
                .Select(p => new VehiclePhotoVm
                {
                    Id = p.Id,
                    Url = p.Url,
                    SortOrder = p.SortOrder,
                    IsCover = p.IsCover
                }).ToList(),
        };

        return PartialView("_VehicleFormModal", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModal(VehicleFormVm vm)
    {
        var userId = CurrentUserId;

        // Basic validation for free text (dropdown yok artık)
        vm.MakeName = Norm(vm.MakeName);
        vm.ModelName = Norm(vm.ModelName);
        vm.TrimName = string.IsNullOrWhiteSpace(vm.TrimName) ? null : Norm(vm.TrimName);

        if (string.IsNullOrWhiteSpace(vm.MakeName))
            ModelState.AddModelError(nameof(vm.MakeName), "Make is required.");

        if (string.IsNullOrWhiteSpace(vm.ModelName))
            ModelState.AddModelError(nameof(vm.ModelName), "Model is required.");

        if (!ModelState.IsValid)
            return PartialView("_VehicleFormModal", vm);

        var make = await GetOrCreateMakeAsync(vm.MakeName);
        var model = await GetOrCreateModelAsync(make.Id, vm.ModelName);
        var trim = await GetOrCreateTrimAsync(model.Id, vm.TrimName);

        // CREATE
        if (vm.Id is null || vm.Id <= 0)
        {
            var entity = new Vehicle
            {
                OwnerId = userId,

                MakeId = make.Id,
                ModelId = model.Id,
                TrimId = trim?.Id,

                Year = vm.Year,
                Mileage = vm.Mileage,
                MileageUnit = vm.MileageUnit,
                EngineLiters = vm.EngineLiters,
                FuelType = vm.FuelType,
                Transmission = vm.Transmission,
                BodyType = vm.BodyType,
                Seats = vm.Seats,
                Doors = vm.Doors,
                Colour = vm.Colour,
                TotalOwners = vm.TotalOwners,
                NctExpiry = vm.NctExpiry,
                IsPublished = vm.IsPublished
            };

            _db.Vehicles.Add(entity);
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // UPDATE
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == vm.Id.Value && x.OwnerId == userId);
        if (v == null) return NotFound();

        v.MakeId = make.Id;
        v.ModelId = model.Id;
        v.TrimId = trim?.Id;

        v.Year = vm.Year;
        v.Mileage = vm.Mileage;
        v.MileageUnit = vm.MileageUnit;

        v.EngineLiters = vm.EngineLiters;
        v.FuelType = vm.FuelType;
        v.Transmission = vm.Transmission;
        v.BodyType = vm.BodyType;

        v.Seats = vm.Seats;
        v.Doors = vm.Doors;
        v.Colour = vm.Colour;

        v.TotalOwners = vm.TotalOwners;
        v.NctExpiry = vm.NctExpiry;

        v.IsPublished = vm.IsPublished;
        v.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    // =========================
    // PAGE CREATE/EDIT
    // =========================

    [HttpGet]
    public IActionResult Create()
        => View(new VehicleFormVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleFormVm vm)
    {
        var userId = CurrentUserId;

        vm.MakeName = Norm(vm.MakeName);
        vm.ModelName = Norm(vm.ModelName);
        vm.TrimName = string.IsNullOrWhiteSpace(vm.TrimName) ? null : Norm(vm.TrimName);

        if (string.IsNullOrWhiteSpace(vm.MakeName))
            ModelState.AddModelError(nameof(vm.MakeName), "Make is required.");

        if (string.IsNullOrWhiteSpace(vm.ModelName))
            ModelState.AddModelError(nameof(vm.ModelName), "Model is required.");

        if (!ModelState.IsValid)
            return View(vm);

        var make = await GetOrCreateMakeAsync(vm.MakeName);
        var model = await GetOrCreateModelAsync(make.Id, vm.ModelName);
        var trim = await GetOrCreateTrimAsync(model.Id, vm.TrimName);

        var entity = new Vehicle
        {
            OwnerId = userId,

            MakeId = make.Id,
            ModelId = model.Id,
            TrimId = trim?.Id,

            Year = vm.Year,
            Mileage = vm.Mileage,
            MileageUnit = vm.MileageUnit,
            EngineLiters = vm.EngineLiters,
            FuelType = vm.FuelType,
            Transmission = vm.Transmission,
            BodyType = vm.BodyType,
            Seats = vm.Seats,
            Doors = vm.Doors,
            Colour = vm.Colour,
            TotalOwners = vm.TotalOwners,
            NctExpiry = vm.NctExpiry,
            IsPublished = vm.IsPublished
        };

        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = CurrentUserId;

        // ✅ Lazy loading için Include yok
        var v = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);

        if (v == null) return NotFound();

        var vm = new VehicleFormVm
        {
            Id = v.Id,

            MakeName = v.Make?.Name ?? "",
            ModelName = v.Model?.Name ?? "",
            TrimName = v.Trim?.Name,

            Year = v.Year,
            Mileage = v.Mileage,
            MileageUnit = v.MileageUnit,
            EngineLiters = v.EngineLiters,
            FuelType = v.FuelType,
            Transmission = v.Transmission,
            BodyType = v.BodyType,
            Seats = v.Seats,
            Doors = v.Doors,
            Colour = v.Colour,
            TotalOwners = v.TotalOwners,
            NctExpiry = v.NctExpiry,
            IsPublished = v.IsPublished
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleFormVm vm)
    {
        var userId = CurrentUserId;

        if (id != vm.Id) return BadRequest();

        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (v == null) return NotFound();

        vm.MakeName = Norm(vm.MakeName);
        vm.ModelName = Norm(vm.ModelName);
        vm.TrimName = string.IsNullOrWhiteSpace(vm.TrimName) ? null : Norm(vm.TrimName);

        if (string.IsNullOrWhiteSpace(vm.MakeName))
            ModelState.AddModelError(nameof(vm.MakeName), "Make is required.");

        if (string.IsNullOrWhiteSpace(vm.ModelName))
            ModelState.AddModelError(nameof(vm.ModelName), "Model is required.");

        if (!ModelState.IsValid)
            return View(vm);

        var make = await GetOrCreateMakeAsync(vm.MakeName);
        var model = await GetOrCreateModelAsync(make.Id, vm.ModelName);
        var trim = await GetOrCreateTrimAsync(model.Id, vm.TrimName);

        v.MakeId = make.Id;
        v.ModelId = model.Id;
        v.TrimId = trim?.Id;

        v.Year = vm.Year;
        v.Mileage = vm.Mileage;
        v.MileageUnit = vm.MileageUnit;
        v.EngineLiters = vm.EngineLiters;
        v.FuelType = vm.FuelType;
        v.Transmission = vm.Transmission;
        v.BodyType = vm.BodyType;
        v.Seats = vm.Seats;
        v.Doors = vm.Doors;
        v.Colour = vm.Colour;
        v.TotalOwners = vm.TotalOwners;
        v.NctExpiry = vm.NctExpiry;
        v.IsPublished = vm.IsPublished;

        v.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // DELETE
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;

        var v = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);

        if (v == null) return NotFound();

        // ✅ Photos yüklü değilse explicit load
        await _db.Entry(v).Collection(x => x.Photos).LoadAsync();

        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // =========================
    // PHOTOS
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhotos(int vehicleId, List<IFormFile> files)
    {
        var userId = CurrentUserId;

        if (files == null || files.Count == 0)
            return RedirectToAction(nameof(Edit), new { id = vehicleId });

        if (files.Count > 10)
        {
            TempData["PhotoError"] = "You can upload maximum 10 photos at once.";
            return RedirectToAction(nameof(Edit), new { id = vehicleId });
        }

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);

        if (vehicle == null) return NotFound();

        // ✅ Photos koleksiyonunu yöneteceğiz => explicit load
        await _db.Entry(vehicle).Collection(v => v.Photos).LoadAsync();

        var uploadRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles", vehicleId.ToString());
        Directory.CreateDirectory(uploadRoot);

        // 1) overflow varsa en eski kadar sil (large + medium + thumb)
        var currentPhotos = vehicle.Photos.OrderBy(p => p.SortOrder).ToList();
        var totalAfter = currentPhotos.Count + files.Count;
        var overflow = totalAfter - 10;

        if (overflow > 0)
        {
            var toDelete = currentPhotos.Take(overflow).ToList();
            foreach (var p in toDelete)
            {
                DeleteImageVariantsFromDisk(p.Url);
                _db.VehiclePhotos.Remove(p);
                vehicle.Photos.Remove(p);
            }
            await _db.SaveChangesAsync();
        }

        // 2) ekle
        var nextSort = vehicle.Photos.Any()
            ? vehicle.Photos.Max(p => p.SortOrder) + 1
            : 0;

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;
            if (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.StartsWith("image/"))
                continue;

            var baseName = $"{Guid.NewGuid():N}";
            var largeFileName = $"{baseName}_large.jpg";
            var mediumFileName = $"{baseName}_medium.jpg";
            var thumbFileName = $"{baseName}_thumb.jpg";

            var largePath = Path.Combine(uploadRoot, largeFileName);
            var mediumPath = Path.Combine(uploadRoot, mediumFileName);
            var thumbPath = Path.Combine(uploadRoot, thumbFileName);

            try
            {
                await using var input = file.OpenReadStream();
                using var img = await ImageSharpImage.LoadAsync(input);

                img.Mutate(x => x.AutoOrient());

                using (var large = img.Clone(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1600, 1600)
                })))
                {
                    await large.SaveAsJpegAsync(largePath, new JpegEncoder { Quality = 85 });
                }

                using (var medium = img.Clone(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(900, 900)
                })))
                {
                    await medium.SaveAsJpegAsync(mediumPath, new JpegEncoder { Quality = 80 });
                }

                using (var thumb = img.Clone(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(400, 300)
                })))
                {
                    await thumb.SaveAsJpegAsync(thumbPath, new JpegEncoder { Quality = 75 });
                }
            }
            catch
            {
                SafeDeleteFile(largePath);
                SafeDeleteFile(mediumPath);
                SafeDeleteFile(thumbPath);
                continue;
            }

            var urlLarge = $"/uploads/vehicles/{vehicleId}/{largeFileName}";

            var photo = new VehiclePhoto
            {
                VehicleId = vehicleId,
                Url = urlLarge,
                SortOrder = nextSort++,
                IsCover = false
            };

            _db.VehiclePhotos.Add(photo);
            vehicle.Photos.Add(photo);
        }

        // 3) cover garanti
        if (!vehicle.Photos.Any(p => p.IsCover) && vehicle.Photos.Any())
        {
            var first = vehicle.Photos.OrderBy(p => p.SortOrder).First();
            first.IsCover = true;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = vehicleId });

        // ---- local helpers ----
        void DeleteImageVariantsFromDisk(string? urlLarge)
        {
            if (string.IsNullOrWhiteSpace(urlLarge)) return;

            var relative = urlLarge.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var largePhysical = Path.Combine(wwwroot, relative);

            SafeDeleteFile(largePhysical);
            SafeDeleteFile(largePhysical.Replace("_large.jpg", "_medium.jpg"));
            SafeDeleteFile(largePhysical.Replace("_large.jpg", "_thumb.jpg"));
        }

        void SafeDeleteFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int vehicleId)
    {
        var userId = CurrentUserId;

        var isOwner = await _db.Vehicles.AsNoTracking()
            .AnyAsync(v => v.Id == vehicleId && v.OwnerId == userId);

        if (!isOwner) return NotFound();

        var photo = await _db.VehiclePhotos.FirstOrDefaultAsync(p => p.Id == id && p.VehicleId == vehicleId);
        if (photo == null) return NotFound();

        var largePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            photo.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

        SafeDeleteFile(largePath);
        SafeDeleteFile(largePath.Replace("_large.jpg", "_medium.jpg"));
        SafeDeleteFile(largePath.Replace("_large.jpg", "_thumb.jpg"));

        var wasCover = photo.IsCover;

        _db.VehiclePhotos.Remove(photo);
        await _db.SaveChangesAsync();

        if (wasCover)
        {
            var remaining = await _db.VehiclePhotos
                .Where(p => p.VehicleId == vehicleId)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            if (remaining.Any())
            {
                remaining.ForEach(p => p.IsCover = false);
                remaining.First().IsCover = true;
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Edit), new { id = vehicleId });

        static void SafeDeleteFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(int id, int vehicleId)
    {
        var userId = CurrentUserId;

        var isOwner = await _db.Vehicles.AsNoTracking()
            .AnyAsync(v => v.Id == vehicleId && v.OwnerId == userId);

        if (!isOwner) return NotFound();

        var photos = await _db.VehiclePhotos
            .Where(p => p.VehicleId == vehicleId)
            .ToListAsync();

        if (!photos.Any()) return RedirectToAction(nameof(Edit), new { id = vehicleId });

        foreach (var p in photos)
            p.IsCover = (p.Id == id);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }

    // =========================
    // GET OR CREATE HELPERS
    // =========================
    private async Task<Make> GetOrCreateMakeAsync(string makeName)
    {
        makeName = Norm(makeName);

        var make = await _db.Makes.FirstOrDefaultAsync(x => x.Name == makeName);
        if (make != null) return make;

        make = new Make { Name = makeName };
        _db.Makes.Add(make);
        await _db.SaveChangesAsync();
        return make;
    }

    private async Task<VehicleModel> GetOrCreateModelAsync(int makeId, string modelName)
    {
        modelName = Norm(modelName);

        var model = await _db.Models.FirstOrDefaultAsync(x => x.MakeId == makeId && x.Name == modelName);
        if (model != null) return model;

        model = new VehicleModel { MakeId = makeId, Name = modelName };
        _db.Models.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    // ✅ alias kullanıldı (VehicleTrim)
    private async Task<VehicleTrim?> GetOrCreateTrimAsync(int modelId, string? trimName)
    {
        trimName = string.IsNullOrWhiteSpace(trimName) ? null : Norm(trimName);

        if (string.IsNullOrWhiteSpace(trimName))
            return null;

        var trim = await _db.Trims.FirstOrDefaultAsync(x => x.ModelId == modelId && x.Name == trimName);
        if (trim != null) return trim;

        trim = new VehicleTrim { ModelId = modelId, Name = trimName };
        _db.Trims.Add(trim);
        await _db.SaveChangesAsync();
        return trim;
    }
}
