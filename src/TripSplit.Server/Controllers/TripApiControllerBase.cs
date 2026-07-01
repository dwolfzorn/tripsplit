using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Repositories;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Controllers;

public abstract class TripApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // Loads the caller's role on a trip and applies the given predicate;
    // returns the role so callers can also use it (e.g. to allow self-removal).
    protected async Task<(bool Authorized, TripRole? Role)> AuthorizeTripRoleAsync(
        ITripRepository repository, Guid tripId, Func<TripRole?, bool> isAuthorized, CancellationToken ct)
    {
        var role = await repository.GetRoleAsync(tripId, CurrentUserId, ct);
        return (isAuthorized(role), role);
    }
}
