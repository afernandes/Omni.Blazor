using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniParallax"/>: a depth-layer scene driven by a
/// single --omni-parallax-progress var. Native CSS scroll-driven engine + JS fallback;
/// reduced-motion / mobile / Disabled turn it off. JS interop is loose in tests (no browser).
/// </summary>
public class OmniParallaxTests : TestContextBase
{
    [Fact]
    public void Renders_scene_div_with_mode_attribute()
    {
        var cut = RenderComponent<OmniParallax>(p => p.AddChildContent("x"));

        var div = cut.Find("div");
        Assert.Contains("omni-parallax", div.ClassName);
        Assert.True(div.HasAttribute("data-omni-parallax-mode"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniParallax>(p => p.Add(c => c.Class, "hero").AddChildContent("x"));

        Assert.Contains("hero", cut.Find("div").ClassName);
    }

    [Fact]
    public void MouseParallax_emits_mr_variable()
    {
        var on = RenderComponent<OmniParallax>(p => p
            .Add(c => c.MouseParallax, true)
            .Add(c => c.MouseStrength, 2)
            .AddChildContent("x"));
        Assert.Contains("--omni-px-mr: 48", on.Find("div").GetAttribute("style") ?? "");   // 2 * 24

        var off = RenderComponent<OmniParallax>(p => p.AddChildContent("x"));
        Assert.DoesNotContain("--omni-px-mr", off.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniParallax>(p => p.Add(c => c.Style, "min-height: 400px").AddChildContent("x"));

        Assert.Contains("min-height: 400px", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Disabled_sets_off_mode()
    {
        var cut = RenderComponent<OmniParallax>(p => p.Add(c => c.Disabled, true).AddChildContent("x"));

        cut.WaitForAssertion(() =>
            Assert.Equal("off", cut.Find("div").GetAttribute("data-omni-parallax-mode")));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniParallax>(p => p
            .AddUnmatched("data-testid", "px")
            .AddUnmatched("aria-label", "Cena")
            .AddChildContent("x"));

        var div = cut.Find("div");
        Assert.Equal("px", div.GetAttribute("data-testid"));
        Assert.Equal("Cena", div.GetAttribute("aria-label"));
    }
}
