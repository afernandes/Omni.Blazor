using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSlider"/>: track/thumb rendering,
/// single vs range mode, disabled state, and the cross-cutting splat. Pointer
/// drag is exercised via JS interop which bUnit can't run; keyboard nav and
/// rendering invariants are covered here.
/// </summary>
public class OmniSliderTests : TestContextBase
{
    [Fact]
    public void Renders_track_and_single_thumb_by_default()
    {
        var cut = Render<OmniSlider>();

        Assert.NotNull(cut.Find("div.omni-slider"));
        Assert.Equal(1, cut.FindAll("div.omni-slider-thumb").Count);
        Assert.Contains("omni-slider-single", cut.Find("div.omni-slider").ClassName);
    }

    [Fact]
    public void Range_mode_renders_two_thumbs()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Range, true)
            .Add(c => c.ValueStart, 10)
            .Add(c => c.ValueEnd, 90));

        Assert.Equal(2, cut.FindAll("div.omni-slider-thumb").Count);
        Assert.Contains("omni-slider-range", cut.Find("div.omni-slider").ClassName);
    }

    [Fact]
    public void Disabled_applies_modifier_class_and_sets_thumb_tabindex_minus_one()
    {
        var cut = Render<OmniSlider>(p => p.Add(c => c.Disabled, true));

        Assert.Contains("omni-slider-disabled", cut.Find("div.omni-slider").ClassName);
        Assert.Equal("-1", cut.Find("div.omni-slider-thumb").GetAttribute("tabindex"));
    }

    [Fact]
    public void ShowValueLabel_renders_value_balloon()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 42)
            .Add(c => c.ShowValueLabel, true));

        Assert.Contains("42", cut.Find("div.omni-slider-value-label").TextContent);
    }

    [Fact]
    public void Color_emits_css_variable_on_root_style()
    {
        var cut = Render<OmniSlider>(p => p.Add(c => c.Color, "red"));
        Assert.Contains("--omni-slider-accent:red", cut.Find("div.omni-slider").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSlider>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-slider").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniSlider>(p => p.Add(c => c.Style, "margin: 4px"));
        Assert.Contains("margin: 4px", cut.Find("div.omni-slider").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSlider>(p => p
            .AddUnmatched("data-testid", "sl"));

        Assert.Equal("sl", cut.Find("div.omni-slider").GetAttribute("data-testid"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────
    // After the manual OnParametersSet → ParameterState migration, the
    // RecomputeCurrent and RecomputeTicks handlers must fire on first detect
    // for all relevant parameters, then stay quiet when only an unrelated
    // parameter (Class) changes.

    [Fact]
    public void Recompute_runs_on_initial_render()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 42)
            .Add(c => c.ShowTicks, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.Step, 1));

        // RecomputeCurrent fired at least once → _currentStart populated.
        Assert.True(cut.Instance._currentRecomputeCount > 0);
        Assert.Equal(10, cut.Instance._currentStart); // clamped 42 → Max=10
        // RecomputeTicks fired → _ticks populated.
        Assert.True(cut.Instance._ticksRecomputeCount > 0);
        Assert.Equal(11, cut.Instance._ticks.Count); // 0..10 step 1 = 11 ticks
    }

    [Fact]
    public void Recompute_does_NOT_run_when_unrelated_parameter_changes()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.ShowTicks, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var currentBefore = cut.Instance._currentRecomputeCount;
        var ticksBefore = cut.Instance._ticksRecomputeCount;
        var tickElsBefore = cut.FindAll(".omni-slider-tick").Count;

        cut.Render(p => p.Add(c => c.Class, "new-class"));

        // No tracked parameter changed → counters frozen.
        Assert.Equal(currentBefore, cut.Instance._currentRecomputeCount);
        Assert.Equal(ticksBefore, cut.Instance._ticksRecomputeCount);
        // Tick DOM count stable.
        Assert.Equal(tickElsBefore, cut.FindAll(".omni-slider-tick").Count);
    }

    [Fact]
    public void Recompute_does_NOT_run_when_Color_or_AriaLabel_changes()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.ShowTicks, true));

        var currentBefore = cut.Instance._currentRecomputeCount;
        var ticksBefore = cut.Instance._ticksRecomputeCount;

        cut.Render(p => p
            .Add(c => c.Color, "red")
            .Add(c => c.AriaLabel, "Volume"));

        Assert.Equal(currentBefore, cut.Instance._currentRecomputeCount);
        Assert.Equal(ticksBefore, cut.Instance._ticksRecomputeCount);
    }

    [Fact]
    public void RecomputeCurrent_runs_again_when_Value_changes()
    {
        var cut = Render<OmniSlider>(p => p.Add(c => c.Value, 10));

        var before = cut.Instance._currentRecomputeCount;

        cut.Render(p => p.Add(c => c.Value, 50));

        Assert.Equal(before + 1, cut.Instance._currentRecomputeCount);
        Assert.Equal(50, cut.Instance._currentStart);
    }

    [Fact]
    public void RecomputeTicks_does_NOT_run_when_only_Value_changes()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 10)
            .Add(c => c.ShowTicks, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.Step, 1));

        var ticksBefore = cut.Instance._ticksRecomputeCount;

        // Value change must NOT recompute the tick layout — ticks depend only
        // on Min/Max/Step/ShowTicks.
        cut.Render(p => p.Add(c => c.Value, 7));

        Assert.Equal(ticksBefore, cut.Instance._ticksRecomputeCount);
    }

    [Fact]
    public void RecomputeTicks_runs_again_when_Step_changes()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.ShowTicks, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.Step, 1));

        var before = cut.Instance._ticksRecomputeCount;
        var countBefore = cut.Instance._ticks.Count;

        cut.Render(p => p.Add(c => c.Step, 2));

        Assert.True(cut.Instance._ticksRecomputeCount > before);
        Assert.NotEqual(countBefore, cut.Instance._ticks.Count); // 11 → 6
    }

    [Fact]
    public void RecomputeTicks_runs_again_when_ShowTicks_toggles()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.ShowTicks, false)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 5)
            .Add(c => c.Step, 1));

        Assert.Empty(cut.Instance._ticks);
        var before = cut.Instance._ticksRecomputeCount;

        cut.Render(p => p.Add(c => c.ShowTicks, true));

        Assert.True(cut.Instance._ticksRecomputeCount > before);
        Assert.Equal(6, cut.Instance._ticks.Count); // 0..5 step 1
    }

    [Fact]
    public void Min_or_Max_change_recomputes_BOTH_current_and_ticks()
    {
        var cut = Render<OmniSlider>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.ShowTicks, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.Step, 1));

        var currentBefore = cut.Instance._currentRecomputeCount;
        var ticksBefore = cut.Instance._ticksRecomputeCount;

        // Min change shifts clamp boundary AND tick layout.
        cut.Render(p => p.Add(c => c.Min, 2));

        Assert.True(cut.Instance._currentRecomputeCount > currentBefore);
        Assert.True(cut.Instance._ticksRecomputeCount > ticksBefore);
    }
}
