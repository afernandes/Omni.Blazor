using System.Text;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Fluent <c>style="..."</c> composer. Twin of <see cref="CssBuilder"/> for
/// inline styles. Joins declarations with <c>;</c>.
///
/// <code>
/// var style = StyleBuilder.Default("display:flex")
///     .AddStyle("min-width", $"{Width}px", Width > 0)
///     .AddStyle(Style)        // splat from consumer
///     .Build();
/// </code>
/// </summary>
public struct StyleBuilder
{
    private StringBuilder? _sb;

    private StyleBuilder(string? initial)
    {
        _sb = StringBuilderPool.Rent();
        if (!string.IsNullOrWhiteSpace(initial)) _sb.Append(initial.TrimEnd(';'));
    }

    public static StyleBuilder Default(string? initial = null) => new(initial);
    public static StyleBuilder Empty() => new(null);

    /// <summary>Append a raw declaration like <c>"display:flex"</c> (no trailing <c>;</c>).</summary>
    public StyleBuilder AddStyle(string? declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration)) return this;
        var sb = _sb ??= StringBuilderPool.Rent();
        if (sb.Length > 0) sb.Append("; ");
        sb.Append(declaration.TrimEnd(';'));
        return this;
    }

    public StyleBuilder AddStyle(string? declaration, bool when) => when ? AddStyle(declaration) : this;

    /// <summary>Append <c>"prop: value"</c>.</summary>
    public StyleBuilder AddStyle(string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return this;
        return AddStyle($"{property}: {value}");
    }

    public StyleBuilder AddStyle(string property, string? value, bool when) =>
        when ? AddStyle(property, value) : this;

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
}
