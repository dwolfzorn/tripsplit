using TripSplit.Shared.Models;

namespace TripSplit.Shared.Calc;

// Direct port of the calculation functions from the original index.html
// (calculateAttendeeWeights, calculateOwedByAttendee, calculateBalances,
// roundBalancesToWholeDollars, computeSettlementTransfers). Pure functions,
// no DB/HTTP access, so the client and server can both call this and stay
// in sync, and it's straightforward to unit test for parity with the
// original JS behavior.
public static class SplitCalculator
{
    public static double? GetColIndex(Attendee attendee, List<ColCity> colCities)
    {
        var match = colCities.FirstOrDefault(c => c.City == attendee.City);
        if (match is null) return null;
        return match.Index > 0 ? match.Index : null;
    }

    public static (List<AttendeeWeight> Rows, List<AttendeeWeight> ValidRows, decimal TotalAdjusted) CalculateAttendeeWeights(
        List<Attendee> attendees, List<ColCity> colCities)
    {
        var rows = attendees.Select(a =>
        {
            var col = GetColIndex(a, colCities);
            var valid = !string.IsNullOrWhiteSpace(a.Name) && a.Income is > 0 && col is > 0;
            var adjustedIncome = valid ? a.Income!.Value * ((decimal)col!.Value / 100m) : 0m;
            return new AttendeeWeight(a.Id, a.Name, a.Income, col, a.City, adjustedIncome, valid);
        }).ToList();

        var validRows = rows.Where(r => r.Valid).ToList();
        var totalAdjusted = validRows.Sum(r => r.AdjustedIncome);

        return (rows, validRows, totalAdjusted);
    }

    public static decimal GetItemizedTotal(List<Expense> expenses) =>
        expenses.Sum(e => e.Cost ?? 0m);

    // Exclusion-tag-aware per-expense split: each expense is divided only
    // among the attendees not excluded by any tag applied to it, renormalized
    // to that subset's COL-adjusted weights. Summing each person's per-expense
    // owed amounts gives their total owed.
    public static Dictionary<int, decimal> CalculateOwedByAttendee(
        List<Expense> expenses, List<ExclusionTag> tags, List<AttendeeWeight> validRows)
    {
        var owedById = validRows.ToDictionary(r => r.Id, _ => 0m);

        foreach (var expense in expenses)
        {
            var cost = expense.Cost ?? 0m;
            if (cost <= 0) continue;

            var excludedIds = expense.TagIds
                .Select(tagId => tags.FirstOrDefault(t => t.Id == tagId))
                .Where(t => t is not null)
                .SelectMany(t => t!.MemberIds)
                .ToHashSet();

            var eligibleRows = validRows.Where(r => !excludedIds.Contains(r.Id)).ToList();
            var eligibleTotal = eligibleRows.Sum(r => r.AdjustedIncome);

            foreach (var row in eligibleRows)
            {
                var share = eligibleTotal > 0 ? row.AdjustedIncome / eligibleTotal : 0m;
                owedById[row.Id] += cost * share;
            }
        }

        return owedById;
    }

    public static SplitResult CalculateSplit(TripDto trip)
    {
        var tripCost = GetItemizedTotal(trip.Expenses);
        var (rows, validRows, totalAdjusted) = CalculateAttendeeWeights(trip.Attendees, trip.ColCities);
        var owedById = CalculateOwedByAttendee(trip.Expenses, trip.Tags, validRows);

        return new SplitResult
        {
            Rows = rows,
            ValidRows = validRows,
            TotalAdjusted = totalAdjusted,
            TripCost = tripCost,
            OwedById = owedById
        };
    }

    // Per-person balance = paid minus owed. Whole-dollar rounded so the
    // settle-up transfers always sum exactly.
    public static List<BalanceRow> CalculateBalances(TripDto trip, SplitResult split)
    {
        var paidById = new Dictionary<int, decimal>();
        foreach (var e in trip.Expenses)
        {
            var cost = e.Cost ?? 0m;
            if (e.PurchaserId is int purchaserId && cost > 0)
            {
                paidById[purchaserId] = paidById.GetValueOrDefault(purchaserId) + cost;
            }
        }

        var balances = split.ValidRows.Select(r =>
        {
            var paid = paidById.GetValueOrDefault(r.Id);
            var owed = split.OwedById.GetValueOrDefault(r.Id);
            return new BalanceRow { Id = r.Id, Name = r.Name, Paid = paid, Balance = paid - owed };
        }).ToList();

        RoundBalancesToWholeDollars(balances);
        return balances;
    }

    // Rounds each balance to the nearest whole dollar (half rounds toward
    // +infinity, matching JS Math.round), then corrects any rounding drift so
    // the set still sums to zero. The correction is absorbed by the
    // largest-magnitude balance on whichever side is causing the imbalance.
    public static void RoundBalancesToWholeDollars(List<BalanceRow> balances)
    {
        if (balances.Count == 0) return;

        foreach (var b in balances)
        {
            b.Balance = Math.Floor(b.Balance + 0.5m);
        }

        var drift = balances.Sum(b => b.Balance);
        if (drift == 0) return;

        var candidates = balances.Where(b => drift > 0 ? b.Balance > 0 : b.Balance < 0).ToList();
        var pool = candidates.Count > 0 ? candidates : balances;

        var target = pool.Aggregate((max, b) => Math.Abs(b.Balance) > Math.Abs(max.Balance) ? b : max);
        target.Balance -= drift;
    }

    // Greedy minimal-transfer settlement: repeatedly match the biggest debtor
    // with the biggest creditor.
    public static List<Transfer> ComputeSettlementTransfers(List<BalanceRow> balances)
    {
        const decimal eps = 0.005m;

        var debtors = balances.Where(b => b.Balance < -eps)
            .OrderByDescending(b => -b.Balance)
            .Select(b => new MutableParty(b.Name, -b.Balance))
            .ToList();

        var creditors = balances.Where(b => b.Balance > eps)
            .OrderByDescending(b => b.Balance)
            .Select(b => new MutableParty(b.Name, b.Balance))
            .ToList();

        var transfers = new List<Transfer>();
        int i = 0, j = 0;
        while (i < debtors.Count && j < creditors.Count)
        {
            var debtor = debtors[i];
            var creditor = creditors[j];
            var amount = Math.Min(debtor.Amount, creditor.Amount);

            if (amount > eps)
            {
                transfers.Add(new Transfer(debtor.Name, creditor.Name, amount));
            }

            debtor.Amount -= amount;
            creditor.Amount -= amount;

            if (debtor.Amount <= eps) i++;
            if (creditor.Amount <= eps) j++;
        }

        return transfers;
    }

    private sealed class MutableParty(string name, decimal amount)
    {
        public string Name { get; } = name;
        public decimal Amount { get; set; } = amount;
    }
}
