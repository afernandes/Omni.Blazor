using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniDescriptionList"/> + <see cref="OmniDescriptionItem"/>:
/// grid wrapper, column variable, bordered modifier, term/value rendering, splat.
/// </summary>
public class OmniDescriptionListTests : TestContextBase
{
    [Fact]
    public void Renders_container_and_inner_dl()
    {
        var cut = RenderComponent<OmniDescriptionList>();
        Assert.NotNull(cut.Find(".omni-desc > dl.omni-desc-grid"));
    }

    [Fact]
    public void Columns_sets_css_variable()
    {
        var cut = RenderComponent<OmniDescriptionList>(p => p.Add(c => c.Columns, 3));
        Assert.Contains("--omni-desc-cols:3", cut.Find(".omni-desc-grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Columns_below_one_clamps_to_one()
    {
        var cut = RenderComponent<OmniDescriptionList>(p => p.Add(c => c.Columns, 0));
        Assert.Contains("--omni-desc-cols:1", cut.Find(".omni-desc-grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Bordered_adds_modifier_class()
    {
        var cut = RenderComponent<OmniDescriptionList>(p => p.Add(c => c.Bordered, true));
        Assert.Contains("omni-desc-bordered", cut.Find(".omni-desc").ClassName);
    }

    [Fact]
    public void Item_renders_term_and_value()
    {
        var cut = RenderComponent<OmniDescriptionItem>(p => p
            .Add(c => c.Term, "Plano")
            .AddChildContent("Pro"));
        Assert.Equal("Plano", cut.Find(".omni-desc-term").TextContent);
        Assert.Equal("Pro", cut.Find(".omni-desc-value").TextContent);
    }

    [Fact]
    public void Renders_items_inside_list()
    {
        var cut = RenderComponent<OmniDescriptionList>(p => p
            .AddChildContent<OmniDescriptionItem>(i => i.Add(x => x.Term, "A").AddChildContent("1"))
            .AddChildContent<OmniDescriptionItem>(i => i.Add(x => x.Term, "B").AddChildContent("2")));
        Assert.Equal(2, cut.FindAll(".omni-desc-grid > .omni-desc-item").Count);
    }

    [Fact]
    public void List_appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniDescriptionList>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "d1"));
        var root = cut.Find(".omni-desc");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("d1", root.GetAttribute("data-testid"));
    }
}
