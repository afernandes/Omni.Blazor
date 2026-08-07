using Microsoft.AspNetCore.Components;

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

    /// <summary>Strongly typed value selector for this dimension or measure.</summary>
    [Parameter, EditorRequired] public Func<TItem, object?> Value { get; set; } = default!;

    /// <summary>Header text.</summary>
    [Parameter] public string? Title { get; set; }

    internal string GetTitle() => Title ?? string.Empty;

    internal object? GetValue(TItem item) => item is null ? null : Value(item);

    /// <summary>Group-key text for a value (used in row/column headers).</summary>
    internal string KeyText(object? key) => key?.ToString() ?? "(vazio)";

    protected override void OnInitialized() => Register();

    public void Dispose() => Unregister();

    protected abstract void Register();
    protected abstract void Unregister();
}
