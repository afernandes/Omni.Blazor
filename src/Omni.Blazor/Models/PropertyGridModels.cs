using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Models;

/// <summary>Field change reported by <c>OmniPropertyGrid</c>.</summary>
public sealed record PropertyGridChangedEventArgs<TModel>(
    TModel Model,
    FieldIdentifier Field,
    bool IsModified)
    where TModel : class;
