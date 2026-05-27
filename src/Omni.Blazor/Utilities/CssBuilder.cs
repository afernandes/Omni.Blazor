using System.Collections.Concurrent;
using System.Text;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Fluent class-name composer backed by a small pooled <see cref="StringBuilder"/>.
/// Inspired by MudBlazor's CssBuilder — reads naturally when composition has
/// many conditions and avoids the array allocations of a <c>params string?[]</c>
/// signature.
///
/// <code>
/// var css = CssBuilder.Default("omni-btn")
///     .AddClass($"omni-btn-{Variant.ToString().ToLowerInvariant()}")
///     .AddClass("omni-btn-icon-only", IconOnly)
///     .AddClass(Class)         // splat from consumer
///     .Build();
/// </code>
/// </summary>
public struct CssBuilder
{
    private StringBuilder? _sb;

    private CssBuilder(string? initial)
    {
        _sb = StringBuilderPool.Rent();
        if (!string.IsNullOrWhiteSpace(initial)) _sb.Append(initial);
    }

    /// <summary>Start a new builder with an optional initial class.</summary>
    public static CssBuilder Default(string? initial = null) => new(initial);

    /// <summary>Start an empty builder.</summary>
    public static CssBuilder Empty() => new(null);

    /// <summary>Append a class name. Null/empty values are ignored.</summary>
    public CssBuilder AddClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return this;
        var sb = EnsureSb();
        if (sb.Length > 0) sb.Append(' ');
        sb.Append(value);
        return this;
    }

    /// <summary>Append <paramref name="value"/> only when <paramref name="when"/> is true.</summary>
    public CssBuilder AddClass(string? value, bool when) => when ? AddClass(value) : this;

    /// <summary>Append <paramref name="value"/> only when <paramref name="when"/> returns true (lazy).</summary>
    public CssBuilder AddClass(string? value, Func<bool> when) => when() ? AddClass(value) : this;

    /// <summary>Append the result of another builder (e.g. from a derived class).</summary>
    public CssBuilder AddClass(CssBuilder other)
    {
        var s = other.Build();
        return AddClass(s);
    }

    /// <summary>
    /// Finalize the builder, returning the composed class string and returning
    /// the underlying buffer to the pool. Subsequent calls return the empty string.
    /// </summary>
    public string Build()
    {
        var sb = _sb;
        if (sb is null) return string.Empty;
        _sb = null;
        var result = sb.ToString();
        StringBuilderPool.Return(sb);
        return result;
    }

    public override string ToString() => Build();

    private StringBuilder EnsureSb() => _sb ??= StringBuilderPool.Rent();
}

/// <summary>
/// Minimal local pool — the BCL's internal StringBuilderCache isn't public.
/// Cap of 32 keeps memory bounded; overflow buffers are GC'd normally.
/// </summary>
internal static class StringBuilderPool
{
    private const int MaxPooled = 32;
    private const int MaxCapacity = 1024;
    private static readonly ConcurrentBag<StringBuilder> _bag = new();

    public static StringBuilder Rent()
    {
        if (_bag.TryTake(out var sb))
        {
            sb.Clear();
            return sb;
        }
        return new StringBuilder(128);
    }

    public static void Return(StringBuilder sb)
    {
        if (sb.Capacity > MaxCapacity) return;          // don't pool oversized buffers
        if (_bag.Count >= MaxPooled) return;            // bounded pool
        _bag.Add(sb);
    }
}
