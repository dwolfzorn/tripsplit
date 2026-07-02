using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Authorization;
using TripSplit.Server.Repositories;

namespace TripSplit.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/trips/{tripId:guid}/members")]
public class TripMembersController(ITripRepository repository) : TripApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid tripId, CancellationToken ct)
    {
        var role = await repository.GetRoleAsync(tripId, CurrentUserId, ct);
        if (role is null) return NotFound();

        return Ok(await repository.GetMembersAsync(tripId, ct));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid tripId, Guid userId, CancellationToken ct)
    {
        var currentRole = await repository.GetRoleAsync(tripId, CurrentUserId, ct);
        if (currentRole is null) return NotFound();

        // A member may always remove themselves ("leave trip"); removing
        // someone else requires Owner.
        if (userId != CurrentUserId && !TripAuthorization.IsOwner(currentRole)) return Forbid();

        var removed = await repository.RemoveMemberAsync(tripId, userId, ct);
        return removed ? NoContent() : BadRequest("Cannot remove the last remaining owner.");
    }

    [HttpPost("{userId:guid}/make-owner")]
    public async Task<IActionResult> MakeOwner(Guid tripId, Guid userId, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(repository, tripId, TripAuthorization.IsOwner, ct);
        if (!authorized) return Forbid();

        var promoted = await repository.MakeOwnerAsync(tripId, userId, ct);
        return promoted ? NoContent() : NotFound();
    }
}
