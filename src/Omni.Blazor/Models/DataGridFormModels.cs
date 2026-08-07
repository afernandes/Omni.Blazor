using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Placement used by the generated create and edit DataForm.</summary>
public enum DataGridFormPresentation
{
    /// <summary>Renders the editor below the grid without an overlay.</summary>
    Inline,

    /// <summary>Renders the editor in a centered modal overlay.</summary>
    Dialog,

    /// <summary>Renders the editor in a right-side modal panel.</summary>
    Drawer
}

/// <summary>Placement of a generated row action in <c>OmniDataGridForm</c>.</summary>
public enum DataGridFormActionPlacement
{
    /// <summary>Renders the action as a button directly in the actions column.</summary>
    Inline,

    /// <summary>Renders the action in the row's overflow menu.</summary>
    Menu,

    /// <summary>Lets the configured overflow policy choose inline or menu placement.</summary>
    Auto
}

/// <summary>How automatic action overflow is handled.</summary>
public enum DataGridFormActionOverflow
{
    /// <summary>Only actions explicitly marked for the menu are moved.</summary>
    Manual,

    /// <summary>Moves low-priority automatic actions beyond the configured inline limit.</summary>
    Automatic
}

/// <summary>Presentation used when an authorization policy is not satisfied.</summary>
public enum DataGridFormUnauthorizedBehavior
{
    /// <summary>Does not render the unauthorized action.</summary>
    Hide,

    /// <summary>Renders the action disabled.</summary>
    Disable
}

/// <summary>Operation reported by <c>OmniDataGridForm</c> lifecycle events.</summary>
public enum DataGridFormOperation
{
    /// <summary>A new item was created.</summary>
    Create,

    /// <summary>An existing item was updated.</summary>
    Edit,

    /// <summary>An item was deleted.</summary>
    Delete,

    /// <summary>A schema-defined custom row action was executed.</summary>
    Custom,

    /// <summary>A schema-defined action was executed for a selected item snapshot.</summary>
    Bulk
}

/// <summary>Outcome of a provider mutation, including expected domain and concurrency failures.</summary>
public enum DataGridFormMutationStatus
{
    /// <summary>The persistence operation completed successfully.</summary>
    Success,

    /// <summary>The provider rejected one or more domain values.</summary>
    ValidationFailed,

    /// <summary>The item changed after it was loaded and the submitted version is stale.</summary>
    Conflict,

    /// <summary>The target item no longer exists.</summary>
    NotFound,

    /// <summary>The current user is not allowed to perform the operation.</summary>
    Forbidden,

    /// <summary>An expected persistence failure occurred.</summary>
    Failure,

    /// <summary>Persistence succeeded, but refreshing the grid failed.</summary>
    RefreshFailed
}

/// <summary>
/// Typed provider mutation result. Expected validation, authorization and optimistic
/// concurrency outcomes do not need exceptions; unexpected failures may retain one.
/// </summary>
public sealed class DataGridFormMutationResult<TItem> where TItem : class
{
    private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

    private DataGridFormMutationResult(
        DataGridFormMutationStatus status,
        TItem? item,
        TItem? currentItem,
        string? message,
        IReadOnlyList<string>? errors,
        Exception? exception)
    {
        Status = status;
        Item = item;
        CurrentItem = currentItem;
        Message = message;
        Errors = errors is null or { Count: 0 } ? NoErrors : errors.ToArray();
        Exception = exception;
    }

    /// <summary>Structured mutation outcome.</summary>
    public DataGridFormMutationStatus Status { get; }

    /// <summary>Whether persistence completed successfully.</summary>
    public bool Succeeded => Status == DataGridFormMutationStatus.Success;

    /// <summary>Authoritative persisted item for successful create or update operations.</summary>
    public TItem? Item { get; }

    /// <summary>Current server item supplied when optimistic concurrency detects a conflict.</summary>
    public TItem? CurrentItem { get; }

    /// <summary>Human-readable summary safe to present to the user.</summary>
    public string? Message { get; }

    /// <summary>Domain validation messages. The collection is always non-null and immutable.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Observed unexpected exception, when one exists.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a successful create or update result with its authoritative item.</summary>
    public static DataGridFormMutationResult<TItem> Success(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(DataGridFormMutationStatus.Success, item, null, null, null, null);
    }

    /// <summary>Creates a successful delete result.</summary>
    public static DataGridFormMutationResult<TItem> Deleted()
        => new(DataGridFormMutationStatus.Success, null, null, null, null, null);

    /// <summary>Creates a domain validation failure.</summary>
    public static DataGridFormMutationResult<TItem> ValidationFailed(
        IEnumerable<string> errors,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(errors);
        string[] snapshot = errors.Where(static error => !string.IsNullOrWhiteSpace(error)).ToArray();
        if (snapshot.Length == 0)
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        return new(DataGridFormMutationStatus.ValidationFailed, null, null, message, snapshot, null);
    }

    /// <summary>Creates an optimistic concurrency conflict with the latest item when available.</summary>
    public static DataGridFormMutationResult<TItem> Conflict(
        TItem? currentItem = null,
        string? message = null)
        => new(DataGridFormMutationStatus.Conflict, null, currentItem, message, null, null);

    /// <summary>Creates a not-found result.</summary>
    public static DataGridFormMutationResult<TItem> NotFound(string? message = null)
        => new(DataGridFormMutationStatus.NotFound, null, null, message, null, null);

    /// <summary>Creates an authorization failure.</summary>
    public static DataGridFormMutationResult<TItem> Forbidden(string? message = null)
        => new(DataGridFormMutationStatus.Forbidden, null, null, message, null, null);

    /// <summary>Creates an expected or unexpected persistence failure.</summary>
    public static DataGridFormMutationResult<TItem> Failure(
        string? message = null,
        Exception? exception = null)
        => new(DataGridFormMutationStatus.Failure, null, null, message, null, exception);

    internal static DataGridFormMutationResult<TItem> RefreshFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(DataGridFormMutationStatus.RefreshFailed, null, null, exception.Message, null, exception);
    }
}

/// <summary>Successful DataGridForm operation.</summary>
public sealed record DataGridFormOperationEventArgs<TItem, TKey>(
    DataGridFormOperation Operation,
    TItem? Item,
    TKey? Key,
    string? ActionId = null,
    int AffectedCount = 1)
    where TItem : class
    where TKey : notnull;

/// <summary>Failed DataGridForm operation with structured provider or exception details.</summary>
public sealed record DataGridFormOperationFailedEventArgs<TItem, TKey>(
    DataGridFormOperation Operation,
    TItem? Item,
    TKey? Key,
    DataGridFormMutationStatus Status,
    string? Message,
    IReadOnlyList<string> Errors,
    Exception? Exception = null,
    TItem? CurrentItem = null,
    string? ActionId = null,
    int AffectedCount = 1)
    where TItem : class
    where TKey : notnull;

/// <summary>
/// Cancellable CRUD source used by <c>OmniDataGridForm</c>. Implementations own
/// persistence and authorization; the component owns interaction state and validation.
/// </summary>
public interface IDataGridFormProvider<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    /// <summary>Loads one grid window using the DataGrid server-side state.</summary>
    ValueTask<GridLoadResult<TItem>> LoadAsync(
        GridState<TItem> state,
        CancellationToken cancellationToken);

    /// <summary>Persists a new validated item and returns its structured outcome.</summary>
    ValueTask<DataGridFormMutationResult<TItem>> CreateAsync(
        TItem item,
        CancellationToken cancellationToken);

    /// <summary>Persists a validated edit and returns its structured outcome.</summary>
    ValueTask<DataGridFormMutationResult<TItem>> UpdateAsync(
        TKey key,
        TItem item,
        CancellationToken cancellationToken);

    /// <summary>Deletes one item by its stable key and returns its structured outcome.</summary>
    ValueTask<DataGridFormMutationResult<TItem>> DeleteAsync(
        TKey key,
        CancellationToken cancellationToken);
}

/// <summary>Delegate adapter for applications that do not need a provider class.</summary>
public sealed class DelegateDataGridFormProvider<TItem, TKey> : IDataGridFormProvider<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly GridDataProvider<TItem> _load;
    private readonly Func<TItem, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> _create;
    private readonly Func<TKey, TItem, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> _update;
    private readonly Func<TKey, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> _delete;

    /// <summary>Creates a provider from four cancellable delegates.</summary>
    public DelegateDataGridFormProvider(
        GridDataProvider<TItem> load,
        Func<TItem, CancellationToken, ValueTask<TItem>> create,
        Func<TKey, TItem, CancellationToken, ValueTask<TItem>> update,
        Func<TKey, CancellationToken, ValueTask> delete)
    {
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(delete);
        _load = load;
        _create = async (item, cancellationToken) =>
            DataGridFormMutationResult<TItem>.Success(await create(item, cancellationToken));
        _update = async (key, item, cancellationToken) =>
            DataGridFormMutationResult<TItem>.Success(await update(key, item, cancellationToken));
        _delete = async (key, cancellationToken) =>
        {
            await delete(key, cancellationToken);
            return DataGridFormMutationResult<TItem>.Deleted();
        };
    }

    /// <summary>Creates a provider from delegates that return structured mutation outcomes.</summary>
    public DelegateDataGridFormProvider(
        GridDataProvider<TItem> load,
        Func<TItem, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> create,
        Func<TKey, TItem, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> update,
        Func<TKey, CancellationToken, ValueTask<DataGridFormMutationResult<TItem>>> delete)
    {
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(delete);
        _load = load;
        _create = create;
        _update = update;
        _delete = delete;
    }

    /// <inheritdoc />
    public ValueTask<GridLoadResult<TItem>> LoadAsync(
        GridState<TItem> state,
        CancellationToken cancellationToken)
        => _load(state, cancellationToken);

    /// <inheritdoc />
    public ValueTask<DataGridFormMutationResult<TItem>> CreateAsync(
        TItem item,
        CancellationToken cancellationToken)
        => _create(item, cancellationToken);

    /// <inheritdoc />
    public ValueTask<DataGridFormMutationResult<TItem>> UpdateAsync(
        TKey key,
        TItem item,
        CancellationToken cancellationToken)
        => _update(key, item, cancellationToken);

    /// <inheritdoc />
    public ValueTask<DataGridFormMutationResult<TItem>> DeleteAsync(
        TKey key,
        CancellationToken cancellationToken)
        => _delete(key, cancellationToken);
}

/// <summary>Context supplied to a strongly typed custom row action.</summary>
public sealed class DataGridFormActionContext<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly Func<CancellationToken, ValueTask> _refresh;

    internal DataGridFormActionContext(
        TItem item,
        TKey key,
        Func<CancellationToken, ValueTask> refresh)
    {
        Item = item;
        Key = key;
        _refresh = refresh;
    }

    /// <summary>Row item that owns the action.</summary>
    public TItem Item { get; }

    /// <summary>Stable row key resolved by the CRUD schema.</summary>
    public TKey Key { get; }

    /// <summary>Reloads the server source or reshapes the local grid.</summary>
    public ValueTask RefreshAsync(CancellationToken cancellationToken = default)
        => _refresh(cancellationToken);
}

/// <summary>Immutable selected-item snapshot supplied to a typed bulk action.</summary>
public sealed class DataGridFormBulkActionContext<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly Func<CancellationToken, ValueTask> _refresh;
    private readonly Func<ValueTask> _clearSelection;

    internal DataGridFormBulkActionContext(
        IReadOnlyList<TItem> items,
        IReadOnlyList<TKey> keys,
        Func<CancellationToken, ValueTask> refresh,
        Func<ValueTask> clearSelection)
    {
        Items = items;
        Keys = keys;
        _refresh = refresh;
        _clearSelection = clearSelection;
    }

    /// <summary>Stable immutable snapshot of selected items.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>Stable immutable snapshot of selected keys in item order.</summary>
    public IReadOnlyList<TKey> Keys { get; }

    /// <summary>Reloads the server source or reshapes the local grid.</summary>
    public ValueTask RefreshAsync(CancellationToken cancellationToken = default)
        => _refresh(cancellationToken);

    /// <summary>Clears the active grid selection.</summary>
    public ValueTask ClearSelectionAsync() => _clearSelection();
}

/// <summary>Immutable display metadata for one generated DataGrid column.</summary>
public sealed record DataGridFormColumn<TItem>(
    string PropertyName,
    string? Title,
    Func<TItem, object?> Property,
    Func<TItem, string?>? TextSelector,
    RenderFragment<TItem>? Template,
    string? Width,
    bool Sortable,
    bool Resizable,
    bool Filterable,
    ColumnFilterType FilterType,
    bool Groupable,
    bool CanHide,
    bool Visible)
    where TItem : class;

/// <summary>Immutable DataGrid presentation options used by a CRUD schema.</summary>
public sealed record DataGridFormGridOptions<TItem>(
    IReadOnlyList<DataGridFormColumn<TItem>> Columns,
    bool AllowSearch,
    bool AllowPaging,
    int PageSize,
    bool AllowSorting,
    bool AllowColumnFilter,
    bool AllowColumnResize,
    bool AllowColumnVisibility,
    bool AllowGrouping,
    bool AllowExport,
    bool Virtualize,
    string? Height,
    float RowHeight,
    string? SearchPlaceholder,
    string? EmptyText)
    where TItem : class;

/// <summary>Immutable presentation and overflow options for the generated row-actions column.</summary>
public sealed record DataGridFormActionsColumnOptions(
    string Width,
    bool Resizable,
    FrozenPosition? Frozen,
    DataGridFormActionOverflow Overflow,
    int MaximumInlineActions,
    string? MenuText,
    string MenuIcon,
    string? MenuAriaLabel,
    DataGridFormActionPlacement ReorderPlacement);

/// <summary>Immutable presentation and overflow options for selected-item actions.</summary>
public sealed record DataGridFormBulkActionsOptions(
    DataGridFormActionOverflow Overflow,
    int MaximumInlineActions,
    string? MenuText,
    string MenuIcon,
    string? MenuAriaLabel);

/// <summary>Immutable create-operation options.</summary>
public sealed record DataGridFormCreateOptions<TItem>(
    Func<TItem> Factory,
    string? Text,
    string? Icon,
    string? Title,
    DataGridFormPresentation Presentation,
    string? Width,
    string? AuthorizationPolicy,
    DataGridFormUnauthorizedBehavior UnauthorizedBehavior,
    Func<bool>? VisibleWhen,
    Func<bool>? DisabledWhen)
    where TItem : class;

/// <summary>Immutable edit-operation options.</summary>
public sealed record DataGridFormEditOptions<TItem>(
    Func<TItem, TItem> CloneItem,
    Func<TItem, string?>? Title,
    string? Text,
    string? Icon,
    DataGridFormPresentation Presentation,
    string? Width,
    DataGridFormActionPlacement Placement,
    int Priority,
    string? Group,
    string? Shortcut,
    string? Description,
    string? AuthorizationPolicy,
    DataGridFormUnauthorizedBehavior UnauthorizedBehavior,
    Func<TItem, bool>? VisibleWhen,
    Func<TItem, bool>? DisabledWhen)
    where TItem : class;

/// <summary>Immutable delete-operation options.</summary>
public sealed record DataGridFormDeleteOptions<TItem>(
    Func<TItem, string?>? Confirmation,
    Func<TItem, string?>? Title,
    string? Text,
    string? Icon,
    string? ConfirmText,
    string? CancelText,
    DataGridFormActionPlacement Placement,
    int Priority,
    string? Group,
    string? Shortcut,
    string? Description,
    string? AuthorizationPolicy,
    DataGridFormUnauthorizedBehavior UnauthorizedBehavior,
    Func<TItem, bool>? VisibleWhen,
    Func<TItem, bool>? DisabledWhen)
    where TItem : class;

/// <summary>Immutable custom row action.</summary>
public sealed record DataGridFormAction<TItem, TKey>(
    string Id,
    string Text,
    string? Icon,
    ButtonVariant Variant,
    DataGridFormActionPlacement Placement,
    int Priority,
    int Order,
    string? Group,
    string? Shortcut,
    string? Description,
    string? AuthorizationPolicy,
    DataGridFormUnauthorizedBehavior UnauthorizedBehavior,
    Func<TItem, bool>? VisibleWhen,
    Func<TItem, bool>? DisabledWhen,
    Func<TItem, string?>? Confirmation,
    Func<DataGridFormActionContext<TItem, TKey>, CancellationToken, ValueTask> Execute)
    where TItem : class
    where TKey : notnull;

/// <summary>Immutable custom action executed against a selected-item snapshot.</summary>
public sealed record DataGridFormBulkAction<TItem, TKey>(
    string Id,
    string Text,
    string? Icon,
    ButtonVariant Variant,
    DataGridFormActionPlacement Placement,
    int Priority,
    int Order,
    string? Group,
    string? Shortcut,
    string? Description,
    string? AuthorizationPolicy,
    DataGridFormUnauthorizedBehavior UnauthorizedBehavior,
    Func<IReadOnlyList<TItem>, bool>? VisibleWhen,
    Func<IReadOnlyList<TItem>, bool>? DisabledWhen,
    Func<IReadOnlyList<TItem>, string?>? Confirmation,
    Func<DataGridFormBulkActionContext<TItem, TKey>, CancellationToken, ValueTask> Execute)
    where TItem : class
    where TKey : notnull;
