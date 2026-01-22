using System.ComponentModel.DataAnnotations;

namespace Presentation.ViewModels.Profile
{
    public class ProfileVm
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";

        [MaxLength(120)]
        public string? FullName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? City { get; set; }
    }
}
