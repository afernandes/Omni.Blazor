using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

public class ContextMenuService
{
    public bool IsOpen { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public IReadOnlyList<ContextMenuItem> Items { get; private set; } = Array.Empty<ContextMenuItem>();
    public event Action? OnChange;

    public void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items)
        => Open(args.ClientX, args.ClientY, items);

    public void Open(double x, double y, IEnumerable<ContextMenuItem> items)
    {
        X = x; Y = y;
        Items = items.ToList();
        IsOpen = true;
        OnChange?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Items = Array.Empty<ContextMenuItem>();
        OnChange?.Invoke();
    }
}

public class ContextMenuItem
{
    public string? Text { get; set; }
    public string? Icon { get; set; }
    public string? Shortcut { get; set; }
    public bool IsSeparator { get; set; }
    public bool IsDanger { get; set; }
    public Func<Task>? OnClick { get; set; }

    public static ContextMenuItem Separator() => new() { IsSeparator = true };
}
