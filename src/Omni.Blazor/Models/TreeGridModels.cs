namespace Omni.Blazor.Models;

/// <summary>Horizontal alignment for a tree-grid column.</summary>
public enum TreeGridColumnAlign
{
    /// <summary>Aligns content to the inline start.</summary>
    Start,

    /// <summary>Centers content.</summary>
    Center,

    /// <summary>Aligns content to the inline end.</summary>
    End
}

/// <summary>
/// Asynchronous, cancellable child source shared by hierarchical components.
/// </summary>
public delegate ValueTask<IReadOnlyList<TItem>> HierarchyChildrenProvider<TItem>(
    TItem parent,
    CancellationToken cancellationToken);
