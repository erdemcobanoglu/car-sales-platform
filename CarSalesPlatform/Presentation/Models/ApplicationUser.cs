using Microsoft.AspNetCore.Identity;

namespace Presentation.Models;

public class ApplicationUser : IdentityUser
{
    // İleride ekstra alanlar eklenebilir
    public virtual UserProfile? Profile { get; set; }
}
