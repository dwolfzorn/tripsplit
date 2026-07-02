using TripSplit.Server.Data;

namespace TripSplit.Server.Repositories;

public interface IShareLinkRepository
{
    Task<ShareLink> GetOrCreateActiveAsync(Guid tripId, Guid createdByUserId, CancellationToken ct = default);
    Task<ShareLink> RegenerateAsync(Guid tripId, Guid createdByUserId, CancellationToken ct = default);
    Task RevokeAsync(Guid tripId, CancellationToken ct = default);
    Task<ShareLink?> GetActiveByTokenAsync(string token, CancellationToken ct = default);
}
