using TripSplit.Shared.Models;

namespace TripSplit.Server.Repositories;

public interface ITripRepository
{
    Task<TripDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(TripDto trip, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, TripDto trip, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
