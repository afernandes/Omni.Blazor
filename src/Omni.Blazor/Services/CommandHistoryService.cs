using System.Text.Json;
using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>
/// Most-recently-used tracking for <c>OmniCommandPalette</c>. Records which
/// command labels were run and persists the order to <c>localStorage</c> so the
/// palette can surface "recent" commands and break ranking ties by recency.
///
/// Scoped (one per circuit on Server, one per app on WASM). Persistence is
/// best-effort — every JS call is guarded so prerender / a missing DOM never
/// throws. A monotonic counter (not a wall clock) orders entries, so it needs no
/// time source and stays stable across reloads.
/// </summary>
public sealed class CommandHistoryService
{
    private readonly IJSRuntime _js;

    // key -> (command label -> recency rank; larger = more recent)
    private readonly Dictionary<string, Dictionary<string, long>> _cache = new();
    private readonly Dictionary<string, long> _counter = new();

    public CommandHistoryService(IJSRuntime js) => _js = js;

    private static string StorageKey(string ns) => $"omni-cmdk-mru:{ns}";

    /// <summary>Load (and cache) the recency map for a namespace. Empty if nothing stored.</summary>
    public async Task<IReadOnlyDictionary<string, long>> LoadAsync(string ns)
    {
        if (_cache.TryGetValue(ns, out var cached)) return cached;

        var map = new Dictionary<string, long>();
        try
        {
            var json = await _js.InvokeAsync<string?>("omniBlazor.storageGet", StorageKey(ns));
            if (!string.IsNullOrEmpty(json))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
                if (parsed is not null) map = parsed;
            }
        }
        catch { /* prerender / no storage / bad json — start empty */ }

        _cache[ns] = map;
        _counter[ns] = map.Count == 0 ? 0 : map.Values.Max();
        return map;
    }

    /// <summary>Mark a command label as just used and persist.</summary>
    public async Task RecordAsync(string ns, string? label)
    {
        if (string.IsNullOrEmpty(label)) return;

        var map = _cache.TryGetValue(ns, out var m) ? m : (Dictionary<string, long>)await LoadAsync(ns);
        var next = (_counter.TryGetValue(ns, out var c) ? c : 0) + 1;
        _counter[ns] = next;
        map[label] = next;

        try { await _js.InvokeVoidAsync("omniBlazor.storageSet", StorageKey(ns), JsonSerializer.Serialize(map)); }
        catch { /* best-effort */ }
    }

    /// <summary>Clear a namespace's history (memory + storage).</summary>
    public async Task ClearAsync(string ns)
    {
        _cache[ns] = new();
        _counter[ns] = 0;
        try { await _js.InvokeVoidAsync("omniBlazor.storageRemove", StorageKey(ns)); }
        catch { /* best-effort */ }
    }
}
