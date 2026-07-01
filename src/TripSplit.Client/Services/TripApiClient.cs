using System.Net;
using System.Net.Http.Json;
using TripSplit.Shared.Models;

namespace TripSplit.Client.Services;

public class TripApiClient(HttpClient http)
{
    public async Task<TripEnvelope?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/trips/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TripEnvelope>(cancellationToken: ct);
    }

    public async Task<Guid> CreateAsync(TripDto trip, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/trips", trip, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateTripResponse>(cancellationToken: ct);
        return result!.Id;
    }

    public async Task<TripEnvelope> SaveAsync(Guid id, TripDto trip, int rowVersion, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/trips/{id}", new TripEnvelope { Trip = trip, RowVersion = rowVersion }, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var current = await response.Content.ReadFromJsonAsync<TripEnvelope>(cancellationToken: ct);
            throw new TripConflictException(current!);
        }
        response.EnsureSuccessStatusCode();
        return new TripEnvelope { Trip = trip, RowVersion = rowVersion + 1 };
    }

    public async Task<List<TripSummary>> ListMineAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/trips", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TripSummary>>(cancellationToken: ct) ?? [];
    }

    public async Task<List<MemberInfo>> GetMembersAsync(Guid tripId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/trips/{tripId}/members", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MemberInfo>>(cancellationToken: ct) ?? [];
    }

    public async Task RemoveMemberAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/trips/{tripId}/members/{userId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MakeOwnerAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/trips/{tripId}/members/{userId}/make-owner", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetOrCreateShareLinkAsync(Guid tripId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/trips/{tripId}/share", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ShareLinkResponse>(cancellationToken: ct);
        return result!.Token;
    }

    public async Task<string> RegenerateShareLinkAsync(Guid tripId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/trips/{tripId}/share/regenerate", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ShareLinkResponse>(cancellationToken: ct);
        return result!.Token;
    }

    public async Task<SharePreview?> GetSharePreviewAsync(string token, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/share/{token}/preview", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SharePreview>(cancellationToken: ct);
    }

    public async Task<Guid> RedeemShareLinkAsync(string token, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/share/{token}/redeem", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RedeemResponse>(cancellationToken: ct);
        return result!.TripId;
    }

    private record CreateTripResponse(Guid Id);
    private record ShareLinkResponse(string Token);
    private record RedeemResponse(Guid TripId);

    public record SharePreview(Guid TripId, string TripTitle, string InviterEmail);
}
