using System.ComponentModel.DataAnnotations;

namespace Presentation.ViewModels.Profile;

public class ChangePasswordVm
{
    [Required]
    public string CurrentPassword { get; set; } = "";

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = "";

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}
