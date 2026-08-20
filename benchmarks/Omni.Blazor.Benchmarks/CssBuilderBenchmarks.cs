using BenchmarkDotNet.Attributes;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Benchmarks;

/// <summary>
/// The hottest path in the library: every component builds its root class string
/// through <see cref="CssBuilder"/> on every single render, so its allocation cost
/// is multiplied by component count times render count.
///
/// The shapes below are copied from real components rather than invented, so the
/// numbers describe what actually ships:
/// <list type="bullet">
///   <item><see cref="Typical"/> — an OmniButton-sized chain: base class, two
///   conditional modifiers, a switch-selected variant and the consumer's Class.</item>
///   <item><see cref="AllConditionsFalse"/> — the common case in practice, where
///   most modifiers are off. Worth its own measurement because a builder that only
///   allocates for appended values behaves very differently here.</item>
///   <item><see cref="LongChain"/> — an OmniDataGrid-sized chain, the upper end of
///   what any component in the library does.</item>
/// </list>
/// </summary>
[MemoryDiagnoser]
public class CssBuilderBenchmarks
{
    private const string ConsumerClass = "my-app-button";

    [Benchmark(Baseline = true)]
    public string Typical() => CssBuilder.Default("omni-btn")
        .AddClass("omni-btn-primary")
        .AddClass("omni-btn-icon", true)
        .AddClass("omni-btn-block", false)
        .AddClass("omni-btn-loading", false)
        .AddClass(ConsumerClass)
        .Build();

    [Benchmark]
    public string AllConditionsFalse() => CssBuilder.Default("omni-btn")
        .AddClass("omni-btn-icon", false)
        .AddClass("omni-btn-block", false)
        .AddClass("omni-btn-loading", false)
        .AddClass((string?)null)
        .Build();

    [Benchmark]
    public string LongChain() => CssBuilder.Default("omni-data-grid")
        .AddClass("omni-data-grid-striped", true)
        .AddClass("omni-data-grid-bordered", true)
        .AddClass("omni-data-grid-hover", true)
        .AddClass("omni-data-grid-compact", false)
        .AddClass("omni-data-grid-sticky", true)
        .AddClass("omni-data-grid-grouped", false)
        .AddClass("omni-data-grid-virtualized", true)
        .AddClass("omni-data-grid-selectable", true)
        .AddClass(ConsumerClass)
        .Build();

    [Benchmark]
    public string StyleTypical() => StyleBuilder.Default("display:flex")
        .AddStyle("gap", "8px")
        .AddStyle("min-width", "0", true)
        .AddStyle("max-height", "240px", false)
        .Build();
}
