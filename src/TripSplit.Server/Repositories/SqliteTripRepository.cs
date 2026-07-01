using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TripSplit.Server.Data;
using TripSplit.Shared.Json;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Repositories;

// Stores each trip as its existing JSON export shape in a single column,
// rather than a normalized relational schema - the app never queries inside
// a trip, so there's no need to model per-section tables yet. Swapping to a
// normalized schema later only requires a new ITripRepository implementation;
// callers (the API controller) are unaffected.
public class SqliteTripRepository(TripDbContext db) : ITripRepository
{
    public async Task<TripDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return record is null ? null : JsonSerializer.Deserialize<TripDto>(record.Json, TripJson.Options);
    }

    public async Task<Guid> CreateAsync(TripDto trip, CancellationToken ct = default)
    {
        var id = trip.Id == Guid.Empty ? Guid.NewGuid() : trip.Id;
        trip.Id = id;
        trip.SchemaVersion = TripSchema.CurrentVersion;

        var now = DateTimeOffset.UtcNow;
        db.Trips.Add(new TripRecord
        {
            Id = id,
            Json = JsonSerializer.Serialize(trip, TripJson.Options),
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, TripDto trip, CancellationToken ct = default)
    {
        var record = await db.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (record is null) return false;

        trip.Id = id;
        trip.SchemaVersion = TripSchema.CurrentVersion;
        record.Json = JsonSerializer.Serialize(trip, TripJson.Options);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (record is null) return false;
        db.Trips.Remove(record);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
