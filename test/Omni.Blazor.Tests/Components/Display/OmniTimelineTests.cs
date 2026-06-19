using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniTimeline"/> / <see cref="OmniTimelineItem"/>:
/// root + orientation/line-position modifier classes, the three item regions,
/// point style/variant/size classes, content slots, and the cross-cutting splat.
/// </summary>
public class OmniTimelineTests : TestContextBase
{
    private static RenderFragment TwoItems() => b =>
    {
        b.OpenComponent<OmniTimelineItem>(0);
        b.AddAttribute(1, nameof(OmniTimelineItem.Label), "A");
        b.AddAttribute(2, nameof(OmniTimelineItem.Text), "First");
        b.CloseComponent();

        b.OpenComponent<OmniTimelineItem>(3);
        b.AddAttribute(4, nameof(OmniTimelineItem.Label), "B");
        b.AddAttribute(5, nameof(OmniTimelineItem.Text), "Second");
        b.CloseComponent();
    };

    private IRenderedComponent<OmniTimeline> RenderTimeline(
        Action<ComponentParameterCollectionBuilder<OmniTimeline>>? extra = null)
        => Render<OmniTimeline>(p =>
        {
            p.Add(t => t.ChildContent, TwoItems());
            extra?.Invoke(p);
        });

    // ─── Cross-cutting ────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_with_base_and_default_classes()
    {
        var cut = RenderTimeline();
        var root = cut.Find("div.omni-timeline");
        Assert.Contains("omni-timeline", root.ClassName);
        Assert.Contains("omni-timeline-vertical", root.ClassName);   // default Orientation
        Assert.Contains("omni-timeline-center", root.ClassName);     // default LinePosition
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderTimeline(p => p.Add(t => t.Class, "my-tl"));
        Assert.Contains("my-tl", cut.Find("div.omni-timeline").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderTimeline(p => p.Add(t => t.Style, "--omni-timeline-line-width: 4px"));
        Assert.Contains("--omni-timeline-line-width: 4px", cut.Find("div.omni-timeline").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderTimeline(p => p.AddUnmatched("data-testid", "tl1"));
        Assert.Equal("tl1", cut.Find("div.omni-timeline").GetAttribute("data-testid"));
    }

    // ─── Layout / modifiers ───────────────────────────────────────────────

    [Theory]
    [InlineData(Orientation.Vertical, "omni-timeline-vertical")]
    [InlineData(Orientation.Horizontal, "omni-timeline-horizontal")]
    public void Orientation_maps_to_root_class(Orientation o, string expected)
    {
        var cut = RenderTimeline(p => p.Add(t => t.Orientation, o));
        Assert.Contains(expected, cut.Find("div.omni-timeline").ClassName);
    }

    [Theory]
    [InlineData(TimelineLinePosition.Center, "omni-timeline-center")]
    [InlineData(TimelineLinePosition.Start, "omni-timeline-start")]
    [InlineData(TimelineLinePosition.End, "omni-timeline-end")]
    [InlineData(TimelineLinePosition.Alternate, "omni-timeline-alternate")]
    public void LinePosition_maps_to_root_class(TimelineLinePosition pos, string expected)
    {
        var cut = RenderTimeline(p => p.Add(t => t.LinePosition, pos));
        Assert.Contains(expected, cut.Find("div.omni-timeline").ClassName);
    }

    [Fact]
    public void Reverse_adds_modifier_class()
    {
        var cut = RenderTimeline(p => p.Add(t => t.Reverse, true));
        Assert.Contains("omni-timeline-reverse", cut.Find("div.omni-timeline").ClassName);
    }

    [Fact]
    public void Renders_one_item_per_child_with_three_regions()
    {
        var cut = RenderTimeline();
        Assert.Equal(2, cut.FindAll(".omni-timeline-item").Count);
        Assert.Equal(2, cut.FindAll(".omni-timeline-content-start").Count);
        Assert.Equal(2, cut.FindAll(".omni-timeline-axis .omni-timeline-point").Count);
        Assert.Equal(2, cut.FindAll(".omni-timeline-content-end").Count);
    }

    [Fact]
    public void Item_renders_label_and_text_in_their_slots()
    {
        var cut = RenderTimeline();
        var first = cut.FindAll(".omni-timeline-item")[0];
        Assert.Contains("A", first.QuerySelector(".omni-timeline-content-start")!.TextContent);
        Assert.Contains("First", first.QuerySelector(".omni-timeline-content-end")!.TextContent);
    }

    // ─── Item point styling ───────────────────────────────────────────────

    [Fact]
    public void Item_defaults_to_base_filled_md_point()
    {
        var cut = Render<OmniTimelineItem>();
        var point = cut.Find(".omni-timeline-point");
        Assert.Contains("omni-timeline-point-base", point.ClassName);
        Assert.Contains("omni-timeline-point-filled", point.ClassName);
        Assert.Contains("omni-timeline-size-md", cut.Find(".omni-timeline-item").ClassName);
    }

    [Theory]
    [InlineData(TimelinePointStyle.Accent, "omni-timeline-point-accent")]
    [InlineData(TimelinePointStyle.Good, "omni-timeline-point-good")]
    [InlineData(TimelinePointStyle.Danger, "omni-timeline-point-danger")]
    [InlineData(TimelinePointStyle.Info, "omni-timeline-point-info")]
    public void PointStyle_maps_to_point_class(TimelinePointStyle style, string expected)
    {
        var cut = Render<OmniTimelineItem>(p => p.Add(i => i.PointStyle, style));
        Assert.Contains(expected, cut.Find(".omni-timeline-point").ClassName);
    }

    [Theory]
    [InlineData(TimelinePointVariant.Outlined, "omni-timeline-point-outlined")]
    [InlineData(TimelinePointVariant.Text, "omni-timeline-point-text")]
    public void PointVariant_maps_to_point_class(TimelinePointVariant variant, string expected)
    {
        var cut = Render<OmniTimelineItem>(p => p.Add(i => i.PointVariant, variant));
        Assert.Contains(expected, cut.Find(".omni-timeline-point").ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-timeline-size-sm")]
    [InlineData(ComponentSize.Lg, "omni-timeline-size-lg")]
    [InlineData(ComponentSize.Xl, "omni-timeline-size-xl")]
    public void PointSize_maps_to_item_class(ComponentSize size, string expected)
    {
        var cut = Render<OmniTimelineItem>(p => p.Add(i => i.PointSize, size));
        Assert.Contains(expected, cut.Find(".omni-timeline-item").ClassName);
    }

    [Fact]
    public void PointContent_renders_inside_the_dot()
    {
        var cut = Render<OmniTimelineItem>(p => p
            .Add(i => i.PointContent, b => b.AddMarkupContent(0, "<span class=\"x\">★</span>")));
        Assert.NotNull(cut.Find(".omni-timeline-point .x"));
    }

    [Fact]
    public void Item_appends_consumer_Class_and_splats_attributes()
    {
        var cut = Render<OmniTimelineItem>(p => p
            .Add(i => i.Class, "my-item")
            .AddUnmatched("data-testid", "it1"));
        var root = cut.Find(".omni-timeline-item");
        Assert.Contains("my-item", root.ClassName);
        Assert.Equal("it1", root.GetAttribute("data-testid"));
    }
}
