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
        var cut = Render<OmniNumeric<int>>();

        var root = cut.Find("div.omni-numeric");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find("input.omni-numeric-input"));
        Assert.NotNull(cut.Find("div.omni-numeric-spinner"));
    }

    [Fact]
    public void Attaches_numeric_filters_through_the_inputs_module()
    {
        var cut = Render<OmniNumeric<decimal>>(p => p
            .Add(c => c.Min, 1)
            .Add(c => c.Max, 10));

        var input = cut.Find("input.omni-numeric-input");
        Assert.Null(input.GetAttribute("onkeypress"));
        Assert.Null(input.GetAttribute("onpaste"));

        var invocation = JSInterop.VerifyInvoke("omniBlazor.numericAttach");
        Assert.Equal(false, invocation.Arguments[1]);
        Assert.Equal(1m, invocation.Arguments[3]);
        Assert.Equal(10m, invocation.Arguments[4]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AutoDecimalSeparator_forwards_the_requested_fixed_scale(int decimals)
    {
        Render<OmniNumeric<decimal>>(p => p
            .Add(c => c.Decimals, decimals)
            .Add(c => c.AutoDecimalSeparator, true));

        var invocation = JSInterop.VerifyInvoke("omniBlazor.numericAttach");
        Assert.Equal(true, invocation.Arguments[5]);
        Assert.Equal(decimals, invocation.Arguments[6]);
    }

    [Fact]
    public void AutoDecimalSeparator_rejects_a_negative_scale()
    {
        var exception = Assert.ThrowsAny<Exception>(() =>
            Render<OmniNumeric<decimal>>(p => p
                .Add(c => c.Decimals, -1)
                .Add(c => c.AutoDecimalSeparator, true)));

        Assert.Contains("Decimals", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Interop_unavailable_during_handoff_does_not_break_render_or_schedule_detach()
    {
        JSInterop.SetupVoid("omniBlazor.numericAttach")
            .SetException(new InvalidOperationException("JavaScript interop is unavailable."));

        var cut = Render<OmniNumeric<decimal>>();
        cut.Dispose();

        Assert.DoesNotContain(
            JSInterop.Invocations,
            invocation => invocation.Identifier == "omniBlazor.numericDetach");
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-numeric-sm")]
    [InlineData(ComponentSize.Lg, "omni-numeric-lg")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniNumeric<int>>(p => p.Add(c => c.Size, size));
        Assert.Contains(expected, cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void ShowSpinButtons_false_hides_spinner()
    {
        var cut = Render<OmniNumeric<int>>(p => p
            .Add(c => c.ShowSpinButtons, false));

        Assert.Empty(cut.FindAll("div.omni-numeric-spinner"));
    }

    [Fact]
    public void Renders_prefix_and_suffix()
    {
        var cut = Render<OmniNumeric<decimal>>(p => p
            .Add(c => c.Prefix, "R$")
            .Add(c => c.Suffix, "%"));

        Assert.Contains("R$", cut.Find(".omni-numeric-prefix").TextContent);
        Assert.Contains("%", cut.Find(".omni-numeric-suffix").TextContent);
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = Render<OmniNumeric<int>>(p => p.Add(c => c.Disabled, true));
        Assert.Contains("omni-numeric-disabled", cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniNumeric<int>>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-numeric").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniNumeric<int>>(p => p.Add(c => c.Style, "width: 120px"));
        Assert.Equal("width: 120px", cut.Find("div.omni-numeric").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniNumeric<int>>(p => p
            .AddUnmatched("data-testid", "num1"));

        Assert.Equal("num1", cut.Find("div.omni-numeric").GetAttribute("data-testid"));
    }
}
