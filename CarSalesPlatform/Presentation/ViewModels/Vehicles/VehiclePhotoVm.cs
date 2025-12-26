namespace Presentation.ViewModels.Vehicles;

public class VehiclePhotoVm
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}
