using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class Trim
{
    public int Id { get; set; }

    public int ModelId { get; set; }
    public VehicleModel Model { get; set; } = null!;

    [MaxLength(120)]
    public string Name { get; set; } = "";   // Zetec

    [MaxLength(120)]
    public string? Level { get; set; }       // Base Trim

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

