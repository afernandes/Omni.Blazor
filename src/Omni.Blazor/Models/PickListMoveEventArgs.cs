namespace Omni.Blazor.Models;

/// <summary>
/// Describes a move performed by an <c>OmniPickList</c> — which items crossed
/// over and in which direction. Raised via the <c>Moved</c> callback after the
/// bound <c>Source</c>/<c>Target</c> collections have already been updated.
/// </summary>
/// <typeparam name="TItem">The list item type.</typeparam>
public sealed class PickListMoveEventArgs<TItem>
{
    /// <summary><c>true</c> when items moved from source → target; <c>false</c> for target → source.</summary>
    public bool ToTarget { get; init; }

    /// <summary>The items that were moved.</summary>
    public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();
}
