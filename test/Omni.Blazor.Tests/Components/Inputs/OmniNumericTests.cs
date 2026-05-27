using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniNumeric{TValue}"/>: root rendering,
/// size modifiers, spinner buttons, Prefix/Suffix slots, and the cross-cutting splat.
/// </summary>
public class OmniNumericTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_input_and_spinner_by_default()
    {
        var cut = RenderComponent<OmniNumeric<int>>();

        var root = cut.Find("div.omni-numeric");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find("input.omni-numeric-input"));
        Assert.NotNull(cut.Find("div.omni-numeric-spinner"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-numeric-sm")]
    [InlineData(ComponentSize.Lg, "omni-numeric-lg")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p.Add(c => c.Size, size));
        Assert.Contains(expected, cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void ShowSpinButtons_false_hides_spinner()
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p
            .Add(c => c.ShowSpinButtons, false));

        Assert.Empty(cut.FindAll("div.omni-numeric-spinner"));
    }

    [Fact]
    public void Renders_prefix_and_suffix()
    {
        var cut = RenderComponent<OmniNumeric<decimal>>(p => p
            .Add(c => c.Prefix, "R$")
            .Add(c => c.Suffix, "%"));

        Assert.Contains("R$",  cut.Find(".omni-numeric-prefix").TextContent);
        Assert.Contains("%",   cut.Find(".omni-numeric-suffix").TextContent);
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p.Add(c => c.Disabled, true));
        Assert.Contains("omni-numeric-disabled", cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p.Add(c => c.Style, "width: 120px"));
        Assert.Equal("width: 120px", cut.Find("div.omni-numeric").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniNumeric<int>>(p => p
            .AddUnmatched("data-testid", "num1"));

        Assert.Equal("num1", cut.Find("div.omni-numeric").GetAttribute("data-testid"));
    }
}
