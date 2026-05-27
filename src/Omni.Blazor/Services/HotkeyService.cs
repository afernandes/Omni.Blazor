using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Registry of keyboard shortcuts. Scoped: one instance per circuit (Server) or
/// per app (WASM). Components typically use <c>OmniHotkey</c>; code-behind /
/// service consumers call <see cref="RegisterAsync(string, Func{KeyboardEventArgs, Task}, bool, bool)"/>
/// directly.
///
/// Memory-leak defenses:
///   • A single <see cref="DotNetObjectReference"/> for the whole service. JS
///     calls back into <see cref="OnHotkeyAsync"/> with the registration id;
///     the service dispatches to the correct handler. Individual registrations
///     never own their own DotNet refs.
///   • Each <see cref="RegisterAsync(string, Func{KeyboardEventArgs, Task}, bool, bool)"/>
///     returns an <see cref="IAsyncDisposable"/>. Disposing it removes the
///     entry from the dictionary and tells JS to unregister.
///   • Dispose handles are idempotent — calling Dispose twice is safe.
///   • <see cref="DisposeAsync"/> on the service iterates remaining handles and
///     unregisters each (defense in depth for consumers that forget).
/// </summary>
public sealed class HotkeyService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, Entry> _entries = new();
    private DotNetObjectReference<HotkeyService>? _selfRef;
    private bool _disposed;

    public HotkeyService(IJSRuntime js) => _js = js;

    /// <summary>Number of active registrations (handy for leak tests).</summary>
    public int RegistrationCount => _entries.Count;

    public Task<IAsyncDisposable> RegisterAsync(
        string combo,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
    {
        var combos = HotkeyCombo.ParseMany(combo);
        if (combos.Length == 0)
            throw new ArgumentException($"Could not parse any combo from '{combo}'.", nameof(combo));
        return RegisterAsync(combos, handler, preventDefault, stopPropagation);
    }

    public Task<IAsyncDisposable> RegisterAsync(
        HotkeyCombo combo,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
        => RegisterAsync(new[] { combo }, handler, preventDefault, stopPropagation);

    public async Task<IAsyncDisposable> RegisterAsync(
        IEnumerable<HotkeyCombo> combos,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        var comboArray = combos as HotkeyCombo[] ?? combos.ToArray();
        if (comboArray.Length == 0)
            throw new ArgumentException("At least one combo is required.", nameof(combos));

        var id = $"hk-{Guid.NewGuid():N}";
        var entry = new Entry(id, handler);
        _entries[id] = entry;
        _selfRef ??= DotNetObjectReference.Create(this);

        try
        {
            await _js.InvokeVoidAsync("omniBlazor.registerHotkey",
                id, _selfRef, nameof(OnHotkeyAsync),
                comboArray.Select(c => new
                {
                    key   = c.Key,
                    ctrl  = c.Modifiers.HasFlag(Modifier.Ctrl),
                    alt   = c.Modifiers.HasFlag(Modifier.Alt),
                    shift = c.Modifiers.HasFlag(Modifier.Shift),
                    meta  = c.Modifiers.HasFlag(Modifier.Meta)
                }),
                preventDefault, stopPropagation);
        }
        catch
        {
            // Roll back partial state so a failed register doesn't leave a ghost entry.
            _entries.Remove(id);
            throw;
        }
        return new Handle(this, id);
    }

    /// <summary>Toggle a registration without unregistering it.</summary>
    public async Task SetDisabledAsync(IAsyncDisposable handle, bool disabled)
    {
        if (handle is Handle h && !h.Disposed && _entries.ContainsKey(h.Id))
        {
            try { await _js.InvokeVoidAsync("omniBlazor.setHotkeyDisabled", h.Id, disabled); }
            catch { }
        }
    }

    [JSInvokable]
    public async Task OnHotkeyAsync(string id, KeyboardEventArgs args)
    {
        if (_entries.TryGetValue(id, out var entry))
        {
            try { await entry.Handler(args); }
            catch { /* swallow handler exceptions to keep dispatch alive */ }
        }
    }

    private async ValueTask UnregisterAsync(string id)
    {
        if (!_entries.Remove(id)) return;
        if (_disposed) return;
        try { await _js.InvokeVoidAsync("omniBlazor.unregisterHotkey", id); }
        catch { /* JS may already be gone during teardown */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Snapshot ids before iterating — UnregisterAsync mutates the dictionary.
        var ids = _entries.Keys.ToArray();
        foreach (var id in ids)
        {
            _entries.Remove(id);
            try { await _js.InvokeVoidAsync("omniBlazor.unregisterHotkey", id); }
            catch { }
        }

        _selfRef?.Dispose();
        _selfRef = null;
    }

    private sealed record Entry(string Id, Func<KeyboardEventArgs, Task> Handler);

    private sealed class Handle : IAsyncDisposable
    {
        private readonly HotkeyService _svc;
        public string Id { get; }
        public bool Disposed { get; private set; }

        public Handle(HotkeyService svc, string id) { _svc = svc; Id = id; }

        public async ValueTask DisposeAsync()
        {
            if (Disposed) return;
            Disposed = true;
            await _svc.UnregisterAsync(Id);
        }
    }
}
