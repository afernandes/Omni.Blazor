using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Marketing;

/// <summary>
/// Behavioural contract for <see cref="OmniMosaicCard"/>: Name, Meta, Icon,
/// Wide/Featured modifiers, and cross-cutting splat.
/// </summary>
public class OmniMosaicCardTests : TestContextBase
{
    [Fact]
    public void Renders_root_div_with_base_class_and_name()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "Alpha"));

        var root = cut.Find("div.omni-mosaic-card");
        Assert.Contains("omni-mosaic-card", root.ClassName);
        Assert.Contains("Alpha", cut.Find(".omni-mosaic-nm").TextContent);
    }

    [Fact]
    public void Renders_meta_when_set()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Meta, "subtitle"));

        Assert.Contains("subtitle", cut.Find(".omni-mosaic-mt").TextContent);
    }

    [Fact]
    public void Skips_meta_div_when_unset()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A"));

        Assert.Empty(cut.FindAll(".omni-mosaic-mt"));
    }

    [Fact]
    public void Renders_icon_when_set()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Icon, "star"));

        Assert.NotNull(cut.Find(".omni-mosaic-ic"));
    }

    [Fact]
    public void Wide_adds_modifier_class()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Wide, true));

        Assert.Contains("omni-mosaic-card-wide", cut.Find("div.omni-mosaic-card").ClassName);
    }

    [Fact]
    public void Featured_adds_modifier_class()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Featured, true));

        Assert.Contains("omni-mosaic-card-featured", cut.Find("div.omni-mosaic-card").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Class, "card-fancy"));

        Assert.Contains("card-fancy", cut.Find("div.omni-mosaic-card").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .Add(c => c.Style, "padding: 4px"));

        Assert.Equal("padding: 4px", cut.Find("div.omni-mosaic-card").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMosaicCard>(p => p
            .Add(c => c.Name, "A")
            .AddUnmatched("data-testid", "mc1"));

        Assert.Equal("mc1", cut.Find("div.omni-mosaic-card").GetAttribute("data-testid"));
    }
}
