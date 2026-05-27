using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniMain"/>: the &lt;main&gt; slot of
/// the layout grid, with optional NoPadding + Container wrap.
/// </summary>
public class OmniMainTests : TestContextBase
{
    [Fact]
    public void Renders_main_element_with_base_classes()
    {
        var cut = RenderComponent<OmniMain>(p => p.AddChildContent("body"));

        var main = cut.Find("main");
        Assert.Contains("omni-body", main.ClassName);
        Assert.Contains("omni-main", main.ClassName);
        Assert.Equal("main", main.Id);
        Assert.Equal("-1", main.GetAttribute("tabindex"));
        Assert.Contains("body", main.TextContent);
    }

    [Fact]
    public void NoPadding_applies_modifier_class()
    {
        var cut = RenderComponent<OmniMain>(p => p
            .Add(c => c.NoPadding, true)
            .AddChildContent("X"));

        Assert.Contains("omni-main-no-padding", cut.Find("main").ClassName);
    }

    [Fact]
    public void Container_param_wraps_children_in_OmniContainer()
    {
        var cut = RenderComponent<OmniMain>(p => p
            .Add(c => c.Container, ContainerMaxWidth.Xl)
            .AddChildContent("body"));

        // OmniContainer renders a div.omni-container inside main.
        var container = cut.Find("main .omni-container");
        Assert.NotNull(container);
        Assert.Contains("omni-container-xl", container.ClassName);
    }

    [Fact]
    public void Container_null_renders_children_directly()
    {
        var cut = RenderComponent<OmniMain>(p => p.AddChildContent("<span data-testid=\"raw\">x</span>"));

        Assert.Empty(cut.FindAll("main .omni-container"));
        Assert.NotNull(cut.Find("[data-testid='raw']"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMain>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("main").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMain>(p => p
            .Add(c => c.Style, "padding: 0")
            .AddChildContent("X"));

        Assert.Equal("padding: 0", cut.Find("main").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMain>(p => p
            .AddUnmatched("data-testid", "main")
            .AddUnmatched("aria-label", "Main")
            .AddChildContent("X"));

        var main = cut.Find("main");
        Assert.Equal("main", main.GetAttribute("data-testid"));
        Assert.Equal("Main", main.GetAttribute("aria-label"));
    }
}
