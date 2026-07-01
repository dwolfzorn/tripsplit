using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Authorization;
using TripSplit.Server.Repositories;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/trips")]
public class TripsController(ITripRepository repository) : TripApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TripSummary>>> ListMine(CancellationToken ct)
    {
        var trips = await repository.ListForUserAsync(CurrentUserId, ct);
        return Ok(trips);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create(TripDto trip, CancellationToken ct)
    {
        var id = await repository.CreateAsync(trip, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { tripId = id }, new { id });
    }

    [HttpGet("{tripId:guid}")]
    public async Task<ActionResult<TripEnvelope>> Get(Guid tripId, CancellationToken ct)
    {
        var (authorized, role) = await AuthorizeTripRoleAsync(repository, tripId, TripAuthorization.CanEdit, ct);
        if (!authorized) return NotFound();

        var envelope = await repository.GetAsync(tripId, ct);
        if (envelope is null) return NotFound();

        envelope.Role = role!.Value;
        return Ok(envelope);
    }

    [HttpPut("{tripId:guid}")]
    public async Task<ActionResult<TripEnvelope>> Update(Guid tripId, TripEnvelope envelope, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(repository, tripId, TripAuthorization.CanEdit, ct);
        if (!authorized) return NotFound();

        if (envelope.Trip.Id != Guid.Empty && envelope.Trip.Id != tripId)
        {
            return BadRequest("Trip id in the request body does not match the route id.");
        }

        var (result, current) = await repository.UpdateAsync(tripId, envelope.Trip, envelope.RowVersion, ct);
        return result switch
        {
            UpdateResult.Success => NoContent(),
            UpdateResult.NotFound => NotFound(),
            UpdateResult.Conflict => Conflict(current),
            _ => throw new InvalidOperationException()
        };
    }

    [HttpDelete("{tripId:guid}")]
    public async Task<IActionResult> Delete(Guid tripId, CancellationToken ct)
    {
        var (authorized, _) = await AuthorizeTripRoleAsync(repository, tripId, TripAuthorization.CanDelete, ct);
        if (!authorized) return Forbid();

        var deleted = await repository.DeleteAsync(tripId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
