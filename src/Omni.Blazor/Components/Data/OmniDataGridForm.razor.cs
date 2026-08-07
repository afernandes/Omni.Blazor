using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;
using Omni.Blazor.Services;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Coordinates a generated <see cref="OmniDataGrid{TItem}"/>, detached
/// <see cref="OmniDataForm{TModel}"/> drafts and cancellable CRUD persistence.
/// </summary>
public partial class OmniDataGridForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem,
    TKey>
    where TItem : class
    where TKey : notnull
{
    private static readonly object ActionsColumnKey = new();
    private readonly object _actionsMenuOwner = new();
    private readonly object _bulkActionsMenuOwner = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<TKey, RowOperation> _rowOperations = [];
    private readonly HashSet<TItem> _selectedInternal = [];
    private readonly GridDataProvider<TItem> _providerDelegate;
    private readonly Func<TItem, object?> _gridKeySelector;
    private readonly Func<bool> _unsavedChangesPredicate;
    private readonly OmniEntityEditor<TItem, TKey> _entityEditor;
    private readonly IOmniEntityMutationProvider<TItem, TKey> _entityMutationProvider;
    private IReadOnlyDictionary<string, bool> _authorizationDecisions = new Dictionary<string, bool>();
    private DataGridFormSchema<TItem, TKey>? _authorizationSchema;
    private CancellationTokenSource? _authorizationOperation;
    private int _authorizationSequence;
    private OmniDataGrid<TItem>? _grid;
    private OmniDataGridFormEditor<TItem>? _editor;
    private TItem? _draft;
    private TItem? _editingSource;
    private DataGridFormOperation? _editorOperation;
    private TItem? _confirmationItem;
    private DataGridFormAction<TItem, TKey>? _confirmationAction;
    private DataGridFormBulkAction<TItem, TKey>? _bulkConfirmationAction;
    private IReadOnlyList<TItem>? _bulkConfirmationItems;
    private IReadOnlyList<TItem> _selectionSnapshot = Array.Empty<TItem>();
    private HashSet<TItem>? _selectionSource;
    private int _selectionFingerprint;
    private DataGridFormOperationFailedEventArgs<TItem, TKey>? _operationFailure;
    private CancellationTokenSource? _createOperation;
    private CancellationTokenSource? _bulkOperation;
    private bool _createBusy;
    private bool _saving;
    private bool _confirmationBusy;
    private bool _bulkConfirmationBusy;
    private bool _editorDirty;
    private bool _discardConfirmationOpen;
    private int _disposeState;

    [Inject]
    private ContextMenuService ContextMenu { get; set; } = default!;

    [Inject]
    private IDataGridFormPolicyEvaluator PolicyEvaluator { get; set; } = default!;

    /// <summary>Creates the stable DataGrid provider delegate once per component instance.</summary>
    public OmniDataGridForm()
    {
        _providerDelegate = LoadProviderAsync;
        _gridKeySelector = item => ResolveKey(item);
        _unsavedChangesPredicate = () => _editorDirty;
        _entityEditor = new OmniEntityEditor<TItem, TKey>(ResolveKey);
        _entityMutationProvider = new DataGridFormMutationProviderAdapter(this);
    }

    /// <summary>Immutable form, grid and operation schema.</summary>
    [Parameter, EditorRequired]
    public DataGridFormSchema<TItem, TKey> Schema { get; set; } = default!;

    /// <summary>
    /// Mutable local source. Use either Items or Provider, never both. Local
    /// create/edit/delete operations mutate this list only after validation succeeds.
    /// </summary>
    [Parameter]
    public IList<TItem>? Items { get; set; }

    /// <summary>Raised after a local collection mutation.</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Cancellable server-side source and CRUD persistence provider.</summary>
    [Parameter]
    public IDataGridFormProvider<TItem, TKey>? Provider { get; set; }

    /// <summary>Disables every generated operation and editor.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes data read-only while preserving grid navigation and inspection.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Minimum local item count enforced before deletion. Default zero.</summary>
    [Parameter]
    public int MinimumItems { get; set; }

    /// <summary>Maximum local item count enforced before creation. Default unlimited.</summary>
    [Parameter]
    public int MaximumItems { get; set; } = int.MaxValue;

    /// <summary>Shows move-up and move-down actions in local-list mode.</summary>
    [Parameter]
    public bool AllowReorder { get; set; }

    /// <summary>Allows the schema-defined create workflow. Default true.</summary>
    [Parameter]
    public bool AllowCreate { get; set; } = true;

    /// <summary>Allows the schema-defined edit workflow. Default true.</summary>
    [Parameter]
    public bool AllowEdit { get; set; } = true;

    /// <summary>Allows the schema-defined delete workflow. Default true.</summary>
    [Parameter]
    public bool AllowDelete { get; set; } = true;

    /// <summary>
    /// Renders the semantic editor form element. Disable when this component is
    /// embedded inside another form to avoid nested HTML forms. Default true.
    /// </summary>
    [Parameter]
    public bool RenderEditorFormElement { get; set; } = true;

    /// <summary>Optional runtime width override for the generated row-actions column.</summary>
    [Parameter]
    public string? ActionsWidth { get; set; }

    /// <summary>Optional runtime override for the generated row-actions column resizer.</summary>
    [Parameter]
    public bool? ActionsResizable { get; set; }

    /// <summary>Optionally freezes the generated row-actions column on the left or right edge.</summary>
    [Parameter]
    public FrozenPosition? ActionsFrozen { get; set; }

    /// <summary>Optional visible label for the row overflow-menu button. The default is icon-only.</summary>
    [Parameter]
    public string? ActionsMenuText { get; set; }

    /// <summary>Optional runtime icon override for the row overflow-menu button.</summary>
    [Parameter]
    public string? ActionsMenuIcon { get; set; }

    /// <summary>Accessible label for the row overflow-menu button.</summary>
    [Parameter]
    public string? ActionsMenuAriaLabel { get; set; }

    /// <summary>Optionally overrides generated move-up and move-down action placement.</summary>
    [Parameter]
    public DataGridFormActionPlacement? ReorderActionsPlacement { get; set; }

    /// <summary>Shows operation failures in the component. Default true.</summary>
    [Parameter]
    public bool ShowOperationErrors { get; set; } = true;

    /// <summary>Asks for confirmation before closing a modified generated editor. Default true.</summary>
    [Parameter]
    public bool ConfirmDiscardChanges { get; set; } = true;

    /// <summary>Protects route changes and browser unload while the generated editor is modified. Default true.</summary>
    [Parameter]
    public bool GuardNavigationWithUnsavedChanges { get; set; } = true;

    /// <summary>Externally controlled selected-item set used by schema-defined bulk actions.</summary>
    [Parameter]
    public HashSet<TItem>? SelectedItems { get; set; }

    /// <summary>Raised after the DataGrid selection or a bulk action clears the selected set.</summary>
    [Parameter]
    public EventCallback<HashSet<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Maximum items accepted in one bulk-action snapshot. Default 1,000.</summary>
    [Parameter]
    public int MaximumSelectedItems { get; set; } = 1_000;

    /// <summary>Controlled DataGrid layout, sort, filter, grouping and search preferences.</summary>
    [Parameter]
    public DataGridViewState? ViewState { get; set; }

    /// <summary>Raised after the generated DataGrid view preferences change.</summary>
    [Parameter]
    public EventCallback<DataGridViewState> ViewStateChanged { get; set; }

    /// <summary>Optional stable local-storage key used to restore generated DataGrid preferences.</summary>
    [Parameter]
    public string? PersistViewStateKey { get; set; }

    /// <summary>Additional content rendered in the DataGrid toolbar.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Custom empty-state content.</summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Custom DataGrid loading content.</summary>
    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Custom renderer for the latest structured operation failure.</summary>
    [Parameter]
    public RenderFragment<DataGridFormOperationFailedEventArgs<TItem, TKey>>? OperationErrorTemplate { get; set; }

    /// <summary>Raised after create, edit, delete or custom action success.</summary>
    [Parameter]
    public EventCallback<DataGridFormOperationEventArgs<TItem, TKey>> OperationCompleted { get; set; }

    /// <summary>Raised after a handled operation failure.</summary>
    [Parameter]
    public EventCallback<DataGridFormOperationFailedEventArgs<TItem, TKey>> OperationFailed { get; set; }

    /// <summary>Whether one or more persistence/custom operations are active.</summary>
    public bool HasActiveOperation => _createBusy || _rowOperations.Count != 0 || _bulkOperation is not null;

    private DataGridFormGridOptions<TItem> GridOptions => Schema.Grid;
    private DataGridFormActionsColumnOptions ActionsColumnOptions => Schema.ActionsColumn;
    private string EffectiveActionsWidth => ActionsWidth ?? ActionsColumnOptions.Width;
    private bool EffectiveActionsResizable => ActionsResizable ?? ActionsColumnOptions.Resizable;
    private FrozenPosition? EffectiveActionsFrozen => ActionsFrozen ?? ActionsColumnOptions.Frozen;
    private string? EffectiveActionsMenuText => ActionsMenuText ?? ActionsColumnOptions.MenuText;
    private string EffectiveActionsMenuIcon => ActionsMenuIcon ?? ActionsColumnOptions.MenuIcon;
    private DataGridFormActionPlacement EffectiveReorderPlacement =>
        ReorderActionsPlacement ?? ActionsColumnOptions.ReorderPlacement;
    private bool IsLocalMode => Provider is null;
    private IEnumerable<TItem>? LocalData => IsLocalMode ? Items : null;
    private GridDataProvider<TItem>? EffectiveProvider => Provider is null ? null : _providerDelegate;
    private Func<TItem, object?> GridKeySelector => _gridKeySelector;
    private HashSet<TItem> Selection => SelectedItems ?? _selectedInternal;
    private bool HasBulkActions
    {
        get
        {
            foreach (DataGridFormBulkAction<TItem, TKey> action in Schema.BulkActions)
            {
                if (IsAuthorizationVisible(action.AuthorizationPolicy, action.UnauthorizedBehavior)) return true;
            }
            return false;
        }
    }
    private bool HasRowActions
    {
        get
        {
            if (AllowReorder && IsLocalMode) return true;
            if (AllowEdit
                && Schema.EditOptions is { } edit
                && IsAuthorizationVisible(edit.AuthorizationPolicy, edit.UnauthorizedBehavior))
                return true;
            if (AllowDelete
                && Schema.DeleteOptions is { } delete
                && IsAuthorizationVisible(delete.AuthorizationPolicy, delete.UnauthorizedBehavior))
                return true;
            foreach (DataGridFormAction<TItem, TKey> action in Schema.Actions)
            {
                if (IsAuthorizationVisible(action.AuthorizationPolicy, action.UnauthorizedBehavior)) return true;
            }
            return false;
        }
    }
    private string ActionsMenuLabel => string.IsNullOrWhiteSpace(ActionsMenuAriaLabel ?? ActionsColumnOptions.MenuAriaLabel)
        ? Texts.DataGridFormMoreActions
        : ActionsMenuAriaLabel ?? ActionsColumnOptions.MenuAriaLabel!;
    private bool IsActionsMenuOpen => ContextMenu.IsOwnedBy(_actionsMenuOwner);
    private bool IsBulkActionsMenuOpen => ContextMenu.IsOwnedBy(_bulkActionsMenuOwner);
    private string BulkActionsMenuLabel => string.IsNullOrWhiteSpace(Schema.BulkActionsBar.MenuAriaLabel)
        ? Texts.DataGridFormMoreActions
        : Schema.BulkActionsBar.MenuAriaLabel;

    private string RootCss => CssBuilder.Default("omni-data-grid-form")
        .AddClass("omni-data-grid-form-disabled", Disabled)
        .AddClass(Class)
        .Build();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Schema);
        if ((Items is null) == (Provider is null))
            throw new InvalidOperationException("OmniDataGridForm requires exactly one source: Items or Provider.");
        if (MinimumItems < 0) throw new ArgumentOutOfRangeException(nameof(MinimumItems));
        if (MaximumItems < 1) throw new ArgumentOutOfRangeException(nameof(MaximumItems));
        if (MaximumSelectedItems < 1) throw new ArgumentOutOfRangeException(nameof(MaximumSelectedItems));
        if (Selection.Count > MaximumSelectedItems)
            throw new InvalidOperationException(
                $"OmniDataGridForm received {Selection.Count} selected items; the configured maximum is {MaximumSelectedItems}.");
        if (MinimumItems > MaximumItems)
            throw new ArgumentException("MinimumItems cannot exceed MaximumItems.");
        if (Items is { Count: var count } && count > MaximumItems)
            throw new InvalidOperationException(
                $"OmniDataGridForm received {count} local items; the configured maximum is {MaximumItems}.");
        UpdateSelectionSnapshot(Selection);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(_authorizationSchema, Schema))
            await RefreshAuthorizationCoreAsync();
    }

    /// <summary>Opens a create draft when creation is configured.</summary>
    public Task BeginCreateAsync()
        => InvokeAsync(OpenCreate);

    /// <summary>Opens a detached edit draft for one row.</summary>
    public Task BeginEditAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return InvokeAsync(() => OpenEdit(item));
    }

    /// <summary>Reloads the current provider window or reshapes the local grid.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => InvokeAsync(async () => await RefreshCoreAsync(cancellationToken));

    /// <summary>Reevaluates every distinct schema authorization policy once, sequentially and latest-wins.</summary>
    public Task RefreshAuthorizationAsync(CancellationToken cancellationToken = default)
        => InvokeAsync(async () =>
        {
            await RefreshAuthorizationCoreAsync(cancellationToken);
            if (Volatile.Read(ref _disposeState) == 0) StateHasChanged();
        });

    /// <summary>Captures the generated DataGrid view preferences.</summary>
    public DataGridViewState CaptureViewState()
        => _grid?.CaptureViewState()
           ?? throw new InvalidOperationException("The generated DataGrid has not rendered yet.");

    /// <summary>Applies view preferences to the generated DataGrid.</summary>
    public Task ApplyViewStateAsync(DataGridViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return _grid?.ApplyViewStateAsync(state)
               ?? throw new InvalidOperationException("The generated DataGrid has not rendered yet.");
    }

    /// <summary>Restores the generated DataGrid declaration and optionally clears browser persistence.</summary>
    public Task ResetViewStateAsync(bool clearPersisted = true)
        => _grid?.ResetViewStateAsync(clearPersisted)
           ?? throw new InvalidOperationException("The generated DataGrid has not rendered yet.");

    private async Task RefreshAuthorizationCoreAsync(CancellationToken cancellationToken = default)
    {
        HashSet<string> policies = CollectAuthorizationPolicies();
        _authorizationSchema = Schema;
        CancellationTokenSource next = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token,
            cancellationToken);
        CancellationTokenSource? previous = Interlocked.Exchange(ref _authorizationOperation, next);
        previous?.Cancel();
        int sequence = Interlocked.Increment(ref _authorizationSequence);
        try
        {
            if (policies.Count == 0)
            {
                _authorizationDecisions = new Dictionary<string, bool>();
                return;
            }

            var decisions = new Dictionary<string, bool>(policies.Count, StringComparer.Ordinal);
            foreach (string policy in policies)
            {
                next.Token.ThrowIfCancellationRequested();
                decisions.Add(policy, await PolicyEvaluator.AuthorizeAsync(policy, next.Token));
            }
            if (sequence == Volatile.Read(ref _authorizationSequence) && !next.IsCancellationRequested)
                _authorizationDecisions = decisions;
        }
        catch (OperationCanceledException) when (next.IsCancellationRequested)
        {
        }
        finally
        {
            Interlocked.CompareExchange(ref _authorizationOperation, null, next);
            next.Dispose();
        }
    }

    private HashSet<string> CollectAuthorizationPolicies()
    {
        var policies = new HashSet<string>(StringComparer.Ordinal);
        AddPolicy(Schema.CreateOptions?.AuthorizationPolicy);
        AddPolicy(Schema.EditOptions?.AuthorizationPolicy);
        AddPolicy(Schema.DeleteOptions?.AuthorizationPolicy);
        foreach (DataGridFormAction<TItem, TKey> action in Schema.Actions)
            AddPolicy(action.AuthorizationPolicy);
        foreach (DataGridFormBulkAction<TItem, TKey> action in Schema.BulkActions)
            AddPolicy(action.AuthorizationPolicy);
        return policies;

        void AddPolicy(string? policy)
        {
            if (!string.IsNullOrWhiteSpace(policy)) policies.Add(policy);
        }
    }

    private bool IsAuthorized(string? policy)
        => string.IsNullOrWhiteSpace(policy)
           || (_authorizationDecisions.TryGetValue(policy, out bool authorized) && authorized);

    private bool IsAuthorizationVisible(
        string? policy,
        DataGridFormUnauthorizedBehavior behavior)
        => IsAuthorized(policy) || behavior == DataGridFormUnauthorizedBehavior.Disable;

    private void OpenCreate()
    {
        if (!CanCreate || Schema.CreateOptions is not { } options) return;
        TItem draft = options.Factory()
            ?? throw new InvalidOperationException("DataGridForm create factory returned null.");
        _operationFailure = null;
        _editingSource = null;
        _draft = draft;
        _editorOperation = DataGridFormOperation.Create;
    }

    private void OpenEdit(TItem item)
    {
        if (!CanEdit(item) || Schema.EditOptions is not { } options) return;
        TItem draft = options.CloneItem(item)
            ?? throw new InvalidOperationException("DataGridForm edit clone returned null.");
        if (ReferenceEquals(draft, item))
            throw new InvalidOperationException(
                "DataGridForm edit Clone must return a detached instance so Cancel cannot mutate the live row.");
        TKey sourceKey = ResolveKey(item);
        TKey draftKey = ResolveKey(draft);
        if (!EqualityComparer<TKey>.Default.Equals(sourceKey, draftKey))
            throw new InvalidOperationException("DataGridForm edit Clone must preserve the stable row key.");
        _operationFailure = null;
        _editingSource = item;
        _draft = draft;
        _editorOperation = DataGridFormOperation.Edit;
    }

    private bool CanCreate
        => IsCreateVisible
           && !Disabled
           && !ReadOnly
           && AllowCreate
           && _bulkOperation is null
           && !_createBusy
           && Schema.CreateOptions is not null
           && IsAuthorized(Schema.CreateOptions.AuthorizationPolicy)
           && !(Schema.CreateOptions.DisabledWhen?.Invoke() ?? false)
           && (!IsLocalMode || (Items is { IsReadOnly: false } items && items.Count < MaximumItems));

    private bool IsCreateVisible
        => AllowCreate
           && Schema.CreateOptions is { } options
           && IsAuthorizationVisible(options.AuthorizationPolicy, options.UnauthorizedBehavior)
           && (options.VisibleWhen?.Invoke() ?? true);

    private bool CanDelete(TItem item)
        => IsDeleteVisible(item)
           && !Disabled
           && !ReadOnly
           && AllowDelete
           && _bulkOperation is null
           && !IsRowBusy(item)
           && Schema.DeleteOptions is not null
           && IsAuthorized(Schema.DeleteOptions.AuthorizationPolicy)
           && !(Schema.DeleteOptions.DisabledWhen?.Invoke(item) ?? false)
           && (!IsLocalMode || (Items is { IsReadOnly: false } items && items.Count > MinimumItems));

    private bool IsDeleteVisible(TItem item)
        => AllowDelete
           && Schema.DeleteOptions is { } options
           && IsAuthorizationVisible(options.AuthorizationPolicy, options.UnauthorizedBehavior)
           && (options.VisibleWhen?.Invoke(item) ?? true);

    private bool CanEdit(TItem item)
        => IsEditVisible(item)
           && !Disabled
           && !ReadOnly
           && AllowEdit
           && _bulkOperation is null
           && !IsRowBusy(item)
           && Schema.EditOptions is not null
           && IsAuthorized(Schema.EditOptions.AuthorizationPolicy)
           && !(Schema.EditOptions.DisabledWhen?.Invoke(item) ?? false)
           && (!IsLocalMode || Items is { IsReadOnly: false });

    private bool IsEditVisible(TItem item)
        => AllowEdit
           && Schema.EditOptions is { } options
           && IsAuthorizationVisible(options.AuthorizationPolicy, options.UnauthorizedBehavior)
           && (options.VisibleWhen?.Invoke(item) ?? true);

    private async Task SaveEditorAsync(EditContext context)
    {
        if (_saving || _draft is null || _editorOperation is null) return;
        TItem draft = _draft;
        DataGridFormOperation operation = _editorOperation.Value;
        if (operation == DataGridFormOperation.Create)
            await SaveCreateAsync(draft);
        else if (operation == DataGridFormOperation.Edit && _editingSource is not null)
            await SaveEditAsync(_editingSource, draft);
    }

    private async Task SaveCreateAsync(TItem draft)
    {
        if (!CanCreate) return;
        CancellationTokenSource operation = BeginCreateOperation();
        TItem? completedItem = null;
        TKey? completedKey = default;
        bool notifyItemsChanged = false;
        bool committed = false;
        _saving = true;
        _operationFailure = null;
        try
        {
            EntityMutationResult<TItem> result = await _entityEditor.CreateAsync(
                draft,
                Items,
                Provider is null ? null : _entityMutationProvider,
                MaximumItems,
                operation.Token);
            if (!result.Succeeded)
            {
                await ReportFailureAsync(DataGridFormOperation.Create, draft, default, result);
                return;
            }
            TItem persisted = result.Item
                ?? throw new InvalidOperationException(
                    "A successful entity create result must contain the authoritative item.");
            notifyItemsChanged = result.LocalCollectionChanged;
            committed = true;
            CloseEditor();
            await RefreshCoreAsync(operation.Token);
            completedItem = persisted;
            completedKey = ResolveKey(persisted);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            DataGridFormMutationResult<TItem> failure = committed
                ? DataGridFormMutationResult<TItem>.RefreshFailure(exception)
                : DataGridFormMutationResult<TItem>.Failure(exception.Message, exception);
            await ReportFailureAsync(DataGridFormOperation.Create, draft, default, failure);
        }
        finally
        {
            _saving = false;
            EndCreateOperation(operation);
        }
        if (notifyItemsChanged)
            await NotifyItemsChangedAsync();
        if (completedItem is not null)
            await NotifyCompletedAsync(DataGridFormOperation.Create, completedItem, completedKey!);
    }

    private async Task SaveEditAsync(TItem source, TItem draft)
    {
        TKey key = ResolveKey(source);
        if (!EqualityComparer<TKey>.Default.Equals(key, ResolveKey(draft)))
        {
            await ReportFailureAsync(
                DataGridFormOperation.Edit,
                draft,
                key,
                DataGridFormMutationResult<TItem>.Failure(
                    "DataGridForm does not allow changing a stable row key during edit."));
            return;
        }
        if (!TryBeginRowOperation(key, DataGridFormOperation.Edit, null, out CancellationTokenSource operation))
            return;
        TItem? completedItem = null;
        bool notifyItemsChanged = false;
        bool committed = false;
        _saving = true;
        _operationFailure = null;
        try
        {
            EntityMutationResult<TItem> result = await _entityEditor.UpdateAsync(
                source,
                draft,
                Items,
                Provider is null ? null : _entityMutationProvider,
                operation.Token);
            if (!result.Succeeded)
            {
                await ReportFailureAsync(DataGridFormOperation.Edit, draft, key, result);
                return;
            }
            TItem persisted = result.Item
                ?? throw new InvalidOperationException(
                    "A successful entity update result must contain the authoritative item.");
            notifyItemsChanged = result.LocalCollectionChanged;
            committed = true;
            CloseEditor();
            await RefreshCoreAsync(operation.Token);
            completedItem = persisted;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            DataGridFormMutationResult<TItem> failure = committed
                ? DataGridFormMutationResult<TItem>.RefreshFailure(exception)
                : DataGridFormMutationResult<TItem>.Failure(exception.Message, exception);
            await ReportFailureAsync(DataGridFormOperation.Edit, draft, key, failure);
        }
        finally
        {
            _saving = false;
            EndRowOperation(key, operation);
        }
        if (notifyItemsChanged)
            await NotifyItemsChangedAsync();
        if (completedItem is not null)
            await NotifyCompletedAsync(DataGridFormOperation.Edit, completedItem, key);
    }

    private Task RequestDeleteAsync(TItem item)
    {
        if (!CanDelete(item)) return Task.CompletedTask;
        _operationFailure = null;
        _confirmationItem = item;
        _confirmationAction = null;
        return Task.CompletedTask;
    }

    private async Task DeleteConfirmedAsync(TItem item)
    {
        TKey key = ResolveKey(item);
        if (!TryBeginRowOperation(key, DataGridFormOperation.Delete, null, out CancellationTokenSource operation))
            return;
        bool completed = false;
        bool notifyItemsChanged = false;
        bool committed = false;
        _confirmationBusy = true;
        _operationFailure = null;
        try
        {
            EntityMutationResult<TItem> result = await _entityEditor.DeleteAsync(
                item,
                Items,
                Provider is null ? null : _entityMutationProvider,
                MinimumItems,
                operation.Token);
            if (!result.Succeeded)
            {
                await ReportFailureAsync(DataGridFormOperation.Delete, item, key, result);
                return;
            }
            notifyItemsChanged = result.LocalCollectionChanged;
            committed = true;
            ClearConfirmation();
            await RefreshCoreAsync(operation.Token);
            completed = true;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            DataGridFormMutationResult<TItem> failure = committed
                ? DataGridFormMutationResult<TItem>.RefreshFailure(exception)
                : DataGridFormMutationResult<TItem>.Failure(exception.Message, exception);
            await ReportFailureAsync(DataGridFormOperation.Delete, item, key, failure);
        }
        finally
        {
            _confirmationBusy = false;
            EndRowOperation(key, operation);
        }
        if (notifyItemsChanged)
            await NotifyItemsChangedAsync();
        if (completed)
            await NotifyCompletedAsync(DataGridFormOperation.Delete, item, key);
    }

    private Task RequestActionAsync(TItem item, DataGridFormAction<TItem, TKey> action)
    {
        if (!CanExecuteRowAction(item, action)) return Task.CompletedTask;
        _operationFailure = null;
        if (action.Confirmation is not null)
        {
            _confirmationItem = item;
            _confirmationAction = action;
            return Task.CompletedTask;
        }
        return ExecuteActionAsync(item, action);
    }

    private bool HasVisibleMenuActions(TItem item)
    {
        if (Schema.EditOptions is { } editOptions && ShouldRenderEditInMenu(item, editOptions))
            return true;
        foreach (DataGridFormAction<TItem, TKey> action in Schema.Actions)
        {
            if (ShouldRenderActionInMenu(item, action)) return true;
        }
        if (Schema.DeleteOptions is { } deleteOptions && ShouldRenderDeleteInMenu(item, deleteOptions))
            return true;
        return ShouldRenderReorderInMenu(item, -1) || ShouldRenderReorderInMenu(item, 1);
    }

    private bool IsRowActionVisible(TItem item, DataGridFormAction<TItem, TKey> action)
        => IsAuthorizationVisible(action.AuthorizationPolicy, action.UnauthorizedBehavior)
           && (action.VisibleWhen?.Invoke(item) ?? true);

    private bool CanExecuteRowAction(TItem item, DataGridFormAction<TItem, TKey> action)
        => IsRowActionVisible(item, action)
           && IsAuthorized(action.AuthorizationPolicy)
           && !Disabled
           && !ReadOnly
           && _bulkOperation is null
           && !IsRowBusy(item)
           && !(action.DisabledWhen?.Invoke(item) ?? false);

    private bool ShouldRenderEditInline(TItem item, DataGridFormEditOptions<TItem> options)
        => IsEditVisible(item) && ShouldPlaceInline(item, options.Placement, options.Priority, orderIndex: 0);

    private bool ShouldRenderEditInMenu(TItem item, DataGridFormEditOptions<TItem> options)
        => IsEditVisible(item) && !ShouldPlaceInline(item, options.Placement, options.Priority, orderIndex: 0);

    private bool ShouldRenderActionInline(TItem item, DataGridFormAction<TItem, TKey> action)
        => IsRowActionVisible(item, action)
           && ShouldPlaceInline(item, action.Placement, action.Priority, GetActionOrderIndex(action));

    private bool ShouldRenderActionInMenu(TItem item, DataGridFormAction<TItem, TKey> action)
        => IsRowActionVisible(item, action)
           && !ShouldPlaceInline(item, action.Placement, action.Priority, GetActionOrderIndex(action));

    private bool ShouldRenderDeleteInline(TItem item, DataGridFormDeleteOptions<TItem> options)
        => IsDeleteVisible(item)
           && ShouldPlaceInline(item, options.Placement, options.Priority, Schema.Actions.Count + 1);

    private bool ShouldRenderDeleteInMenu(TItem item, DataGridFormDeleteOptions<TItem> options)
        => IsDeleteVisible(item)
           && !ShouldPlaceInline(item, options.Placement, options.Priority, Schema.Actions.Count + 1);

    private bool ShouldRenderReorderInline(TItem item, int offset)
        => AllowReorder
           && IsLocalMode
           && ShouldPlaceInline(
               item,
               EffectiveReorderPlacement,
               offset < 0 ? 25 : 24,
               Schema.Actions.Count + (offset < 0 ? 2 : 3));

    private bool ShouldRenderReorderInMenu(TItem item, int offset)
        => AllowReorder
           && IsLocalMode
           && !ShouldPlaceInline(
               item,
               EffectiveReorderPlacement,
               offset < 0 ? 25 : 24,
               Schema.Actions.Count + (offset < 0 ? 2 : 3));

    private bool ShouldPlaceInline(
        TItem item,
        DataGridFormActionPlacement placement,
        int priority,
        int orderIndex)
    {
        if (placement == DataGridFormActionPlacement.Inline) return true;
        if (placement == DataGridFormActionPlacement.Menu) return false;
        if (ActionsColumnOptions.Overflow == DataGridFormActionOverflow.Manual) return true;

        int available = ActionsColumnOptions.MaximumInlineActions - CountPinnedInlineActions(item);
        if (available <= 0) return false;
        int higherRanked = CountHigherRankedAutomaticActions(item, priority, orderIndex);
        return higherRanked < available;
    }

    private int CountPinnedInlineActions(TItem item)
    {
        int count = 0;
        if (Schema.EditOptions is { Placement: DataGridFormActionPlacement.Inline } && IsEditVisible(item)) count++;
        foreach (DataGridFormAction<TItem, TKey> action in Schema.Actions)
        {
            if (action.Placement == DataGridFormActionPlacement.Inline && IsRowActionVisible(item, action)) count++;
        }
        if (Schema.DeleteOptions is { Placement: DataGridFormActionPlacement.Inline } && IsDeleteVisible(item)) count++;
        if (AllowReorder && IsLocalMode && EffectiveReorderPlacement == DataGridFormActionPlacement.Inline) count += 2;
        return count;
    }

    private int CountHigherRankedAutomaticActions(TItem item, int priority, int orderIndex)
    {
        int count = 0;
        if (Schema.EditOptions is { Placement: DataGridFormActionPlacement.Auto } edit
            && IsEditVisible(item)
            && Outranks(edit.Priority, 0, priority, orderIndex))
            count++;
        for (int index = 0; index < Schema.Actions.Count; index++)
        {
            DataGridFormAction<TItem, TKey> action = Schema.Actions[index];
            if (action.Placement == DataGridFormActionPlacement.Auto
                && IsRowActionVisible(item, action)
                && Outranks(action.Priority, index + 1, priority, orderIndex))
                count++;
        }
        if (Schema.DeleteOptions is { Placement: DataGridFormActionPlacement.Auto } delete
            && IsDeleteVisible(item)
            && Outranks(delete.Priority, Schema.Actions.Count + 1, priority, orderIndex))
            count++;
        if (AllowReorder && IsLocalMode && EffectiveReorderPlacement == DataGridFormActionPlacement.Auto)
        {
            if (Outranks(25, Schema.Actions.Count + 2, priority, orderIndex)) count++;
            if (Outranks(24, Schema.Actions.Count + 3, priority, orderIndex)) count++;
        }
        return count;
    }

    private int GetActionOrderIndex(DataGridFormAction<TItem, TKey> target)
    {
        for (int index = 0; index < Schema.Actions.Count; index++)
        {
            if (ReferenceEquals(Schema.Actions[index], target)) return index + 1;
        }
        return int.MaxValue;
    }

    private static bool Outranks(int candidatePriority, int candidateOrder, int priority, int order)
        => candidatePriority > priority || (candidatePriority == priority && candidateOrder < order);

    private bool IsActionsMenuDisabled(TItem item)
        => Disabled || ReadOnly || _bulkOperation is not null || IsRowBusy(item);

    private Task OpenActionsMenuAsync(MouseEventArgs args, TItem item)
    {
        if (IsActionsMenuDisabled(item)) return Task.CompletedTask;

        List<ContextMenuItem> items = new(Schema.Actions.Count + 4);
        if (Schema.EditOptions is { } editOptions && ShouldRenderEditInMenu(item, editOptions))
        {
            items.Add(new ContextMenuItem
            {
                Text = editOptions.Text ?? Texts.Edit,
                Icon = editOptions.Icon,
                Group = editOptions.Group,
                Shortcut = editOptions.Shortcut,
                Description = editOptions.Description,
                Disabled = !CanEdit(item),
                OnClick = () => DispatchMenuActionAsync(() => BeginEditAsync(item))
            });
        }

        foreach (DataGridFormAction<TItem, TKey> action in Schema.Actions)
        {
            if (!ShouldRenderActionInMenu(item, action)) continue;
            items.Add(new ContextMenuItem
            {
                Text = action.Text,
                Icon = action.Icon,
                Group = action.Group,
                Shortcut = action.Shortcut,
                Description = action.Description,
                IsDanger = action.Variant == ButtonVariant.Danger,
                Disabled = !CanExecuteRowAction(item, action),
                OnClick = () => DispatchMenuActionAsync(() => RequestActionAsync(item, action))
            });
        }

        if (Schema.DeleteOptions is { } deleteOptions && ShouldRenderDeleteInMenu(item, deleteOptions))
        {
            items.Add(new ContextMenuItem
            {
                Text = deleteOptions.Text ?? Texts.Remove,
                Icon = deleteOptions.Icon,
                Group = deleteOptions.Group,
                Shortcut = deleteOptions.Shortcut,
                Description = deleteOptions.Description,
                IsDanger = true,
                Disabled = !CanDelete(item),
                OnClick = () => DispatchMenuActionAsync(() => RequestDeleteAsync(item))
            });
        }

        if (ShouldRenderReorderInMenu(item, -1))
        {
            items.Add(new ContextMenuItem
            {
                Text = Texts.MoveUp,
                Icon = "arrow-up",
                Disabled = !CanMove(item, -1),
                OnClick = () => DispatchMenuActionAsync(() => MoveAsync(item, -1))
            });
        }
        if (ShouldRenderReorderInMenu(item, 1))
        {
            items.Add(new ContextMenuItem
            {
                Text = Texts.MoveDown,
                Icon = "arrow-down",
                Disabled = !CanMove(item, 1),
                OnClick = () => DispatchMenuActionAsync(() => MoveAsync(item, 1))
            });
        }

        if (items.Count != 0)
            ContextMenu.OpenAnchored(args, items, _actionsMenuOwner);
        return Task.CompletedTask;
    }

    private Task DispatchMenuActionAsync(Func<Task> action)
    {
        if (Volatile.Read(ref _disposeState) != 0) return Task.CompletedTask;
        return InvokeAsync(async () =>
        {
            if (Volatile.Read(ref _disposeState) != 0) return;
            await action();
            if (Volatile.Read(ref _disposeState) == 0) StateHasChanged();
        });
    }

    private async Task ExecuteActionAsync(TItem item, DataGridFormAction<TItem, TKey> action)
    {
        TKey key = ResolveKey(item);
        if (!TryBeginRowOperation(key, DataGridFormOperation.Custom, action.Id, out CancellationTokenSource operation))
            return;
        bool completed = false;
        _operationFailure = null;
        try
        {
            DataGridFormActionContext<TItem, TKey> context = new(item, key, RefreshCoreAsync);
            await action.Execute(context, operation.Token);
            completed = true;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                DataGridFormOperation.Custom,
                item,
                key,
                DataGridFormMutationResult<TItem>.Failure(exception.Message, exception),
                action.Id);
        }
        finally
        {
            EndRowOperation(key, operation);
        }
        if (completed)
            await NotifyCompletedAsync(DataGridFormOperation.Custom, item, key, action.Id);
    }

    private async Task SelectionChangedAsync(HashSet<TItem> selection)
    {
        if (selection.Count > MaximumSelectedItems)
        {
            TItem[] overflow = selection.Skip(MaximumSelectedItems).ToArray();
            foreach (TItem item in overflow) selection.Remove(item);
        }
        UpdateSelectionSnapshot(selection, force: true);
        if (SelectedItemsChanged.HasDelegate)
            await SelectedItemsChanged.InvokeAsync(selection);
    }

    private bool IsBulkActionVisible(DataGridFormBulkAction<TItem, TKey> action)
        => IsAuthorizationVisible(action.AuthorizationPolicy, action.UnauthorizedBehavior)
           && (action.VisibleWhen?.Invoke(_selectionSnapshot) ?? true);

    private bool CanExecuteBulkAction(DataGridFormBulkAction<TItem, TKey> action)
        => IsBulkActionVisible(action)
           && IsAuthorized(action.AuthorizationPolicy)
           && !Disabled
           && !ReadOnly
           && _selectionSnapshot.Count != 0
           && !_createBusy
           && !_saving
           && _rowOperations.Count == 0
           && _bulkOperation is null
           && !(action.DisabledWhen?.Invoke(_selectionSnapshot) ?? false);

    private bool ShouldRenderBulkActionInline(DataGridFormBulkAction<TItem, TKey> action)
        => IsBulkActionVisible(action) && ShouldPlaceBulkActionInline(action);

    private bool HasVisibleBulkMenuActions
    {
        get
        {
            foreach (DataGridFormBulkAction<TItem, TKey> action in Schema.BulkActions)
            {
                if (IsBulkActionVisible(action) && !ShouldPlaceBulkActionInline(action)) return true;
            }
            return false;
        }
    }

    private bool ShouldPlaceBulkActionInline(DataGridFormBulkAction<TItem, TKey> action)
    {
        if (action.Placement == DataGridFormActionPlacement.Inline) return true;
        if (action.Placement == DataGridFormActionPlacement.Menu) return false;
        DataGridFormBulkActionsOptions options = Schema.BulkActionsBar;
        if (options.Overflow == DataGridFormActionOverflow.Manual) return true;

        int available = options.MaximumInlineActions - CountPinnedInlineBulkActions();
        if (available <= 0) return false;
        int order = GetBulkActionOrderIndex(action);
        int higherRanked = 0;
        for (int index = 0; index < Schema.BulkActions.Count; index++)
        {
            DataGridFormBulkAction<TItem, TKey> candidate = Schema.BulkActions[index];
            if (candidate.Placement == DataGridFormActionPlacement.Auto
                && IsBulkActionVisible(candidate)
                && Outranks(candidate.Priority, index, action.Priority, order))
                higherRanked++;
        }
        return higherRanked < available;
    }

    private int CountPinnedInlineBulkActions()
    {
        int count = 0;
        foreach (DataGridFormBulkAction<TItem, TKey> action in Schema.BulkActions)
        {
            if (action.Placement == DataGridFormActionPlacement.Inline && IsBulkActionVisible(action)) count++;
        }
        return count;
    }

    private int GetBulkActionOrderIndex(DataGridFormBulkAction<TItem, TKey> target)
    {
        for (int index = 0; index < Schema.BulkActions.Count; index++)
        {
            if (ReferenceEquals(Schema.BulkActions[index], target)) return index;
        }
        return int.MaxValue;
    }

    private bool IsBulkActionsMenuDisabled
        => Disabled
           || ReadOnly
           || _selectionSnapshot.Count == 0
           || _createBusy
           || _saving
           || _rowOperations.Count != 0
           || _bulkOperation is not null;

    private Task OpenBulkActionsMenuAsync(MouseEventArgs args)
    {
        if (IsBulkActionsMenuDisabled) return Task.CompletedTask;
        List<ContextMenuItem> items = new(Schema.BulkActions.Count);
        foreach (DataGridFormBulkAction<TItem, TKey> action in Schema.BulkActions)
        {
            if (!IsBulkActionVisible(action) || ShouldPlaceBulkActionInline(action)) continue;
            items.Add(new ContextMenuItem
            {
                Text = action.Text,
                Icon = action.Icon,
                Group = action.Group,
                Shortcut = action.Shortcut,
                Description = action.Description,
                IsDanger = action.Variant == ButtonVariant.Danger,
                Disabled = !CanExecuteBulkAction(action),
                OnClick = () => DispatchMenuActionAsync(() => RequestBulkActionAsync(action))
            });
        }
        if (items.Count != 0)
            ContextMenu.OpenAnchored(args, items, _bulkActionsMenuOwner);
        return Task.CompletedTask;
    }

    private bool IsBulkActionRunning(DataGridFormBulkAction<TItem, TKey> action)
        => _bulkOperation is not null
           && (_bulkConfirmationAction is null
               || string.Equals(_bulkConfirmationAction.Id, action.Id, StringComparison.Ordinal));

    private Task RequestBulkActionAsync(DataGridFormBulkAction<TItem, TKey> action)
    {
        if (!CanExecuteBulkAction(action)) return Task.CompletedTask;
        _operationFailure = null;
        IReadOnlyList<TItem> snapshot = _selectionSnapshot;
        if (action.Confirmation is not null)
        {
            _bulkConfirmationAction = action;
            _bulkConfirmationItems = snapshot;
            return Task.CompletedTask;
        }
        return ExecuteBulkActionAsync(action, snapshot);
    }

    private async Task ConfirmBulkActionAsync()
    {
        if (_bulkConfirmationBusy
            || _bulkConfirmationAction is not { } action
            || _bulkConfirmationItems is not { Count: > 0 } items)
            return;
        _bulkConfirmationBusy = true;
        try
        {
            if (await ExecuteBulkActionAsync(action, items)) ClearBulkConfirmation();
        }
        finally
        {
            _bulkConfirmationBusy = false;
        }
    }

    private async Task<bool> ExecuteBulkActionAsync(
        DataGridFormBulkAction<TItem, TKey> action,
        IReadOnlyList<TItem> items)
    {
        if (_bulkOperation is not null || items.Count == 0) return false;
        CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _bulkOperation = operation;
        bool completed = false;
        try
        {
            TKey[] keys = new TKey[items.Count];
            var uniqueKeys = new HashSet<TKey>();
            for (int index = 0; index < items.Count; index++)
            {
                TKey key = ResolveKey(items[index]);
                if (!uniqueKeys.Add(key))
                    throw new InvalidOperationException($"DataGridForm bulk selection contains duplicate key '{key}'.");
                keys[index] = key;
            }
            DataGridFormBulkActionContext<TItem, TKey> context = new(
                items,
                keys,
                RefreshCoreAsync,
                ClearSelectionCoreAsync);
            await action.Execute(context, operation.Token);
            completed = true;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                DataGridFormOperation.Bulk,
                null,
                default,
                DataGridFormMutationResult<TItem>.Failure(exception.Message, exception),
                action.Id,
                items.Count);
        }
        finally
        {
            if (ReferenceEquals(_bulkOperation, operation)) _bulkOperation = null;
            operation.Dispose();
        }
        if (!completed) return false;
        if (IsLocalMode) await NotifyItemsChangedAsync();
        await NotifyCompletedAsync(
            DataGridFormOperation.Bulk,
            null,
            default,
            action.Id,
            items.Count);
        return true;
    }

    private Task CancelBulkConfirmationAsync()
    {
        if (!_bulkConfirmationBusy) ClearBulkConfirmation();
        return Task.CompletedTask;
    }

    private void ClearBulkConfirmation()
    {
        _bulkConfirmationAction = null;
        _bulkConfirmationItems = null;
    }

    private async ValueTask ClearSelectionCoreAsync()
    {
        Selection.Clear();
        UpdateSelectionSnapshot(Selection, force: true);
        if (SelectedItemsChanged.HasDelegate)
            await SelectedItemsChanged.InvokeAsync(Selection);
        await InvokeAsync(StateHasChanged);
    }

    private async Task ConfirmPendingAsync()
    {
        if (_confirmationBusy || _confirmationItem is null) return;
        TItem item = _confirmationItem;
        if (_confirmationAction is { } action)
        {
            _confirmationBusy = true;
            try
            {
                await ExecuteActionAsync(item, action);
                if (_operationFailure is null) ClearConfirmation();
            }
            finally
            {
                _confirmationBusy = false;
            }
        }
        else
        {
            await DeleteConfirmedAsync(item);
        }
    }

    private Task CancelEditorAsync()
    {
        if (_saving) return Task.CompletedTask;
        if (ConfirmDiscardChanges && (_editor?.IsModified ?? _editorDirty))
        {
            _discardConfirmationOpen = true;
            return Task.CompletedTask;
        }
        CloseEditor();
        return Task.CompletedTask;
    }

    private Task EditorDirtyChangedAsync(bool dirty)
    {
        _editorDirty = dirty;
        return Task.CompletedTask;
    }

    private Task CancelDiscardAsync()
    {
        _discardConfirmationOpen = false;
        return Task.CompletedTask;
    }

    private Task DiscardEditorAsync()
    {
        _discardConfirmationOpen = false;
        CloseEditor();
        return Task.CompletedTask;
    }

    private Task CancelConfirmationAsync()
    {
        if (!_confirmationBusy) ClearConfirmation();
        return Task.CompletedTask;
    }

    private void CloseEditor()
    {
        _draft = null;
        _editingSource = null;
        _editorOperation = null;
        _editor = null;
        _editorDirty = false;
        _discardConfirmationOpen = false;
    }

    private void ClearConfirmation()
    {
        _confirmationItem = null;
        _confirmationAction = null;
    }

    private DataGridFormPresentation EditorPresentation
        => _editorOperation switch
        {
            DataGridFormOperation.Create => Schema.CreateOptions?.Presentation ?? DataGridFormPresentation.Dialog,
            DataGridFormOperation.Edit => Schema.EditOptions?.Presentation ?? DataGridFormPresentation.Dialog,
            _ => DataGridFormPresentation.Dialog
        };

    private string? EditorTitle
        => _editorOperation switch
        {
            DataGridFormOperation.Create => Schema.CreateOptions?.Title ?? Schema.CreateOptions?.Text ?? Texts.Add,
            DataGridFormOperation.Edit when _editingSource is not null
                => Schema.EditOptions?.Title?.Invoke(_editingSource) ?? Schema.EditOptions?.Text ?? Texts.Edit,
            _ => null
        };

    private string EditorCss => EditorPresentation == DataGridFormPresentation.Drawer
        ? "omni-data-grid-form-editor omni-data-grid-form-editor-drawer"
        : "omni-data-grid-form-editor omni-data-grid-form-editor-dialog";

    private string? EditorStyle
    {
        get
        {
            string? width = _editorOperation switch
            {
                DataGridFormOperation.Create => Schema.CreateOptions?.Width,
                DataGridFormOperation.Edit => Schema.EditOptions?.Width,
                _ => null
            };
            return string.IsNullOrWhiteSpace(width) ? null : $"width:{width}";
        }
    }

    private string ConfirmationTitleId => $"{Id}-confirmation-title";
    private string ConfirmationMessageId => $"{Id}-confirmation-message";
    private string ConfirmationTitle
        => _confirmationAction is not null
            ? Texts.Confirm
            : Schema.DeleteOptions?.Title?.Invoke(_confirmationItem!) ?? Texts.Confirm;
    private string ConfirmationMessage
        => _confirmationAction?.Confirmation?.Invoke(_confirmationItem!)
           ?? Schema.DeleteOptions?.Confirmation?.Invoke(_confirmationItem!)
           ?? Texts.DataGridFormDeleteConfirmation;
    private string ConfirmationConfirmText
        => _confirmationAction is not null
            ? Texts.Confirm
            : Schema.DeleteOptions?.ConfirmText ?? Texts.Confirm;
    private string ConfirmationCancelText => Schema.DeleteOptions?.CancelText ?? Texts.Cancel;
    private string? ConfirmationIcon => _confirmationAction?.Icon ?? Schema.DeleteOptions?.Icon;
    private ButtonVariant ConfirmationVariant => _confirmationAction?.Variant ?? ButtonVariant.Danger;
    private string SelectedItemsText
        => string.Format(CultureInfo.CurrentCulture, Texts.DataGridFormSelectedCount, _selectionSnapshot.Count);
    private string BulkConfirmationMessage
        => _bulkConfirmationAction?.Confirmation?.Invoke(_bulkConfirmationItems ?? _selectionSnapshot)
           ?? Texts.DataGridFormBulkConfirmation;

    private void UpdateSelectionSnapshot(HashSet<TItem> selection, bool force = false)
    {
        int fingerprint = selection.Count;
        unchecked
        {
            foreach (TItem item in selection)
                fingerprint = (fingerprint * 397) ^ EqualityComparer<TKey>.Default.GetHashCode(ResolveKey(item));
        }
        if (!force
            && ReferenceEquals(selection, _selectionSource)
            && fingerprint == _selectionFingerprint)
            return;
        _selectionSource = selection;
        _selectionFingerprint = fingerprint;
        _selectionSnapshot = selection.Count == 0 ? Array.Empty<TItem>() : selection.ToArray();
    }

    private bool IsRowBusy(TItem item) => _rowOperations.ContainsKey(ResolveKey(item));

    private bool IsRowOperation(TItem item, DataGridFormOperation operation)
        => _rowOperations.TryGetValue(ResolveKey(item), out RowOperation? current)
           && current.Operation == operation;

    private bool IsRowAction(TItem item, string actionId)
        => _rowOperations.TryGetValue(ResolveKey(item), out RowOperation? current)
           && current.Operation == DataGridFormOperation.Custom
           && StringComparer.Ordinal.Equals(current.ActionId, actionId);

    private bool CanMove(TItem item, int offset)
    {
        if (Disabled || ReadOnly || _bulkOperation is not null || !AllowReorder || Items is not { IsReadOnly: false } items)
            return false;
        int index = FindUniqueIndex(items, ResolveKey(item));
        int target = index + offset;
        return index >= 0 && (uint)target < (uint)items.Count && !IsRowBusy(item);
    }

    private async Task MoveAsync(TItem item, int offset)
    {
        if (!CanMove(item, offset) || Items is null) return;
        TKey key = ResolveKey(item);
        int index = FindUniqueIndex(Items, key);
        int target = index + offset;
        if ((uint)index >= (uint)Items.Count || (uint)target >= (uint)Items.Count) return;
        TItem value = Items[index];
        Items.RemoveAt(index);
        Items.Insert(target, value);
        await NotifyItemsChangedAsync();
        await RefreshCoreAsync(_lifetime.Token);
    }

    private TKey ResolveKey(TItem item)
    {
        TKey key = Schema.KeySelector(item);
        return key is null
            ? throw new InvalidOperationException("DataGridForm key selector returned null.")
            : key;
    }

    private int FindUniqueIndex(IList<TItem> items, TKey key)
    {
        int found = -1;
        for (int index = 0; index < items.Count; index++)
        {
            if (!EqualityComparer<TKey>.Default.Equals(ResolveKey(items[index]), key)) continue;
            if (found >= 0) throw new InvalidOperationException($"DataGridForm found duplicate row key '{key}'.");
            found = index;
        }
        return found;
    }

    private IList<TItem> GetMutableItems()
        => Items is { IsReadOnly: false } items
            ? items
            : throw new InvalidOperationException("DataGridForm local Items must be mutable for CRUD operations.");

    private async Task NotifyItemsChangedAsync()
    {
        if (Items is not null && ItemsChanged.HasDelegate)
            await ItemsChanged.InvokeAsync(Items);
    }

    private ValueTask<GridLoadResult<TItem>> LoadProviderAsync(
        GridState<TItem> state,
        CancellationToken cancellationToken)
        => Provider?.LoadAsync(state, cancellationToken)
           ?? ValueTask.FromException<GridLoadResult<TItem>>(
               new InvalidOperationException("DataGridForm provider is no longer available."));

    private async ValueTask RefreshCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_grid is not null) await _grid.RefreshAsync();
    }

    private CancellationTokenSource BeginCreateOperation()
    {
        if (_createOperation is not null)
            throw new InvalidOperationException("A DataGridForm create operation is already running.");
        CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _createOperation = operation;
        _createBusy = true;
        return operation;
    }

    private void EndCreateOperation(CancellationTokenSource operation)
    {
        if (ReferenceEquals(_createOperation, operation)) _createOperation = null;
        _createBusy = false;
        operation.Dispose();
    }

    private bool TryBeginRowOperation(
        TKey key,
        DataGridFormOperation operation,
        string? actionId,
        out CancellationTokenSource cancellation)
    {
        if (_rowOperations.ContainsKey(key) || Volatile.Read(ref _disposeState) != 0)
        {
            cancellation = null!;
            return false;
        }
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _rowOperations.Add(key, new RowOperation(cancellation, operation, actionId));
        return true;
    }

    private void EndRowOperation(TKey key, CancellationTokenSource operation)
    {
        if (_rowOperations.TryGetValue(key, out RowOperation? current)
            && ReferenceEquals(current.Cancellation, operation))
            _rowOperations.Remove(key);
        operation.Dispose();
    }

    private async Task NotifyCompletedAsync(
        DataGridFormOperation operation,
        TItem? item,
        TKey? key,
        string? actionId = null,
        int affectedCount = 1)
    {
        if (OperationCompleted.HasDelegate)
            await OperationCompleted.InvokeAsync(
                new DataGridFormOperationEventArgs<TItem, TKey>(operation, item, key, actionId, affectedCount));
    }

    private async Task ReportFailureAsync(
        DataGridFormOperation operation,
        TItem? item,
        TKey? key,
        EntityMutationResult<TItem> result,
        string? actionId = null,
        int affectedCount = 1)
    {
        DataGridFormMutationResult<TItem> legacyResult = result.Status switch
        {
            EntityMutationStatus.ValidationFailed => DataGridFormMutationResult<TItem>.ValidationFailed(
                result.Errors.Count == 0 ? [result.Message ?? Texts.DataGridFormValidationFailed] : result.Errors,
                result.Message),
            EntityMutationStatus.Conflict => DataGridFormMutationResult<TItem>.Conflict(result.CurrentItem, result.Message),
            EntityMutationStatus.NotFound => DataGridFormMutationResult<TItem>.NotFound(result.Message),
            EntityMutationStatus.Forbidden => DataGridFormMutationResult<TItem>.Forbidden(result.Message),
            _ => DataGridFormMutationResult<TItem>.Failure(result.Message, result.Exception)
        };
        await ReportFailureAsync(operation, item, key, legacyResult, actionId, affectedCount);
    }

    private async Task ReportFailureAsync(
        DataGridFormOperation operation,
        TItem? item,
        TKey? key,
        DataGridFormMutationResult<TItem> result,
        string? actionId = null,
        int affectedCount = 1)
    {
        string message = result.Message ?? (result.Status switch
        {
            DataGridFormMutationStatus.ValidationFailed => Texts.DataGridFormValidationFailed,
            DataGridFormMutationStatus.Conflict => Texts.DataGridFormConflict,
            DataGridFormMutationStatus.NotFound => Texts.DataGridFormNotFound,
            DataGridFormMutationStatus.Forbidden => Texts.DataGridFormForbidden,
            DataGridFormMutationStatus.RefreshFailed => Texts.DataGridFormRefreshFailed,
            _ => result.Exception?.Message ?? Texts.DataGridFormOperationFailed
        });
        DataGridFormOperationFailedEventArgs<TItem, TKey> failure = new(
            operation,
            item,
            key,
            result.Status,
            message,
            result.Errors,
            result.Exception,
            result.CurrentItem,
            actionId,
            affectedCount);
        _operationFailure = failure;
        if (OperationFailed.HasDelegate)
            await OperationFailed.InvokeAsync(failure);
    }

    /// <summary>Cancels active work and releases every owned token source.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        _lifetime.Cancel();
        _entityEditor.Dispose();
        _createOperation?.Cancel();
        _createOperation?.Dispose();
        _createOperation = null;
        _bulkOperation?.Cancel();
        _bulkOperation?.Dispose();
        _bulkOperation = null;
        Interlocked.Exchange(ref _authorizationOperation, null)?.Cancel();
        foreach (RowOperation operation in _rowOperations.Values)
        {
            operation.Cancellation.Cancel();
            operation.Cancellation.Dispose();
        }
        _rowOperations.Clear();
        ContextMenu.Close(_actionsMenuOwner);
        ContextMenu.Close(_bulkActionsMenuOwner);
        _lifetime.Dispose();
        CloseEditor();
        ClearConfirmation();
        ClearBulkConfirmation();
        GC.SuppressFinalize(this);
    }

    private sealed record RowOperation(
        CancellationTokenSource Cancellation,
        DataGridFormOperation Operation,
        string? ActionId);

    private sealed class DataGridFormMutationProviderAdapter(OmniDataGridForm<TItem, TKey> owner)
        : IOmniEntityMutationProvider<TItem, TKey>
    {
        public async ValueTask<EntityMutationResult<TItem>> CreateAsync(
            TItem item,
            CancellationToken cancellationToken)
            => Convert(await RequireProvider().CreateAsync(item, cancellationToken));

        public async ValueTask<EntityMutationResult<TItem>> UpdateAsync(
            TKey key,
            TItem item,
            CancellationToken cancellationToken)
            => Convert(await RequireProvider().UpdateAsync(key, item, cancellationToken));

        public async ValueTask<EntityMutationResult<TItem>> DeleteAsync(
            TKey key,
            CancellationToken cancellationToken)
            => Convert(await RequireProvider().DeleteAsync(key, cancellationToken));

        private IDataGridFormProvider<TItem, TKey> RequireProvider()
            => owner.Provider
               ?? throw new InvalidOperationException("The DataGridForm persistence provider is no longer available.");

        private static EntityMutationResult<TItem> Convert(DataGridFormMutationResult<TItem> result)
            => result.Status switch
            {
                DataGridFormMutationStatus.Success when result.Item is not null => EntityMutationResult<TItem>.Success(result.Item),
                DataGridFormMutationStatus.Success => EntityMutationResult<TItem>.Deleted(),
                DataGridFormMutationStatus.ValidationFailed => EntityMutationResult<TItem>.ValidationFailed(
                    result.Errors.Count == 0 ? [result.Message ?? "Validation failed."] : result.Errors,
                    result.Message),
                DataGridFormMutationStatus.Conflict => EntityMutationResult<TItem>.Conflict(result.CurrentItem, result.Message),
                DataGridFormMutationStatus.NotFound => EntityMutationResult<TItem>.NotFound(result.Message),
                DataGridFormMutationStatus.Forbidden => EntityMutationResult<TItem>.Forbidden(result.Message),
                _ => EntityMutationResult<TItem>.Failure(result.Message, result.Exception)
            };
    }
}
