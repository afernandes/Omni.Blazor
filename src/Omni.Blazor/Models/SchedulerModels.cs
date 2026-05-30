using Omni.Blazor.Components;

namespace Omni.Blazor.Models;

/// <summary>
/// Normalized appointment the scheduler views work with. The original consumer
/// item lives in <see cref="Data"/> so typed callbacks can cast it back to
/// <c>TItem</c>. Mirrors Radzen's <c>AppointmentData</c>.
/// </summary>
public class OmniAppointmentData
{
    /// <summary>Appointment start (inclusive).</summary>
    public DateTime Start { get; set; }

    /// <summary>Appointment end (exclusive).</summary>
    public DateTime End { get; set; }

    /// <summary>Display text (projected from the consumer's <c>TextProperty</c>).</summary>
    public string? Text { get; set; }

    /// <summary>The original consumer item this appointment was projected from.</summary>
    public object? Data { get; set; }
}

/// <summary>Args for <c>OmniScheduler.SlotSelect</c> — a click on an empty time slot / day cell.</summary>
public class SchedulerSlotSelectEventArgs
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public IEnumerable<OmniAppointmentData> Appointments { get; set; } = Array.Empty<OmniAppointmentData>();
    public IOmniSchedulerView? View { get; set; }

    /// <summary>Set by <see cref="PreventDefault"/>; reserved for future built-in slot behaviour.</summary>
    public bool IsDefaultPrevented { get; private set; }

    public void PreventDefault() => IsDefaultPrevented = true;
}

/// <summary>Args for <c>OmniScheduler.AppointmentSelect</c> — a click on an appointment.</summary>
public class SchedulerAppointmentSelectEventArgs<TItem>
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Text { get; set; }
    public TItem? Data { get; set; }
}

/// <summary>
/// Args for <c>OmniScheduler.AppointmentMouseEnter</c>/<c>AppointmentMouseLeave</c>.
/// Carries the hovered item plus the pointer coordinates so consumers can open a
/// tooltip (e.g. <c>TooltipService.Open(args.ClientX, args.ClientY, ...)</c>).
/// Mirrors Radzen's <c>SchedulerAppointmentMouseEventArgs</c>.
/// </summary>
public class SchedulerAppointmentMouseEventArgs<TItem>
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Text { get; set; }
    public TItem? Data { get; set; }
    public double ClientX { get; set; }
    public double ClientY { get; set; }
}

/// <summary>
/// Sync render hook for an appointment. Populate <see cref="Attributes"/> to add
/// CSS classes / inline styles / data-* attributes per appointment without
/// mutating the bound model. Mirrors Radzen's <c>AppointmentRender</c>.
/// </summary>
public class SchedulerAppointmentRenderEventArgs<TItem>
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TItem? Data { get; set; }
    public Dictionary<string, object> Attributes { get; } = new();
}

/// <summary>Sync render hook for a slot — populate <see cref="Attributes"/> to style individual slots.</summary>
public class SchedulerSlotRenderEventArgs
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public IOmniSchedulerView? View { get; set; }
    public IEnumerable<OmniAppointmentData> Appointments { get; set; } = Array.Empty<OmniAppointmentData>();
    public Dictionary<string, object> Attributes { get; } = new();
}

/// <summary>Args for <c>OmniScheduler.LoadData</c> — fired when the visible range changes (server-side loading).</summary>
public class SchedulerLoadDataEventArgs
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public IOmniSchedulerView? View { get; set; }
}

/// <summary>
/// Args for <c>OmniScheduler.TodaySelect</c>. Reassign <see cref="Today"/> to make
/// the "today" button navigate somewhere other than the real today.
/// </summary>
public class SchedulerTodaySelectEventArgs
{
    public DateTime Today { get; set; }
}

/// <summary>Args for <c>OmniScheduler.DaySelect</c> — a click on a day header / number.</summary>
public class SchedulerDaySelectEventArgs
{
    public DateTime Day { get; set; }
    public IEnumerable<OmniAppointmentData> Appointments { get; set; } = Array.Empty<OmniAppointmentData>();
    public IOmniSchedulerView? View { get; set; }
}

/// <summary>Args for <c>OmniScheduler.MonthSelect</c> — a click on a month header (year-style views).</summary>
public class SchedulerMonthSelectEventArgs
{
    public DateTime MonthStart { get; set; }
    public IEnumerable<OmniAppointmentData> Appointments { get; set; } = Array.Empty<OmniAppointmentData>();
    public IOmniSchedulerView? View { get; set; }
}

/// <summary>Args for <c>OmniScheduler.MoreSelect</c> — a click on the "+N more" overflow link.</summary>
public class SchedulerMoreSelectEventArgs
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public IEnumerable<OmniAppointmentData> Appointments { get; set; } = Array.Empty<OmniAppointmentData>();
    public IOmniSchedulerView? View { get; set; }

    public bool IsDefaultPrevented { get; private set; }

    public void PreventDefault() => IsDefaultPrevented = true;
}

/// <summary>
/// Args for <c>OmniScheduler.AppointmentMove</c> — an appointment was dropped onto
/// a new slot. The consumer typically does
/// <c>newStart = SlotDate; newEnd = SlotDate + TimeSpan;</c> then calls
/// <c>Reload()</c>.
/// </summary>
public class SchedulerAppointmentMoveEventArgs
{
    /// <summary>The appointment being moved.</summary>
    public OmniAppointmentData Appointment { get; set; } = default!;

    /// <summary>The slot date/time the appointment was dropped onto (the proposed new start).</summary>
    public DateTime SlotDate { get; set; }

    /// <summary>The appointment's original duration (End − Start).</summary>
    public TimeSpan TimeSpan { get; set; }
}
