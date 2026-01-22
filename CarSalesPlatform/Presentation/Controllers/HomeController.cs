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

        #region olad v1
        // ✅ DataTables endpoint (supports guest search q)
        //[HttpPost]
        //public async Task<IActionResult> Datatable(string? q)
        //{
        //    var draw = Request.Form["draw"].FirstOrDefault();
        //    var start = int.TryParse(Request.Form["start"].FirstOrDefault(), out var s) ? s : 0;
        //    var length = int.TryParse(Request.Form["length"].FirstOrDefault(), out var l) ? l : 10;
        //    var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

        //    var sortColIndex = Request.Form["order[0][column]"].FirstOrDefault();
        //    var sortDir = Request.Form["order[0][dir]"].FirstOrDefault();
        //    var sortColName = Request.Form[$"columns[{sortColIndex}][data]"].FirstOrDefault();

        //    var query = _db.Vehicles
        //        .AsNoTracking()
        //        .Where(v => v.IsPublished)
        //        .Include(v => v.Make)
        //        .Include(v => v.Model)
        //        .Include(v => v.Trim)
        //        .Include(v => v.Photos)
        //        .Select(v => new VehicleListItemVm
        //        {
        //            Id = v.Id,
        //            Make = v.Make.Name,
        //            Model = v.Model.Name,
        //            Trim = v.Trim != null ? v.Trim.Name : null,
        //            Year = v.Year,
        //            Mileage = v.Mileage,
        //            MileageUnit = v.MileageUnit.ToString(),
        //            FuelType = v.FuelType.ToString(),
        //            Transmission = v.Transmission.ToString(),
        //            BodyType = v.BodyType.ToString(),
        //            CoverPhotoUrl = v.Photos
        //                .OrderBy(p => p.SortOrder)
        //                .Where(p => p.IsCover)
        //                .Select(p => p.Url)
        //                .FirstOrDefault()
        //                ?? v.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()
        //        });

        //    // ✅ Apply navbar search (q) first
        //    q = q?.Trim();
        //    if (!string.IsNullOrWhiteSpace(q))
        //    {
        //        var like = $"%{q}%";
        //        query = query.Where(x =>
        //            EF.Functions.Like(x.Make, like) ||
        //            EF.Functions.Like(x.Model, like) ||
        //            (x.Trim != null && EF.Functions.Like(x.Trim, like)) ||
        //            EF.Functions.Like(x.FuelType, like) ||
        //            EF.Functions.Like(x.Transmission, like) ||
        //            EF.Functions.Like(x.BodyType, like) ||
        //            EF.Functions.Like(x.Year.ToString(), like) ||
        //            EF.Functions.Like(x.Mileage.ToString(), like)
        //        );
        //    }

        //    var recordsTotal = await query.CountAsync();

        //    // ✅ Apply DataTables search as additional filter
        //    if (!string.IsNullOrWhiteSpace(searchValue))
        //    {
        //        var sLike = $"%{searchValue}%";
        //        query = query.Where(x =>
        //            EF.Functions.Like(x.Make, sLike) ||
        //            EF.Functions.Like(x.Model, sLike) ||
        //            (x.Trim != null && EF.Functions.Like(x.Trim, sLike)) ||
        //            EF.Functions.Like(x.FuelType, sLike) ||
        //            EF.Functions.Like(x.Transmission, sLike) ||
        //            EF.Functions.Like(x.BodyType, sLike) ||
        //            EF.Functions.Like(x.Year.ToString(), sLike) ||
        //            EF.Functions.Like(x.Mileage.ToString(), sLike)
        //        );
        //    }

        //    var recordsFiltered = await query.CountAsync();

        //    query = (sortColName, sortDir?.ToLowerInvariant()) switch
        //    {
        //        ("make", "desc") => query.OrderByDescending(x => x.Make),
        //        ("make", _) => query.OrderBy(x => x.Make),

        //        ("model", "desc") => query.OrderByDescending(x => x.Model),
        //        ("model", _) => query.OrderBy(x => x.Model),

        //        ("year", "desc") => query.OrderByDescending(x => x.Year),
        //        ("year", _) => query.OrderBy(x => x.Year),

        //        ("mileage", "desc") => query.OrderByDescending(x => x.Mileage),
        //        ("mileage", _) => query.OrderBy(x => x.Mileage),

        //        _ => query.OrderByDescending(x => x.Id)
        //    };

        //    var data = await query.Skip(start).Take(length).ToListAsync();

        //    foreach (var item in data)
        //    {
        //        if (!string.IsNullOrWhiteSpace(item.CoverPhotoUrl))
        //            item.CoverPhotoUrl = item.CoverPhotoUrl.Replace("_large.jpg", "_medium.jpg");
        //    }

        //    return Json(new { draw, recordsTotal, recordsFiltered, data });
        //}

        #endregion

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
                .Include(v => v.Make)
                .Include(v => v.Model)
                .Include(v => v.Trim)
                .Include(v => v.Photos)
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

            // ✅ Sorting (entity level)
            baseQuery = (sortColName, sortDir?.ToLowerInvariant()) switch
            {
                ("make", "desc") => baseQuery.OrderByDescending(x => x.Make.Name),
                ("make", _) => baseQuery.OrderBy(x => x.Make.Name),

                ("model", "desc") => baseQuery.OrderByDescending(x => x.Model.Name),
                ("model", _) => baseQuery.OrderBy(x => x.Model.Name),

                ("year", "desc") => baseQuery.OrderByDescending(x => x.Year),
                ("year", _) => baseQuery.OrderBy(x => x.Year),

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
                })
                .ToListAsync();

            // ✅ medium photo in list
            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.CoverPhotoUrl))
                    item.CoverPhotoUrl = item.CoverPhotoUrl.Replace("_large.jpg", "_medium.jpg");
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
                    // text fields
                    EF.Functions.Like(v.Make.Name, like) ||
                    EF.Functions.Like(v.Model.Name, like) ||
                    (v.Trim != null && EF.Functions.Like(v.Trim.Name, like)) ||

                    // numeric
                    (isNumber && (v.Year == number || v.Mileage == number)) ||

                    // enum exact match (SQL-safe)
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
                .AsNoTracking()
                .Where(x => x.IsPublished)
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
