using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniStatGroup"/>: grid wrapper, column
/// variable, child rendering, and the cross-cutting splat.
/// </summary>
public class OmniStatGroupTests : TestContextBase
{
    [Fact]
    public void Renders_container_and_inner_grid()
    {
        var cut = RenderComponent<OmniStatGroup>();
        Assert.NotNull(cut.Find(".omni-stat-group > .omni-stat-group-grid"));
    }

    [Fact]
    public void Columns_sets_css_variable_on_grid()
    {
        var cut = RenderComponent<OmniStatGroup>(p => p.Add(c => c.Columns, 3));
        Assert.Contains("--omni-stat-cols:3", cut.Find(".omni-stat-group-grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Columns_below_one_clamps_to_one()
    {
        var cut = RenderComponent<OmniStatGroup>(p => p.Add(c => c.Columns, 0));
        Assert.Contains("--omni-stat-cols:1", cut.Find(".omni-stat-group-grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Renders_stat_children_inside_grid()
    {
        var cut = RenderComponent<OmniStatGroup>(p => p
            .AddChildContent<OmniStat>(s => s.Add(x => x.Value, "1"))
            .AddChildContent<OmniStat>(s => s.Add(x => x.Value, "2")));
        Assert.Equal(2, cut.FindAll(".omni-stat-group-grid > .omni-stat").Count);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniStatGroup>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "g1"));
        var root = cut.Find(".omni-stat-group");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("g1", root.GetAttribute("data-testid"));
    }
}
