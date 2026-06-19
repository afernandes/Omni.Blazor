using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniSparkline"/>: variants, root class,
/// cross-cutting splat.
/// </summary>
public class OmniSparklineTests : TestContextBase
{
    private static readonly double[] SampleData = { 1, 3, 2, 5, 4 };

    [Fact]
    public void Renders_default_line_variant()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData));

        var root = cut.Find("div.omni-sparkline");
        Assert.Contains("omni-sparkline", root.ClassName);
        Assert.Contains("omni-sparkline-line", root.ClassName);
    }

    [Theory]
    [InlineData(SparklineVariant.Line,   "omni-sparkline-line")]
    [InlineData(SparklineVariant.Area,   "omni-sparkline-area")]
    [InlineData(SparklineVariant.Column, "omni-sparkline-column")]
    [InlineData(SparklineVariant.Bar,    "omni-sparkline-bar")]
    [InlineData(SparklineVariant.Pie,    "omni-sparkline-pie")]
    public void Applies_variant_class(SparklineVariant variant, string expected)
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData)
            .Add(c => c.Variant, variant));

        Assert.Contains(expected, cut.Find("div.omni-sparkline").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData)
            .Add(c => c.Class, "my-spark"));

        Assert.Contains("my-spark", cut.Find("div.omni-sparkline").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData)
            .Add(c => c.Style, "width: 100px"));

        Assert.Equal("width: 100px", cut.Find("div.omni-sparkline").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData)
            .AddUnmatched("data-testid", "sp1"));

        Assert.Equal("sp1", cut.Find("div.omni-sparkline").GetAttribute("data-testid"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────

    [Fact]
    public void Normalize_runs_on_initial_render()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData));

        // Normalized array and point cache populated by RecomputeFromData()
        // through ParameterState first detect.
        Assert.Equal(SampleData.Length, cut.Instance._normalized.Length);
        Assert.Equal(SampleData.Length, cut.Instance._points.Length);
    }

    [Fact]
    public void Normalize_does_NOT_run_when_unrelated_parameter_changes()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData));

        var normalizedBefore = cut.Instance._normalized;
        var pointsBefore = cut.Instance._points;

        // Re-render only changing Class — Data reference unchanged.
        cut.Render(p => p.Add(c => c.Class, "new-class"));

        // Reference equality: RecomputeFromData() never ran, the arrays are
        // the exact same instances.
        Assert.Same(normalizedBefore, cut.Instance._normalized);
        Assert.Same(pointsBefore, cut.Instance._points);
    }

    [Fact]
    public void Normalize_runs_again_when_Data_reference_changes()
    {
        var cut = Render<OmniSparkline>(p => p
            .Add(c => c.Data, SampleData));

        var normalizedBefore = cut.Instance._normalized;

        // New array reference -> Data parameter "changed".
        cut.Render(p => p.Add(c => c.Data, new double[] { 10, 20, 30 }));

        Assert.NotSame(normalizedBefore, cut.Instance._normalized);
        Assert.Equal(3, cut.Instance._normalized.Length);
    }
}
