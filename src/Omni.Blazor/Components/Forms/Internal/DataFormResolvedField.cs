using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

internal sealed class DataFormResolvedField<TModel> where TModel : class
{
    public required DataFormField<TModel> Metadata { get; init; }
    public required PropertyInfo Property { get; init; }
    public required Type RendererType { get; init; }
    public required int Order { get; init; }
    public required int ColumnSpan { get; init; }
    public required Dictionary<string, object?> RendererParameters { get; init; }
    public required string FieldId { get; init; }
    public required string EditorId { get; init; }
    public long LookupVersion { get; set; }
    public Func<TModel, bool>? ContainerVisible { get; set; }

    public bool IsVisible(TModel model)
        => (ContainerVisible?.Invoke(model) ?? true)
           && Metadata.Visible
           && (Metadata.VisibleWhen?.Invoke(model) ?? true);

    public FieldIdentifier GetFieldIdentifier(TModel model)
        => Metadata.PropertyPath.GetFieldIdentifier(model);

    public object? GetValue(TModel model) => Metadata.PropertyPath.GetValue(model);
}

/// <summary>Infrastructure-only resolved group consumed by the internal DataForm renderer.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DataFormResolvedGroup<TModel> where TModel : class
{
    internal DataFormResolvedGroup(
        DataFormGroup<TModel> metadata,
        IReadOnlyList<DataFormResolvedField<TModel>> fields,
        IReadOnlyList<DataFormResolvedGroup<TModel>> groups)
    {
        Metadata = metadata;
        Fields = fields;
        Groups = groups;
    }

    internal DataFormGroup<TModel> Metadata { get; }
    internal IReadOnlyList<DataFormResolvedField<TModel>> Fields { get; }
    internal IReadOnlyList<DataFormResolvedGroup<TModel>> Groups { get; }

    internal bool IsVisible(TModel model) => Metadata.IsVisible(model);
}
