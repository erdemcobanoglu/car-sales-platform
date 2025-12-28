using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Models;
using Presentation.ViewModels.Vehicles;

namespace Presentation.Controllers;

public class VehiclesController : Controller
{
    private readonly AppDbContext _db;

    public VehiclesController(AppDbContext db)
    {
        _db = db;
    }

    // LIST PAGE
    [HttpGet]
    public IActionResult Index() => View();

    // DataTables server-side endpoint
    [HttpPost]
    public async Task<IActionResult> Datatable()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var s) ? s : 0;
        var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var l) ? l : 10;
        var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

        var sortColIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDir = Request.Form["order[0][dir]"].FirstOrDefault();
        var sortColName = Request.Form[$"columns[{sortColIndex}][data]"].FirstOrDefault();

        var query = _db.Vehicles
            .AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Trim)
            .Include(v => v.Photos)
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

                // ✅ NEW: publish status
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
            // SQL LIKE için %...%
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

        return Json(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data
        });
    }

    // DETAILS (optional)
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var v = await _db.Vehicles
            .AsNoTracking()
            .Include(x => x.Make)
            .Include(x => x.Model)
            .Include(x => x.Trim)
            .Include(x => x.Photos.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (v == null) return NotFound();
        return View(v);
    }

    // =========================
    // MODAL CREATE/EDIT (NEW)
    // =========================

    [HttpGet]
    public async Task<IActionResult> CreateModal()
    {
        await FillLookups();
        return PartialView("_VehicleFormModal", new VehicleFormVm());
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var v = await _db.Vehicles.Include(x => x.Photos.OrderBy(p => p.SortOrder))
                                  .FirstOrDefaultAsync(x => x.Id == id);

        if (v == null) return NotFound();

        var vm = new VehicleFormVm
        {
            Id = v.Id,
            MakeId = v.MakeId,
            ModelId = v.ModelId,
            TrimId = v.TrimId,
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

            // ✅ NEW
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

        await FillLookups(vm.MakeId, vm.ModelId, vm.TrimId);
        return PartialView("_VehicleFormModal", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModal(VehicleFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await FillLookups(vm.MakeId, vm.ModelId, vm.TrimId);
            return PartialView("_VehicleFormModal", vm);
        }

        // CREATE
        if (vm.Id is null || vm.Id <= 0)
        {
            var entity = new Vehicle
            {
                MakeId = vm.MakeId,
                ModelId = vm.ModelId,
                TrimId = vm.TrimId,

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

                // ✅ NEW
                IsPublished = vm.IsPublished
            };

            _db.Vehicles.Add(entity);
            await _db.SaveChangesAsync();

            return Json(new { ok = true });
        }

        // UPDATE
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == vm.Id.Value);
        if (v == null) return NotFound();

        v.MakeId = vm.MakeId;
        v.ModelId = vm.ModelId;
        v.TrimId = vm.TrimId;

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

        // ✅ NEW
        v.IsPublished = vm.IsPublished;

        v.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // Dependent dropdown endpoints (NEW)
    [HttpGet]
    public async Task<IActionResult> GetModels(int makeId)
    {
        var items = await _db.Models.AsNoTracking()
            .Where(x => x.MakeId == makeId)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetTrims(int modelId)
    {
        var items = await _db.Trims.AsNoTracking()
            .Where(x => x.ModelId == modelId)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Json(items);
    }

    // =========================
    // PAGE CREATE/EDIT (OLD)
    // (istersen sonra sileriz)
    // =========================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FillLookups();
        return View(new VehicleFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await FillLookups(vm.MakeId, vm.ModelId, vm.TrimId);
            return View(vm);
        }

        var entity = new Vehicle
        {
            MakeId = vm.MakeId,
            ModelId = vm.ModelId,
            TrimId = vm.TrimId,

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

            // ✅ NEW
            IsPublished = vm.IsPublished
        };

        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound();

        var vm = new VehicleFormVm
        {
            Id = v.Id,
            MakeId = v.MakeId,
            ModelId = v.ModelId,
            TrimId = v.TrimId,
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

            // ✅ NEW
            IsPublished = v.IsPublished
        };

        await FillLookups(vm.MakeId, vm.ModelId, vm.TrimId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleFormVm vm)
    {
        if (id != vm.Id) return BadRequest();

        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await FillLookups(vm.MakeId, vm.ModelId, vm.TrimId);
            return View(vm);
        }

        v.MakeId = vm.MakeId;
        v.ModelId = vm.ModelId;
        v.TrimId = vm.TrimId;

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

        // ✅ NEW
        v.IsPublished = vm.IsPublished;

        v.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // DELETE (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await _db.Vehicles
            .Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (v == null) return NotFound();

        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    // --- Lookups ---
    private async Task FillLookups(int? makeId = null, int? modelId = null, int? trimId = null)
    {
        var makes = await _db.Makes.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        ViewBag.Makes = new SelectList(makes, "Id", "Name", makeId);

        var modelsQuery = _db.Models.AsNoTracking().OrderBy(x => x.Name).AsQueryable();
        if (makeId.HasValue) modelsQuery = modelsQuery.Where(x => x.MakeId == makeId.Value);
        var models = await modelsQuery.ToListAsync();
        ViewBag.Models = new SelectList(models, "Id", "Name", modelId);

        var trimsQuery = _db.Trims.AsNoTracking().OrderBy(x => x.Name).AsQueryable();
        if (modelId.HasValue) trimsQuery = trimsQuery.Where(x => x.ModelId == modelId.Value);
        var trims = await trimsQuery.ToListAsync();
        ViewBag.Trims = new SelectList(trims, "Id", "Name", trimId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhotos(int vehicleId, List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return RedirectToAction(nameof(Edit), new { id = vehicleId });

        if (files.Count > 10)
        {
            TempData["PhotoError"] = "You can upload maximum 10 photos at once.";
            return RedirectToAction(nameof(Edit), new { id = vehicleId });
        }

        var vehicle = await _db.Vehicles
            .Include(v => v.Photos)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle == null) return NotFound();

        var uploadRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles", vehicleId.ToString());
        Directory.CreateDirectory(uploadRoot);

        // 1) Taşma varsa: aşan kadar en eski fotoğrafları sil
        var currentPhotos = vehicle.Photos
            .OrderBy(p => p.SortOrder)
            .ToList();

        var totalAfter = currentPhotos.Count + files.Count;
        var overflow = totalAfter - 10;

        if (overflow > 0)
        {
            var toDelete = currentPhotos.Take(overflow).ToList();

            foreach (var p in toDelete)
            {
                var relative = p.Url?.TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString());

                if (!string.IsNullOrWhiteSpace(relative))
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                _db.VehiclePhotos.Remove(p);
                vehicle.Photos.Remove(p);
            }

            await _db.SaveChangesAsync();
        }

        // 2) Yeni fotoğrafları ekle
        var nextSort = vehicle.Photos.Any() ? vehicle.Photos.Max(p => p.SortOrder) + 1 : 0;

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;
            if (!file.ContentType.StartsWith("image/")) continue;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadRoot, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/vehicles/{vehicleId}/{fileName}";

            var photo = new VehiclePhoto
            {
                VehicleId = vehicleId,
                Url = url,
                SortOrder = nextSort++,
                IsCover = false
            };

            _db.VehiclePhotos.Add(photo);
            vehicle.Photos.Add(photo);
        }

        // 3) Cover garanti: hiç cover yoksa ilk foto cover olsun
        if (!vehicle.Photos.Any(p => p.IsCover) && vehicle.Photos.Any())
        {
            var first = vehicle.Photos.OrderBy(p => p.SortOrder).First();
            first.IsCover = true;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int vehicleId)
    {
        var photo = await _db.VehiclePhotos.FirstOrDefaultAsync(p => p.Id == id && p.VehicleId == vehicleId);
        if (photo == null) return NotFound();

        // dosyayı da sil
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            photo.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        var wasCover = photo.IsCover;

        _db.VehiclePhotos.Remove(photo);
        await _db.SaveChangesAsync();

        // cover silindiyse yeni cover ata
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
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(int id, int vehicleId)
    {
        var photos = await _db.VehiclePhotos
            .Where(p => p.VehicleId == vehicleId)
            .ToListAsync();

        if (!photos.Any()) return RedirectToAction(nameof(Edit), new { id = vehicleId });

        foreach (var p in photos)
            p.IsCover = (p.Id == id);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }
}
