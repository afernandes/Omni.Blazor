using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>A row dimension of an <see cref="OmniPivotGrid{TItem}"/>. Multiple nest.</summary>
public sealed class OmniPivotRow<TItem> : OmniPivotFieldBase<TItem>
{
    protected override void Register() => Grid?.AddRow(this);
    protected override void Unregister() => Grid?.RemoveRow(this);
}
