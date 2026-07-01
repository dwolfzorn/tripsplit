using System.Text.Json.Nodes;
using TripSplit.Shared.Calc;
using TripSplit.Shared.Import;
using TripSplit.Shared.Models;

namespace TripSplit.Tests;

// Asserts parity with the original JS implementation in index.html: expected
// values below were computed by re-running the original algorithm (ported
// 1:1 to Python, mirroring the JS float arithmetic) against
// Fixtures/example-trip.json. A 1-cent tolerance accounts for decimal vs.
// double arithmetic differences.
public class SplitCalculatorTests
{
    private static TripDto LoadExampleTrip()
    {
        var raw = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "example-trip.json")))!;
        return TripImporter.ParseTripImport(raw).Data;
    }

    [Fact]
    public void CalculateSplit_MatchesReferenceAdjustedIncomesAndOwedAmounts()
    {
        var trip = LoadExampleTrip();
        var split = SplitCalculator.CalculateSplit(trip);

        Assert.Equal(4785m, split.TripCost);
        Assert.Equal(276900m, split.TotalAdjusted);

        AssertRow(split, "Alex", adjustedIncome: 95000m, owed: 1528.44m);
        AssertRow(split, "Brianna", adjustedIncome: 54600m, owed: 977.51m);
        AssertRow(split, "Carlos", adjustedIncome: 82500m, owed: 1477.00m);
        AssertRow(split, "Dana", adjustedIncome: 44800m, owed: 802.06m);

        // The group as a whole always owes exactly the trip total.
        Assert.Equal(split.TripCost, Math.Round(split.OwedById.Values.Sum(), 2));
    }

    private static void AssertRow(SplitResult split, string name, decimal adjustedIncome, decimal owed)
    {
        var row = Assert.Single(split.ValidRows, r => r.Name == name);
        Assert.Equal(adjustedIncome, row.AdjustedIncome);
        Assert.Equal(owed, Math.Round(split.OwedById[row.Id], 2));
    }

    [Fact]
    public void CalculateBalances_MatchesReferenceWholeDollarBalances()
    {
        var trip = LoadExampleTrip();
        var split = SplitCalculator.CalculateSplit(trip);
        var balances = SplitCalculator.CalculateBalances(trip, split);

        Assert.Equal(0m, balances.Sum(b => b.Balance));

        AssertBalance(balances, "Alex", paid: 2290m, balance: 762m);
        AssertBalance(balances, "Brianna", paid: 1350m, balance: 372m);
        AssertBalance(balances, "Carlos", paid: 595m, balance: -882m);
        AssertBalance(balances, "Dana", paid: 550m, balance: -252m);
    }

    private static void AssertBalance(List<BalanceRow> balances, string name, decimal paid, decimal balance)
    {
        var row = Assert.Single(balances, b => b.Name == name);
        Assert.Equal(paid, row.Paid);
        Assert.Equal(balance, row.Balance);
    }

    [Fact]
    public void ComputeSettlementTransfers_MatchesReferenceGreedyMatching()
    {
        var trip = LoadExampleTrip();
        var split = SplitCalculator.CalculateSplit(trip);
        var balances = SplitCalculator.CalculateBalances(trip, split);
        var transfers = SplitCalculator.ComputeSettlementTransfers(balances);

        Assert.Equal(
            [
                ("Carlos", "Alex", 762m),
                ("Carlos", "Brianna", 120m),
                ("Dana", "Brianna", 252m)
            ],
            transfers.Select(t => (t.From, t.To, t.Amount)));
    }

    [Fact]
    public void RoundBalancesToWholeDollars_CorrectsDriftToKeepSumZero()
    {
        var balances = new List<BalanceRow>
        {
            new() { Id = 1, Name = "A", Balance = 10.4m },
            new() { Id = 2, Name = "B", Balance = 10.4m },
            new() { Id = 3, Name = "C", Balance = -20.8m }
        };

        SplitCalculator.RoundBalancesToWholeDollars(balances);

        Assert.Equal(0m, balances.Sum(b => b.Balance));
    }

    [Fact]
    public void CalculateAttendeeWeights_ExcludesAttendeesMissingRequiredFields()
    {
        var trip = new TripDto
        {
            ColCities = [new ColCity { City = "New York, NY", Index = 100 }],
            Attendees =
            [
                new Attendee { Id = 1, Name = "Complete", Income = 50000m, City = "New York, NY" },
                new Attendee { Id = 2, Name = "", Income = 50000m, City = "New York, NY" }, // missing name
                new Attendee { Id = 3, Name = "No income", Income = null, City = "New York, NY" },
                new Attendee { Id = 4, Name = "Unknown city", Income = 50000m, City = "Nowhere" }
            ]
        };

        var (rows, validRows, _) = SplitCalculator.CalculateAttendeeWeights(trip.Attendees, trip.ColCities);

        Assert.Equal(4, rows.Count);
        var valid = Assert.Single(validRows);
        Assert.Equal("Complete", valid.Name);
    }
}
