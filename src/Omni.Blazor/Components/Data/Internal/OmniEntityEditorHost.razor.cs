using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;
using Omni.Blazor.Services;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

public partial class OmniEntityEditorHost<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem,
    TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly Func<bool> _unsavedChangesPredicate;
    private OmniEntityEditor<TItem, TKey>? _controller;
    private EntityEditorSchema<TItem, TKey>? _controllerSchema;
    private TItem? _draft;
    private TItem? _sourceItem;
    private EntityEditorOperation? _operation;
    private EntityMutationResult<TItem>? _failure;
    private bool _saving;
    private bool _dirty;
    private bool _deleteConfirmationOpen;
    private bool _discardConfirmationOpen;
    private int _disposeState;

    public OmniEntityEditorHost()
        => _unsavedChangesPredicate = () => _dirty;

    /// <summary>Immutable entity editor schema.</summary>
    [Parameter, EditorRequired]
    public EntityEditorSchema<TItem, TKey> Schema { get; set; } = default!;

    /// <summary>Current local or provider-backed snapshot shown by the owning data surface.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = default!;

    /// <summary>Optional persistence provider. When absent, mutations update Items directly.</summary>
    [Parameter]
    public IOmniEntityMutationProvider<TItem, TKey>? Provider { get; set; }

    /// <summary>Disables every generated mutation.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the surface read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Maximum local item count. Default unlimited.</summary>
    [Parameter]
    public int MaximumItems { get; set; } = int.MaxValue;

    /// <summary>Minimum local item count. Default zero.</summary>
    [Parameter]
    public int MinimumItems { get; set; }

    /// <summary>Asks before discarding a modified draft.</summary>
    [Parameter]
    public bool ConfirmDiscardChanges { get; set; } = true;

    /// <summary>Protects navigation while a draft has unsaved changes.</summary>
    [Parameter]
    public bool GuardNavigationWithUnsavedChanges { get; set; } = true;

    /// <summary>Additional toolbar content.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Wrapped Scheduler, Kanban or Gantt content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised after a local collection mutation.</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Requests a provider-backed data refresh after successful persistence.</summary>
    [Parameter]
    public EventCallback RefreshRequested { get; set; }

    /// <summary>Raised after a successful mutation.</summary>
    [Parameter]
    public EventCallback<EntityEditorOperationEventArgs<TItem, TKey>> OperationCompleted { get; set; }

    /// <summary>Raised after a handled mutation failure.</summary>
    [Parameter]
    public EventCallback<EntityEditorOperationFailedEventArgs<TItem, TKey>> OperationFailed { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Schema);
        ArgumentNullException.ThrowIfNull(Items);
        if (MaximumItems < 1) throw new ArgumentOutOfRangeException(nameof(MaximumItems));
        if (MinimumItems < 0 || MinimumItems > MaximumItems)
            throw new ArgumentOutOfRangeException(nameof(MinimumItems));
        if (!ReferenceEquals(_controllerSchema, Schema))
        {
            _controller?.Dispose();
            _controller = new OmniEntityEditor<TItem, TKey>(Schema.KeySelector);
            _controllerSchema = Schema;
            CloseEditor();
        }
    }

    /// <summary>Opens a detached create draft.</summary>
    public Task BeginCreateAsync()
        => InvokeAsync(() =>
        {
            if (!CanCreate || Schema.CreateOptions is not { } options) return;
            _failure = null;
            _sourceItem = null;
            _draft = options.Factory()
                ?? throw new InvalidOperationException("Entity editor create factory returned null.");
            _operation = EntityEditorOperation.Create;
            StateHasChanged();
        });

    /// <summary>Opens a caller-created detached draft, for example from a selected Scheduler slot.</summary>
    public Task BeginCreateAsync(TItem draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return InvokeAsync(() =>
        {
            if (!CanCreate) return;
            _failure = null;
            _sourceItem = null;
            _draft = draft;
            _operation = EntityEditorOperation.Create;
            StateHasChanged();
        });
    }

    /// <summary>Opens a detached edit draft for one entity.</summary>
    public Task BeginEditAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return InvokeAsync(() =>
        {
            if (!CanEdit || Schema.EditOptions is not { } options) return;
            TItem draft = options.CloneItem(item)
                ?? throw new InvalidOperationException("Entity editor clone returned null.");
            if (ReferenceEquals(item, draft))
                throw new InvalidOperationException("Entity editor Clone must return a detached instance.");
            if (!EqualityComparer<TKey>.Default.Equals(Schema.KeySelector(item), Schema.KeySelector(draft)))
                throw new InvalidOperationException("Entity editor Clone must preserve the stable key.");
            _failure = null;
            _sourceItem = item;
            _draft = draft;
            _operation = EntityEditorOperation.Edit;
            StateHasChanged();
        });
    }

    private bool CanCreate => !Disabled && !ReadOnly && !_saving && Schema.CreateOptions is not null
        && (Provider is not null || Items.Count < MaximumItems);

    private bool CanEdit => !Disabled && !ReadOnly && !_saving && Schema.EditOptions is not null;

    private bool CanDeleteDraft => _operation == EntityEditorOperation.Edit
        && _sourceItem is not null
        && Schema.DeleteOptions is not null
        && !Disabled
        && !ReadOnly
        && (Provider is not null || Items.Count > MinimumItems);

    private EntityEditorPresentation EditorPresentation => _operation switch
    {
        EntityEditorOperation.Create => Schema.CreateOptions?.Presentation ?? EntityEditorPresentation.Drawer,
        EntityEditorOperation.Edit => Schema.EditOptions?.Presentation ?? EntityEditorPresentation.Drawer,
        _ => EntityEditorPresentation.Drawer
    };

    private DataGridFormPresentation LegacyPresentation => EditorPresentation switch
    {
        EntityEditorPresentation.Inline => DataGridFormPresentation.Inline,
        EntityEditorPresentation.Dialog => DataGridFormPresentation.Dialog,
        _ => DataGridFormPresentation.Drawer
    };

    private string? EditorTitle => _operation switch
    {
        EntityEditorOperation.Create => Schema.CreateOptions?.Title ?? Texts.Add,
        EntityEditorOperation.Edit when _sourceItem is not null => Schema.EditOptions?.Title?.Invoke(_sourceItem) ?? Texts.Edit,
        _ => null
    };

    private string EditorCss => CssBuilder.Default("omni-data-grid-form-editor")
        .AddClass("omni-data-grid-form-editor-dialog", EditorPresentation == EntityEditorPresentation.Dialog)
        .AddClass("omni-data-grid-form-editor-drawer", EditorPresentation == EntityEditorPresentation.Drawer)
        .Build();

    private string? EditorStyle
    {
        get
        {
            string? width = _operation switch
            {
                EntityEditorOperation.Create => Schema.CreateOptions?.Width,
                EntityEditorOperation.Edit => Schema.EditOptions?.Width,
                _ => null
            };
            return string.IsNullOrWhiteSpace(width) ? null : $"--omni-data-grid-form-editor-width:{width}";
        }
    }

    private async Task SaveAsync(EditContext context)
    {
        if (_saving || _draft is null || _operation is null || _controller is null) return;
        _saving = true;
        _failure = null;
        EntityEditorOperation operation = _operation.Value;
        TItem submitted = _draft;
        TKey? key = _sourceItem is null ? default : Schema.KeySelector(_sourceItem);
        try
        {
            EntityMutationResult<TItem> result = operation switch
            {
                EntityEditorOperation.Create => await _controller.CreateAsync(
                    submitted, Items, Provider, MaximumItems),
                EntityEditorOperation.Edit when _sourceItem is not null => await _controller.UpdateAsync(
                    _sourceItem, submitted, Items, Provider),
                _ => EntityMutationResult<TItem>.Failure("The entity editor no longer has an active draft.")
            };
            if (!result.Succeeded)
            {
                await ReportFailureAsync(operation, submitted, key, result);
                return;
            }
            TItem? authoritative = result.Item;
            TKey? authoritativeKey = authoritative is null ? key : Schema.KeySelector(authoritative);
            CloseEditor();
            await NotifySourceChangedAsync(result.LocalCollectionChanged);
            if (OperationCompleted.HasDelegate)
                await OperationCompleted.InvokeAsync(
                    new EntityEditorOperationEventArgs<TItem, TKey>(operation, authoritative, authoritativeKey));
        }
        catch (OperationCanceledException) when (Volatile.Read(ref _disposeState) != 0)
        {
        }
        finally
        {
            _saving = false;
        }
    }

    private Task RequestDeleteAsync()
    {
        if (!CanDeleteDraft) return Task.CompletedTask;
        _deleteConfirmationOpen = true;
        return Task.CompletedTask;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_saving || _sourceItem is null || _controller is null) return;
        TItem item = _sourceItem;
        TKey key = Schema.KeySelector(item);
        _saving = true;
        _failure = null;
        try
        {
            EntityMutationResult<TItem> result = await _controller.DeleteAsync(
                item, Items, Provider, MinimumItems);
            if (!result.Succeeded)
            {
                await ReportFailureAsync(EntityEditorOperation.Delete, item, key, result);
                return;
            }
            _deleteConfirmationOpen = false;
            CloseEditor();
            await NotifySourceChangedAsync(result.LocalCollectionChanged);
            if (OperationCompleted.HasDelegate)
                await OperationCompleted.InvokeAsync(
                    new EntityEditorOperationEventArgs<TItem, TKey>(EntityEditorOperation.Delete, item, key));
        }
        catch (OperationCanceledException) when (Volatile.Read(ref _disposeState) != 0)
        {
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task NotifySourceChangedAsync(bool localCollectionChanged)
    {
        if (localCollectionChanged && ItemsChanged.HasDelegate)
            await ItemsChanged.InvokeAsync(Items);
        if (!localCollectionChanged && Provider is not null && RefreshRequested.HasDelegate)
            await RefreshRequested.InvokeAsync();
    }

    private async Task ReportFailureAsync(
        EntityEditorOperation operation,
        TItem? item,
        TKey? key,
        EntityMutationResult<TItem> result)
    {
        _failure = result;
        if (OperationFailed.HasDelegate)
            await OperationFailed.InvokeAsync(
                new EntityEditorOperationFailedEventArgs<TItem, TKey>(operation, item, key, result));
    }

    private Task DirtyChangedAsync(bool dirty)
    {
        _dirty = dirty;
        return Task.CompletedTask;
    }

    private Task CancelEditorAsync()
    {
        if (_saving) return Task.CompletedTask;
        if (ConfirmDiscardChanges && _dirty)
            _discardConfirmationOpen = true;
        else
            CloseEditor();
        return Task.CompletedTask;
    }

    private Task CancelDeleteAsync()
    {
        if (!_saving) _deleteConfirmationOpen = false;
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

    private void CloseEditor()
    {
        _draft = null;
        _sourceItem = null;
        _operation = null;
        _dirty = false;
        _discardConfirmationOpen = false;
        _deleteConfirmationOpen = false;
    }

    private string FailureMessage => _failure?.Message ?? _failure?.Status switch
    {
        EntityMutationStatus.ValidationFailed => Texts.DataGridFormValidationFailed,
        EntityMutationStatus.Conflict => Texts.DataGridFormConflict,
        EntityMutationStatus.NotFound => Texts.DataGridFormNotFound,
        EntityMutationStatus.Forbidden => Texts.DataGridFormForbidden,
        _ => Texts.DataGridFormOperationFailed
    };

    private string DeleteTitle => _sourceItem is null
        ? Texts.Confirm
        : Schema.DeleteOptions?.Title?.Invoke(_sourceItem) ?? Texts.Confirm;

    private string DeleteMessage => _sourceItem is null
        ? Texts.DataGridFormDeleteConfirmation
        : Schema.DeleteOptions?.Confirmation?.Invoke(_sourceItem) ?? Texts.DataGridFormDeleteConfirmation;

    private string DeleteTitleId => $"{Id}-delete-title";
    private string DeleteMessageId => $"{Id}-delete-message";
    private string DiscardTitleId => $"{Id}-discard-title";
    private string DiscardMessageId => $"{Id}-discard-message";

    private string RootCss => CssBuilder.Default("omni-entity-editor")
        .AddClass("omni-entity-editor-disabled", Disabled)
        .AddClass(Class)
        .Build();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        _controller?.Dispose();
        _controller = null;
        CloseEditor();
        GC.SuppressFinalize(this);
    }
}
