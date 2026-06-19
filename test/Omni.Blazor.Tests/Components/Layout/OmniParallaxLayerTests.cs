using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniParallaxLayer"/>: emits the per-layer custom
/// properties (Speed/Range/overscan/axis multipliers) the SCSS contract consumes, using
/// InvariantCulture so locales with comma decimals don't break CSS. Axis inherits the scene.
/// </summary>
public class OmniParallaxLayerTests : TestContextBase
{
    [Fact]
    public void Renders_layer_with_default_custom_props()
    {
        var cut = Render<OmniParallaxLayer>(p => p.AddChildContent("x"));

        var div = cut.Find("div");
        Assert.Contains("omni-parallax-layer", div.ClassName);
        var s = div.GetAttribute("style") ?? "";
        Assert.Contains("--omni-px-speed: 0.3", s);
        Assert.Contains("--omni-px-range: 300", s);
        Assert.Contains("--omni-px-x: 0", s);
        Assert.Contains("--omni-px-y: 1", s);   // vertical default
    }

    [Fact]
    public void Speed_and_Range_emit_invariant_culture_and_overscan()
    {
        var cut = Render<OmniParallaxLayer>(p => p
            .Add(c => c.Speed, 0.5)
            .Add(c => c.Range, 200)
            .AddChildContent("x"));

        var s = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-px-speed: 0.5", s);     // not "0,5"
        Assert.Contains("--omni-px-range: 200", s);
        Assert.Contains("--omni-px-overscan: 70", s);   // 0.5*200*0.5 + 20
    }

    [Theory]
    [InlineData(ParallaxAxis.Vertical, "--omni-px-x: 0", "--omni-px-y: 1")]
    [InlineData(ParallaxAxis.Horizontal, "--omni-px-x: 1", "--omni-px-y: 0")]
    [InlineData(ParallaxAxis.Both, "--omni-px-x: 1", "--omni-px-y: 1")]
    public void Axis_sets_multipliers(ParallaxAxis axis, string xExp, string yExp)
    {
        var cut = Render<OmniParallaxLayer>(p => p.Add(c => c.Axis, axis).AddChildContent("x"));

        var s = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains(xExp, s);
        Assert.Contains(yExp, s);
    }

    [Fact]
    public void Inherits_axis_from_parent_scene()
    {
        var cut = Render<OmniParallax>(p => p
            .Add(c => c.Axis, ParallaxAxis.Horizontal)
            .AddChildContent(b =>
            {
                b.OpenComponent<OmniParallaxLayer>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "x")));
                b.CloseComponent();
            }));

        var s = cut.Find(".omni-parallax-layer").GetAttribute("style") ?? "";
        Assert.Contains("--omni-px-x: 1", s);
        Assert.Contains("--omni-px-y: 0", s);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniParallaxLayer>(p => p.Add(c => c.Class, "bg").AddChildContent("x"));

        Assert.Contains("bg", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniParallaxLayer>(p => p.Add(c => c.Style, "z-index: 2").AddChildContent("x"));

        Assert.Contains("z-index: 2", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniParallaxLayer>(p => p.AddUnmatched("data-testid", "ly").AddChildContent("x"));

        Assert.Equal("ly", cut.Find("div").GetAttribute("data-testid"));
    }
}
