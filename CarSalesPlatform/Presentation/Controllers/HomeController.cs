using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.Models.Enums;
using Presentation.Models;
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

        // ✅ Guest search comes here
        [HttpGet]
        public IActionResult Index(string? q)
        {
            ViewBag.Search = q?.Trim();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Datatable(string? q)
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var s) ? s : 0;
            var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var l) ? l : 10;

            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

            var sortColIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortDir = Request.Form["order[0][dir]"].FirstOrDefault();
            var sortColName = Request.Form[$"columns[{sortColIndex}][data]"].FirstOrDefault();

            // ✅ Base query (published only)
            var baseQuery = _db.Vehicles
                .AsNoTracking()
                .Where(v => v.IsPublished)
                .AsQueryable();

            // ✅ recordsTotal BEFORE any filtering
            var recordsTotal = await baseQuery.CountAsync();

            // ✅ Apply navbar search (q)
            q = q?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                ApplySearchFilter(ref baseQuery, q);
            }

            // ✅ Apply DataTables search (search box)
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                ApplySearchFilter(ref baseQuery, searchValue);
            }

            // ✅ recordsFiltered AFTER filtering
            var recordsFiltered = await baseQuery.CountAsync();

            
            baseQuery = (sortColName, sortDir?.ToLowerInvariant()) switch
            {
                ("make", "desc") => baseQuery.OrderByDescending(x => x.Make.Name),
                ("make", _) => baseQuery.OrderBy(x => x.Make.Name),

                ("model", "desc") => baseQuery.OrderByDescending(x => x.Model.Name),
                ("model", _) => baseQuery.OrderBy(x => x.Model.Name),

                ("trim", "desc") => baseQuery.OrderByDescending(x => x.Trim!.Name),
                ("trim", _) => baseQuery.OrderBy(x => x.Trim!.Name),

                ("year", "desc") => baseQuery.OrderByDescending(x => x.Year),
                ("year", _) => baseQuery.OrderBy(x => x.Year),

                ("price", "desc") => baseQuery.OrderByDescending(x => x.Price),
                ("price", _) => baseQuery.OrderBy(x => x.Price),

                ("mileage", "desc") => baseQuery.OrderByDescending(x => x.Mileage),
                ("mileage", _) => baseQuery.OrderBy(x => x.Mileage),

                _ => baseQuery.OrderByDescending(x => x.Id)
            };

            // ✅ Paging + Projection
            var data = await baseQuery
                .Skip(start)
                .Take(length)
                .Select(v => new VehicleListItemVm
                {
                    Id = v.Id,
                    Make = v.Make.Name,
                    Model = v.Model.Name,
                    Trim = v.Trim != null ? v.Trim.Name : null,
                    Year = v.Year,
                    Price = v.Price,
                    Mileage = v.Mileage,
                    MileageUnit = v.MileageUnit.ToString(),
                    FuelType = v.FuelType.ToString(),
                    Transmission = v.Transmission.ToString(),
                    BodyType = v.BodyType.ToString(),

                    // cover (önce IsCover, yoksa ilk foto)
                    CoverPhotoUrl = v.Photos
                        .OrderBy(p => p.SortOrder)
                        .Where(p => p.IsCover)
                        .Select(p => p.Url)
                        .FirstOrDefault()
                        ?? v.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault(),

                    // ✅ fallback için tüm url listesi
                    PhotoUrls = v.Photos
                        .OrderBy(p => p.SortOrder)
                        .Select(p => p.Url)
                        .ToList()
                })
                .ToListAsync();

            // ✅ medium photo in list (hem cover hem list)
            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.CoverPhotoUrl))
                    item.CoverPhotoUrl = item.CoverPhotoUrl.Replace("_large.jpg", "_medium.jpg");

                if (item.PhotoUrls != null && item.PhotoUrls.Count > 0)
                {
                    for (int i = 0; i < item.PhotoUrls.Count; i++)
                    {
                        var u = item.PhotoUrls[i];
                        if (!string.IsNullOrWhiteSpace(u))
                            item.PhotoUrls[i] = u.Replace("_large.jpg", "_medium.jpg");
                    }
                }
            }

            return Json(new { draw, recordsTotal, recordsFiltered, data });

            // -------------------------
            // Local helper (safe search)
            // -------------------------
            static void ApplySearchFilter(ref IQueryable<Vehicle> query, string term)
            {
                term = term.Trim();
                var like = $"%{term}%";

                // number search (year/mileage)
                var isNumber = int.TryParse(term, out var number);

                // enum text search (diesel/petrol/automatic/etc.)
                bool fuelParsed = Enum.TryParse<FuelType>(term, true, out var fuel);
                bool transParsed = Enum.TryParse<TransmissionType>(term, true, out var trans);
                bool bodyParsed = Enum.TryParse<BodyType>(term, true, out var body);

                query = query.Where(v =>
                    EF.Functions.Like(v.Make.Name, like) ||
                    EF.Functions.Like(v.Model.Name, like) ||
                    (v.Trim != null && EF.Functions.Like(v.Trim.Name, like)) ||

                    (isNumber && (v.Year == number || v.Mileage == number)) ||

                    (fuelParsed && v.FuelType == fuel) ||
                    (transParsed && v.Transmission == trans) ||
                    (bodyParsed && v.BodyType == body)
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var v = await _db.Vehicles
                .Where(x => x.IsPublished && x.Id == id)
                .FirstOrDefaultAsync();

            if (v == null) return NotFound();

            await _db.Entry(v)
                .Collection(x => x.Photos)
                .Query()
                .OrderBy(p => p.SortOrder)
                .LoadAsync();

            return View(v);
        }
    }
}
