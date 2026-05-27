using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSwitch"/>: boolean toggle round-trip,
/// label/childcontent rendering, and the cross-cutting splat.
/// </summary>
public class OmniSwitchTests : TestContextBase
{
    [Fact]
    public void Renders_label_with_omni_switch_class()
    {
        var cut = RenderComponent<OmniSwitch>();
        var label = cut.Find("label");
        Assert.Contains("omni-switch", label.ClassName);
    }

    [Fact]
    public void Initial_Value_reflects_in_checked_attribute()
    {
        var cut = RenderComponent<OmniSwitch>(p => p.Add(c => c.Value, true));
        Assert.True(cut.Find("input").HasAttribute("checked"));
    }

    [Fact]
    public void Click_toggles_Value_via_ValueChanged()
    {
        bool? captured = null;
        var cut = RenderComponent<OmniSwitch>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("input").Change(true);
        Assert.True(captured);
    }

    [Fact]
    public void Renders_label_text()
    {
        var cut = RenderComponent<OmniSwitch>(p => p.Add(c => c.Label, "On"));
        Assert.Contains("On", cut.Find("label").TextContent);
    }

    [Fact]
    public void ChildContent_wins_over_label()
    {
        var cut = RenderComponent<OmniSwitch>(p => p
            .Add(c => c.Label, "Ignored")
            .Add(c => c.ChildContent, b => b.AddContent(0, "Custom")));

        Assert.Contains("Custom",  cut.Find("label").TextContent);
        Assert.DoesNotContain("Ignored", cut.Find("label").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSwitch>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("label").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniSwitch>(p => p.Add(c => c.Style, "margin:4px"));
        Assert.Equal("margin:4px", cut.Find("label").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSwitch>(p => p
            .AddUnmatched("data-testid", "sw1"));

        Assert.Equal("sw1", cut.Find("label").GetAttribute("data-testid"));
    }
}
