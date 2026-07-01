namespace TripSplit.Shared.Models;

public class ExclusionTag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<int> MemberIds { get; set; } = [];
}
