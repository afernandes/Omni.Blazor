namespace Omni.Blazor.Models;

/// <summary>
/// Payload passed to the <c>DateRender</c> callback on calendar-based
/// components. Lets the consumer attach a CSS class, extra HTML attributes,
/// a tooltip, or mark a date disabled — without touching the bound model.
///
/// Inspired by Radzen's <c>DateRenderEventArgs</c>. Use it to highlight
/// holidays, mark anniversaries, disable specific dates that aren't in a
/// fixed list, or add accessible labels.
///
/// <code>
/// void OnDateRender(DateRenderEventArgs args)
/// {
///     if (Holidays.Contains(args.Date))
///         args.CssClass = "omni-calendar-day-holiday";
///     if (args.Date.DayOfWeek == DayOfWeek.Sunday)
///         args.Disabled = true;
/// }
/// </code>
/// </summary>
public sealed class DateRenderEventArgs
{
    /// <summary>The date being rendered.</summary>
    public DateOnly Date { get; init; }

    /// <summary>True when the date falls outside the visible month (spillover cell).</summary>
    public bool IsOutOfMonth { get; init; }

    /// <summary>True when today's date.</summary>
    public bool IsToday { get; init; }

    /// <summary>Set true to disable the cell. Already-disabled cells (MinDate/MaxDate/etc.) remain disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>Extra CSS class(es) appended to the day cell.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Tooltip text shown via the library's <c>TooltipService</c> (the styled
    /// tooltip, not the browser's native <c>title</c> popup). Prefer this over
    /// <c>Attributes["title"]</c> — if you do set <c>title</c>, the calendar
    /// auto-promotes it into this property so you never get the unstyled
    /// browser tooltip.
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>Extra HTML attributes splatted on the day cell (e.g. <c>aria-label</c>, <c>data-*</c>).</summary>
    public Dictionary<string, object> Attributes { get; } = new();
}
