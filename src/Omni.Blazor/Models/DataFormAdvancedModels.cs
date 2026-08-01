using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Models;

/// <summary>Responsive, immutable grid definition used by a DataForm or one of its groups.</summary>
public sealed class DataFormLayout
{
    internal DataFormLayout(
        int columns,
        IReadOnlyDictionary<Breakpoint, int> responsiveColumns,
        string? rowGap,
        string? columnGap)
    {
        Columns = columns;
        ResponsiveColumns = responsiveColumns;
        RowGap = rowGap;
        ColumnGap = columnGap;
    }

    /// <summary>Column count used below the first configured breakpoint.</summary>
    public int Columns { get; }

    /// <summary>Column overrides keyed by the standard Omni breakpoints.</summary>
    public IReadOnlyDictionary<Breakpoint, int> ResponsiveColumns { get; }

    /// <summary>Optional CSS row gap.</summary>
    public string? RowGap { get; }

    /// <summary>Optional CSS column gap.</summary>
    public string? ColumnGap { get; }

    internal int GetColumns(Breakpoint breakpoint)
    {
        int result = Columns;
        for (Breakpoint current = Breakpoint.Sm; current <= breakpoint; current++)
        {
            if (ResponsiveColumns.TryGetValue(current, out int value)) result = value;
        }
        return result;
    }

    internal static DataFormLayout Default { get; } =
        new(1, new Dictionary<Breakpoint, int>(), null, null);
}

/// <summary>Fluent builder for a responsive DataForm grid.</summary>
public sealed class DataFormLayoutBuilder
{
    private readonly Action _ensureMutable;
    private readonly Dictionary<Breakpoint, int> _responsiveColumns = [];
    private int _columns = 1;
    private string? _rowGap;
    private string? _columnGap;

    internal DataFormLayoutBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    /// <summary>Sets the mobile-first base column count.</summary>
    public DataFormLayoutBuilder Columns(int value)
    {
        _ensureMutable();
        ValidateColumns(value);
        _columns = value;
        return this;
    }

    /// <summary>Sets a column override beginning at a standard Omni breakpoint.</summary>
    public DataFormLayoutBuilder Columns(Breakpoint breakpoint, int value)
    {
        _ensureMutable();
        ValidateColumns(value);
        if (breakpoint == Breakpoint.Xs) _columns = value;
        else _responsiveColumns[breakpoint] = value;
        return this;
    }

    /// <summary>Sets the CSS row gap.</summary>
    public DataFormLayoutBuilder RowGap(string? value)
    {
        _ensureMutable();
        _rowGap = value;
        return this;
    }

    /// <summary>Sets the CSS column gap.</summary>
    public DataFormLayoutBuilder ColumnGap(string? value)
    {
        _ensureMutable();
        _columnGap = value;
        return this;
    }

    internal DataFormLayout Build()
        => new(
            _columns,
            new System.Collections.ObjectModel.ReadOnlyDictionary<Breakpoint, int>(
                new Dictionary<Breakpoint, int>(_responsiveColumns)),
            _rowGap,
            _columnGap);

    private static void ValidateColumns(int value)
    {
        if (value is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(value), value, "DataForm columns must be between 1 and 12.");
    }
}

/// <summary>Immutable visual and semantic group in a DataForm schema.</summary>
public sealed class DataFormGroup<TModel> where TModel : class
{
    internal DataFormGroup(
        string id,
        string title,
        string? description,
        int order,
        bool visible,
        Func<TModel, bool>? visibleWhen,
        DataFormLayout layout,
        IReadOnlyList<DataFormGroup<TModel>> groups)
    {
        Id = id;
        Title = title;
        Description = description;
        Order = order;
        Visible = visible;
        VisibleWhen = visibleWhen;
        Layout = layout;
        Groups = groups;
    }

    /// <summary>Stable schema identity used for rendering and accessibility.</summary>
    public string Id { get; }

    /// <summary>Group legend.</summary>
    public string Title { get; }

    /// <summary>Optional text rendered below the legend.</summary>
    public string? Description { get; }

    /// <summary>Display order among sibling fields and groups.</summary>
    public int Order { get; }

    /// <summary>Whether the group is statically visible.</summary>
    public bool Visible { get; }

    /// <summary>Responsive grid definition owned by this group.</summary>
    public DataFormLayout Layout { get; }

    /// <summary>Nested groups declared inside this group.</summary>
    public IReadOnlyList<DataFormGroup<TModel>> Groups { get; }

    internal Func<TModel, bool>? VisibleWhen { get; }

    internal bool IsVisible(TModel model)
        => Visible && (VisibleWhen?.Invoke(model) ?? true);
}

/// <summary>Context supplied to application-defined DataForm editor resolvers.</summary>
public sealed class DataFormEditorResolverContext
{
    internal DataFormEditorResolverContext(
        Type modelType,
        string propertyPath,
        PropertyInfo property)
    {
        ModelType = modelType;
        PropertyPath = propertyPath;
        Property = property;
    }

    /// <summary>Model type owning the schema.</summary>
    public Type ModelType { get; }

    /// <summary>Full property path, including nested objects.</summary>
    public string PropertyPath { get; }

    /// <summary>Leaf property to be edited.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Exact value type accepted by the generated binding.</summary>
    public Type ValueType => Property.PropertyType;

    /// <summary>Returns all annotations of a requested type without ambiguity.</summary>
    public IEnumerable<TAttribute> GetAttributes<TAttribute>() where TAttribute : Attribute
        => Property.GetCustomAttributes<TAttribute>(inherit: true);
}

/// <summary>Allows an application to choose a custom component for a DataForm property.</summary>
public interface IDataFormEditorResolver
{
    /// <summary>
    /// Returns a component type or null when the resolver does not handle the property.
    /// Components must expose the conventional Value, ValueChanged and ValueExpression parameters.
    /// </summary>
    Type? Resolve(DataFormEditorResolverContext context);
}

/// <summary>Severity of a DataForm schema diagnostic.</summary>
public enum DataFormDiagnosticSeverity
{
    /// <summary>Informational diagnostic.</summary>
    Info,
    /// <summary>Recoverable schema warning.</summary>
    Warning,
    /// <summary>Configuration error that prevents a field from rendering.</summary>
    Error
}

/// <summary>Actionable diagnostic produced while resolving a DataForm schema.</summary>
public sealed record DataFormDiagnostic(
    string Code,
    string Message,
    DataFormDiagnosticSeverity Severity,
    string? PropertyPath = null);

/// <summary>Immutable runtime state for one DataForm field.</summary>
public sealed record DataFormFieldState(
    string PropertyPath,
    bool IsTouched,
    bool IsModified,
    bool IsValidating,
    bool IsValid,
    IReadOnlyList<string> Errors);

/// <summary>Outcome of an explicit DataForm field-validation operation.</summary>
public enum DataFormValidationStatus
{
    /// <summary>The field has no validation messages.</summary>
    Valid,
    /// <summary>The field has one or more validation messages.</summary>
    Invalid,
    /// <summary>The caller or component lifetime cancelled the operation.</summary>
    Canceled,
    /// <summary>A newer validation for the same field replaced the operation.</summary>
    Superseded
}

/// <summary>
/// Immutable result returned by explicit field validation. Cancellation and
/// latest-wins replacement are intentionally distinct from invalid data.
/// </summary>
public sealed record DataFormValidationResult(
    string PropertyPath,
    DataFormValidationStatus Status,
    IReadOnlyList<string> Errors)
{
    /// <summary>Whether validation completed and the field has no messages.</summary>
    public bool IsValid => Status == DataFormValidationStatus.Valid;

    /// <summary>Whether validation completed with one or more messages.</summary>
    public bool IsInvalid => Status == DataFormValidationStatus.Invalid;

    /// <summary>Whether the operation did not complete because it was cancelled or replaced.</summary>
    public bool IsCanceled => Status is DataFormValidationStatus.Canceled or DataFormValidationStatus.Superseded;
}

/// <summary>Event data emitted when one generated field state changes.</summary>
public sealed record DataFormFieldStateChangedEventArgs<TModel>(
    TModel Model,
    DataFormFieldState State)
    where TModel : class;

/// <summary>Immutable aggregate DataForm validation-state snapshot.</summary>
public sealed record DataFormValidationStateChangedEventArgs<TModel>(
    TModel Model,
    bool IsValidating,
    bool IsValid,
    IReadOnlyList<DataFormFieldState> Fields,
    IReadOnlyList<string> Errors)
    where TModel : class;

internal sealed record DataFormPropertyPath(
    string Path,
    PropertyInfo[] Properties,
    LambdaExpression Expression)
{
    public PropertyInfo Leaf => Properties[^1];

    public object? GetValue(object model)
    {
        object? current = model;
        foreach (PropertyInfo property in Properties)
        {
            if (current is null) return null;
            current = property.GetValue(current);
        }
        return current;
    }

    public object GetOwner(object model)
    {
        object? current = model;
        for (int index = 0; index < Properties.Length - 1; index++)
        {
            current = current is null ? null : Properties[index].GetValue(current);
            if (current is null)
            {
                throw new InvalidOperationException(
                    $"OmniDataForm cannot bind '{Path}' because '{Properties[index].Name}' is null. " +
                    "Initialize nested model objects before rendering the form.");
            }
        }
        return current ?? model;
    }

    public FieldIdentifier GetFieldIdentifier(object model)
        => new(GetOwner(model), Leaf.Name);
}

internal interface IDataFormFieldValidator<TModel> where TModel : class
{
    bool IsSynchronous { get; }
    string? Validate(TModel model, object? value);
    ValueTask<string?> ValidateAsync(TModel model, object? value, CancellationToken cancellationToken);
}

internal sealed class DataFormFieldValidator<TModel, TValue> : IDataFormFieldValidator<TModel>
    where TModel : class
{
    private readonly Func<TValue, TModel, string?>? _validator;
    private readonly Func<TValue, TModel, CancellationToken, ValueTask<string?>>? _asyncValidator;

    public DataFormFieldValidator(Func<TValue, TModel, CancellationToken, ValueTask<string?>> validator)
        => _asyncValidator = validator;

    public DataFormFieldValidator(Func<TValue, TModel, string?> validator)
        => _validator = validator;

    public bool IsSynchronous => _validator is not null;

    public string? Validate(TModel model, object? value)
        => _validator?.Invoke((TValue)value!, model);

    public ValueTask<string?> ValidateAsync(
        TModel model,
        object? value,
        CancellationToken cancellationToken)
        => _validator is not null
            ? ValueTask.FromResult(_validator((TValue)value!, model))
            : _asyncValidator!((TValue)value!, model, cancellationToken);
}
