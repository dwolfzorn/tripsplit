using TripSplit.Shared.Import;

namespace TripSplit.Tests;

public class LenientNumberParserTests
{
    [Theory]
    [InlineData("45.50 USD", 45.50)]
    [InlineData("1200", 1200)]
    [InlineData("  42", 42)]
    [InlineData("-12.5 (refund)", -12.5)]
    public void ParseLeadingDecimal_ParsesLeadingNumericPrefix(string input, decimal expected)
    {
        Assert.Equal(expected, LenientNumberParser.ParseLeadingDecimal(input));
    }

    [Theory]
    [InlineData("USD 45.50")]
    [InlineData("")]
    [InlineData("not a number")]
    public void ParseLeadingDecimal_ReturnsNullWhenNoLeadingNumber(string input)
    {
        Assert.Null(LenientNumberParser.ParseLeadingDecimal(input));
    }
}
