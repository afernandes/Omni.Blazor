using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCardGroup"/>: orientation + responsive
/// classes, child rendering, and the cross-cutting splat.
/// </summary>
public class OmniCardGroupTests : TestContextBase
{
    [Fact]
    public void Renders_root_horizontal_responsive_by_default()
    {
        var cut = RenderComponent<OmniCardGroup>();
        var root = cut.Find(".omni-card-group");
        Assert.Contains("omni-card-group-horizontal", root.ClassName);
        Assert.Contains("omni-card-group-responsive", root.ClassName);
    }

    [Fact]
    public void Vertical_uses_vertical_class_and_drops_responsive()
    {
        var cut = RenderComponent<OmniCardGroup>(p => p.Add(c => c.Orientation, Orientation.Vertical));
        var root = cut.Find(".omni-card-group");
        Assert.Contains("omni-card-group-vertical", root.ClassName);
        Assert.DoesNotContain("omni-card-group-horizontal", root.ClassName);
        // responsive only applies to horizontal
        Assert.DoesNotContain("omni-card-group-responsive", root.ClassName);
    }

    [Fact]
    public void Responsive_false_removes_responsive_class()
    {
        var cut = RenderComponent<OmniCardGroup>(p => p.Add(c => c.Responsive, false));
        Assert.DoesNotContain("omni-card-group-responsive", cut.Find(".omni-card-group").ClassName);
    }

    [Fact]
    public void Renders_card_children()
    {
        var cut = RenderComponent<OmniCardGroup>(p => p
            .AddChildContent<OmniCard>(c => c.Add(x => x.Title, "One"))
            .AddChildContent<OmniCard>(c => c.Add(x => x.Title, "Two")));
        Assert.Equal(2, cut.FindAll(".omni-card-group > .omni-card").Count);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniCardGroup>(p => p
            .Add(c => c.Class, "kpis")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "cg1"));
        var root = cut.Find(".omni-card-group");
        Assert.Contains("kpis", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("cg1", root.GetAttribute("data-testid"));
    }
}
