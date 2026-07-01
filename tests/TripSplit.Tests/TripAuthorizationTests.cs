using TripSplit.Server.Authorization;
using TripSplit.Shared.Models;

namespace TripSplit.Tests;

public class TripAuthorizationTests
{
    [Theory]
    [InlineData(TripRole.Owner, true)]
    [InlineData(TripRole.Editor, true)]
    [InlineData(null, false)]
    public void CanEdit_ReturnsExpected(TripRole? role, bool expected)
    {
        Assert.Equal(expected, TripAuthorization.CanEdit(role));
    }

    [Theory]
    [InlineData(TripRole.Owner, true)]
    [InlineData(TripRole.Editor, false)]
    [InlineData(null, false)]
    public void CanDelete_ReturnsExpected(TripRole? role, bool expected)
    {
        Assert.Equal(expected, TripAuthorization.CanDelete(role));
    }

    [Theory]
    [InlineData(TripRole.Owner, true)]
    [InlineData(TripRole.Editor, false)]
    [InlineData(null, false)]
    public void IsOwner_ReturnsExpected(TripRole? role, bool expected)
    {
        Assert.Equal(expected, TripAuthorization.IsOwner(role));
    }
}
