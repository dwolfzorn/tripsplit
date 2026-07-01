using System.Globalization;
using System.Text.RegularExpressions;

namespace TripSplit.Shared.Import;

// Mirrors JS's parseFloat: parses the longest valid leading numeric prefix of
// the string and ignores anything after it, rather than requiring the whole
// string to be numeric. Used for the Excel paste-to-fill feature, where a
// pasted cost cell may carry trailing annotations (e.g. "45.50 USD").
public static partial class LenientNumberParser
{
    [GeneratedRegex(@"^\s*[-+]?(\d+\.?\d*|\.\d+)([eE][-+]?\d+)?")]
    private static partial Regex LeadingNumber();

    public static decimal? ParseLeadingDecimal(string value)
    {
        var match = LeadingNumber().Match(value);
        if (!match.Success) return null;
        return decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
