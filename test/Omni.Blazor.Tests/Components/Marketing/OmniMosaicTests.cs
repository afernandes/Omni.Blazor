using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Marketing;

/// <summary>
/// Behavioural contract for <see cref="OmniMosaic"/>: layout wrapper that
/// hosts <see cref="OmniMosaicCard"/> children and supports the cross-cutting splat.
/// </summary>
public class OmniMosaicTests : TestContextBase
{
    [Fact]
    public void Renders_root_div_with_base_class()
    {
        var cut = Render<OmniMosaic>(p => p.AddChildContent("body"));

        var root = cut.Find("div.omni-mosaic");
        Assert.Contains("omni-mosaic", root.ClassName);
    }

    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<OmniMosaic>(p => p.AddChildContent("<span class='child'>x</span>"));

        Assert.NotNull(cut.Find("span.child"));
    }

    [Fact]
    public void Hosts_OmniMosaicCard_children()
    {
        var cut = Render<OmniMosaic>(p => p
            .AddChildContent<OmniMosaicCard>(c => c.Add(x => x.Name, "Card A")));

        Assert.NotNull(cut.Find("div.omni-mosaic"));
        Assert.NotNull(cut.Find("div.omni-mosaic-card"));
        Assert.Contains("Card A", cut.Find(".omni-mosaic-nm").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniMosaic>(p => p
            .Add(c => c.Class, "my-grid")
            .AddChildContent("x"));

        Assert.Contains("my-grid", cut.Find("div.omni-mosaic").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniMosaic>(p => p
            .Add(c => c.Style, "gap: 8px")
            .AddChildContent("x"));

        Assert.Equal("gap: 8px", cut.Find("div.omni-mosaic").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniMosaic>(p => p
            .AddUnmatched("data-testid", "mosaic1")
            .AddChildContent("x"));

        Assert.Equal("mosaic1", cut.Find("div.omni-mosaic").GetAttribute("data-testid"));
    }
}
