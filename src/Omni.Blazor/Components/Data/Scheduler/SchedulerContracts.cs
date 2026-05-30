using System.Globalization;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Contract a view (Day/Week/Month/…) exposes to its parent
/// <see cref="OmniScheduler{TItem}"/>. Mirrors Radzen's <c>ISchedulerView</c>:
/// the scheduler reads <see cref="Text"/>/<see cref="Icon"/> to build the view
/// tabs, <see cref="Title"/> for the navigation title, and calls
/// <see cref="Next"/>/<see cref="Prev"/> when the user navigates.
/// </summary>
public interface IOmniSchedulerView
{
    /// <summary>Label shown on the view-switch tab.</summary>
    string Text { get; }

    /// <summary>Icon name shown on the view-switch tab.</summary>
    string Icon { get; }

    /// <summary>Human title for the current range (shown in the nav bar).</summary>
    string Title { get; }

    /// <summary>Inclusive start of the visible range (used to filter appointments + LoadData).</summary>
    DateTime StartDate { get; }

    /// <summary>Exclusive end of the visible range.</summary>
    DateTime EndDate { get; }

    /// <summary>Date the scheduler should move to when "next" is pressed.</summary>
    DateTime Next();

    /// <summary>Date the scheduler should move to when "previous" is pressed.</summary>
    DateTime Prev();
}

/// <summary>
/// Non-generic surface the child views cascade-consume from
/// <see cref="OmniScheduler{TItem}"/>. Mirrors Radzen's <c>IScheduler</c> —
/// keeps the views decoupled from the scheduler's <c>TItem</c>.
/// </summary>
public interface IOmniScheduler
{
    /// <summary>The date currently in view (changes as the user navigates).</summary>
    DateTime CurrentDate { get; }

    /// <summary>Culture used for date/time formatting and first-day-of-week.</summary>
    CultureInfo Culture { get; }

    /// <summary>True when an <c>AppointmentMove</c> handler is wired (enables drag-and-drop).</summary>
    bool AppointmentsDraggable { get; }

    /// <summary>True when a mouse enter/leave handler is wired (enables hover tooltips).</summary>
    bool HasAppointmentMouseHandlers { get; }

    /// <summary>Register a view with the scheduler (called from the view's <c>OnInitialized</c>).</summary>
    void AddView(IOmniSchedulerView view);

    /// <summary>Unregister a view (called from the view's <c>Dispose</c>).</summary>
    void RemoveView(IOmniSchedulerView view);

    /// <summary>Whether <paramref name="view"/> is the active view.</summary>
    bool IsSelected(IOmniSchedulerView view);

    /// <summary>Appointments overlapping the half-open range [<paramref name="start"/>, <paramref name="end"/>).</summary>
    IEnumerable<OmniAppointmentData> GetAppointmentsInRange(DateTime start, DateTime end);

    /// <summary>Whether a single appointment overlaps the half-open range [<paramref name="start"/>, <paramref name="end"/>).</summary>
    bool IsAppointmentInRange(OmniAppointmentData item, DateTime start, DateTime end);

    /// <summary>Render an appointment's content (uses the consumer <c>Template</c> when set, else its text).</summary>
    RenderFragment RenderAppointment(OmniAppointmentData item);

    /// <summary>Extra HTML attributes for an appointment, produced by the <c>AppointmentRender</c> hook.</summary>
    IReadOnlyDictionary<string, object>? GetAppointmentAttributes(OmniAppointmentData item);

    /// <summary>Extra HTML attributes for a slot, produced by the <c>SlotRender</c> hook.</summary>
    IReadOnlyDictionary<string, object>? GetSlotAttributes(DateTime start, DateTime end, IOmniSchedulerView view, IEnumerable<OmniAppointmentData> appointments);

    /// <summary>Raise <c>SlotSelect</c> for an empty slot click.</summary>
    Task SelectSlotAsync(DateTime start, DateTime end, IOmniSchedulerView view, IEnumerable<OmniAppointmentData> appointments);

    /// <summary>Raise <c>AppointmentSelect</c> for an appointment click.</summary>
    Task SelectAppointmentAsync(OmniAppointmentData data);

    /// <summary>Raise <c>DaySelect</c> for a day header / number click.</summary>
    Task SelectDayAsync(DateTime day, IEnumerable<OmniAppointmentData> appointments, IOmniSchedulerView view);

    /// <summary>Raise <c>MonthSelect</c> for a month header click (year-style views).</summary>
    Task SelectMonthAsync(DateTime monthStart, IEnumerable<OmniAppointmentData> appointments, IOmniSchedulerView view);

    /// <summary>Raise <c>MoreSelect</c> for the "+N more" overflow link. Returns true if default was prevented.</summary>
    Task<bool> SelectMoreAsync(DateTime start, DateTime end, IEnumerable<OmniAppointmentData> appointments, IOmniSchedulerView view);

    /// <summary>Raise <c>AppointmentMove</c> after a drag-and-drop drop on a slot.</summary>
    Task MoveAppointmentAsync(OmniAppointmentData appointment, DateTime slotDate);

    /// <summary>Raise <c>AppointmentMouseEnter</c> when the pointer enters an appointment.</summary>
    Task MouseEnterAppointmentAsync(OmniAppointmentData appointment, double clientX, double clientY);

    /// <summary>Raise <c>AppointmentMouseLeave</c> when the pointer leaves an appointment.</summary>
    Task MouseLeaveAppointmentAsync(OmniAppointmentData appointment, double clientX, double clientY);
}
