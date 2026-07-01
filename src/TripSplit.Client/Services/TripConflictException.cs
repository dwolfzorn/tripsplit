using TripSplit.Shared.Models;

namespace TripSplit.Client.Services;

public class TripConflictException(TripEnvelope current) : Exception("Trip was updated elsewhere.")
{
    public TripEnvelope Current { get; } = current;
}
