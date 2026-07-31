using System.Reflection;

namespace Omni.Blazor.Models;

/// <summary>Reusable configuration unit for a model-specific DataForm schema.</summary>
public interface IDataFormSchemaProfile<TModel> where TModel : class
{
    /// <summary>Applies the profile to a mutable schema builder.</summary>
    void Configure(DataFormSchemaBuilder<TModel> builder);
}

/// <summary>
/// Fluent defaults applied to fields selected by a type or attribute
/// convention. Explicit field metadata always has precedence.
/// </summary>
public sealed class DataFormConventionBuilder<TModel> where TModel : class
{
    private readonly Action _ensureMutable;
    private Func<PropertyInfo, string?>? _label;
    private string? _placeholder;
    private string? _hint;
    private int? _order;
    private int? _span;
    private bool? _visible;
    private bool? _required;
    private string? _requiredError;
    private bool? _disabled;
    private bool? _readOnly;

    internal DataFormConventionBuilder(Action ensureMutable)
        => _ensureMutable = ensureMutable;

    /// <summary>Sets a fixed label.</summary>
    public DataFormConventionBuilder<TModel> Label(string? value)
        => Label(_ => value);

    /// <summary>Computes a label from the matched property metadata.</summary>
    public DataFormConventionBuilder<TModel> Label(Func<PropertyInfo, string?> value)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(value);
        _label = value;
        return this;
    }

    /// <summary>Sets the default placeholder.</summary>
    public DataFormConventionBuilder<TModel> Placeholder(string? value)
        => Set(ref _placeholder, value);

    /// <summary>Sets the default helper text.</summary>
    public DataFormConventionBuilder<TModel> Hint(string? value)
        => Set(ref _hint, value);

    /// <summary>Sets the default display order.</summary>
    public DataFormConventionBuilder<TModel> Order(int value)
        => Set(ref _order, value);

    /// <summary>Sets the default grid span.</summary>
    public DataFormConventionBuilder<TModel> Span(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 12);
        return Set(ref _span, value);
    }

    /// <summary>Sets default field visibility.</summary>
    public DataFormConventionBuilder<TModel> Visible(bool value = true)
        => Set(ref _visible, value);

    /// <summary>Marks matching fields as required by default.</summary>
    public DataFormConventionBuilder<TModel> Required(string? error = null)
    {
        _ensureMutable();
        if (error is not null) ArgumentException.ThrowIfNullOrWhiteSpace(error);
        _required = true;
        _requiredError = error;
        return this;
    }

    /// <summary>Disables matching fields by default.</summary>
    public DataFormConventionBuilder<TModel> Disabled(bool value = true)
        => Set(ref _disabled, value);

    /// <summary>Makes matching fields read-only by default.</summary>
    public DataFormConventionBuilder<TModel> ReadOnly(bool value = true)
        => Set(ref _readOnly, value);

    internal DataFormFieldConvention<TModel> Build(Func<PropertyInfo, bool> predicate)
        => new(
            predicate,
            _label,
            _placeholder,
            _hint,
            _order,
            _span,
            _visible,
            _required,
            _requiredError,
            _disabled,
            _readOnly);

    private DataFormConventionBuilder<TModel> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

internal sealed record DataFormFieldConvention<TModel>(
    Func<PropertyInfo, bool> Predicate,
    Func<PropertyInfo, string?>? Label,
    string? Placeholder,
    string? Hint,
    int? Order,
    int? Span,
    bool? Visible,
    bool? Required,
    string? RequiredError,
    bool? Disabled,
    bool? ReadOnly)
    where TModel : class;

internal readonly record struct DataFormConventionDefaults(
    string? Label,
    string? Placeholder,
    string? Hint,
    int? Order,
    int? Span,
    bool? Visible,
    bool? Required,
    string? RequiredError,
    bool? Disabled,
    bool? ReadOnly);
