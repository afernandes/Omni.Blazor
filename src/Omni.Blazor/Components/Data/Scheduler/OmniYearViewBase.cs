using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Shared base for the year-spanning scheduler views (Year, Year Planner, Year
/// Timeline). Computes the 12-month window honouring a configurable fiscal
/// <see cref="StartMonth"/>. Mirrors Radzen's <c>SchedulerYearViewBase</c>.
/// </summary>
public abstract class OmniYearViewBase : OmniSchedulerViewBase
{
    /// <summary>First month of the displayed year (fiscal start). Default January.</summary>
    [Parameter] public Month StartMonth { get; set; } = Month.January;

    /// <summary>The first month shown — current year, or the previous one when the
    /// current month is before <see cref="StartMonth"/>.</summary>
    protected DateTime YearFirstMonth()
    {
        var startMonthNumber = (int)StartMonth + 1; // Month enum is 0-based
        var year = CurrentDate.Year + (CurrentDate.Month < startMonthNumber ? -1 : 0);
        return new DateTime(year, startMonthNumber, 1);
    }

    /// <summary>The 12 month-starts of the displayed year.</summary>
    protected IReadOnlyList<DateTime> MonthStarts()
    {
        var first = YearFirstMonth();
        var months = new DateTime[12];
        for (int i = 0; i < 12; i++) months[i] = first.AddMonths(i);
        return months;
    }

    public override DateTime StartDate => YearFirstMonth();
    public override DateTime EndDate => YearFirstMonth().AddYears(1);
    public override DateTime Next() => CurrentDate.AddYears(1);
    public override DateTime Prev() => CurrentDate.AddYears(-1);

    public override string Title
    {
        get
        {
            var months = MonthStarts();
            return StartMonth == Month.January
                ? months[0].ToString("yyyy", Culture)
                : $"{months[0].ToString("MMM yyyy", Culture)} – {months[11].ToString("MMM yyyy", Culture)}";
        }
    }
}
