namespace TripSplit.Shared.Calc;

public record AttendeeWeight(
    int Id,
    string Name,
    decimal? Income,
    double? Col,
    string City,
    decimal AdjustedIncome,
    bool Valid);
