using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

public partial class OmniKanbanForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem,
    TKey>
    where TItem : class
    where TKey : notnull
{
    private OmniEntityEditorHost<TItem, TKey>? _editor;

    /// <summary>Mutable card snapshot rendered by the Kanban.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = default!;

    /// <summary>Raised after CRUD or board movement changes the local source.</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Strongly typed board and card schema.</summary>
    [Parameter, EditorRequired]
    public KanbanSchema<TItem> KanbanSchema { get; set; } = default!;

    /// <summary>Reusable DataForm and CRUD definition.</summary>
    [Parameter, EditorRequired]
    public EntityEditorSchema<TItem, TKey> EditorSchema { get; set; } = default!;

    /// <summary>Optional persistence provider.</summary>
    [Parameter]
    public IOmniEntityMutationProvider<TItem, TKey>? Provider { get; set; }

    /// <summary>Creates a prefilled card for the selected Kanban column.</summary>
    [Parameter]
    public Func<KanbanColumn, TItem>? ColumnFactory { get; set; }

    /// <summary>Custom card content.</summary>
    [Parameter]
    public RenderFragment<TItem>? CardTemplate { get; set; }

    /// <summary>Additional generated toolbar content.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Additional CSS class applied directly to the board.</summary>
    [Parameter]
    public string? BoardClass { get; set; }

    /// <summary>Additional inline styles applied directly to the board.</summary>
    [Parameter]
    public string? BoardStyle { get; set; }

    /// <summary>Disables generated entity operations.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes generated entity operations read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Minimum local card count.</summary>
    [Parameter]
    public int MinimumItems { get; set; }

    /// <summary>Maximum local card count.</summary>
    [Parameter]
    public int MaximumItems { get; set; } = int.MaxValue;

    /// <summary>Raised after a card is selected for editing.</summary>
    [Parameter]
    public EventCallback<TItem> CardClick { get; set; }

    /// <summary>Raised after a card is moved between board positions.</summary>
    [Parameter]
    public EventCallback<KanbanCardMovedEventArgs<TItem>> CardMoved { get; set; }

    /// <summary>Raised after a column add action is requested.</summary>
    [Parameter]
    public EventCallback<KanbanColumn> AddCard { get; set; }

    /// <summary>Requests that the owner reload provider-backed Items.</summary>
    [Parameter]
    public EventCallback RefreshRequested { get; set; }

    /// <summary>Raised after a successful generated CRUD operation.</summary>
    [Parameter]
    public EventCallback<EntityEditorOperationEventArgs<TItem, TKey>> OperationCompleted { get; set; }

    /// <summary>Raised after a handled generated CRUD failure.</summary>
    [Parameter]
    public EventCallback<EntityEditorOperationFailedEventArgs<TItem, TKey>> OperationFailed { get; set; }

    /// <summary>Opens the generated create editor.</summary>
    public Task BeginCreateAsync() => _editor?.BeginCreateAsync() ?? Task.CompletedTask;

    /// <summary>Opens the generated edit editor.</summary>
    public Task BeginEditAsync(TItem item) => _editor?.BeginEditAsync(item) ?? Task.CompletedTask;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(KanbanSchema);
        ArgumentNullException.ThrowIfNull(EditorSchema);
    }

    private async Task HandleCardClickAsync(TItem item)
    {
        if (CardClick.HasDelegate) await CardClick.InvokeAsync(item);
        if (_editor is not null) await _editor.BeginEditAsync(item);
    }

    private async Task HandleAddCardAsync(KanbanColumn column)
    {
        if (AddCard.HasDelegate) await AddCard.InvokeAsync(column);
        if (ColumnFactory is not null && _editor is not null)
            await _editor.BeginCreateAsync(ColumnFactory(column));
    }

    private Task HandleBoardItemsChangedAsync(IEnumerable<TItem> items)
        => ItemsChanged.HasDelegate ? ItemsChanged.InvokeAsync(Items) : Task.CompletedTask;
}
