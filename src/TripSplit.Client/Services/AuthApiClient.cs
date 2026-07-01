using System.Net.Http.Json;
using System.Text.Json;

namespace TripSplit.Client.Services;

public class AuthApiClient(HttpClient http, AppAuthenticationStateProvider authStateProvider)
{
    public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/auth/register-with-name", new { email, password, firstName, lastName }, ct);
        if (!response.IsSuccessStatusCode) return AuthResult.Failure(await ExtractError(response));

        // Registration doesn't sign the user in; log in right after so
        // registration feels like a single step to the user.
        return await LoginAsync(email, password, ct);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/auth/login?useCookies=true", new { email, password }, ct);
        if (!response.IsSuccessStatusCode) return AuthResult.Failure(await ExtractError(response));

        authStateProvider.NotifyAuthenticationChanged();
        return AuthResult.Success();
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await http.PostAsync("api/auth/logout", null, ct);
        authStateProvider.NotifyAuthenticationChanged();
    }

    public async Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword }, ct);
        return response.IsSuccessStatusCode ? AuthResult.Success() : AuthResult.Failure(await ExtractError(response));
    }

    // Identity's endpoints return either a plain ProblemDetails (title only,
    // e.g. failed login) or a validation problem with a Code -> [messages]
    // "errors" dictionary (e.g. ChangePasswordAsync/CreateAsync failures) -
    // this flattens either shape into one readable line instead of surfacing
    // the raw JSON body to the user.
    private static async Task<string> ExtractError(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return $"Request failed ({(int)response.StatusCode}).";

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errorsElement))
            {
                var messages = errorsElement.EnumerateObject()
                    .SelectMany(prop => prop.Value.EnumerateArray().Select(v => v.GetString()))
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                var joined = string.Join(" ", messages);
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }
            if (doc.RootElement.TryGetProperty("title", out var titleElement))
            {
                var title = titleElement.GetString();
                if (!string.IsNullOrWhiteSpace(title)) return title;
            }
        }
        catch (JsonException)
        {
            // Not JSON - fall through to returning the raw body.
        }

        return body;
    }
}

public record AuthResult(bool IsSuccess, string? Error)
{
    public static AuthResult Success() => new(true, null);
    public static AuthResult Failure(string error) => new(false, error);
}
