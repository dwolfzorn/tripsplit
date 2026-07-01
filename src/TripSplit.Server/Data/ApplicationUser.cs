using Microsoft.AspNetCore.Identity;

namespace TripSplit.Server.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
