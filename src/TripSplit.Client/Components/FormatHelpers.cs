namespace TripSplit.Client.Components;

// Shared display formatting for money values, used across the result tables
// (Split details, Settle up, Expenses) instead of each component redefining
// its own private FormatNum(decimal) helper.
public static class FormatHelpers
{
    public static string Currency(decimal n) => n.ToString("N0");
}
