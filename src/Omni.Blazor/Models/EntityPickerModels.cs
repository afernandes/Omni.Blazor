namespace Omni.Blazor.Models;

/// <summary>Overlay placement used by <c>OmniEntityPicker</c>.</summary>
public enum EntityPickerPresentation
{
    /// <summary>Displays a centered selection dialog.</summary>
    Dialog,

    /// <summary>Displays a right-side selection drawer.</summary>
    Drawer
}

/// <summary>Resolves the entity represented by an externally supplied stable key.</summary>
public delegate ValueTask<TItem?> EntityPickerResolver<TItem, in TKey>(
    TKey key,
    CancellationToken cancellationToken)
    where TItem : class
    where TKey : notnull;
