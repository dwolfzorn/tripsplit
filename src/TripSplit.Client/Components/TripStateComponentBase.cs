using Microsoft.AspNetCore.Components;
using TripSplit.Client.Services;

namespace TripSplit.Client.Components;

// Base for components that render live data from TripState. Blazor does not
// automatically re-render a child component when an ancestor's StateHasChanged
// runs unless the child has [Parameter]s that changed - components with none
// (like these) must subscribe to TripState.Changed themselves to know when to
// refresh. Centralizing the subscribe/unsubscribe here means a new component
// just inherits it instead of re-implementing IDisposable by hand.
public abstract class TripStateComponentBase : ComponentBase, IDisposable
{
    [Inject] protected TripState TripState { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshFromTripState();
        TripState.Changed += HandleTripChanged;
    }

    private void HandleTripChanged()
    {
        RefreshFromTripState();
        InvokeAsync(StateHasChanged);
    }

    // Called once per TripState data change (including the initial render),
    // before the component re-renders. Override to recompute cached derived
    // values (e.g. SplitCalculator results) once per change instead of once
    // per markup access within a render.
    protected virtual void RefreshFromTripState()
    {
    }

    public virtual void Dispose()
    {
        TripState.Changed -= HandleTripChanged;
    }
}
