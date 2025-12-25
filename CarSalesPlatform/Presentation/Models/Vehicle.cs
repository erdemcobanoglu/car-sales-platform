using System.ComponentModel.DataAnnotations; 
using Presentation.Models.Enums;

namespace Presentation.Models;

public class Vehicle
{
    public int Id { get; set; }

    public int MakeId { get; set; }
    public Make Make { get; set; } = null!;

    public int ModelId { get; set; }
    public VehicleModel Model { get; set; } = null!;

    public int? TrimId { get; set; }
    public Trim? Trim { get; set; }

    public int Year { get; set; }
    public int Mileage { get; set; }
    public MileageUnit MileageUnit { get; set; } = MileageUnit.Miles;

    public double EngineLiters { get; set; }
    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyType BodyType { get; set; }

    public int Seats { get; set; }
    public int Doors { get; set; }

    [MaxLength(60)]
    public string Colour { get; set; } = "";

    public int TotalOwners { get; set; }
    public DateOnly? NctExpiry { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

