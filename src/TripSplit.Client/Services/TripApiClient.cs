using System.Net;
using System.Net.Http.Json;
using TripSplit.Shared.Models;

namespace TripSplit.Client.Services;

public class TripApiClient(HttpClient http)
{
    public async Task<TripDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/trips/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TripDto>(cancellationToken: ct);
    }

    public async Task<Guid> CreateAsync(TripDto trip, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/trips", trip, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateTripResponse>(cancellationToken: ct);
        return result!.Id;
    }

    public async Task SaveAsync(Guid id, TripDto trip, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/trips/{id}", trip, ct);
        response.EnsureSuccessStatusCode();
    }

    private record CreateTripResponse(Guid Id);
}
