using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniChart"/>: renders SVG host, applies
/// width/height, and supports the cross-cutting splat.
/// </summary>
public class OmniChartTests : TestContextBase
{
    private static ChartSeries SampleLine() => new()
    {
        Title = "Sales",
        Type = ChartSeriesType.Line,
        Points = new[]
        {
            new ChartDataPoint { Category = "Jan", Value = 10 },
            new ChartDataPoint { Category = "Feb", Value = 20 },
            new ChartDataPoint { Category = "Mar", Value = 15 },
        }
    };

    [Fact]
    public void Renders_default_chart_root_and_svg()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() }));

        var root = cut.Find("div.omni-chart");
        Assert.Contains("omni-chart", root.ClassName);
        Assert.NotNull(cut.Find("svg.omni-chart-svg"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() })
            .Add(c => c.Class, "my-chart"));

        Assert.Contains("my-chart", cut.Find("div.omni-chart").ClassName);
    }

    [Fact]
    public void Width_height_applied_via_inline_style()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() })
            .Add(c => c.Width, "400px")
            .Add(c => c.Height, "120px"));

        var style = cut.Find("div.omni-chart").GetAttribute("style") ?? string.Empty;
        Assert.Contains("width:400px", style);
        Assert.Contains("height:120px", style);
    }

    [Fact]
    public void Forwards_consumer_Style_concatenated_with_size()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() })
            .Add(c => c.Style, "border: 1px solid red"));

        var style = cut.Find("div.omni-chart").GetAttribute("style") ?? string.Empty;
        Assert.Contains("border: 1px solid red", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() })
            .AddUnmatched("data-testid", "chart1"));

        Assert.Equal("chart1", cut.Find("div.omni-chart").GetAttribute("data-testid"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────

    [Fact]
    public void RecomputeFromSeries_runs_on_initial_render()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() }));

        // _series filter + _categories derive populated by first detect.
        Assert.Single(cut.Instance._series);
        Assert.Equal(3, cut.Instance._categories.Length);
    }

    [Fact]
    public void RecomputeFromSeries_does_NOT_run_when_unrelated_parameter_changes()
    {
        var seriesArr = new[] { SampleLine() };
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, seriesArr));

        var seriesBefore = cut.Instance._series;
        var catsBefore = cut.Instance._categories;

        cut.SetParametersAndRender(p => p.Add(c => c.Class, "new-class"));

        // Same list/array instances — RecomputeFromSeries() never ran.
        Assert.Same(seriesBefore, cut.Instance._series);
        Assert.Same(catsBefore, cut.Instance._categories);
    }

    [Fact]
    public void RecomputeFromSeries_runs_again_when_Series_reference_changes()
    {
        var cut = RenderComponent<OmniChart>(p => p
            .Add(c => c.Series, new[] { SampleLine() }));

        var seriesBefore = cut.Instance._series;

        var newSeries = new ChartSeries
        {
            Title = "Other",
            Type = ChartSeriesType.Column,
            Points = new[]
            {
                new ChartDataPoint { Category = "Q1", Value = 5 },
                new ChartDataPoint { Category = "Q2", Value = 8 },
            }
        };
        cut.SetParametersAndRender(p => p.Add(c => c.Series, new[] { newSeries }));

        Assert.NotSame(seriesBefore, cut.Instance._series);
        Assert.Equal(2, cut.Instance._categories.Length);
    }
}
