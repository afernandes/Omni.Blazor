using Microsoft.AspNetCore.Components.Web;

namespace Omni.Blazor.Services;

/// <summary>Positions a context menu at the pointer or relative to its focused trigger.</summary>
public enum ContextMenuPositionMode
{
    /// <summary>Uses the pointer's client coordinates.</summary>
    Pointer,

    /// <summary>Anchors the menu below the focused trigger element.</summary>
    Trigger
}

/// <summary>Scoped state coordinator consumed by <c>OmniContextMenuHost</c>.</summary>
public class ContextMenuService
{
    private object? _owner;

    /// <summary>Stable DOM id of the scoped menu portal.</summary>
    public string Id { get; } = $"omni-context-menu-{Guid.NewGuid():N}";

    /// <summary>Whether the portal currently contains an open menu.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Pointer X coordinate used by pointer-positioned menus.</summary>
    public double X { get; private set; }

    /// <summary>Pointer Y coordinate used by pointer-positioned menus.</summary>
    public double Y { get; private set; }

    /// <summary>Current positioning strategy.</summary>
    public ContextMenuPositionMode PositionMode { get; private set; }

    /// <summary>Whether a trigger-positioned menu aligns its trailing edge with the trigger.</summary>
    public bool AlignEnd { get; private set; }

    /// <summary>Immutable snapshot of the current menu items.</summary>
    public IReadOnlyList<ContextMenuItem> Items { get; private set; } = Array.Empty<ContextMenuItem>();

    internal int Revision { get; private set; }
    internal bool RestoreFocusOnClose { get; private set; }
    /// <summary>Raised whenever the open menu state changes.</summary>
    public event Action? OnChange;

    /// <summary>Opens a menu at the pointer coordinates.</summary>
    public void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items)
        => OpenCore(args.ClientX, args.ClientY, items, owner: null, ContextMenuPositionMode.Pointer, alignEnd: false);

    /// <summary>Opens a menu below the currently focused trigger.</summary>
    public void OpenAnchored(MouseEventArgs args, IEnumerable<ContextMenuItem> items, bool alignEnd = true)
        => OpenCore(args.ClientX, args.ClientY, items, owner: null, ContextMenuPositionMode.Trigger, alignEnd);

    internal void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items, object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        OpenCore(args.ClientX, args.ClientY, items, owner, ContextMenuPositionMode.Pointer, alignEnd: false);
    }

    internal void OpenAnchored(
        MouseEventArgs args,
        IEnumerable<ContextMenuItem> items,
        object owner,
        bool alignEnd = true)
    {
        ArgumentNullException.ThrowIfNull(owner);
        OpenCore(args.ClientX, args.ClientY, items, owner, ContextMenuPositionMode.Trigger, alignEnd);
    }

    /// <summary>Opens a menu at explicit client coordinates.</summary>
    public void Open(double x, double y, IEnumerable<ContextMenuItem> items)
        => OpenCore(x, y, items, owner: null, ContextMenuPositionMode.Pointer, alignEnd: false);

    private void OpenCore(
        double x,
        double y,
        IEnumerable<ContextMenuItem> items,
        object? owner,
        ContextMenuPositionMode positionMode,
        bool alignEnd)
    {
        ArgumentNullException.ThrowIfNull(items);
        ContextMenuItem[] snapshot = items.ToArray();
        X = x;
        Y = y;
        PositionMode = positionMode;
        AlignEnd = alignEnd;
        Items = snapshot;
        _owner = owner;
        RestoreFocusOnClose = false;
        IsOpen = true;
        Revision++;
        Notify();
    }

    /// <summary>Closes the current menu and restores focus to its trigger when available.</summary>
    public void Close() => CloseCore(restoreFocus: true);

    /// <summary>Closes the current menu and optionally restores focus to its trigger.</summary>
    public void Close(bool restoreFocus) => CloseCore(restoreFocus);

    internal void Close(object owner, bool restoreFocus = false)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!ReferenceEquals(_owner, owner)) return;
        CloseCore(restoreFocus);
    }

    internal bool IsOwnedBy(object owner)
        => IsOpen && ReferenceEquals(_owner, owner);

    private void CloseCore(bool restoreFocus)
    {
        if (!IsOpen) return;
        IsOpen = false;
        Items = Array.Empty<ContextMenuItem>();
        _owner = null;
        RestoreFocusOnClose = restoreFocus;
        Revision++;
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}

/// <summary>One immutable-at-open command rendered by <c>OmniContextMenuHost</c>.</summary>
public class ContextMenuItem
{
    /// <summary>Visible and accessible item label.</summary>
    public string? Text { get; set; }

    /// <summary>Optional leading icon.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional keyboard shortcut hint.</summary>
    public string? Shortcut { get; set; }

    /// <summary>Optional secondary description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional visual group label.</summary>
    public string? Group { get; set; }

    /// <summary>Whether this entry is a separator.</summary>
    public bool IsSeparator { get; set; }

    /// <summary>Whether this entry uses destructive styling.</summary>
    public bool IsDanger { get; set; }

    /// <summary>Whether activation is unavailable.</summary>
    public bool Disabled { get; set; }

    /// <summary>Asynchronous activation callback.</summary>
    public Func<Task>? OnClick { get; set; }

    /// <summary>Creates a separator entry.</summary>
    public static ContextMenuItem Separator() => new() { IsSeparator = true };
}
