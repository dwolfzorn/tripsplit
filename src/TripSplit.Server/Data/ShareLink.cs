namespace TripSplit.Server.Data;

public class ShareLink
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string Token { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public bool IsActive(DateTimeOffset now) => !IsRevoked && (ExpiresAt is null || ExpiresAt > now);
}
