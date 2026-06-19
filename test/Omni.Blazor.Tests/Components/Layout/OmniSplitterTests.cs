using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniSplitter"/>: N-pane resizable
/// container. Tests rely on declarative panes registering through the cascading
/// parent reference.
/// </summary>
public class OmniSplitterTests : TestContextBase
{
    [Fact]
    public void Renders_default_horizontal_solid_variant()
    {
        var cut = Render<OmniSplitter>();

        var root = cut.Find(".omni-splitter");
        Assert.Contains("omni-splitter-horizontal", root.ClassName);
        Assert.Contains("omni-splitter-variant-solid", root.ClassName);
        Assert.Contains("width:100%", root.GetAttribute("style") ?? "");
        Assert.Contains("height:100%", root.GetAttribute("style") ?? "");
    }

    [Theory]
    [InlineData(SplitterVariant.Solid, "omni-splitter-variant-solid")]
    [InlineData(SplitterVariant.Line,  "omni-splitter-variant-line")]
    [InlineData(SplitterVariant.Gap,   "omni-splitter-variant-gap")]
    public void Applies_variant_class(SplitterVariant variant, string expected)
    {
        var cut = Render<OmniSplitter>(p => p.Add(c => c.Variant, variant));
        Assert.Contains(expected, cut.Find(".omni-splitter").ClassName);
    }

    [Theory]
    [InlineData(Orientation.Horizontal, "omni-splitter-horizontal")]
    [InlineData(Orientation.Vertical,   "omni-splitter-vertical")]
    public void Applies_orientation_class(Orientation o, string expected)
    {
        var cut = Render<OmniSplitter>(p => p.Add(c => c.Orientation, o));
        Assert.Contains(expected, cut.Find(".omni-splitter").ClassName);
    }

    [Fact]
    public void UseAsOverlay_applies_overlay_class_and_omits_size_style()
    {
        var cut = Render<OmniSplitter>(p => p.Add(c => c.UseAsOverlay, true));

        var root = cut.Find(".omni-splitter");
        Assert.Contains("omni-splitter-overlay", root.ClassName);
        var style = root.GetAttribute("style") ?? "";
        Assert.DoesNotContain("width:", style);
        Assert.DoesNotContain("height:", style);
    }

    [Fact]
    public void Renders_panes_and_inserts_separator_bar()
    {
        var cut = Render<OmniSplitter>(p => p.AddChildContent(builder =>
        {
            builder.OpenComponent<OmniSplitterPane>(0);
            builder.AddAttribute(1, "Size", "50%");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "data-testid", "left");
                b.AddContent(2, "L");
                b.CloseElement();
            }));
            builder.CloseComponent();
            builder.OpenComponent<OmniSplitterPane>(3);
            builder.AddAttribute(4, "Size", "50%");
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "data-testid", "right");
                b.AddContent(2, "R");
                b.CloseElement();
            }));
            builder.CloseComponent();
        }));

        // Both pane child contents render.
        Assert.NotNull(cut.Find("[data-testid='left']"));
        Assert.NotNull(cut.Find("[data-testid='right']"));
        // Between two panes there must be exactly one separator bar.
        Assert.Single(cut.FindAll("[role='separator']"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSplitter>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find(".omni-splitter").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSplitter>(p => p
            .AddUnmatched("data-testid", "sp"));

        Assert.Equal("sp", cut.Find(".omni-splitter").GetAttribute("data-testid"));
    }
}
