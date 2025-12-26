using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.ViewModels.Vehicles;

namespace Presentation.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Home/Index   (veya / default route'ta Home/Index)
        [HttpGet]
        public IActionResult Index() => View();

        // POST: /Home/Datatable
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

        // GET: /Home/Details/5
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
    }
}
