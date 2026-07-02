using System.ComponentModel.DataAnnotations;

namespace TripSplit.Shared.Models;

public class Expense
{
    public string Item { get; set; } = "";

    [Range(0, double.MaxValue, ErrorMessage = "Cost cannot be negative.")]
    public decimal? Cost { get; set; }

    public int? PurchaserId { get; set; }
    public List<int> TagIds { get; set; } = [];
}
