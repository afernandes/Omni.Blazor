using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Behavior hint for scroll operations — mirrors <c>ScrollBehavior</c> in the
/// browser's Scroll API.
/// </summary>
public enum ScrollBehavior { Auto, Smooth, Instant }

/// <summary>
/// Block alignment for <see cref="ScrollManager.ScrollIntoViewAsync"/>.
/// </summary>
public enum ScrollBlock { Start, Center, End, Nearest }

/// <summary>
/// Central façade for all scroll operations. Inspired by MudBlazor's
/// ScrollManager: every scroll-related JS interop lives here so components
/// don't reach into <c>IJSRuntime</c> on their own. Lock counter ensures
/// nested locks compose (a dialog inside a drawer doesn't unlock the body
/// when one of them closes).
/// </summary>
public sealed class ScrollManager
{
    private readonly IJSRuntime _js;

    public ScrollManager(IJSRuntime js) => _js = js;

    // Default selector "auto" tells JS to find the real scroll root — useful
    // in app-shell layouts (OmniLayout) where the scroll happens inside a
    // pane like .omni-body, not on the document. Pass an explicit selector when
    // you know which container should scroll.

    /// <summary>
    /// Lock scroll on the given element. <c>"auto"</c> (default) targets the
    /// real scrolling container (e.g. <c>.omni-body</c> in our app shell);
    /// pass <c>"html"</c> for document-level scroll.
    /// Counter-based: matching <see cref="UnlockScrollAsync"/> calls release.
    /// </summary>
    public ValueTask LockScrollAsync(string selector = "auto")
        => Invoke("omniBlazor.lockScroll", selector);

    /// <summary>Release one lock acquired via <see cref="LockScrollAsync"/>.</summary>
    public ValueTask UnlockScrollAsync(string selector = "auto")
        => Invoke("omniBlazor.unlockScroll", selector);

    /// <summary>Diagnostic — current lock count for a selector. Mostly for tests.</summary>
    public async ValueTask<int> GetLockCountAsync(string selector = "auto")
    {
        try { return await _js.InvokeAsync<int>("omniBlazor.scrollLockCount", selector); }
        catch { return 0; }
    }

    /// <summary>
    /// Scroll <paramref name="selector"/> to a specific position. Defaults to
    /// the auto-detected scrolling container.
    /// </summary>
    public ValueTask ScrollToAsync(string? selector, double top = 0, double left = 0, ScrollBehavior behavior = ScrollBehavior.Auto)
        => Invoke("omniBlazor.scrollTo", selector ?? "auto", new { top, left, behavior = ToJs(behavior) });

    /// <summary>Bring an element into view (closest scrollable ancestor scrolls).</summary>
    public ValueTask ScrollIntoViewAsync(string selector, ScrollBehavior behavior = ScrollBehavior.Smooth, ScrollBlock block = ScrollBlock.Start)
        => Invoke("omniBlazor.scrollIntoView", selector, new { behavior = ToJs(behavior), block = ToJsBlock(block) });

    /// <summary>Scroll a container (or the auto-detected root) to the top.</summary>
    public ValueTask ScrollToTopAsync(string? selector = null, ScrollBehavior behavior = ScrollBehavior.Smooth)
        => Invoke("omniBlazor.scrollToTop", selector ?? "auto", ToJs(behavior));

    /// <summary>Scroll a container (or the auto-detected root) to the bottom.</summary>
    public ValueTask ScrollToBottomAsync(string? selector = null, ScrollBehavior behavior = ScrollBehavior.Smooth)
        => Invoke("omniBlazor.scrollToBottom", selector ?? "auto", ToJs(behavior));

    /// <summary>Read the current vertical scroll offset.</summary>
    public async ValueTask<double> GetScrollOffsetYAsync(string? selector = null)
    {
        try { return await _js.InvokeAsync<double>("omniBlazor.scrollOffsetY", selector ?? "auto"); }
        catch { return 0; }
    }

    /// <summary>
    /// Observa posição de scroll de um container, disparando <paramref name="handler"/>
    /// a cada mudança (throttle de ~1 frame via rAF, suporta ResizeObserver pra
    /// recalcular quando o conteúdo cresce). Dispose o retorno pra parar de observar.
    /// </summary>
    /// <param name="selector">Container a observar. <c>null</c>/<c>"auto"</c> detecta
    /// o scroll root principal (mesma lógica de <c>ScrollToTopAsync</c>).</param>
    /// <param name="handler">Callback async que recebe snapshot de scroll.</param>
    /// <returns><see cref="IAsyncDisposable"/> — <c>await using</c> ou armazene e
    /// <c>DisposeAsync</c> manualmente pra remover o listener.</returns>
    public async ValueTask<IAsyncDisposable> ObserveScrollPositionAsync(
        string? selector,
        Func<ScrollPositionInfo, Task> handler)
    {
        var observer = new ScrollObserver(_js, handler);
        await observer.StartAsync(selector ?? "auto");
        return observer;
    }

    /// <summary>
    /// Token interno que mantém DotNetObjectReference + handler vivos enquanto
    /// o observador está ativo. Dispose desregistra no JS e libera o ref C#.
    /// </summary>
    private sealed class ScrollObserver : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private readonly Func<ScrollPositionInfo, Task> _handler;
        private DotNetObjectReference<ScrollObserver>? _selfRef;
        private string? _token;
        private bool _disposed;

        public ScrollObserver(IJSRuntime js, Func<ScrollPositionInfo, Task> handler)
        {
            _js = js;
            _handler = handler;
        }

        public async Task StartAsync(string selector)
        {
            _selfRef = DotNetObjectReference.Create(this);
            try
            {
                _token = await _js.InvokeAsync<string?>(
                    "omniBlazor.observeScrollPosition",
                    selector,
                    _selfRef,
                    new { method = nameof(OnScrollInternal), callOnInit = true });
            }
            catch { /* SSR — token fica null, dispose no-op */ }
        }

        [JSInvokable]
        public Task OnScrollInternal(ScrollPositionInfo info) => _handler(info);

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_token is not null)
            {
                try { await _js.InvokeVoidAsync("omniBlazor.unobserveScrollPosition", _token); }
                catch { }
            }
            _selfRef?.Dispose();
            _selfRef = null;
        }
    }

    private async ValueTask Invoke(string fn, params object?[] args)
    {
        // Swallow JS-not-ready errors during SSR / prerender. Real failures
        // surface elsewhere; this stays quiet so a missing window doesn't
        // crash a render pass.
        try { await _js.InvokeVoidAsync(fn, args); }
        catch { }
    }

    private static string ToJs(ScrollBehavior b) => b switch
    {
        ScrollBehavior.Smooth  => "smooth",
        ScrollBehavior.Instant => "instant",
        _                      => "auto"
    };

    private static string ToJsBlock(ScrollBlock b) => b switch
    {
        ScrollBlock.Center  => "center",
        ScrollBlock.End     => "end",
        ScrollBlock.Nearest => "nearest",
        _                   => "start"
    };
}
