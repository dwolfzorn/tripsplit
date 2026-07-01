namespace TripSplit.Shared.Models;

public class TripEnvelope
{
    public TripDto Trip { get; set; } = new();
    public int RowVersion { get; set; }
    public TripRole Role { get; set; }
}
