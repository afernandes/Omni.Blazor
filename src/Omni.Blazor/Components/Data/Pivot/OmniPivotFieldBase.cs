using Microsoft.AspNetCore.Components;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Shared base for the three pivot field kinds (<see cref="OmniPivotRow{TItem}"/>,
/// <see cref="OmniPivotColumn{TItem}"/>, <see cref="OmniPivotValue{TItem}"/>).
/// Renders nothing — each subtype registers itself with the parent grid so the
/// grid can build the pivot from the declared rows/columns/values.
/// </summary>
public abstract class OmniPivotFieldBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] internal OmniPivotGrid<TItem>? Grid { get; set; }

    /// <summary>Name of the property this field reads from each data item.</summary>
    [Parameter] public string? Property { get; set; }

    /// <summary>Header text. Falls back to <see cref="Property"/>.</summary>
    [Parameter] public string? Title { get; set; }

    internal string GetTitle() => !string.IsNullOrEmpty(Title) ? Title! : (Property ?? "");

    internal object? GetValue(TItem item)
        => Property is null || item is null ? null : SchedulerReflection.GetValue(item, Property);

    /// <summary>Group-key text for a value (used in row/column headers).</summary>
    internal string KeyText(object? key) => key?.ToString() ?? "(vazio)";

    protected override void OnInitialized() => Register();

    public void Dispose() => Unregister();

    protected abstract void Register();
    protected abstract void Unregister();
}
