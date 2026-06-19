using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCardMedia"/>: image rendering,
/// position, height, cross-cutting splat.
/// </summary>
public class OmniCardMediaTests : TestContextBase
{
    [Fact]
    public void Renders_image_when_src_set()
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .Add(c => c.Alt, "Photo"));

        var img = cut.Find("img");
        Assert.Equal("/x.jpg", img.GetAttribute("src"));
        Assert.Equal("Photo", img.GetAttribute("alt"));
    }

    [Theory]
    [InlineData(CardMediaPosition.Top,     "omni-card-media-top")]
    [InlineData(CardMediaPosition.Bottom,  "omni-card-media-bottom")]
    [InlineData(CardMediaPosition.Overlay, "omni-card-media-overlay")]
    [InlineData(CardMediaPosition.Start,   "omni-card-media-start")]
    [InlineData(CardMediaPosition.End,     "omni-card-media-end")]
    public void Applies_position_class(CardMediaPosition pos, string expected)
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .Add(c => c.Position, pos));

        Assert.Contains(expected, cut.Find("div.omni-card-part").ClassName);
    }

    [Fact]
    public void Height_applies_to_inline_style()
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .Add(c => c.Height, "200px"));

        var style = cut.Find("div.omni-card-part").GetAttribute("style") ?? string.Empty;
        Assert.Contains("height: 200px", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .Add(c => c.Class, "media-x"));

        Assert.Contains("media-x", cut.Find("div.omni-card-part").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_concatenated_with_height()
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .Add(c => c.Style, "border-radius: 4px"));

        var style = cut.Find("div.omni-card-part").GetAttribute("style") ?? string.Empty;
        Assert.Contains("border-radius: 4px", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCardMedia>(p => p
            .Add(c => c.Src, "/x.jpg")
            .AddUnmatched("data-testid", "m1"));

        Assert.Equal("m1", cut.Find("div.omni-card-part").GetAttribute("data-testid"));
    }
}
