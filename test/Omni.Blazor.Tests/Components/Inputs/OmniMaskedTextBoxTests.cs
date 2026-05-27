using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniMaskedTextBox"/>: input rendering,
/// mask attribute forwarding, size modifiers, and the cross-cutting splat.
/// </summary>
public class OmniMaskedTextBoxTests : TestContextBase
{
    [Fact]
    public void Renders_input_with_base_class()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p.Add(c => c.Mask, "999"));
        Assert.Contains("omni-input", cut.Find("input").ClassName);
    }

    [Fact]
    public void Honors_mask_maxlength()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p
            .Add(c => c.Mask, "999.999.999-99"));

        Assert.Equal("14", cut.Find("input").GetAttribute("maxlength"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-input-sm")]
    [InlineData(ComponentSize.Lg, "omni-input-lg")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p
            .Add(c => c.Mask, "999")
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find("input").ClassName);
    }

    [Fact]
    public void Digit_only_mask_sets_numeric_inputmode()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p.Add(c => c.Mask, "999.999"));
        Assert.Equal("numeric", cut.Find("input").GetAttribute("inputmode"));
    }

    [Fact]
    public void Mixed_mask_falls_back_to_text_inputmode()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p.Add(c => c.Mask, "AAA-999"));
        Assert.Equal("text", cut.Find("input").GetAttribute("inputmode"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p
            .Add(c => c.Mask, "999")
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("input").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMaskedTextBox>(p => p
            .Add(c => c.Mask, "999")
            .AddUnmatched("data-testid", "m1"));

        Assert.Equal("m1", cut.Find("input").GetAttribute("data-testid"));
    }
}
