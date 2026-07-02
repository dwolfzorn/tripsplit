namespace TripSplit.Shared.Calc;

public class SplitResult
{
    public List<AttendeeWeight> Rows { get; init; } = [];
    public List<AttendeeWeight> ValidRows { get; init; } = [];
    public decimal TotalAdjusted { get; init; }
    public decimal TripCost { get; init; }
    public Dictionary<int, decimal> OwedById { get; init; } = [];
}
