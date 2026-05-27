using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Description of a single key combination an interceptor should react to.
/// Modifier flags left as <c>null</c> mean "don't care" (matches with or
/// without that modifier). Set them explicitly to require a specific state.
/// </summary>
public sealed record KeyInterceptorOption(
    string Key,
    bool? Ctrl  = null,
    bool? Alt   = null,
    bool? Shift = null,
    bool? Meta  = null,
    bool PreventDefault  = false,
    bool StopPropagation = false);

/// <summary>
/// Element-scoped keyboard listener. Complement to <see cref="HotkeyService"/>:
/// where Hotkey listens at the document level for app-wide shortcuts,
/// KeyInterceptor listens on a specific element (and its descendants) — ideal
/// for ESC inside a dialog, arrow nav inside a list, or Enter-to-confirm
/// inside an inline edit row.
///
/// Memory-leak defenses mirror <see cref="HotkeyService"/>:
///   • One <see cref="DotNetObjectReference"/> per service, lazily created.
///   • Each <c>AttachAsync</c> returns an <see cref="IAsyncDisposable"/>;
///     Dispose removes the entry from the dictionary AND removes the
///     listener from the DOM element.
///   • <see cref="DisposeAsync"/> on the service detaches every remaining
///     listener (defense in depth for consumers that forget).
/// </summary>
public sealed class KeyInterceptorService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, Entry> _entries = new();
    private DotNetObjectReference<KeyInterceptorService>? _selfRef;
    private bool _disposed;

    public KeyInterceptorService(IJSRuntime js) => _js = js;

    /// <summary>Number of active interceptors (for leak tests).</summary>
    public int AttachmentCount => _entries.Count;

    /// <summary>
    /// Attach a keydown interceptor to <paramref name="element"/>. Returns an
    /// <see cref="IAsyncDisposable"/> that detaches when disposed. (Primary
    /// overload — convenience variants below forward here.)
    /// </summary>
    public async Task<IAsyncDisposable> AttachAsync(
        ElementReference element,
        IEnumerable<KeyInterceptorOption> keys,
        Func<string, KeyboardEventArgs, Task> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        var keyArray = keys as KeyInterceptorOption[] ?? keys.ToArray();
        if (keyArray.Length == 0)
            throw new ArgumentException("At least one key is required.", nameof(keys));

        var id = $"ki-{Guid.NewGuid():N}";
        _entries[id] = new Entry(id, handler);
        _selfRef ??= DotNetObjectReference.Create(this);

        try
        {
            await _js.InvokeVoidAsync("omniBlazor.attachKeyListener",
                id, element, _selfRef, nameof(OnKeyAsync),
                keyArray.Select(k => new
                {
                    key = k.Key,
                    ctrl  = k.Ctrl,
                    alt   = k.Alt,
                    shift = k.Shift,
                    meta  = k.Meta,
                    preventDefault  = k.PreventDefault,
                    stopPropagation = k.StopPropagation
                }));
        }
        catch
        {
            _entries.Remove(id);
            throw;
        }
        return new Handle(this, id);
    }

    /// <summary>Convenience: attach with a single key + no modifiers.</summary>
    public Task<IAsyncDisposable> AttachAsync(
        ElementReference element,
        string key,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = false,
        bool stopPropagation = false)
        => AttachAsync(element,
            new[] { new KeyInterceptorOption(key, PreventDefault: preventDefault, StopPropagation: stopPropagation) },
            (_, args) => handler(args));

    /// <summary>Convenience: attach with a parsed combo string ("Ctrl+S", "Escape").</summary>
    public Task<IAsyncDisposable> AttachAsync(
        ElementReference element,
        HotkeyCombo combo,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = false,
        bool stopPropagation = false)
        => AttachAsync(element,
            new[] { new KeyInterceptorOption(
                combo.Key,
                Ctrl:  combo.Modifiers.HasFlag(Modifier.Ctrl),
                Alt:   combo.Modifiers.HasFlag(Modifier.Alt),
                Shift: combo.Modifiers.HasFlag(Modifier.Shift),
                Meta:  combo.Modifiers.HasFlag(Modifier.Meta),
                PreventDefault: preventDefault,
                StopPropagation: stopPropagation) },
            (_, args) => handler(args));

    [JSInvokable]
    public async Task OnKeyAsync(string id, string key, KeyboardEventArgs args)
    {
        if (_entries.TryGetValue(id, out var entry))
        {
            try { await entry.Handler(key, args); } catch { /* keep dispatch alive */ }
        }
    }

    private async ValueTask DetachAsync(string id)
    {
        if (!_entries.Remove(id)) return;
        if (_disposed) return;
        try { await _js.InvokeVoidAsync("omniBlazor.detachKeyListener", id); }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        var ids = _entries.Keys.ToArray();
        foreach (var id in ids)
        {
            _entries.Remove(id);
            try { await _js.InvokeVoidAsync("omniBlazor.detachKeyListener", id); }
            catch { }
        }

        _selfRef?.Dispose();
        _selfRef = null;
    }

    private sealed record Entry(string Id, Func<string, KeyboardEventArgs, Task> Handler);

    private sealed class Handle : IAsyncDisposable
    {
        private readonly KeyInterceptorService _svc;
        private readonly string _id;
        private bool _disposed;

        public Handle(KeyInterceptorService svc, string id) { _svc = svc; _id = id; }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _svc.DetachAsync(_id);
        }
    }
}
