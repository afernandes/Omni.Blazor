using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Services;

public class TooltipService
{
    public RenderFragment? Content { get; private set; }
    public string? Text { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public bool IsOpen { get; private set; }
    public event Action? OnChange;

    public void Open(double x, double y, string text)
    {
        X = x; Y = y; Text = text; Content = null; IsOpen = true;
        OnChange?.Invoke();
    }

    public void Open(double x, double y, RenderFragment content)
    {
        X = x; Y = y; Content = content; Text = null; IsOpen = true;
        OnChange?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false; Content = null; Text = null;
        OnChange?.Invoke();
    }
}
