using TripSplit.Shared.Models;

namespace TripSplit.Server.Data;

public class TripMembership
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public TripRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
