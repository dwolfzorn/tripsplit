using System.Text.Json;

namespace TripSplit.Shared.Json;

public static class TripJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
