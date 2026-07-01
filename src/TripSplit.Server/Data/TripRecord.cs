namespace TripSplit.Server.Data;

public class TripRecord
{
    public Guid Id { get; set; }
    public string Json { get; set; } = "";
    public int RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
