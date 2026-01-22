using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class UserProfile
{
    [Key]
    public string UserId { get; set; } = null!;  // PK ve aynı zamanda FK

    public ApplicationUser User { get; set; } = null!;

    [MaxLength(120)]
    public string? FullName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? City { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
