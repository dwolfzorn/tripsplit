using TripSplit.Server.Data;
using TripSplit.Shared.Models;

namespace TripSplit.Server.Repositories;

public enum UpdateResult
{
    Success,
    NotFound,
    Conflict
}

public interface ITripRepository
{
    Task<TripEnvelope?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(TripDto trip, Guid ownerId, CancellationToken ct = default);
    Task<(UpdateResult Result, TripEnvelope? Current)> UpdateAsync(Guid id, TripDto trip, int expectedRowVersion, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TripSummary>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task<TripRole?> GetRoleAsync(Guid tripId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<MemberInfo>> GetMembersAsync(Guid tripId, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid tripId, Guid userId, CancellationToken ct = default);
    Task<bool> MakeOwnerAsync(Guid tripId, Guid userId, CancellationToken ct = default);
    Task AddMemberAsync(Guid tripId, Guid userId, TripRole role, CancellationToken ct = default);
}
