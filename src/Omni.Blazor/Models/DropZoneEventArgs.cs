using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Models;

/// <summary>
/// Event payload for <see cref="OmniDropZoneContainer{TItem}"/> <c>CanDrop</c>
/// / <c>Drop</c> / <c>DragStart</c> / <c>DragEnd</c>. Mirrors the shape Radzen
/// uses so consumers familiar with that library feel at home.
/// </summary>
public class DropZoneItemEventArgs<TItem>
{
    /// <summary>The zone the item was dragged from.</summary>
    public OmniDropZone<TItem>? FromZone { get; internal set; }
    /// <summary>The zone the item is being dragged into (or was dropped on).</summary>
    public OmniDropZone<TItem>? ToZone { get; internal set; }
    /// <summary>The item being dragged.</summary>
    public TItem? Item { get; internal set; }
    /// <summary>
    /// The item the drag hovered over (or was dropped on) inside <c>ToZone</c>.
    /// Use this for re-ordering: insert <see cref="Item"/> at <see cref="ToItem"/>'s index.
    /// </summary>
    public TItem? ToItem { get; internal set; }
    /// <summary>The raw DataTransfer from the browser drag event.</summary>
    public DataTransfer DataTransfer { get; set; } = default!;
}

/// <summary>
/// Per-item render hook. Use to override HTML attributes (class, style,
/// draggable) or hide the item entirely.
/// </summary>
public class DropZoneItemRenderEventArgs<TItem>
{
    public TItem? Item { get; init; }
    public OmniDropZone<TItem>? Zone { get; init; }
    /// <summary>Whether to render the item at all. Default true.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Mutable attribute bag merged onto the item's root div.</summary>
    public Dictionary<string, object> Attributes { get; } = new();
}
