using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Models;

/// <summary>Immutable metadata for one DataForm wizard step.</summary>
public sealed record DataFormWizardStep<TModel>(
    string Id,
    string Title,
    string? Description,
    string? Icon,
    DataFormSchema<TModel> Schema,
    Func<TModel, bool>? VisibleWhen,
    Func<TModel, bool>? CanEnter,
    Func<TModel, CancellationToken, ValueTask<IReadOnlyList<string>>>? ValidateAsync)
    where TModel : class;

/// <summary>Immutable ordered DataForm wizard schema.</summary>
public sealed class DataFormWizardSchema<TModel> where TModel : class
{
    internal DataFormWizardSchema(IReadOnlyList<DataFormWizardStep<TModel>> steps) => Steps = steps;

    /// <summary>Ordered immutable step definitions.</summary>
    public IReadOnlyList<DataFormWizardStep<TModel>> Steps { get; }

    /// <summary>Creates and builds an immutable wizard schema.</summary>
    public static DataFormWizardSchema<TModel> Create(
        Action<DataFormWizardSchemaBuilder<TModel>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFormWizardSchemaBuilder<TModel> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable wizard schema builder.</summary>
    public static DataFormWizardSchemaBuilder<TModel> Builder() => new();
}

/// <summary>Strongly typed builder for ordered DataForm wizard steps.</summary>
public sealed class DataFormWizardSchemaBuilder<TModel> where TModel : class
{
    private readonly List<DataFormWizardStep<TModel>> _steps = [];
    private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
    private readonly HashSet<string> _propertyPaths = new(StringComparer.Ordinal);
    private bool _built;

    /// <summary>Adds one step backed by an explicit DataForm schema.</summary>
    public DataFormWizardSchemaBuilder<TModel> Step(
        string id,
        string title,
        DataFormSchema<TModel> schema,
        Action<DataFormWizardStepBuilder<TModel>>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.AutoGenerateFields)
            throw new InvalidOperationException(
                $"DataFormWizard step '{id}' must use AutoGenerateFields(false) so validation stays isolated to the step.");
        if (!_ids.Add(id))
            throw new InvalidOperationException($"DataFormWizard step '{id}' was declared more than once.");
        foreach (DataFormField<TModel> field in schema.Fields)
        {
            if (!_propertyPaths.Add(field.Property))
                throw new InvalidOperationException(
                    $"DataFormWizard field '{field.Property}' was assigned to more than one step.");
        }
        DataFormWizardStepBuilder<TModel> builder = new(id, title, schema, EnsureMutable);
        configure?.Invoke(builder);
        _steps.Add(builder.Build());
        return this;
    }

    /// <summary>Builds the immutable wizard schema.</summary>
    public DataFormWizardSchema<TModel> Build()
    {
        EnsureMutable();
        _built = true;
        if (_steps.Count == 0)
            throw new InvalidOperationException("DataFormWizard requires at least one step.");
        return new DataFormWizardSchema<TModel>(Array.AsReadOnly(_steps.ToArray()));
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("DataFormWizard schema is immutable after Build().");
    }
}

/// <summary>Builder for one typed DataForm wizard step.</summary>
public sealed class DataFormWizardStepBuilder<TModel> where TModel : class
{
    private readonly Action _ensureMutable;
    private readonly string _id;
    private readonly string _title;
    private readonly DataFormSchema<TModel> _schema;
    private string? _description;
    private string? _icon;
    private Func<TModel, bool>? _visibleWhen;
    private Func<TModel, bool>? _canEnter;
    private Func<TModel, CancellationToken, ValueTask<IReadOnlyList<string>>>? _validateAsync;

    internal DataFormWizardStepBuilder(
        string id,
        string title,
        DataFormSchema<TModel> schema,
        Action ensureMutable)
    {
        _id = id;
        _title = title;
        _schema = schema;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets supporting step description text.</summary>
    public DataFormWizardStepBuilder<TModel> Description(string? value) => Set(ref _description, value);

    /// <summary>Sets an optional step icon.</summary>
    public DataFormWizardStepBuilder<TModel> Icon(string? value) => Set(ref _icon, value);

    /// <summary>Shows the step only when the model predicate returns true.</summary>
    public DataFormWizardStepBuilder<TModel> VisibleWhen(Func<TModel, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _visibleWhen, predicate);
    }

    /// <summary>Allows navigation into the step only when the model predicate returns true.</summary>
    public DataFormWizardStepBuilder<TModel> CanEnter(Func<TModel, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _canEnter, predicate);
    }

    /// <summary>Adds cancellable step-level validation after field validation succeeds.</summary>
    public DataFormWizardStepBuilder<TModel> ValidateAsync(
        Func<TModel, CancellationToken, ValueTask<IReadOnlyList<string>>> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        return Set(ref _validateAsync, validator);
    }

    internal DataFormWizardStep<TModel> Build()
        => new(_id, _title, _description, _icon, _schema, _visibleWhen, _canEnter, _validateAsync);

    private DataFormWizardStepBuilder<TModel> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Typed state supplied to a custom DataForm wizard header or actions template.</summary>
public sealed record DataFormWizardTemplateContext<TModel>(
    TModel Model,
    EditContext EditContext,
    DataFormWizardStep<TModel> Step,
    int StepIndex,
    int StepCount,
    bool IsFirst,
    bool IsLast,
    bool IsBusy)
    where TModel : class;

/// <summary>DataForm wizard step transition event.</summary>
public sealed record DataFormWizardStepChangedEventArgs<TModel>(
    DataFormWizardStep<TModel> Step,
    int StepIndex,
    bool MovingForward)
    where TModel : class;
