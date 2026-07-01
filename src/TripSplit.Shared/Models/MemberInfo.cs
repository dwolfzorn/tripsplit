namespace TripSplit.Shared.Models;

public class MemberInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public TripRole Role { get; set; }
}
