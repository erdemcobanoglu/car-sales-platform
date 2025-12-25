using Presentation.Models;
using Presentation.Models.Enums;

namespace Presentation.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Lookup data zaten varsa tekrar basma
        if (db.Makes.Any())
            return;

        // ===== MAKE =====
        var ford = new Make
        {
            Name = "Ford"
        };

        // ===== MODEL =====
        var smax = new VehicleModel
        {
            Name = "S-Max",
            Make = ford
        };

        // ===== TRIM =====
        var zetec = new Trim
        {
            Name = "Zetec",
            Level = "Base Trim",
            Model = smax
        };

        db.Makes.Add(ford);
        db.Models.Add(smax);
        db.Trims.Add(zetec);

        // ===== VEHICLE (TEST DATA) =====
        var vehicle = new Vehicle
        {
            Make = ford,
            Model = smax,
            Trim = zetec,

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
