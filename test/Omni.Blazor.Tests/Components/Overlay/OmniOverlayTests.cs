namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniOverlay"/>: standalone backdrop
/// with scroll-lock + Esc lifecycle. The component only renders when
/// <c>Visible=true</c>; tests focus on the root markup and modifier classes.
/// </summary>
public class OmniOverlayTests : TestContextBase
{
    [Fact]
    public void Hidden_when_Visible_false()
    {
        var cut = Render<OmniOverlay>(p => p.Add(c => c.Visible, false));

        // Component is conditionally rendered — markup is empty.
        Assert.Empty(cut.FindAll(".omni-overlay"));
    }

    [Fact]
    public void Renders_root_with_base_and_backdrop_classes_when_visible()
    {
        var cut = Render<OmniOverlay>(p => p.Add(c => c.Visible, true));

        var root = cut.Find(".omni-overlay");
        Assert.Contains("omni-overlay", root.ClassName);
        Assert.Contains("omni-overlay-backdrop", root.ClassName);
        // Default Absolute=false → fixed variant.
        Assert.Contains("omni-overlay-fixed", root.ClassName);
    }

    [Fact]
    public void Absolute_true_swaps_fixed_for_absolute_modifier()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Absolute, true));

        var root = cut.Find(".omni-overlay");
        Assert.Contains("omni-overlay-absolute", root.ClassName);
        Assert.DoesNotContain("omni-overlay-fixed", root.ClassName);
    }

    [Fact]
    public void Modal_false_adds_modeless_modifier()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Modal, false));

        Assert.Contains("omni-overlay-modeless", cut.Find(".omni-overlay").ClassName);
    }

    [Fact]
    public void Modal_true_default_has_no_modeless_modifier()
    {
        var cut = Render<OmniOverlay>(p => p.Add(c => c.Visible, true));

        Assert.DoesNotContain("omni-overlay-modeless", cut.Find(".omni-overlay").ClassName);
    }

    [Fact]
    public void Wraps_ChildContent_in_content_div()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .AddChildContent("<span class=\"probe\">probe</span>"));

        Assert.NotNull(cut.Find(".omni-overlay-content .probe"));
    }

    [Fact]
    public void Without_ChildContent_renders_pure_scrim()
    {
        var cut = Render<OmniOverlay>(p => p.Add(c => c.Visible, true));

        Assert.Empty(cut.FindAll(".omni-overlay-content"));
    }

    [Fact]
    public void ZIndex_emits_inline_style()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.ZIndex, 1500));

        Assert.Contains("z-index:1500", cut.Find(".omni-overlay").GetAttribute("style") ?? "");
    }

    [Fact]
    public void AutoClose_click_fires_VisibleChanged_false()
    {
        var visibility = true;
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, visibility)
            .Add(c => c.AutoClose, true)
            .Add(c => c.VisibleChanged, (bool v) => visibility = v));

        cut.Find(".omni-overlay").Click();
        Assert.False(visibility);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Class, "custom-ov"));

        Assert.Contains("custom-ov", cut.Find(".omni-overlay").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Style, "background: red"));

        Assert.Contains("background: red", cut.Find(".omni-overlay").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniOverlay>(p => p
            .Add(c => c.Visible, true)
            .AddUnmatched("data-testid", "ov")
            .AddUnmatched("aria-hidden", "true"));

        var root = cut.Find(".omni-overlay");
        Assert.Equal("ov", root.GetAttribute("data-testid"));
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
    }
}
