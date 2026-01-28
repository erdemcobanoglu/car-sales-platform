using System.ComponentModel.DataAnnotations;
using Presentation.Models.Enums;

namespace Presentation.ViewModels.Vehicles;

public class VehicleFormVm
{
    public int? Id { get; set; }

    // ✅ Artık textbox ile alınacak (zorunlu)
    [Required, MaxLength(80)]
    public string MakeName { get; set; } = "";

    [Required, MaxLength(80)]
    public string ModelName { get; set; } = "";

    [MaxLength(80)]
    public string? TrimName { get; set; }

    // ✅ DB tarafında yine FK ile gidiyoruz, ama kullanıcı doldurmayacak.
    // Controller GetOrCreate ile set ediyor. Validationsız bırak.
    public int MakeId { get; set; }
    public int ModelId { get; set; }
    public int? TrimId { get; set; }

    [Range(1950, 2100)]
    public int Year { get; set; }

    [Range(0, 2_000_000)]
    public int Mileage { get; set; }

    public MileageUnit MileageUnit { get; set; } = MileageUnit.Miles;

    [Range(0.5, 10.0)]
    public decimal EngineLiters { get; set; }

    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyType BodyType { get; set; }

    [Range(1, 12)]
    public int Seats { get; set; }

    [Range(1, 10)]
    public int Doors { get; set; }

    [MaxLength(60)]
    public string Colour { get; set; } = "";

    [Range(0, 50)]
    public int TotalOwners { get; set; }

    public DateOnly? NctExpiry { get; set; }

    [Range(0, 999999999)]
    public decimal Price { get; set; }

    public bool IsPublished { get; set; }

    public List<VehiclePhotoVm> Photos { get; set; } = new();
}
