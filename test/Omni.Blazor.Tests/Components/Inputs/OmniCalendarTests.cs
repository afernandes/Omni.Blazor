using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniCalendar"/>: grid rendering,
/// navigation buttons, selected-day class, and the cross-cutting splat.
/// </summary>
public class OmniCalendarTests : TestContextBase
{
    [Fact]
    public void Renders_calendar_with_base_class()
    {
        var cut = Render<OmniCalendar>();
        Assert.NotNull(cut.Find("div.omni-calendar"));
        // 42 day cells regardless of month length (6 weeks).
        Assert.Equal(42, cut.FindAll("button.omni-calendar-day").Count);
    }

    [Fact]
    public void Renders_nav_buttons_by_default()
    {
        var cut = Render<OmniCalendar>();
        // Prev + next chevrons.
        Assert.NotEmpty(cut.FindAll("button.omni-calendar-nav"));
    }

    [Fact]
    public void ShowPrevButton_false_hides_prev_chevron()
    {
        var cut = Render<OmniCalendar>(p => p.Add(c => c.ShowPrevButton, false));
        // Only the next chevron should remain.
        Assert.Single(cut.FindAll("button.omni-calendar-nav"));
    }

    [Fact]
    public void Selected_day_renders_with_selected_modifier()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cut = Render<OmniCalendar>(p => p
            .Add(c => c.Selected, today)
            .Add(c => c.ViewMonth, today));

        Assert.NotEmpty(cut.FindAll("button.omni-calendar-day-selected"));
    }

    [Fact]
    public void OnDateSelected_fires_when_a_day_is_clicked()
    {
        DateOnly? captured = null;
        var cut = Render<OmniCalendar>(p => p
            .Add(c => c.OnDateSelected, d => captured = d));

        cut.FindAll("button.omni-calendar-day")[10].Click();
        Assert.NotNull(captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCalendar>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-calendar").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniCalendar>(p => p.Add(c => c.Style, "margin: 4px"));
        Assert.Equal("margin: 4px", cut.Find("div.omni-calendar").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCalendar>(p => p
            .AddUnmatched("data-testid", "cal"));

        Assert.Equal("cal", cut.Find("div.omni-calendar").GetAttribute("data-testid"));
    }

    // ── ParameterState: view rebuild only on tracked params ──

    [Fact]
    public void View_state_populates_on_initial_render()
    {
        var cut = Render<OmniCalendar>();
        // 42 day cells must be present from the very first detect cycle.
        Assert.Equal(42, cut.FindAll("button.omni-calendar-day").Count);
        Assert.True(cut.Instance.RecomputeCount >= 1);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniCalendar>();
        var baseline = cut.Instance.RecomputeCount;

        cut.Render(p => p
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_Selected_changes()
    {
        var cut = Render<OmniCalendar>(p => p
            .Add(c => c.ViewMonth, new DateOnly(2025, 1, 1)));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Selected, new DateOnly(2025, 1, 15)));

        Assert.True(cut.Instance.RecomputeCount > baseline);
        // DOM observation: at least one cell now carries the selected modifier.
        Assert.NotEmpty(cut.FindAll("button.omni-calendar-day-selected"));
    }
}
