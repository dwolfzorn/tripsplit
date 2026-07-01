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
    public async Task<TripEnvelope?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (record is null) return null;

        var trip = JsonSerializer.Deserialize<TripDto>(record.Json, TripJson.Options);
        return trip is null ? null : new TripEnvelope { Trip = trip, RowVersion = record.RowVersion };
    }

    public async Task<Guid> CreateAsync(TripDto trip, Guid ownerId, CancellationToken ct = default)
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
        db.TripMemberships.Add(new TripMembership
        {
            Id = Guid.NewGuid(),
            TripId = id,
            UserId = ownerId,
            Role = TripRole.Owner,
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task<(UpdateResult Result, TripEnvelope? Current)> UpdateAsync(Guid id, TripDto trip, int expectedRowVersion, CancellationToken ct = default)
    {
        var record = await db.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (record is null) return (UpdateResult.NotFound, null);

        trip.Id = id;
        trip.SchemaVersion = TripSchema.CurrentVersion;

        db.Entry(record).Property(r => r.RowVersion).OriginalValue = expectedRowVersion;
        record.Json = JsonSerializer.Serialize(trip, TripJson.Options);
        record.RowVersion = expectedRowVersion + 1;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
            return (UpdateResult.Success, new TripEnvelope { Trip = trip, RowVersion = record.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await GetAsync(id, ct);
            return (UpdateResult.Conflict, current);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (record is null) return false;
        db.Trips.Remove(record);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<TripSummary>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var roleByTripId = await db.TripMemberships
            .Where(m => m.UserId == userId)
            .ToDictionaryAsync(m => m.TripId, m => m.Role, ct);

        var records = await db.Trips.AsNoTracking()
            .Where(t => roleByTripId.Keys.Contains(t.Id))
            .ToListAsync(ct);

        var summaries = new List<TripSummary>();
        foreach (var record in records)
        {
            var trip = JsonSerializer.Deserialize<TripDto>(record.Json, TripJson.Options);
            summaries.Add(new TripSummary
            {
                Id = record.Id,
                Title = BuildTitle(trip, record.CreatedAt),
                UpdatedAt = record.UpdatedAt,
                Role = roleByTripId[record.Id]
            });
        }

        return summaries.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    // Trips created before the Name field existed (or imported from a
    // pre-Name export) have an empty Name in their stored JSON - fall back to
    // a date-based label derived from when the record was created, matching
    // the same "Trip [date]" default new trips get client-side.
    private static string BuildTitle(TripDto? trip, DateTimeOffset createdAt)
    {
        if (trip is not null && !string.IsNullOrWhiteSpace(trip.Name)) return trip.Name;
        return $"Trip {createdAt.LocalDateTime:MMM d, yyyy}";
    }

    public async Task<TripRole?> GetRoleAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var membership = await db.TripMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId, ct);
        return membership?.Role;
    }

    public async Task<IReadOnlyList<MemberInfo>> GetMembersAsync(Guid tripId, CancellationToken ct = default)
    {
        var members = await db.TripMemberships.AsNoTracking()
            .Where(m => m.TripId == tripId)
            .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => new MemberInfo
            {
                UserId = u.Id,
                Email = u.Email ?? "",
                Role = m.Role
            })
            .ToListAsync(ct);
        return members;
    }

    public async Task<bool> RemoveMemberAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var membership = await db.TripMemberships.FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId, ct);
        if (membership is null) return false;

        if (membership.Role == TripRole.Owner)
        {
            var ownerCount = await db.TripMemberships.CountAsync(m => m.TripId == tripId && m.Role == TripRole.Owner, ct);
            if (ownerCount <= 1) return false;
        }

        db.TripMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MakeOwnerAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var membership = await db.TripMemberships.FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId, ct);
        if (membership is null) return false;

        membership.Role = TripRole.Owner;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddMemberAsync(Guid tripId, Guid userId, TripRole role, CancellationToken ct = default)
    {
        var existing = await db.TripMemberships.FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId, ct);
        if (existing is not null) return;

        db.TripMemberships.Add(new TripMembership
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            UserId = userId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
