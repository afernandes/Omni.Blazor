using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed browser-focus façade used by components without direct JS runtime access.</summary>
public sealed class FocusManager
{
    private readonly IJSRuntime _js;

    /// <summary>Creates a focus manager for the current Blazor scope.</summary>
    public FocusManager(IJSRuntime js) => _js = js;

    /// <summary>
    /// Focuses an element by id, or the first focusable descendant when the id
    /// belongs to a component wrapper. Safe during server prerendering.
    /// </summary>
    public async ValueTask FocusAsync(string elementId, bool preventScroll = true)
    {
        if (string.IsNullOrWhiteSpace(elementId)) return;
        try
        {
            await _js.InvokeVoidAsync("omniBlazor.focusElement", elementId, preventScroll);
        }
        catch
        {
            // JS is intentionally optional during SSR/prerendering.
        }
    }
}

