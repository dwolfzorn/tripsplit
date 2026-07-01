using System.ComponentModel.DataAnnotations;

namespace TripSplit.Shared.Models;

public class Attendee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [Range(0, double.MaxValue, ErrorMessage = "Income cannot be negative.")]
    public decimal? Income { get; set; }

    public string City { get; set; } = "";
}
