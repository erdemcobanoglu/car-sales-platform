using System.ComponentModel.DataAnnotations;
using Presentation.Models;
using Presentation.Models.Enums;

namespace Presentation.ViewModels.Vehicles;

public class VehicleFormVm
{
    public int? Id { get; set; }

    [Required] public int MakeId { get; set; }
    [Required] public int ModelId { get; set; }
    public int? TrimId { get; set; }

    [Range(1950, 2100)] public int Year { get; set; }
    [Range(0, 2_000_000)] public int Mileage { get; set; }
    public MileageUnit MileageUnit { get; set; } = MileageUnit.Miles;

    [Range(0.5, 10.0)] public double EngineLiters { get; set; }
    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyType BodyType { get; set; }

    [Range(1, 12)] public int Seats { get; set; }
    [Range(1, 10)] public int Doors { get; set; }

    [MaxLength(60)] public string Colour { get; set; } = "";

    [Range(0, 50)] public int TotalOwners { get; set; }
    public DateOnly? NctExpiry { get; set; }
    public List<VehiclePhotoVm> Photos { get; set; } = new();

}
