using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniDatePicker{TValue}"/>: input
/// rendering with placeholder, trigger button, and the cross-cutting splat.
/// Popover open/close runs through JS interop and isn't exercised here.
/// </summary>
public class OmniDatePickerTests : TestContextBase
{
    [Fact]
    public void Renders_input_group_with_base_class()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>();

        var root = cut.Find("div.omni-datepicker");
        Assert.Contains("omni-input-group", root.ClassName);
        Assert.NotNull(cut.Find("input.omni-datepicker-input"));
    }

    [Fact]
    public void Renders_trigger_button_with_calendar_icon_for_date_value()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>();
        Assert.NotNull(cut.Find("button.omni-datepicker-trigger"));
    }

    [Fact]
    public void Inline_mode_adds_inline_root_modifier()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p
            .Add(c => c.Inline, true));

        Assert.Contains("omni-datepicker-inline-root", cut.Find("div.omni-datepicker").ClassName);
    }

    [Fact]
    public void Disabled_propagates_to_input()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-datepicker").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p.Add(c => c.Style, "min-width: 200px"));
        Assert.Equal("min-width: 200px", cut.Find("div.omni-datepicker").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p
            .AddUnmatched("data-testid", "dp"));

        Assert.Equal("dp", cut.Find("div.omni-datepicker").GetAttribute("data-testid"));
    }

    // ── ParameterState: derived state recomputes only when Value changes ──

    [Fact]
    public void Initial_Value_populates_text_on_first_render()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p
            .Add(c => c.Value, new DateOnly(2025, 6, 4)));

        // DOM observation: the input value reflects the bound DateOnly.
        Assert.Equal("04/06/2025", cut.Find("input.omni-datepicker-input").GetAttribute("value"));
        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p
            .Add(c => c.Value, new DateOnly(2025, 6, 4)));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_Value_changes()
    {
        var cut = RenderComponent<OmniDatePicker<DateOnly?>>(p => p
            .Add(c => c.Value, new DateOnly(2025, 6, 4)));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p.Add(c => c.Value, new DateOnly(2025, 7, 11)));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
        Assert.Equal("11/07/2025", cut.Find("input.omni-datepicker-input").GetAttribute("value"));
    }
}
