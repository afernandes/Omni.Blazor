using System.Globalization;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// A value/measure of an <see cref="OmniPivotGrid{TItem}"/> — an aggregate
/// (<see cref="AggregateFunction"/>) of a property computed at each row×column
/// intersection. Multiple values render side-by-side under every column group.
/// </summary>
public sealed class OmniPivotValue<TItem> : OmniPivotFieldBase<TItem>
{
    /// <summary>Aggregate function. Default <see cref="AggregateFunction.Sum"/>.</summary>
    [Parameter] public AggregateFunction Aggregate { get; set; } = AggregateFunction.Sum;

    /// <summary>Composite format string for the cell value (e.g. <c>"{0:C}"</c>, <c>"{0:P0}"</c>).</summary>
    [Parameter] public string? FormatString { get; set; }

    /// <summary>CSS <c>text-align</c> for the value cells. Default <c>right</c>.</summary>
    [Parameter] public string Align { get; set; } = "right";

    /// <summary>Optional cell template (receives the aggregated value).</summary>
    [Parameter] public RenderFragment<object?>? Template { get; set; }

    internal string Format(object? value)
    {
        if (value is null) return "";
        if (!string.IsNullOrEmpty(FormatString))
            return string.Format(CultureInfo.CurrentCulture, FormatString!, value);
        return value switch
        {
            double d => d.ToString("N2", CultureInfo.CurrentCulture),
            decimal m => m.ToString("N2", CultureInfo.CurrentCulture),
            float f => f.ToString("N2", CultureInfo.CurrentCulture),
            _ => value.ToString() ?? ""
        };
    }

    protected override void Register() => Grid?.AddValue(this);
    protected override void Unregister() => Grid?.RemoveValue(this);
}
