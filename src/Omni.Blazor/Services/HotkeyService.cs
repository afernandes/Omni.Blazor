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

    private static readonly IReadOnlyList<HotkeyCombo[]> NoSequences = Array.Empty<HotkeyCombo[]>();
    private static readonly char[] SeqSeparators = { ' ', ',' };   // sequence steps: "g d" or VS-style "Ctrl+K, Ctrl+D"

    /// <summary>
    /// Register from a spec string. Alternatives split on <c>|</c>; an alternative
    /// with whitespace or commas is a SEQUENCE — <c>"g d"</c> (GitHub/Linear "go to")
    /// or VS-style <c>"Ctrl+K, Ctrl+D"</c> — fired by pressing the steps in succession.
    /// Mix freely: <c>"Ctrl+K|g d"</c> registers both a combo and a sequence.
    /// Note: register distinct, non-overlapping sequences. A bare single key that is
    /// also a sequence's first step (<c>"g"</c> + <c>"g d"</c>) shadows the sequence,
    /// and a sequence that is a prefix of a longer one (<c>"g d"</c> + <c>"g d e"</c>)
    /// fires first — the longer never completes.
    /// </summary>
    public Task<IAsyncDisposable> RegisterAsync(
        string combo,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
    {
        var (singles, sequences) = ParseSpec(combo);
        if (singles.Count == 0 && sequences.Count == 0)
            throw new ArgumentException($"Could not parse any combo or sequence from '{combo}'.", nameof(combo));
        return RegisterCoreAsync(singles, sequences, handler, preventDefault, stopPropagation);
    }

    public Task<IAsyncDisposable> RegisterAsync(
        HotkeyCombo combo,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
        => RegisterCoreAsync(new[] { combo }, NoSequences, handler, preventDefault, stopPropagation);

    public Task<IAsyncDisposable> RegisterAsync(
        IEnumerable<HotkeyCombo> combos,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault = true,
        bool stopPropagation = false)
    {
        var comboArray = combos as IReadOnlyList<HotkeyCombo> ?? combos.ToArray();
        if (comboArray.Count == 0)
            throw new ArgumentException("At least one combo is required.", nameof(combos));
        return RegisterCoreAsync(comboArray, NoSequences, handler, preventDefault, stopPropagation);
    }

    private async Task<IAsyncDisposable> RegisterCoreAsync(
        IReadOnlyList<HotkeyCombo> combos,
        IReadOnlyList<HotkeyCombo[]> sequences,
        Func<KeyboardEventArgs, Task> handler,
        bool preventDefault,
        bool stopPropagation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);
        if (combos.Count == 0 && sequences.Count == 0)
            throw new ArgumentException("At least one combo or sequence is required.");

        var id = $"hk-{Guid.NewGuid():N}";
        var entry = new Entry(id, handler);
        _entries[id] = entry;
        _selfRef ??= DotNetObjectReference.Create(this);

        try
        {
            await _js.InvokeVoidAsync("omniBlazor.registerHotkey",
                id, _selfRef, nameof(OnHotkeyAsync),
                combos.Select(ToJs),
                sequences.Select(seq => seq.Select(ToJs)),
                preventDefault, stopPropagation);
        }
        catch
        {
            // Roll back partial state so a failed register doesn't leave a ghost entry.
            _entries.Remove(id);
            throw;
        }
        return new Handle(this, id);

        static object ToJs(HotkeyCombo c) => new
        {
            key   = c.Key,
            ctrl  = c.Modifiers.HasFlag(Modifier.Ctrl),
            alt   = c.Modifiers.HasFlag(Modifier.Alt),
            shift = c.Modifiers.HasFlag(Modifier.Shift),
            meta  = c.Modifiers.HasFlag(Modifier.Meta)
        };
    }

    /// <summary>
    /// Split a spec into single combos and sequences. <c>|</c> separates alternatives;
    /// an alternative with whitespace becomes an ordered sequence of combos.
    /// </summary>
    internal static (List<HotkeyCombo> singles, List<HotkeyCombo[]> sequences) ParseSpec(string spec)
    {
        var singles = new List<HotkeyCombo>();
        var sequences = new List<HotkeyCombo[]>();
        if (string.IsNullOrWhiteSpace(spec)) return (singles, sequences);

        foreach (var alt in spec.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // A sequence is space- or comma-separated combos, each parsing on its own
            // ("g d", "Ctrl+K Ctrl+D", or VS-style "Ctrl+K, Ctrl+D"). Requiring 2+ valid
            // steps means padding around '+' ("Ctrl + K") or a lone comma key ("Ctrl+,")
            // falls through and parses as a single combo instead of a dead sequence.
            if (alt.Contains(' ') || alt.Contains(','))
            {
                var steps = alt.Split(SeqSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (steps.Length >= 2)
                {
                    var combos = new List<HotkeyCombo>(steps.Length);
                    var ok = true;
                    foreach (var s in steps)
                    {
                        if (HotkeyCombo.TryParse(s, out var c)) combos.Add(c);
                        else { ok = false; break; }
                    }
                    if (ok) { sequences.Add(combos.ToArray()); continue; }
                }
            }
            if (HotkeyCombo.TryParse(alt, out var single)) singles.Add(single);
        }
        return (singles, sequences);
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
