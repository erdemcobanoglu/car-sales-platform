namespace Presentation.ViewModels.Profile
{
    public class ProfileVm
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";

        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
    }
}
