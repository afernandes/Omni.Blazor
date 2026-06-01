using System.Collections;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>Args for <c>OmniTree.SelectionChanged</c> / <c>Collapsed</c> / <c>Expanded</c>.</summary>
public sealed class TreeEventArgs
{
    /// <summary>The value bound to the node.</summary>
    public object? Value { get; init; }

    /// <summary>The node's resolved text.</summary>
    public string? Text { get; init; }
}

/// <summary>
/// Args for <c>OmniTree.Expand</c>. Set <see cref="Children"/> inside the handler
/// to lazily provide the children of the node being expanded.
/// </summary>
public sealed class TreeExpandEventArgs
{
    /// <summary>The value of the node being expanded.</summary>
    public object? Value { get; init; }

    /// <summary>The node's resolved text.</summary>
    public string? Text { get; init; }

    /// <summary>Configure this to supply the node's children on demand.</summary>
    public TreeItemSettings Children { get; } = new();
}

/// <summary>
/// Describes a set of lazily-loaded children (assigned to
/// <see cref="TreeExpandEventArgs.Children"/>). Mirrors the per-level binding so
/// a lazy branch behaves like a declared <c>OmniTreeLevel</c>.
/// </summary>
public sealed class TreeItemSettings
{
    /// <summary>The child data items.</summary>
    public IEnumerable? Data { get; set; }

    /// <summary>Property name read for each child's text.</summary>
    public string? TextProperty { get; set; }

    /// <summary>Explicit text selector (wins over <see cref="TextProperty"/>).</summary>
    public Func<object, string?>? Text { get; set; }

    /// <summary>Property name read for each child's leading icon.</summary>
    public string? IconProperty { get; set; }

    /// <summary>Explicit icon selector (wins over <see cref="IconProperty"/>).</summary>
    public Func<object, string?>? Icon { get; set; }

    /// <summary>Whether a child has (further) children — controls the toggle chevron.</summary>
    public Func<object, bool> HasChildren { get; set; } = _ => true;

    /// <summary>Whether a child starts expanded.</summary>
    public Func<object, bool> Expanded { get; set; } = _ => false;

    /// <summary>Whether a child starts selected.</summary>
    public Func<object, bool> Selected { get; set; } = _ => false;

    /// <summary>Whether a child's checkbox is enabled.</summary>
    public Func<object, bool> Checkable { get; set; } = _ => true;

    /// <summary>Optional per-child template.</summary>
    public RenderFragment<OmniTreeItem>? Template { get; set; }
}
