using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TripSplit.Client.Services;

// Blazor WASM has no built-in notion of server auth state (there's no
// per-circuit connection to push it), so this provider polls a small
// /api/auth/me endpoint and lets login/logout explicitly notify it, mirroring
// the existing TripState event-driven refresh pattern rather than reaching
// for the OIDC-oriented WebAssembly.Authentication package (unneeded for
// same-origin cookie auth).
public class AppAuthenticationStateProvider(HttpClient http) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await http.GetAsync("api/auth/me");
            if (!response.IsSuccessStatusCode) return Anonymous;

            var info = await response.Content.ReadFromJsonAsync<MeResponse>();
            if (info?.Email is null) return Anonymous;

            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, info.Email),
                    new Claim(ClaimTypes.GivenName, info.FirstName ?? ""),
                    new Claim(ClaimTypes.Surname, info.LastName ?? "")
                ],
                authenticationType: "cookie");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (HttpRequestException)
        {
            return Anonymous;
        }
    }

    public void NotifyAuthenticationChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private record MeResponse(string? Email, string? FirstName, string? LastName);
}
