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
        // DataTables standard fields
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var s) ? s : 0;
        var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var l) ? l : 10;
        var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

        // Sorting
        var sortColIndex = Request.Form["order[0][column]"].FirstOrDefault();
        var sortDir = Request.Form["order[0][dir]"].FirstOrDefault(); // asc/desc
        var sortColName = Request.Form[$"columns[{sortColIndex}][data]"].FirstOrDefault();

        // Base query
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
                CoverPhotoUrl = v.Photos
                    .OrderBy(p => p.SortOrder)
                    .Where(p => p.IsCover)
                    .Select(p => p.Url)
                    .FirstOrDefault()
                    ?? v.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()
            });

        var recordsTotal = await query.CountAsync();

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(x =>
                x.Make.Contains(searchValue) ||
                x.Model.Contains(searchValue) ||
                (x.Trim != null && x.Trim.Contains(searchValue)) ||
                x.FuelType.Contains(searchValue) ||
                x.Transmission.Contains(searchValue) ||
                x.BodyType.Contains(searchValue) ||
                x.Year.ToString().Contains(searchValue) ||
                x.Mileage.ToString().Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync();

        // Sorting (simple mapping)
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

    // CREATE
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
            NctExpiry = vm.NctExpiry
        };

        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // EDIT
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
            NctExpiry = v.NctExpiry
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
}
