namespace TripSplit.Shared.Models;

public class TripSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public TripRole Role { get; set; }
}
