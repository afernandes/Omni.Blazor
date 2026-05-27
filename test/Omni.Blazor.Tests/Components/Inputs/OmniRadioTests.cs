using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniRadio{TValue}"/>: label rendering,
/// selection through the parent group, and the cross-cutting splat. Most
/// behaviour is covered by <see cref="OmniRadioGroupTests"/>; this file
/// pins down the standalone bits.
/// </summary>
public class OmniRadioTests : TestContextBase
{
    [Fact]
    public void Renders_radio_input_with_base_class()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Label, "X"));

        Assert.Equal("radio", cut.Find("input").GetAttribute("type"));
        Assert.Contains("omni-radio", cut.Find("label").ClassName);
        Assert.Contains("X", cut.Find("label").TextContent);
    }

    [Fact]
    public void Disabled_propagates_to_input()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Contains("omni-radio-disabled", cut.Find("label").ClassName);
    }

    [Fact]
    public void ChildContent_overrides_label()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Label, "Ignored")
            .Add(c => c.ChildContent, b => b.AddContent(0, "Custom")));

        Assert.Contains("Custom",  cut.Find("label").TextContent);
        Assert.DoesNotContain("Ignored", cut.Find("label").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("label").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("label").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniRadio<string>>(p => p
            .Add(c => c.Value, "x")
            .AddUnmatched("data-testid", "r1"));

        Assert.Equal("r1", cut.Find("label").GetAttribute("data-testid"));
    }
}
