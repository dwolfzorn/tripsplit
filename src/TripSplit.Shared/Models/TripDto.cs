namespace TripSplit.Shared.Models;

public static class TripSchema
{
    public const int CurrentVersion = 3;
    public static readonly int[] SupportedVersions = [1, 2, 3];
}

public class TripDto
{
    public Guid Id { get; set; }
    public int SchemaVersion { get; set; } = TripSchema.CurrentVersion;
    public DateTimeOffset? ExportedAt { get; set; }
    public List<ColCity> ColCities { get; set; } = [];
    public List<Attendee> Attendees { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<ExclusionTag> Tags { get; set; } = [];
}
