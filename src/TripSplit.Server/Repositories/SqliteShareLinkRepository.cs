using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TripSplit.Server.Data;

namespace TripSplit.Server.Repositories;

public class SqliteShareLinkRepository(TripDbContext db) : IShareLinkRepository
{
    public async Task<ShareLink> GetOrCreateActiveAsync(Guid tripId, Guid createdByUserId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        // SQLite's EF Core provider can't translate ORDER BY over DateTimeOffset
        // server-side, and there's at most one active link per trip in practice
        // anyway (RevokeAsync clears the rest) - order client-side instead.
        var existing = (await db.ShareLinks
                .Where(s => s.TripId == tripId && !s.IsRevoked)
                .ToListAsync(ct))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (existing is not null && existing.IsActive(now)) return existing;

        return await CreateAsync(tripId, createdByUserId, ct);
    }

    public async Task<ShareLink> RegenerateAsync(Guid tripId, Guid createdByUserId, CancellationToken ct = default)
    {
        await RevokeAsync(tripId, ct);
        return await CreateAsync(tripId, createdByUserId, ct);
    }

    public async Task RevokeAsync(Guid tripId, CancellationToken ct = default)
    {
        var active = await db.ShareLinks.Where(s => s.TripId == tripId && !s.IsRevoked).ToListAsync(ct);
        foreach (var link in active) link.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ShareLink?> GetActiveByTokenAsync(string token, CancellationToken ct = default)
    {
        var link = await db.ShareLinks.FirstOrDefaultAsync(s => s.Token == token, ct);
        return link is not null && link.IsActive(DateTimeOffset.UtcNow) ? link : null;
    }

    private async Task<ShareLink> CreateAsync(Guid tripId, Guid createdByUserId, CancellationToken ct)
    {
        var link = new ShareLink
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            Token = GenerateToken(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ShareLinks.Add(link);
        await db.SaveChangesAsync(ct);
        return link;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
