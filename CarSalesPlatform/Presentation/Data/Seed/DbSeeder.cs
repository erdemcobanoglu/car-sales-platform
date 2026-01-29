using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presentation.Models;
using Presentation.Models.Enums;

namespace Presentation.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        // 0) Seed user (Vehicles.OwnerId için kullanılacak)
        const string seedEmail = "seed@local.com";

        // Identity default policy'ye takılmasın diye güçlü şifre:
        const string seedPassword = "Seed!12345";

        var seedUser = await userManager.FindByEmailAsync(seedEmail);
        if (seedUser == null)
        {
            seedUser = new ApplicationUser
            {
                UserName = seedEmail,
                Email = seedEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(seedUser, seedPassword);
            if (!createResult.Succeeded)
                throw new Exception("Seed user oluşturulamadı: " +
                                    string.Join(" | ", createResult.Errors.Select(e => e.Description)));
        }

        var ownerId = seedUser.Id;

        // 1) Mevcut araçlarda OwnerId boşsa düzelt (idempotent)
        var badVehicles = await db.Vehicles
            .Where(v => v.OwnerId == null || v.OwnerId == "")
            .ToListAsync();

        foreach (var v in badVehicles)
            v.OwnerId = ownerId;

        if (badVehicles.Count > 0)
            await db.SaveChangesAsync();

        // Zaten araç varsa tekrar seed basma
        if (await db.Vehicles.AnyAsync())
        {
            await tx.CommitAsync();
            return;
        }

        // ===== LOOKUP SEED (Make/Model/Trim) =====
        var ford = await EnsureMake(db, "Ford");
        var vw = await EnsureMake(db, "Volkswagen");
        var bmw = await EnsureMake(db, "BMW");
        var toy = await EnsureMake(db, "Toyota");
        var audi = await EnsureMake(db, "Audi");

        var smax = await EnsureModel(db, ford, "S-Max");
        var focus = await EnsureModel(db, ford, "Focus");
        var golf = await EnsureModel(db, vw, "Golf");
        var passat = await EnsureModel(db, vw, "Passat");
        var series3 = await EnsureModel(db, bmw, "3 Series");
        var series5 = await EnsureModel(db, bmw, "5 Series");
        var corolla = await EnsureModel(db, toy, "Corolla");
        var rav4 = await EnsureModel(db, toy, "RAV4");
        var a4 = await EnsureModel(db, audi, "A4");
        var q5 = await EnsureModel(db, audi, "Q5");

        var zetec = await EnsureTrim(db, smax, "Zetec", "Base Trim");
        var titanium = await EnsureTrim(db, smax, "Titanium", "Mid Trim");
        var trend = await EnsureTrim(db, focus, "Trend", "Base Trim");
        var comfort = await EnsureTrim(db, golf, "Comfortline", "Mid Trim");
        var rline = await EnsureTrim(db, passat, "R-Line", "Sport");
        var se = await EnsureTrim(db, series3, "SE", "Base Trim");
        var msport = await EnsureTrim(db, series3, "M Sport", "Sport");
        var luxury = await EnsureTrim(db, series5, "Luxury", "Premium");
        var active = await EnsureTrim(db, rav4, "Active", "Mid Trim");
        var sline = await EnsureTrim(db, a4, "S line", "Sport");
        var sport = await EnsureTrim(db, q5, "Sport", "Mid Trim");

        await db.SaveChangesAsync();

        // ===== TEST VEHICLES =====
        var vehiclesToCreate = new List<(Make make, VehicleModel model, Trim? trim, int year, int mileage, decimal engine, FuelType fuel, TransmissionType trans, BodyType body, int seats, int doors, string colour, int owners, DateOnly? nct)>
        {
            (ford, smax, zetec,    2017,  91344, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.MPV, 7, 5, "Silver", 2, new DateOnly(2025,11,1)),
            (ford, smax, titanium, 2019,  60210, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.MPV, 7, 5, "Black",  1, new DateOnly(2026, 5,1)),
            (ford, focus, trend,   2016, 120500, 1.5m, FuelType.Petrol,  TransmissionType.Manual,    BodyType.Hatchback, 5, 5, "Blue",  3, new DateOnly(2025, 8,1)),
            (vw, golf, comfort,    2018,  84500, 1.6m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Hatchback, 5, 5, "White", 2, new DateOnly(2026, 2,1)),
            (vw, passat, rline,    2020,  45500, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "Grey",  1, new DateOnly(2026, 9,1)),
            (bmw, series3, se,     2017, 110000, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "Black", 2, new DateOnly(2025,12,1)),
            (bmw, series3, msport, 2019,  70000, 2.0m, FuelType.Petrol,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "Red",   1, new DateOnly(2026, 7,1)),
            (bmw, series5, luxury, 2018,  98000, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "Navy",  2, new DateOnly(2026, 1,1)),
            (toy, corolla, null,   2015, 150200, 1.4m, FuelType.Petrol,  TransmissionType.Manual,    BodyType.Sedan, 5, 4, "Silver",3, new DateOnly(2025, 6,1)),
            (toy, rav4, active,    2021,  39000, 2.5m, FuelType.Hybrid,  TransmissionType.Automatic, BodyType.SUV,  5, 5, "White", 1, new DateOnly(2027, 3,1)),
            (audi, a4, sline,      2019,  76000, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "Grey",  2, new DateOnly(2026, 4,1)),
            (audi, q5, sport,      2020,  52000, 2.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.SUV,  5, 5, "Black", 1, new DateOnly(2026,11,1)),
            (vw, golf, comfort,    2014, 170000, 1.2m, FuelType.Petrol,  TransmissionType.Manual,    BodyType.Hatchback, 5, 5, "Green",4, new DateOnly(2025, 4,1)),
            (ford, focus, trend,   2022,  21000, 1.0m, FuelType.Petrol,  TransmissionType.Manual,    BodyType.Hatchback, 5, 5, "Orange",1, new DateOnly(2027,10,1)),
            (bmw, series5, luxury, 2021,  33000, 3.0m, FuelType.Diesel,  TransmissionType.Automatic, BodyType.Sedan, 5, 4, "White", 1, new DateOnly(2027, 1,1)),
        };

        var createdVehicles = new List<Vehicle>();

        foreach (var v in vehiclesToCreate)
        {
            var vehicle = new Vehicle
            {
                OwnerId = ownerId, // ✅ default seed user

                MakeId = v.make.Id,
                ModelId = v.model.Id,
                TrimId = v.trim?.Id,

                Year = v.year,
                Mileage = v.mileage,
                MileageUnit = MileageUnit.Miles,

                EngineLiters = v.engine,
                FuelType = v.fuel,
                Transmission = v.trans,
                BodyType = v.body,

                Seats = v.seats,
                Doors = v.doors,
                Colour = v.colour,

                TotalOwners = v.owners,
                NctExpiry = v.nct,

                IsPublished = true
            };

            db.Vehicles.Add(vehicle);
            createdVehicles.Add(vehicle);
        }

        await db.SaveChangesAsync();

        // ===== FOTO SEED =====  
        const int photosPerVehicle = 5;
        int vehicleIndex = 1;

        foreach (var vehicle in createdVehicles)
        {
            for (int i = 0; i < photosPerVehicle; i++)
            {
                db.VehiclePhotos.Add(new VehiclePhoto
                {
                    VehicleId = vehicle.Id,
                    Url = $"/uploads/seed/v{vehicleIndex:00}_{i:00}.jpg",
                    SortOrder = i,
                    IsCover = (i == 0)
                });
            }
            vehicleIndex++;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task<Make> EnsureMake(AppDbContext db, string name)
    {
        var make = await db.Makes.FirstOrDefaultAsync(x => x.Name == name);
        if (make is not null) return make;

        make = new Make { Name = name };
        db.Makes.Add(make);
        await db.SaveChangesAsync();
        return make;
    }

    private static async Task<VehicleModel> EnsureModel(AppDbContext db, Make make, string name)
    {
        var model = await db.Models.FirstOrDefaultAsync(x => x.MakeId == make.Id && x.Name == name);
        if (model is not null) return model;

        model = new VehicleModel
        {
            MakeId = make.Id,
            Name = name
        };

        db.Models.Add(model);
        await db.SaveChangesAsync();
        return model;
    }

    private static async Task<Trim> EnsureTrim(AppDbContext db, VehicleModel model, string name, string? level)
    {
        var trim = await db.Trims.FirstOrDefaultAsync(x => x.ModelId == model.Id && x.Name == name);
        if (trim is not null) return trim;

        trim = new Trim { ModelId = model.Id, Name = name, Level = level };
        db.Trims.Add(trim);
        await db.SaveChangesAsync();
        return trim;
    }
}
