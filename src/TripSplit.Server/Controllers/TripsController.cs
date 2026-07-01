using Microsoft.AspNetCore.Mvc;
using TripSplit.Server.Repositories;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController(ITripRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> Create(TripDto trip, CancellationToken ct)
    {
        var id = await repository.CreateAsync(trip, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDto>> Get(Guid id, CancellationToken ct)
    {
        var trip = await repository.GetAsync(id, ct);
        return trip is null ? NotFound() : Ok(trip);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TripDto trip, CancellationToken ct)
    {
        if (trip.Id != Guid.Empty && trip.Id != id)
        {
            return BadRequest("Trip id in the request body does not match the route id.");
        }

        var updated = await repository.UpdateAsync(id, trip, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
