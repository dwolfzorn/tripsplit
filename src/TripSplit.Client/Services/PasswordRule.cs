namespace TripSplit.Client.Services;

public record PasswordRule(string Description, Func<string, bool> IsMet);

// Mirrors the PasswordOptions pinned in Program.cs (AddIdentityCore) - kept
// in sync manually since the client can't read server-side options.
public static class PasswordRules
{
    public static readonly IReadOnlyList<PasswordRule> All =
    [
        new("At least 8 characters", p => p.Length >= 8),
        new("At least one digit", p => p.Any(char.IsDigit)),
        new("At least one lowercase letter", p => p.Any(char.IsLower)),
        new("At least one uppercase letter", p => p.Any(char.IsUpper)),
        new("At least one special character", p => p.Any(c => !char.IsLetterOrDigit(c)))
    ];
}
