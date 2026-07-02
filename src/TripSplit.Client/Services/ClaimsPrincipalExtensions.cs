using System.Security.Claims;

namespace TripSplit.Client.Services;

public static class ClaimsPrincipalExtensions
{
    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        var first = user.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
        var last = user.FindFirst(ClaimTypes.Surname)?.Value ?? "";
        var name = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Identity?.Name ?? "" : name;
    }
}
