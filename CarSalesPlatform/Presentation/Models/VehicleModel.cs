
using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class VehicleModel
{
    public int Id { get; set; }

    public int MakeId { get; set; }
    public Make Make { get; set; } = null!;

    [MaxLength(120)]
    public string Name { get; set; } = "";
     


    public ICollection<Trim> Trims { get; set; } = new List<Trim>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
