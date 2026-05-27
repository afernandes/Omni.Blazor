using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Tracks the current responsive breakpoint by subscribing to a debounced
/// <c>window.resize</c> listener in JS. Scoped: one instance per circuit /
/// per WASM app. A single JS listener is shared across all subscribers so we
/// don't pay 1 callback per consumer.
///
/// Typical usage in code-behind:
/// <code>
/// var sub = await Breakpoints.SubscribeAsync(bp => isMobile = bp &lt;= Breakpoint.Sm);
/// // ... later
/// await sub.DisposeAsync();
/// </code>
///
/// Or declaratively wrap a tree with <c>OmniBreakpointProvider</c>.
/// </summary>
public sealed class BreakpointService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, Func<Breakpoint, Task>> _subscribers = new();
    private DotNetObjectReference<BreakpointService>? _selfRef;
    private Breakpoint _current = Breakpoint.Md;
    private bool _initialized;
    private bool _disposed;

    public BreakpointService(IJSRuntime js) => _js = js;

    /// <summary>Last known breakpoint. Defaults to Md until the first resize event.</summary>
    public Breakpoint Current => _current;

    /// <summary>Number of active subscribers (for leak tests).</summary>
    public int SubscriberCount => _subscribers.Count;

    /// <summary>
    /// Subscribe to breakpoint changes. The handler fires once immediately with
    /// the current breakpoint (so consumers don't have to deal with an
    /// uninitialized first frame) and on every change thereafter.
    /// </summary>
    /// <remarks>
    /// We always call <c>omniBlazor.subscribeViewport</c> here, regardless of
    /// the cached <see cref="_initialized"/> flag. The JS side is idempotent
    /// (<c>viewportSubs.set</c> replaces, listener attach is guarded), and
    /// always re-attaching defends against a race condition during navigation
    /// between layouts: when an outgoing layout's drawer disposes, it fires
    /// an async <c>unsubscribeViewport</c> that races with the new layout's
    /// drawer mounting and calling <c>SubscribeAsync</c>. If we trusted the
    /// cached flag, the new drawer would skip the JS call and the resize
    /// listener would stay detached.
    /// </remarks>
    public async Task<IAsyncDisposable> SubscribeAsync(Func<Breakpoint, Task> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        var id = $"bp-{Guid.NewGuid():N}";
        _subscribers[id] = handler;
        _selfRef ??= DotNetObjectReference.Create(this);

        try
        {
            var bp = await _js.InvokeAsync<string>("omniBlazor.subscribeViewport",
                "service", _selfRef, nameof(OnBreakpointChanged));
            _current = Parse(bp);
            _initialized = true;
        }
        catch { /* SSR / pre-interactive — keep last known bp */ }

        // Fire the initial value to the new subscriber immediately.
        try { await handler(_current); } catch { }
        return new Handle(this, id);
    }

    /// <summary>Convenience overload taking a synchronous action.</summary>
    public Task<IAsyncDisposable> SubscribeAsync(Action<Breakpoint> handler)
        => SubscribeAsync(bp => { handler(bp); return Task.CompletedTask; });

    [JSInvokable]
    public async Task OnBreakpointChanged(string name)
    {
        var bp = Parse(name);
        if (bp == _current) return;
        _current = bp;
        // Snapshot the handlers — a subscriber may dispose itself inside the callback.
        var snapshot = _subscribers.Values.ToArray();
        foreach (var h in snapshot)
        {
            try { await h(bp); } catch { }
        }
    }

    private static Breakpoint Parse(string name) => name switch
    {
        "xs"  => Breakpoint.Xs,
        "sm"  => Breakpoint.Sm,
        "md"  => Breakpoint.Md,
        "lg"  => Breakpoint.Lg,
        "xl"  => Breakpoint.Xl,
        "xxl" => Breakpoint.Xxl,
        _     => Breakpoint.Md
    };

    private async ValueTask RemoveAsync(string id)
    {
        _subscribers.Remove(id);
        if (_subscribers.Count == 0 && _initialized && !_disposed)
        {
            try { await _js.InvokeVoidAsync("omniBlazor.unsubscribeViewport", "service"); } catch { }
            _initialized = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _subscribers.Clear();
        if (_initialized)
        {
            try { await _js.InvokeVoidAsync("omniBlazor.unsubscribeViewport", "service"); } catch { }
            _initialized = false;
        }
        _selfRef?.Dispose();
        _selfRef = null;
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly BreakpointService _svc;
        private readonly string _id;
        private bool _disposed;

        public Handle(BreakpointService svc, string id) { _svc = svc; _id = id; }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _svc.RemoveAsync(_id);
        }
    }
}

/// <summary>Convenience comparisons on <see cref="Breakpoint"/>.</summary>
public static class BreakpointExtensions
{
    public static bool IsMobile(this Breakpoint bp) => bp <= Breakpoint.Sm;
    public static bool IsTablet(this Breakpoint bp) => bp == Breakpoint.Md;
    public static bool IsDesktop(this Breakpoint bp) => bp >= Breakpoint.Lg;
    public static bool AtLeast(this Breakpoint bp, Breakpoint min) => bp >= min;
    public static bool AtMost(this Breakpoint bp, Breakpoint max) => bp <= max;
}
