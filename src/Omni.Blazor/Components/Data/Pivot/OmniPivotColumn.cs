using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>A column dimension of an <see cref="OmniPivotGrid{TItem}"/>. Multiple nest.</summary>
public sealed class OmniPivotColumn<TItem> : OmniPivotFieldBase<TItem>
{
    /// <summary>Optional fixed width for the value columns under this group.</summary>
    [Parameter] public string? Width { get; set; }

    protected override void Register() => Grid?.AddColumn(this);
    protected override void Unregister() => Grid?.RemoveColumn(this);
}
