using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class Make
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = "";

    public virtual ICollection<VehicleModel> Models { get; set; } = new List<VehicleModel>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

// test