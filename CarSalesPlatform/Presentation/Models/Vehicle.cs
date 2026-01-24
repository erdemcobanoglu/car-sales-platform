using System.ComponentModel.DataAnnotations;
using Presentation.Models.Enums;

namespace Presentation.Models;

public class Vehicle
{
    public int Id { get; set; }

    public int MakeId { get; set; }
    public virtual Make Make { get; set; } = null!;

    public int ModelId { get; set; }
    public virtual VehicleModel Model { get; set; } = null!;

    public int? TrimId { get; set; }
    public virtual Trim? Trim { get; set; }

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
    public string Colour { get; set; } = string.Empty;

    public int TotalOwners { get; set; }
    public DateOnly? NctExpiry { get; set; }

    // 🔹 İLAN YAYIN DURUMU
    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Identity user FK
    public string OwnerId { get; set; } = null!;
    public virtual ApplicationUser Owner { get; set; } = null!;

    public virtual ICollection<VehiclePhoto> Photos { get; set; } = new List<VehiclePhoto>();
}
