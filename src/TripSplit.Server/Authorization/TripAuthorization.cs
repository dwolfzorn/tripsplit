using TripSplit.Shared.Models;

namespace TripSplit.Server.Authorization;

public static class TripAuthorization
{
    public static bool CanEdit(TripRole? role) => role is TripRole.Owner or TripRole.Editor;

    public static bool CanDelete(TripRole? role) => role is TripRole.Owner;

    public static bool IsOwner(TripRole? role) => role is TripRole.Owner;
}
