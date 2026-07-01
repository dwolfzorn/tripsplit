using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Data;
using TripSplit.Server.Repositories;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Controllers;

[ApiController]
[Route("api/share")]
public class ShareRedemptionController(
    IShareLinkRepository shareLinkRepository,
    ITripRepository tripRepository,
    UserManager<ApplicationUser> userManager) : TripApiControllerBase
{
    [HttpGet("{token}/preview")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview(string token, CancellationToken ct)
    {
        var link = await shareLinkRepository.GetActiveByTokenAsync(token, ct);
        if (link is null) return NotFound();

        var envelope = await tripRepository.GetAsync(link.TripId, ct);
        if (envelope is null) return NotFound();

        var inviter = await userManager.FindByIdAsync(link.CreatedByUserId.ToString());

        return Ok(new
        {
            tripId = link.TripId,
            tripTitle = envelope.Trip.Attendees.Count > 0
                ? string.Join(", ", envelope.Trip.Attendees.Take(2).Select(a => a.Name))
                : "Untitled trip",
            inviterEmail = inviter?.Email ?? "someone"
        });
    }

    [HttpPost("{token}/redeem")]
    [Authorize]
    public async Task<IActionResult> Redeem(string token, CancellationToken ct)
    {
        var link = await shareLinkRepository.GetActiveByTokenAsync(token, ct);
        if (link is null) return NotFound();

        var existingRole = await tripRepository.GetRoleAsync(link.TripId, CurrentUserId, ct);
        if (existingRole is null)
        {
            await tripRepository.AddMemberAsync(link.TripId, CurrentUserId, TripRole.Editor, ct);
        }

        return Ok(new { tripId = link.TripId });
    }
}
