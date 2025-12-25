namespace Presentation.ViewModels.Vehicles;

public class VehicleListItemVm
{
    public int Id { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string? Trim { get; set; }

    public int Year { get; set; }
    public int Mileage { get; set; }
    public string MileageUnit { get; set; } = "Miles";

    public string FuelType { get; set; } = "";
    public string Transmission { get; set; } = "";
    public string BodyType { get; set; } = "";

    public string? CoverPhotoUrl { get; set; }
}
