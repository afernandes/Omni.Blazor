using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniStat"/>: required Value, optional
/// Label/Delta/Card, cross-cutting splat.
/// </summary>
public class OmniStatTests : TestContextBase
{
    [Fact]
    public void Renders_default_with_value()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1.234"));

        var root = cut.Find("div.omni-stat");
        Assert.Contains("omni-stat", root.ClassName);
        Assert.Contains("1.234", cut.Find(".omni-stat-value").TextContent);
    }

    [Fact]
    public void Label_renders_above_value()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Label, "Revenue")
            .Add(c => c.Value, "R$ 100"));

        Assert.Contains("Revenue", cut.Find(".omni-stat-label").TextContent);
    }

    [Fact]
    public void Card_adds_modifier_class()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .Add(c => c.Card, true));

        Assert.Contains("omni-stat-card", cut.Find("div.omni-stat").ClassName);
    }

    [Fact]
    public void Delta_renders_when_set()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .Add(c => c.Delta, "+5%"));

        Assert.Contains("+5%", cut.Find(".omni-stat-delta").TextContent);
    }

    [Fact]
    public void DeltaDown_applies_down_modifier()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .Add(c => c.Delta, "-3%")
            .Add(c => c.DeltaDown, true));

        Assert.Contains("omni-stat-delta-down", cut.Find(".omni-stat-delta").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .Add(c => c.Class, "my-stat"));

        Assert.Contains("my-stat", cut.Find("div.omni-stat").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .Add(c => c.Style, "background: red"));

        Assert.Equal("background: red", cut.Find("div.omni-stat").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniStat>(p => p
            .Add(c => c.Value, "1")
            .AddUnmatched("data-testid", "st1"));

        Assert.Equal("st1", cut.Find("div.omni-stat").GetAttribute("data-testid"));
    }
}
