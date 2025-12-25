using Microsoft.EntityFrameworkCore;
using Presentation.Models;
using Presentation.Models.Enums;

namespace Presentation.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1) Lookup seed (Make/Model/Trim)
        if (!await db.Makes.AnyAsync())
        {
            var ford = new Make { Name = "Ford" };
            var smax = new VehicleModel { Name = "S-Max", Make = ford };
            var zetec = new Trim { Name = "Zetec", Level = "Base Trim", Model = smax };

            db.Makes.Add(ford);
            db.Models.Add(smax);
            db.Trims.Add(zetec);

            await db.SaveChangesAsync();
        }

        // Lookup'ları DB'den çek (seed daha önce basılmış olabilir)
        var fordMake = await db.Makes.FirstOrDefaultAsync(x => x.Name == "Ford");
        var smaxModel = await db.Models
            .Include(m => m.Make)
            .FirstOrDefaultAsync(x => x.Name == "S-Max" && x.Make.Name == "Ford");
        var zetecTrim = await db.Trims
            .Include(t => t.Model)
            .FirstOrDefaultAsync(x => x.Name == "Zetec" && x.Model.Name == "S-Max");

        // 2) Vehicle seed (ayrı kontrol)
        if (!await db.Vehicles.AnyAsync())
        {
            if (fordMake is null || smaxModel is null) return;

            var vehicle = new Vehicle
            {
                MakeId = fordMake.Id,
                ModelId = smaxModel.Id,
                TrimId = zetecTrim?.Id,

                Year = 2017,
                Mileage = 91344,
                MileageUnit = MileageUnit.Miles,

                EngineLiters = 2.0,
                FuelType = FuelType.Diesel,
                Transmission = TransmissionType.Automatic,
                BodyType = BodyType.MPV,

                Seats = 7,
                Doors = 5,
                Colour = "Silver",

                TotalOwners = 2,
                NctExpiry = new DateOnly(2025, 11, 1)
            };

            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
        }
    }
}
