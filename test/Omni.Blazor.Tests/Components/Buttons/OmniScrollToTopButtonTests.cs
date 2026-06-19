namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniScrollToTopButton"/>: the root
/// renders a <c>&lt;button&gt;</c> with position/variant/size modifier
/// classes, the optional progress ring SVG, and the standard Class/Style/
/// Attributes splat. Scroll-driven visibility relies on a real JS observer
/// and is exercised in integration tests.
/// </summary>
public class OmniScrollToTopButtonTests : TestContextBase
{
    [Fact]
    public void Renders_default_button_with_position_variant_size_classes()
    {
        var cut = Render<OmniScrollToTopButton>();

        var btn = cut.Find("button");
        Assert.Contains("omni-scroll-top-btn", btn.ClassName);
        Assert.Contains("omni-fab-bottom-right", btn.ClassName);
        Assert.Contains("omni-stt-variant-primary", btn.ClassName);
        Assert.Contains("omni-stt-lg", btn.ClassName);
        Assert.Equal("Voltar ao topo", btn.GetAttribute("title"));
    }

    [Theory]
    [InlineData(FabPosition.BottomLeft,   "omni-fab-bottom-left")]
    [InlineData(FabPosition.TopRight,     "omni-fab-top-right")]
    [InlineData(FabPosition.BottomCenter, "omni-fab-bottom-center")]
    [InlineData(FabPosition.Static,       "omni-fab-static")]
    public void Applies_position_modifier(FabPosition pos, string expectedClass)
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.Position, pos));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Theory]
    [InlineData(ButtonVariant.Default, "omni-stt-variant-default")]
    [InlineData(ButtonVariant.Primary, "omni-stt-variant-primary")]
    [InlineData(ButtonVariant.Ghost,   "omni-stt-variant-ghost")]
    [InlineData(ButtonVariant.Danger,  "omni-stt-variant-danger")]
    public void Applies_variant_modifier(ButtonVariant variant, string expectedClass)
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.Variant, variant));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-stt-sm")]
    [InlineData(ComponentSize.Md, "omni-stt-md")]
    [InlineData(ComponentSize.Lg, "omni-stt-lg")]
    [InlineData(ComponentSize.Xl, "omni-stt-xl")]
    public void Applies_size_modifier(ComponentSize size, string expectedClass)
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.Size, size));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Fact]
    public void ShowProgress_adds_modifier_class_and_renders_ring_svg()
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.ShowProgress, true));

        var btn = cut.Find("button");
        Assert.Contains("omni-stt-with-progress", btn.ClassName);
        Assert.NotNull(cut.Find("svg.omni-scroll-top-ring"));
    }

    [Fact]
    public void ShowProgress_disabled_omits_ring_svg()
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.ShowProgress, false));

        Assert.Empty(cut.FindAll("svg.omni-scroll-top-ring"));
        Assert.DoesNotContain("omni-stt-with-progress", cut.Find("button").ClassName);
    }

    [Fact]
    public void Initial_state_is_hidden_via_aria_and_tabindex()
    {
        // Button is in the DOM (so the scroll observer can wire up) but is
        // aria-hidden and tabindex=-1 until the scroll observer fires.
        var cut = Render<OmniScrollToTopButton>();

        var btn = cut.Find("button");
        Assert.Equal("true", btn.GetAttribute("aria-hidden"));
        Assert.Equal("-1", btn.GetAttribute("tabindex"));
        Assert.DoesNotContain("omni-scroll-top-visible", btn.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniScrollToTopButton>(p => p.Add(c => c.Class, "custom-stt"));

        Assert.Contains("custom-stt", cut.Find("button").ClassName);
    }

    [Fact]
    public void Consumer_Style_is_appended_alongside_progress_var()
    {
        // Without progress, only consumer Style is present.
        var cutNoRing = Render<OmniScrollToTopButton>(p => p.Add(c => c.Style, "z-index: 99"));
        Assert.Contains("z-index: 99", cutNoRing.Find("button").GetAttribute("style") ?? "");

        // With progress, the --omni-stt-percent CSS var AND consumer Style coexist.
        var cutRing = Render<OmniScrollToTopButton>(p => p
            .Add(c => c.ShowProgress, true)
            .Add(c => c.Style, "z-index: 99"));
        var style = cutRing.Find("button").GetAttribute("style") ?? "";
        Assert.Contains("--omni-stt-percent", style);
        Assert.Contains("z-index: 99", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniScrollToTopButton>(p => p
            .AddUnmatched("data-testid", "stt")
            .AddUnmatched("id", "main-stt"));

        var btn = cut.Find("button");
        Assert.Equal("stt", btn.GetAttribute("data-testid"));
        Assert.Equal("main-stt", btn.GetAttribute("id"));
    }
}
