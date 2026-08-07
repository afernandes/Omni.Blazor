using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Presentation used by reusable entity-editor adapters.</summary>
public enum EntityEditorPresentation
{
    /// <summary>Renders the editor below the data surface.</summary>
    Inline,

    /// <summary>Renders the editor in a centered modal overlay.</summary>
    Dialog,

    /// <summary>Renders the editor in a right-side modal panel.</summary>
    Drawer
}

/// <summary>Mutation performed by the headless entity editor.</summary>
public enum EntityEditorOperation
{
    /// <summary>Creates an item.</summary>
    Create,

    /// <summary>Updates an existing item.</summary>
    Edit,

    /// <summary>Deletes an existing item.</summary>
    Delete
}

/// <summary>Structured outcome of a reusable entity mutation.</summary>
public enum EntityMutationStatus
{
    /// <summary>The mutation completed successfully.</summary>
    Success,

    /// <summary>The provider rejected one or more domain values.</summary>
    ValidationFailed,

    /// <summary>The submitted version is stale.</summary>
    Conflict,

    /// <summary>The target item no longer exists.</summary>
    NotFound,

    /// <summary>The current user cannot perform the mutation.</summary>
    Forbidden,

    /// <summary>The source is already running an incompatible mutation.</summary>
    Busy,

    /// <summary>An expected or unexpected persistence failure occurred.</summary>
    Failure
}

/// <summary>Immutable provider/local mutation result used by every entity-editor adapter.</summary>
public sealed class EntityMutationResult<TItem> where TItem : class
{
    private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

    private EntityMutationResult(
        EntityMutationStatus status,
        TItem? item,
        TItem? currentItem,
        string? message,
        IReadOnlyList<string>? errors,
        Exception? exception,
        bool localCollectionChanged)
    {
        Status = status;
        Item = item;
        CurrentItem = currentItem;
        Message = message;
        Errors = errors is null or { Count: 0 } ? NoErrors : Array.AsReadOnly(errors.ToArray());
        Exception = exception;
        LocalCollectionChanged = localCollectionChanged;
    }

    /// <summary>Structured mutation status.</summary>
    public EntityMutationStatus Status { get; }

    /// <summary>Whether persistence or the local mutation completed successfully.</summary>
    public bool Succeeded => Status == EntityMutationStatus.Success;

    /// <summary>Authoritative created or updated item.</summary>
    public TItem? Item { get; }

    /// <summary>Latest provider item returned for an optimistic concurrency conflict.</summary>
    public TItem? CurrentItem { get; }

    /// <summary>Human-readable message safe to present to the user.</summary>
    public string? Message { get; }

    /// <summary>Immutable domain-validation messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Observed unexpected exception, when present.</summary>
    public Exception? Exception { get; }

    /// <summary>Whether an owned local list was changed.</summary>
    public bool LocalCollectionChanged { get; }

    /// <summary>Creates a successful create or update result.</summary>
    public static EntityMutationResult<TItem> Success(TItem item, bool localCollectionChanged = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(EntityMutationStatus.Success, item, null, null, null, null, localCollectionChanged);
    }

    /// <summary>Creates a successful delete result.</summary>
    public static EntityMutationResult<TItem> Deleted(bool localCollectionChanged = false)
        => new(EntityMutationStatus.Success, null, null, null, null, null, localCollectionChanged);

    /// <summary>Creates a domain-validation result.</summary>
    public static EntityMutationResult<TItem> ValidationFailed(IEnumerable<string> errors, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(errors);
        string[] snapshot = errors.Where(static error => !string.IsNullOrWhiteSpace(error)).ToArray();
        if (snapshot.Length == 0)
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        return new(EntityMutationStatus.ValidationFailed, null, null, message, snapshot, null, false);
    }

    /// <summary>Creates an optimistic-concurrency conflict.</summary>
    public static EntityMutationResult<TItem> Conflict(TItem? currentItem = null, string? message = null)
        => new(EntityMutationStatus.Conflict, null, currentItem, message, null, null, false);

    /// <summary>Creates a not-found result.</summary>
    public static EntityMutationResult<TItem> NotFound(string? message = null)
        => new(EntityMutationStatus.NotFound, null, null, message, null, null, false);

    /// <summary>Creates an authorization result.</summary>
    public static EntityMutationResult<TItem> Forbidden(string? message = null)
        => new(EntityMutationStatus.Forbidden, null, null, message, null, null, false);

    /// <summary>Creates a busy result without starting another mutation.</summary>
    public static EntityMutationResult<TItem> Busy(string? message = null)
        => new(EntityMutationStatus.Busy, null, null, message, null, null, false);

    /// <summary>Creates a persistence or unexpected-failure result.</summary>
    public static EntityMutationResult<TItem> Failure(string? message = null, Exception? exception = null)
        => new(EntityMutationStatus.Failure, null, null, message, null, exception, false);
}

/// <summary>Cancellable persistence contract shared by grid, scheduler, Kanban and Gantt editors.</summary>
public interface IOmniEntityMutationProvider<TItem, in TKey>
    where TItem : class
    where TKey : notnull
{
    /// <summary>Persists a new validated entity.</summary>
    ValueTask<EntityMutationResult<TItem>> CreateAsync(TItem item, CancellationToken cancellationToken);

    /// <summary>Persists a validated entity update.</summary>
    ValueTask<EntityMutationResult<TItem>> UpdateAsync(TKey key, TItem item, CancellationToken cancellationToken);

    /// <summary>Deletes one entity by stable key.</summary>
    ValueTask<EntityMutationResult<TItem>> DeleteAsync(TKey key, CancellationToken cancellationToken);
}

/// <summary>Delegate adapter for applications that do not need a provider class.</summary>
public sealed class DelegateEntityMutationProvider<TItem, TKey> : IOmniEntityMutationProvider<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly Func<TItem, CancellationToken, ValueTask<EntityMutationResult<TItem>>> _create;
    private readonly Func<TKey, TItem, CancellationToken, ValueTask<EntityMutationResult<TItem>>> _update;
    private readonly Func<TKey, CancellationToken, ValueTask<EntityMutationResult<TItem>>> _delete;

    /// <summary>Creates a provider from structured cancellable delegates.</summary>
    public DelegateEntityMutationProvider(
        Func<TItem, CancellationToken, ValueTask<EntityMutationResult<TItem>>> create,
        Func<TKey, TItem, CancellationToken, ValueTask<EntityMutationResult<TItem>>> update,
        Func<TKey, CancellationToken, ValueTask<EntityMutationResult<TItem>>> delete)
    {
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(delete);
        _create = create;
        _update = update;
        _delete = delete;
    }

    /// <inheritdoc />
    public ValueTask<EntityMutationResult<TItem>> CreateAsync(TItem item, CancellationToken cancellationToken)
        => _create(item, cancellationToken);

    /// <inheritdoc />
    public ValueTask<EntityMutationResult<TItem>> UpdateAsync(TKey key, TItem item, CancellationToken cancellationToken)
        => _update(key, item, cancellationToken);

    /// <inheritdoc />
    public ValueTask<EntityMutationResult<TItem>> DeleteAsync(TKey key, CancellationToken cancellationToken)
        => _delete(key, cancellationToken);
}

/// <summary>Immutable editor options shared by every data-surface adapter.</summary>
public sealed class EntityEditorSchema<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    internal EntityEditorSchema(
        Func<TItem, TKey> keySelector,
        DataFormSchema<TItem> formSchema,
        EntityEditorCreateOptions<TItem>? create,
        EntityEditorEditOptions<TItem>? edit,
        EntityEditorDeleteOptions<TItem>? delete)
    {
        KeySelector = keySelector;
        FormSchema = formSchema;
        CreateOptions = create;
        EditOptions = edit;
        DeleteOptions = delete;
    }

    /// <summary>Stable entity key selector.</summary>
    public Func<TItem, TKey> KeySelector { get; }

    /// <summary>Reusable DataForm schema used by detached drafts.</summary>
    public DataFormSchema<TItem> FormSchema { get; }

    /// <summary>Create behavior, or null when creation is unavailable.</summary>
    public EntityEditorCreateOptions<TItem>? CreateOptions { get; }

    /// <summary>Edit-through-copy behavior, or null when editing is unavailable.</summary>
    public EntityEditorEditOptions<TItem>? EditOptions { get; }

    /// <summary>Confirmed-delete behavior, or null when deletion is unavailable.</summary>
    public EntityEditorDeleteOptions<TItem>? DeleteOptions { get; }

    /// <summary>Creates and builds an immutable editor schema.</summary>
    public static EntityEditorSchema<TItem, TKey> Create(Action<EntityEditorSchemaBuilder<TItem, TKey>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EntityEditorSchemaBuilder<TItem, TKey> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot builder.</summary>
    public static EntityEditorSchemaBuilder<TItem, TKey> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public EntityEditorSchema<TItem, TKey> Extend(Action<EntityEditorSchemaBuilder<TItem, TKey>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EntityEditorSchemaBuilder<TItem, TKey> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed builder for reusable entity editing.</summary>
public sealed class EntityEditorSchemaBuilder<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private Func<TItem, TKey>? _keySelector;
    private DataFormSchema<TItem>? _formSchema;
    private EntityEditorCreateOptions<TItem>? _create;
    private EntityEditorEditOptions<TItem>? _edit;
    private EntityEditorDeleteOptions<TItem>? _delete;
    private bool _built;

    /// <summary>Includes all options from an immutable editor schema.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Include(EntityEditorSchema<TItem, TKey> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _keySelector = schema.KeySelector;
        _formSchema = schema.FormSchema;
        _create = schema.CreateOptions;
        _edit = schema.EditOptions;
        _delete = schema.DeleteOptions;
        return this;
    }

    /// <summary>Sets the stable key used by concurrency and persistence.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Key(Func<TItem, TKey> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _keySelector = selector;
        return this;
    }

    /// <summary>Sets the reusable DataForm schema.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Form(DataFormSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _formSchema = schema;
        return this;
    }

    /// <summary>Enables creation through a generated detached draft.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Create(
        Func<TItem> factory,
        string? title = null,
        EntityEditorPresentation presentation = EntityEditorPresentation.Drawer,
        string? width = "720px")
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(factory);
        _create = new EntityEditorCreateOptions<TItem>(factory, title, presentation, width);
        return this;
    }

    /// <summary>Enables safe edit-through-copy.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Edit(
        Func<TItem, TItem> clone,
        Func<TItem, string?>? title = null,
        EntityEditorPresentation presentation = EntityEditorPresentation.Drawer,
        string? width = "720px")
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(clone);
        _edit = new EntityEditorEditOptions<TItem>(clone, title, presentation, width);
        return this;
    }

    /// <summary>Enables confirmed deletion.</summary>
    public EntityEditorSchemaBuilder<TItem, TKey> Delete(
        Func<TItem, string?>? confirmation = null,
        Func<TItem, string?>? title = null)
    {
        EnsureMutable();
        _delete = new EntityEditorDeleteOptions<TItem>(confirmation, title);
        return this;
    }

    /// <summary>Builds the immutable entity-editor schema.</summary>
    public EntityEditorSchema<TItem, TKey> Build()
    {
        EnsureMutable();
        _built = true;
        return new EntityEditorSchema<TItem, TKey>(
            _keySelector ?? throw new InvalidOperationException("Entity editor requires a stable Key selector."),
            _formSchema ?? DataFormSchema<TItem>.Create(static _ => { }),
            _create,
            _edit,
            _delete);
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("Entity editor schema is immutable after Build().");
    }
}

/// <summary>Immutable create behavior.</summary>
public sealed record EntityEditorCreateOptions<TItem>(
    Func<TItem> Factory,
    string? Title,
    EntityEditorPresentation Presentation,
    string? Width)
    where TItem : class;

/// <summary>Immutable edit-through-copy behavior.</summary>
public sealed record EntityEditorEditOptions<TItem>(
    Func<TItem, TItem> CloneItem,
    Func<TItem, string?>? Title,
    EntityEditorPresentation Presentation,
    string? Width)
    where TItem : class;

/// <summary>Immutable confirmed-delete behavior.</summary>
public sealed record EntityEditorDeleteOptions<TItem>(
    Func<TItem, string?>? Confirmation,
    Func<TItem, string?>? Title)
    where TItem : class;

/// <summary>Successful reusable entity-editor operation.</summary>
public sealed record EntityEditorOperationEventArgs<TItem, TKey>(
    EntityEditorOperation Operation,
    TItem? Item,
    TKey? Key)
    where TItem : class
    where TKey : notnull;

/// <summary>Failed reusable entity-editor operation.</summary>
public sealed record EntityEditorOperationFailedEventArgs<TItem, TKey>(
    EntityEditorOperation Operation,
    TItem? Item,
    TKey? Key,
    EntityMutationResult<TItem> Result)
    where TItem : class
    where TKey : notnull;
