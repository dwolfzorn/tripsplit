using System.ComponentModel.DataAnnotations;

namespace TripSplit.Shared.Models;

public class ColCity
{
    public string City { get; set; } = "";

    [Range(0, double.MaxValue, ErrorMessage = "Cost-of-living index cannot be negative.")]
    public double Index { get; set; }
}
