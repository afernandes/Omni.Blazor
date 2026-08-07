using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed JavaScript boundary for context-menu positioning and keyboard behavior.</summary>
public sealed class ContextMenuInteropService
{
    private readonly IOmniOverlayJsModule _js;

    internal ContextMenuInteropService(IOmniOverlayJsModule js) => _js = js;

    /// <summary>Positions a rendered menu, binds dismissal, and focuses its first enabled item.</summary>
    public ValueTask OpenAsync<TReceiver>(
        string key,
        ElementReference root,
        double x,
        double y,
        ContextMenuPositionMode positionMode,
        bool alignEnd,
        DotNetObjectReference<TReceiver> receiver,
        CancellationToken cancellationToken = default)
        where TReceiver : class
        => _js.InvokeVoidAsync(
            "contextMenuOpen",
            cancellationToken,
            key,
            root,
            x,
            y,
            positionMode == ContextMenuPositionMode.Trigger,
            alignEnd,
            receiver);

    /// <summary>Removes menu listeners and optionally restores focus to the opening trigger.</summary>
    public ValueTask CloseAsync(
        string key,
        bool restoreFocus,
        CancellationToken cancellationToken = default)
        => _js.InvokeVoidAsync("contextMenuClose", cancellationToken, key, restoreFocus);
}
