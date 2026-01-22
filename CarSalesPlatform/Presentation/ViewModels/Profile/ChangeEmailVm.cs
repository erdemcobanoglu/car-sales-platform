using System.ComponentModel.DataAnnotations;

namespace Presentation.ViewModels.Profile;

public class ChangeEmailVm
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = "";
}
