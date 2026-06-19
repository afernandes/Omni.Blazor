using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniTimePicker"/>: hour/minute cells,
/// AM/PM toggle for 12-hour format, ShowSeconds, and the cross-cutting splat.
/// </summary>
public class OmniTimePickerTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class_and_two_cells_by_default()
    {
        var cut = Render<OmniTimePicker>();
        Assert.NotNull(cut.Find("div.omni-time-picker"));
        // Hour + minute (no seconds, no AM/PM by default).
        Assert.Equal(2, cut.FindAll("div.omni-time-cell").Count);
    }

    [Fact]
    public void ShowSeconds_adds_third_cell()
    {
        var cut = Render<OmniTimePicker>(p => p.Add(c => c.ShowSeconds, true));
        Assert.Equal(3, cut.FindAll("div.omni-time-cell").Count);
    }

    [Fact]
    public void TwelveHour_format_renders_AM_PM_buttons()
    {
        var cut = Render<OmniTimePicker>(p => p.Add(c => c.HourFormat, "12"));
        Assert.Equal(2, cut.FindAll("button.omni-time-ampm-btn").Count);
    }

    [Fact]
    public void AmPm_state_reflects_in_active_class()
    {
        var cut = Render<OmniTimePicker>(p => p
            .Add(c => c.Time, new TimeOnly(13, 30))
            .Add(c => c.HourFormat, "12"));

        var btns = cut.FindAll("button.omni-time-ampm-btn");
        // At 13:30 the picker is in PM.
        Assert.DoesNotContain("omni-active", btns[0].ClassName);
        Assert.Contains("omni-active",       btns[1].ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniTimePicker>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-time-picker").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniTimePicker>(p => p.Add(c => c.Style, "margin: 4px"));
        Assert.Equal("margin: 4px", cut.Find("div.omni-time-picker").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniTimePicker>(p => p
            .AddUnmatched("data-testid", "tp"));

        Assert.Equal("tp", cut.Find("div.omni-time-picker").GetAttribute("data-testid"));
    }

    // ── ParameterState: FieldIdentifier rebuild fires only when TimeExpression changes ──

    private sealed class Model
    {
        public TimeOnly A { get; set; }
        public TimeOnly B { get; set; }
    }

    [Fact]
    public void Initial_recompute_fires_on_first_render()
    {
        var cut = Render<OmniTimePicker>();
        // First detect cycle always fires the change handler.
        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniTimePicker>();

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_TimeExpression_changes()
    {
        var model = new Model();
        System.Linq.Expressions.Expression<Func<TimeOnly>> first  = () => model.A;
        System.Linq.Expressions.Expression<Func<TimeOnly>> second = () => model.B;

        var cut = Render<OmniTimePicker>(p => p.Add(c => c.TimeExpression, first));
        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.TimeExpression, second));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
