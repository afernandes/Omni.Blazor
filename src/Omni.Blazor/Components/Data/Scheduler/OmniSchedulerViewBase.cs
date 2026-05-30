using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Base class for every scheduler view (Day/Week/Month). A view is a
/// registrar component — it renders nothing on its own unless it's the active
/// view (the concrete <c>.razor</c> gates its markup on <see cref="IsSelectedView"/>).
/// On init it registers itself with the cascaded <see cref="IOmniScheduler"/>;
/// on dispose it unregisters. Mirrors Radzen's <c>SchedulerViewBase</c>.
/// </summary>
public abstract class OmniSchedulerViewBase : OmniComponent, IOmniSchedulerView, IDisposable
{
    /// <summary>The parent scheduler, cascaded from <see cref="OmniScheduler{TItem}"/>.</summary>
    [CascadingParameter] protected internal IOmniScheduler? Scheduler { get; set; }

    /// <inheritdoc />
    public abstract string Text { get; set; }

    /// <inheritdoc />
    public abstract string Icon { get; set; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <inheritdoc />
    public abstract DateTime StartDate { get; }

    /// <inheritdoc />
    public abstract DateTime EndDate { get; }

    /// <inheritdoc />
    public abstract DateTime Next();

    /// <inheritdoc />
    public abstract DateTime Prev();

    /// <summary>The date the scheduler is currently showing (date component only).</summary>
    protected DateTime CurrentDate => (Scheduler?.CurrentDate ?? DateTime.Today).Date;

    /// <summary>Culture for formatting + first-day-of-week (from the scheduler, falls back to current).</summary>
    protected CultureInfo Culture => Scheduler?.Culture ?? CultureInfo.CurrentCulture;

    /// <summary>True when this view is the scheduler's active view.</summary>
    protected bool IsSelectedView => Scheduler is not null && Scheduler.IsSelected(this);

    protected override void OnInitialized() => Scheduler?.AddView(this);

    public virtual void Dispose() => Scheduler?.RemoveView(this);
}
