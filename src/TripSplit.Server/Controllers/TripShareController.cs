using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Authorization;
using TripSplit.Server.Repositories;

namespace TripSplit.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/trips/{tripId:guid}/share")]
public class TripShareController(ITripRepository tripRepository, IShareLinkRepository shareLinkRepository) : TripApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid tripId, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(tripRepository, tripId, TripAuthorization.IsOwner, ct);
        if (!authorized) return Forbid();

        var link = await shareLinkRepository.GetOrCreateActiveAsync(tripId, CurrentUserId, ct);
        return Ok(new { token = link.Token });
    }

    [HttpPost("regenerate")]
    public async Task<IActionResult> Regenerate(Guid tripId, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(tripRepository, tripId, TripAuthorization.IsOwner, ct);
        if (!authorized) return Forbid();

        var link = await shareLinkRepository.RegenerateAsync(tripId, CurrentUserId, ct);
        return Ok(new { token = link.Token });
    }

    [HttpDelete]
    public async Task<IActionResult> Revoke(Guid tripId, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(tripRepository, tripId, TripAuthorization.IsOwner, ct);
        if (!authorized) return Forbid();

        await shareLinkRepository.RevokeAsync(tripId, ct);
        return NoContent();
    }
}
