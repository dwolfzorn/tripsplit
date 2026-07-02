using TripSplit.Server.Data;

namespace TripSplit.Tests;

public class ShareLinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActive_WhenNotRevokedAndNoExpiry_ReturnsTrue()
    {
        var link = new ShareLink { IsRevoked = false, ExpiresAt = null };
        Assert.True(link.IsActive(Now));
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var link = new ShareLink { IsRevoked = true, ExpiresAt = null };
        Assert.False(link.IsActive(Now));
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var link = new ShareLink { IsRevoked = false, ExpiresAt = Now.AddDays(-1) };
        Assert.False(link.IsActive(Now));
    }

    [Fact]
    public void IsActive_WhenNotYetExpired_ReturnsTrue()
    {
        var link = new ShareLink { IsRevoked = false, ExpiresAt = Now.AddDays(1) };
        Assert.True(link.IsActive(Now));
    }
}
