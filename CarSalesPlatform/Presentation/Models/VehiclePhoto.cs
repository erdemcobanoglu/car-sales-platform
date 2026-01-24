using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class VehiclePhoto
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public virtual Vehicle Vehicle { get; set; } = null!;

    [MaxLength(500)]
    public string Url { get; set; } = "";   // Foto URL / path (wwwroot/uploads/..)

    public int SortOrder { get; set; }      // 0..9 (sıralama)

    public bool IsCover { get; set; }       // kapak foto (opsiyonel)

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
