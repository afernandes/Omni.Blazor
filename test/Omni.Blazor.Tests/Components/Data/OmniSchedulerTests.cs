using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniScheduler{TItem}"/>: renders the
/// root + nav, registers child views, projects appointments from
/// <c>Data</c>, honours <c>SelectedIndex</c>, fires selection callbacks, and
/// re-projects only when <c>Data</c> changes (ParameterState contract).
/// </summary>
public class OmniSchedulerTests : TestContextBase
{
    public record TestAppt(DateTime Start, DateTime End, string Text);

    private static List<TestAppt> SampleToday() => new()
    {
        new(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10), "Standup"),
        new(DateTime.Today.AddHours(11), DateTime.Today.AddHours(12), "Entrevista"),
    };

    // Renders the standard Day/Week/Month view set as child content.
    private static RenderFragment Views() => builder =>
    {
        builder.OpenComponent<OmniDayView>(0);
        builder.CloseComponent();
        builder.OpenComponent<OmniWeekView>(1);
        builder.CloseComponent();
        builder.OpenComponent<OmniMonthView>(2);
        builder.CloseComponent();
    };

    private IRenderedComponent<OmniScheduler<TestAppt>> RenderScheduler(
        Action<ComponentParameterCollectionBuilder<OmniScheduler<TestAppt>>>? extra = null,
        List<TestAppt>? data = null)
        => RenderComponent<OmniScheduler<TestAppt>>(p =>
        {
            p.Add(s => s.Data, data ?? SampleToday());
            p.Add(s => s.StartProperty, nameof(TestAppt.Start));
            p.Add(s => s.EndProperty, nameof(TestAppt.End));
            p.Add(s => s.TextProperty, nameof(TestAppt.Text));
            p.Add(s => s.ChildContent, Views());
            extra?.Invoke(p);
        });

    // ─── Cross-cutting (root + splat) ─────────────────────────────────────

    [Fact]
    public void Renders_root_and_navigation()
    {
        var cut = RenderScheduler();

        Assert.NotNull(cut.Find("div.omni-scheduler"));
        Assert.NotNull(cut.Find(".omni-scheduler-nav"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderScheduler(p => p.Add(s => s.Class, "my-sched"));
        Assert.Contains("my-sched", cut.Find("div.omni-scheduler").ClassName);
    }

    [Fact]
    public void Height_applied_via_inline_style()
    {
        var cut = RenderScheduler(p => p.Add(s => s.Height, "480px"));
        var style = cut.Find("div.omni-scheduler").GetAttribute("style") ?? string.Empty;
        Assert.Contains("height:480px", style);
    }

    [Fact]
    public void Forwards_consumer_Style_after_height()
    {
        var cut = RenderScheduler(p => p.Add(s => s.Style, "border: 1px solid red"));
        var style = cut.Find("div.omni-scheduler").GetAttribute("style") ?? string.Empty;
        Assert.Contains("border: 1px solid red", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderScheduler(p => p.AddUnmatched("data-testid", "sched1"));
        Assert.Equal("sched1", cut.Find("div.omni-scheduler").GetAttribute("data-testid"));
    }

    // ─── View registration / selection ────────────────────────────────────

    [Fact]
    public void Registers_one_tab_per_view()
    {
        var cut = RenderScheduler();
        Assert.Equal(3, cut.FindAll(".omni-scheduler-view-tab").Count);
    }

    [Fact]
    public void Selects_first_view_by_default()
    {
        var cut = RenderScheduler();
        // Day view renders the time grid.
        Assert.NotNull(cut.Find(".omni-scheduler-timeview"));
        Assert.Empty(cut.FindAll(".omni-scheduler-monthview"));
    }

    [Fact]
    public void SelectedIndex_picks_the_configured_view()
    {
        var cut = RenderScheduler(p => p.Add(s => s.SelectedIndex, 2));
        Assert.NotNull(cut.Find(".omni-scheduler-monthview"));
        Assert.Empty(cut.FindAll(".omni-scheduler-timeview"));
    }

    [Fact]
    public void Clicking_a_view_tab_switches_views()
    {
        var cut = RenderScheduler();
        // Tabs are ordered Day, Week, Month — click Month.
        cut.FindAll(".omni-scheduler-view-tab")[2].Click();
        Assert.NotNull(cut.Find(".omni-scheduler-monthview"));
    }

    // ─── Appointment projection ───────────────────────────────────────────

    [Fact]
    public void Projects_appointments_from_Data()
    {
        var cut = RenderScheduler();
        Assert.Equal(2, cut.Instance.AppointmentsInternal.Count);
    }

    [Fact]
    public void No_projection_without_start_end_properties()
    {
        var cut = RenderComponent<OmniScheduler<TestAppt>>(p =>
        {
            p.Add(s => s.Data, SampleToday());
            p.Add(s => s.ChildContent, Views());
        });
        Assert.Empty(cut.Instance.AppointmentsInternal);
    }

    [Fact]
    public void Day_view_renders_an_event_for_today()
    {
        var cut = RenderScheduler();
        Assert.NotEmpty(cut.FindAll(".omni-scheduler-event"));
    }

    [Fact]
    public void Time_view_renders_hour_labels_in_HH_mm()
    {
        var cut = RenderScheduler();
        // Hour gutter labels must be real times (e.g. "08:00") — regression
        // guard for the TimeFormat="@TimeFormat" binding (a string literal
        // would render the format string itself).
        var labels = cut.FindAll(".omni-scheduler-tv-slotlabel")
            .Select(e => e.TextContent)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        Assert.NotEmpty(labels);
        Assert.All(labels, t =>
            Assert.Matches(@"^\d{2}:\d{2}$", t));
    }

    // ─── Selection callbacks ──────────────────────────────────────────────

    [Fact]
    public void Clicking_an_appointment_fires_AppointmentSelect()
    {
        SchedulerAppointmentSelectEventArgs<TestAppt>? captured = null;
        var cut = RenderScheduler(p => p.Add(s => s.AppointmentSelect,
            EventCallback.Factory.Create<SchedulerAppointmentSelectEventArgs<TestAppt>>(this, a => captured = a)));

        cut.FindAll(".omni-scheduler-event")[0].Click();

        Assert.NotNull(captured);
        Assert.Equal("Standup", captured!.Data?.Text);
    }

    [Fact]
    public void Clicking_an_empty_slot_fires_SlotSelect()
    {
        SchedulerSlotSelectEventArgs? captured = null;
        var cut = RenderScheduler(p => p.Add(s => s.SlotSelect,
            EventCallback.Factory.Create<SchedulerSlotSelectEventArgs>(this, a => captured = a)));

        cut.FindAll(".omni-scheduler-tv-slot")[0].Click();

        Assert.NotNull(captured);
        Assert.True(captured!.End > captured.Start);
    }

    [Fact]
    public void AppointmentRender_style_merges_with_positioning_style()
    {
        var cut = RenderScheduler(p => p.Add(s => s.AppointmentRender,
            (Action<SchedulerAppointmentRenderEventArgs<TestAppt>>)(e =>
                e.Attributes["style"] = "background:#123456;")));

        var style = cut.Find(".omni-scheduler-event").GetAttribute("style") ?? string.Empty;
        // Both the layout positioning AND the consumer style must survive.
        Assert.Contains("top:", style);
        Assert.Contains("height:", style);
        Assert.Contains("background:#123456", style);
    }

    [Fact]
    public void AppointmentMove_handler_makes_events_draggable()
    {
        var cut = RenderScheduler(p => p.Add(s => s.AppointmentMove,
            EventCallback.Factory.Create<SchedulerAppointmentMoveEventArgs>(this, _ => { })));

        var ev = cut.Find(".omni-scheduler-event");
        Assert.Equal("true", ev.GetAttribute("draggable"));
    }

    [Fact]
    public void Events_not_draggable_without_move_handler()
    {
        var cut = RenderScheduler();
        var ev = cut.Find(".omni-scheduler-event");
        Assert.Equal("false", ev.GetAttribute("draggable"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────

    [Fact]
    public void Rebuilds_appointments_on_initial_render()
    {
        var cut = RenderScheduler();
        Assert.Equal(1, cut.Instance.AppointmentsRebuildCount);
    }

    [Fact]
    public void Does_NOT_rebuild_when_unrelated_parameter_changes()
    {
        var cut = RenderScheduler();
        Assert.Equal(1, cut.Instance.AppointmentsRebuildCount);

        cut.SetParametersAndRender(p => p.Add(s => s.Class, "x"));

        Assert.Equal(1, cut.Instance.AppointmentsRebuildCount);
    }

    [Fact]
    public void Rebuilds_when_Data_reference_changes()
    {
        var cut = RenderScheduler();
        Assert.Equal(1, cut.Instance.AppointmentsRebuildCount);

        cut.SetParametersAndRender(p => p.Add(s => s.Data, new List<TestAppt>
        {
            new(DateTime.Today.AddHours(14), DateTime.Today.AddHours(15), "Novo"),
        }));

        Assert.Equal(2, cut.Instance.AppointmentsRebuildCount);
        Assert.Single(cut.Instance.AppointmentsInternal);
    }

    // ─── SlotRender / mouse / new views ───────────────────────────────────

    [Fact]
    public void SlotRender_applies_style_to_slots()
    {
        var cut = RenderScheduler(p => p.Add(s => s.SlotRender,
            (Action<SchedulerSlotRenderEventArgs>)(e => e.Attributes["style"] = "background:#abcdef;")));

        var slot = cut.Find(".omni-scheduler-tv-slot");
        Assert.Contains("background:#abcdef", slot.GetAttribute("style") ?? string.Empty);
    }

    [Fact]
    public async Task AppointmentMouseEnter_fires_on_hover()
    {
        SchedulerAppointmentMouseEventArgs<TestAppt>? captured = null;
        var cut = RenderScheduler(p => p.Add(s => s.AppointmentMouseEnter,
            EventCallback.Factory.Create<SchedulerAppointmentMouseEventArgs<TestAppt>>(this, a => captured = a)));

        await cut.Find(".omni-scheduler-event").TriggerEventAsync("onmouseenter", new MouseEventArgs());

        Assert.NotNull(captured);
        Assert.Equal("Standup", captured!.Data?.Text);
    }

    [Fact]
    public void MultiDay_view_renders_configured_number_of_columns()
    {
        RenderFragment views = builder =>
        {
            builder.OpenComponent<OmniMultiDayView>(0);
            builder.AddAttribute(1, nameof(OmniMultiDayView.NumberOfDays), 3);
            builder.CloseComponent();
        };
        var cut = RenderComponent<OmniScheduler<TestAppt>>(p => p
            .Add(s => s.Data, SampleToday())
            .Add(s => s.StartProperty, nameof(TestAppt.Start))
            .Add(s => s.EndProperty, nameof(TestAppt.End))
            .Add(s => s.TextProperty, nameof(TestAppt.Text))
            .Add(s => s.ChildContent, views));

        Assert.Equal(3, cut.FindAll(".omni-scheduler-tv-col").Count);
    }

    [Fact]
    public void Year_view_renders_twelve_mini_months()
    {
        RenderFragment views = builder =>
        {
            builder.OpenComponent<OmniYearView>(0);
            builder.CloseComponent();
        };
        var cut = RenderComponent<OmniScheduler<TestAppt>>(p => p
            .Add(s => s.Data, SampleToday())
            .Add(s => s.StartProperty, nameof(TestAppt.Start))
            .Add(s => s.EndProperty, nameof(TestAppt.End))
            .Add(s => s.TextProperty, nameof(TestAppt.Text))
            .Add(s => s.ChildContent, views));

        Assert.Equal(12, cut.FindAll(".omni-scheduler-ym").Count);
    }

    [Fact]
    public void Events_are_keyboard_accessible()
    {
        var cut = RenderScheduler();
        var ev = cut.Find(".omni-scheduler-event");
        Assert.Equal("button", ev.GetAttribute("role"));
        Assert.Equal("0", ev.GetAttribute("tabindex"));
        Assert.False(string.IsNullOrEmpty(ev.GetAttribute("aria-label")));
    }

    [Fact]
    public async Task Pressing_Enter_on_an_event_fires_AppointmentSelect()
    {
        SchedulerAppointmentSelectEventArgs<TestAppt>? captured = null;
        var cut = RenderScheduler(p => p.Add(s => s.AppointmentSelect,
            EventCallback.Factory.Create<SchedulerAppointmentSelectEventArgs<TestAppt>>(this, a => captured = a)));

        await cut.Find(".omni-scheduler-event").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.NotNull(captured);
        Assert.Equal("Standup", captured!.Data?.Text);
    }

    [Fact]
    public void Zero_duration_appointment_is_in_range()
    {
        // A midnight birthday (Start == End) must appear on its day.
        var bday = DateTime.Today.AddDays(3);
        var cut = RenderScheduler(data: new List<TestAppt> { new(bday, bday, "Aniversário") });

        Assert.True(cut.Instance.IsAppointmentInRange(
            cut.Instance.AppointmentsInternal[0], bday, bday.AddDays(1)));
        Assert.False(cut.Instance.IsAppointmentInRange(
            cut.Instance.AppointmentsInternal[0], bday.AddDays(1), bday.AddDays(2)));
    }

    [Fact]
    public void Year_planner_renders_month_rows_and_event_bars()
    {
        RenderFragment views = builder =>
        {
            builder.OpenComponent<OmniYearPlannerView>(0);
            builder.CloseComponent();
        };
        var cut = RenderComponent<OmniScheduler<TestAppt>>(p => p
            .Add(s => s.Data, SampleToday())
            .Add(s => s.StartProperty, nameof(TestAppt.Start))
            .Add(s => s.EndProperty, nameof(TestAppt.End))
            .Add(s => s.TextProperty, nameof(TestAppt.Text))
            .Add(s => s.ChildContent, views));

        Assert.Equal(12, cut.FindAll(".omni-scheduler-yr-row").Count);
        // Today's appointments fall in the current month row → at least one bar.
        Assert.NotEmpty(cut.FindAll(".omni-scheduler-yr-bar"));
    }

    private IRenderedComponent<OmniScheduler<TestAppt>> RenderSingleView<TView>() where TView : IComponent
        => RenderComponent<OmniScheduler<TestAppt>>(p => p
            .Add(s => s.Data, SampleToday())
            .Add(s => s.StartProperty, nameof(TestAppt.Start))
            .Add(s => s.EndProperty, nameof(TestAppt.End))
            .Add(s => s.TextProperty, nameof(TestAppt.Text))
            .Add(s => s.ChildContent, builder => { builder.OpenComponent<TView>(0); builder.CloseComponent(); }));

    // ─── Keyboard navigation (roving cursor) ──────────────────────────────

    [Fact]
    public void Time_view_has_a_single_focused_slot()
    {
        var cut = RenderScheduler();
        Assert.Single(cut.FindAll(".omni-scheduler-tv-slot.omni-focused"));
    }

    [Fact]
    public async Task Arrow_keys_move_the_slot_cursor_and_Enter_selects_it()
    {
        SchedulerSlotSelectEventArgs? captured = null;
        var cut = RenderScheduler(p => p.Add(s => s.SlotSelect,
            EventCallback.Factory.Create<SchedulerSlotSelectEventArgs>(this, a => captured = a)));

        // Day view, StartTime 08:00, 30-min slots → two ArrowDowns = 09:00.
        await cut.Find(".omni-scheduler-tv-body").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowDown" });
        await cut.Find(".omni-scheduler-tv-body").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowDown" });
        await cut.Find(".omni-scheduler-tv-body").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.NotNull(captured);
        Assert.Equal(DateTime.Today.AddHours(9), captured!.Start);
    }

    [Fact]
    public void Clicking_a_slot_moves_the_keyboard_cursor()
    {
        var cut = RenderScheduler();
        // Click the 5th slot — not the default-focused first one.
        cut.FindAll(".omni-scheduler-tv-slot")[4].Click();

        var focused = cut.FindAll(".omni-scheduler-tv-slot.omni-focused");
        Assert.Single(focused);
        // The cursor is now on the clicked slot (index 4).
        Assert.Contains("omni-focused", cut.FindAll(".omni-scheduler-tv-slot")[4].ClassName);
    }

    [Fact]
    public async Task Month_view_Enter_fires_SlotSelect_on_the_focused_day()
    {
        SchedulerSlotSelectEventArgs? captured = null;
        RenderFragment views = builder => { builder.OpenComponent<OmniMonthView>(0); builder.CloseComponent(); };
        var cut = RenderComponent<OmniScheduler<TestAppt>>(p => p
            .Add(s => s.Data, SampleToday())
            .Add(s => s.StartProperty, nameof(TestAppt.Start))
            .Add(s => s.EndProperty, nameof(TestAppt.End))
            .Add(s => s.TextProperty, nameof(TestAppt.Text))
            .Add(s => s.SlotSelect, EventCallback.Factory.Create<SchedulerSlotSelectEventArgs>(this, a => captured = a))
            .Add(s => s.ChildContent, views));

        Assert.Single(cut.FindAll(".omni-scheduler-mv-cell.omni-focused"));

        await cut.Find(".omni-scheduler-mv-grid").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.NotNull(captured);
        // Default cursor sits on today.
        Assert.Equal(DateTime.Today, captured!.Start.Date);
    }

    [Fact]
    public void Planner_is_week_aligned_37_columns_timeline_is_31()
    {
        // The two year-grid views must use distinct column models — the bug was
        // they looked nearly identical.
        var planner = RenderSingleView<OmniYearPlannerView>();
        var timeline = RenderSingleView<OmniYearTimelineView>();

        Assert.Equal(37, planner.FindAll(".omni-scheduler-yr-colhead").Count);
        Assert.Equal(31, timeline.FindAll(".omni-scheduler-yr-colhead").Count);
        // Planner headers are weekday names; timeline headers are day numbers.
        Assert.False(int.TryParse(planner.FindAll(".omni-scheduler-yr-colhead")[0].TextContent.Trim(), out _));
        Assert.Equal("1", timeline.FindAll(".omni-scheduler-yr-colhead")[0].TextContent.Trim());
    }
}
