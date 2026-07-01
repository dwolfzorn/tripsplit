using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TripSplit.Shared.Json;
using TripSplit.Shared.Models;

namespace TripSplit.Client.Services;

// Holds the working TripDto for the current session and acts as the
// recompute/autosave hub: every edit (across attendees, COL cities, expenses,
// tags) flows through NotifyChanged(), which raises Changed (so components
// re-render their dependent views, mirroring the original recompute()
// dependency graph) and schedules a debounced save to the API.
public class TripState(TripApiClient api, NavigationManager nav)
{
    private const int AutosaveDebounceMs = 600;

    public TripDto Trip { get; private set; } = CreateDefaultTrip();

    // A trip is "new" (not yet persisted) exactly when it has no server-assigned
    // id. Derived rather than tracked separately so it can never drift out of
    // sync with Trip.Id.
    public bool IsNew => Trip.Id == Guid.Empty;
    public bool IsSaving { get; private set; }

    public event Action? Changed;
    public event Action<string>? SaveFailed;

    private int _nextAttendeeId = 1;
    private int _nextTagId = 1;
    private CancellationTokenSource? _debounceCts;

    // Serializes save calls so an overlapping debounce cycle never races an
    // in-flight CreateAsync/SaveAsync - it waits for the prior save to finish
    // (correctly updating Trip.Id) before deciding POST vs PUT itself.
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public static TripDto CreateDefaultTrip() => new()
    {
        ColCities =
        [
            new ColCity { City = "New York, NY", Index = 100 },
            new ColCity { City = "San Francisco, CA", Index = 102 },
            new ColCity { City = "Los Angeles, CA", Index = 88 },
            new ColCity { City = "Chicago, IL", Index = 75 }
        ],
        Attendees =
        [
            new Attendee { Id = 1, City = "New York, NY" },
            new Attendee { Id = 2, City = "New York, NY" }
        ],
        Expenses = [new Expense()],
        Tags = []
    };

    public async Task LoadAsync(Guid id)
    {
        CancelPendingAutosave();
        var trip = await api.GetAsync(id);
        Trip = trip ?? CreateDefaultTrip();
        RecalculateNextIds();
        Changed?.Invoke();
    }

    public void StartNewTrip()
    {
        CancelPendingAutosave();
        Trip = CreateDefaultTrip();
        RecalculateNextIds();
        Changed?.Invoke();
    }

    public int NextAttendeeId() => _nextAttendeeId++;
    public int NextTagId() => _nextTagId++;

    // Centralizes referential cleanup so removing an attendee or tag always
    // strips dangling references from every collection that can point to it,
    // rather than each caller re-implementing the same cascade by hand.
    public void RemoveAttendee(int attendeeId)
    {
        Trip.Attendees.RemoveAll(a => a.Id == attendeeId);
        foreach (var t in Trip.Tags) t.MemberIds.RemoveAll(id => id == attendeeId);
        foreach (var e in Trip.Expenses)
        {
            if (e.PurchaserId == attendeeId) e.PurchaserId = null;
        }
        NotifyChanged();
    }

    public void RemoveTag(int tagId)
    {
        Trip.Tags.RemoveAll(t => t.Id == tagId);
        foreach (var e in Trip.Expenses) e.TagIds.RemoveAll(id => id == tagId);
        NotifyChanged();
    }

    // Mirrors applyTripImport: lists are replaced wholesale, but an empty
    // imported section keeps the current session's data rather than wiping
    // it (matches the original's behavior for partial/legacy files).
    public void ApplyImport(TripDto imported)
    {
        if (imported.ColCities.Count > 0) Trip.ColCities = imported.ColCities;
        if (imported.Attendees.Count > 0) Trip.Attendees = imported.Attendees;
        if (imported.Expenses.Count > 0) Trip.Expenses = imported.Expenses;
        Trip.Tags = imported.Tags;
        RecalculateNextIds();
        NotifyChanged();
    }

    public string BuildExportJson()
    {
        var export = new TripDto
        {
            Id = Trip.Id,
            SchemaVersion = TripSchema.CurrentVersion,
            ExportedAt = DateTimeOffset.UtcNow,
            ColCities = Trip.ColCities,
            Attendees = Trip.Attendees,
            Expenses = Trip.Expenses,
            Tags = Trip.Tags
        };
        return JsonSerializer.Serialize(export, TripJson.Options);
    }

    private void RecalculateNextIds()
    {
        _nextAttendeeId = Trip.Attendees.Count > 0 ? Trip.Attendees.Max(a => a.Id) + 1 : 1;
        _nextTagId = Trip.Tags.Count > 0 ? Trip.Tags.Max(t => t.Id) + 1 : 1;
    }

    public void NotifyChanged()
    {
        Changed?.Invoke();
        ScheduleAutosave();
    }

    // Cancels (and disposes) any pending or in-flight debounce timer without
    // starting a new one - used when Trip itself is about to be replaced
    // wholesale (load/new-trip), so a stale save can't land on the new trip.
    private void CancelPendingAutosave()
    {
        var previous = _debounceCts;
        _debounceCts = null;
        previous?.Cancel();
        previous?.Dispose();
    }

    private void ScheduleAutosave()
    {
        var previous = _debounceCts;
        var cts = new CancellationTokenSource();
        _debounceCts = cts;
        previous?.Cancel();
        previous?.Dispose();
        _ = DebouncedSaveAsync(cts.Token);
    }

    private async Task DebouncedSaveAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(AutosaveDebounceMs, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            await _saveLock.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (ct.IsCancellationRequested) return;

            // Snapshot the trip being saved: if Trip gets swapped out from
            // under us (LoadAsync/StartNewTrip) while this save is awaiting
            // the network, only apply the result if it's still current.
            var savingTrip = Trip;
            var wasNew = savingTrip.Id == Guid.Empty;

            IsSaving = true;
            Changed?.Invoke();

            if (wasNew)
            {
                var id = await api.CreateAsync(savingTrip, ct);
                if (ReferenceEquals(Trip, savingTrip))
                {
                    savingTrip.Id = id;
                    nav.NavigateTo($"/trip/{id}", replace: true);
                }
            }
            else
            {
                await api.SaveAsync(savingTrip.Id, savingTrip, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SaveFailed?.Invoke(ex.Message);
            // Don't let a transient failure (network blip, server hiccup)
            // silently drop the edit - retry after another debounce window.
            ScheduleAutosave();
        }
        finally
        {
            IsSaving = false;
            Changed?.Invoke();
            _saveLock.Release();
        }
    }
}
