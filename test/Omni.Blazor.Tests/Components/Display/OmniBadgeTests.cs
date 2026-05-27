using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniBadge"/>: standalone vs overlay
/// modes, variants, dot, numeric Max cap, and cross-cutting splat.
/// </summary>
public class OmniBadgeTests : TestContextBase
{
    [Fact]
    public void Renders_standalone_default_with_text()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Text, "VIP"));

        var root = cut.Find("span.omni-badge");
        Assert.Contains("omni-badge", root.ClassName);
        Assert.Contains("VIP", root.TextContent);
    }

    [Theory]
    [InlineData(BadgeVariant.Good,   "omni-badge-good")]
    [InlineData(BadgeVariant.Warn,   "omni-badge-warn")]
    [InlineData(BadgeVariant.Danger, "omni-badge-danger")]
    [InlineData(BadgeVariant.Info,   "omni-badge-info")]
    [InlineData(BadgeVariant.Accent, "omni-badge-accent")]
    [InlineData(BadgeVariant.Plain,  "omni-badge-plain")]
    [InlineData(BadgeVariant.Solid,  "omni-badge-solid")]
    public void Applies_variant_class(BadgeVariant variant, string expected)
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Variant, variant));

        Assert.Contains(expected, cut.Find("span.omni-badge").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root_standalone()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "user-cls"));

        Assert.Contains("user-cls", cut.Find("span.omni-badge").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root_standalone()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "margin: 2px"));

        Assert.Equal("margin: 2px", cut.Find("span.omni-badge").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root_standalone()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "b1"));

        Assert.Equal("b1", cut.Find("span.omni-badge").GetAttribute("data-testid"));
    }

    [Fact]
    public void Caps_numeric_Content_at_Max()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Content, 150)
            .Add(c => c.Max, 99));

        Assert.Contains("99+", cut.Find("span.omni-badge").TextContent);
    }

    [Fact]
    public void Overlay_mode_renders_wrapper_with_child()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Content, 3)
            .AddChildContent("<button>x</button>"));

        Assert.NotNull(cut.Find("span.omni-badge-wrap"));
        Assert.NotNull(cut.Find("span.omni-badge-overlay"));
        Assert.NotNull(cut.Find("button"));
    }

    [Fact]
    public void Invisible_overlay_hides_badge_but_keeps_child()
    {
        var cut = RenderComponent<OmniBadge>(p => p
            .Add(c => c.Visible, false)
            .Add(c => c.Content, 1)
            .AddChildContent("<button>x</button>"));

        Assert.NotNull(cut.Find("span.omni-badge-wrap"));
        Assert.Empty(cut.FindAll("span.omni-badge-overlay"));
        Assert.NotNull(cut.Find("button"));
    }
}
