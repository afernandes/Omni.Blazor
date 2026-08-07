using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

public partial class OmniGanttForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem,
    TKey>
    where TItem : class
    where TKey : notnull
{
    private OmniEntityEditorHost<TItem, TKey>? _editor;
    private OmniGantt<TItem>? _gantt;

    /// <summary>Mutable task snapshot rendered by the Gantt.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = default!;

    /// <summary>Raised after a successful local mutation.</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Strongly typed task projection and Gantt defaults.</summary>
    [Parameter, EditorRequired]
    public GanttSchema<TItem> GanttSchema { get; set; } = default!;

    /// <summary>Reusable DataForm and CRUD definition.</summary>
    [Parameter, EditorRequired]
    public EntityEditorSchema<TItem, TKey> EditorSchema { get; set; } = default!;

    /// <summary>Optional persistence provider.</summary>
    [Parameter]
    public IOmniEntityMutationProvider<TItem, TKey>? Provider { get; set; }

    /// <summary>Declarative left-pane Gantt columns.</summary>
    [Parameter]
    public RenderFragment? Columns { get; set; }

    /// <summary>Typed task-bar content.</summary>
    [Parameter]
    public RenderFragment<TItem>? TaskTemplate { get; set; }

    /// <summary>Additional generated toolbar content.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Additional CSS class applied directly to the Gantt.</summary>
    [Parameter]
    public string? GanttClass { get; set; }

    /// <summary>Additional inline styles applied directly to the Gantt.</summary>
    [Parameter]
    public string? GanttStyle { get; set; }

    /// <summary>Disables generated entity operations.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes generated entity operations read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Minimum local task count.</summary>
    [Parameter]
    public int MinimumItems { get; set; }

    /// <summary>Maximum local task count.</summary>
    [Parameter]
    public int MaximumItems { get; set; } = int.MaxValue;

    /// <summary>Raised after a task bar is selected.</summary>
    [Parameter]
    public EventCallback<TItem> TaskClick { get; set; }

    /// <summary>Raised after a left-pane row is selected.</summary>
    [Parameter]
    public EventCallback<TItem> RowClick { get; set; }

    /// <summary>Raised after a task move gesture.</summary>
    [Parameter]
    public EventCallback<GanttTaskMovedEventArgs<TItem>> TaskMove { get; set; }

    /// <summary>Raised after a task resize gesture.</summary>
    [Parameter]
    public EventCallback<GanttTaskMovedEventArgs<TItem>> TaskResize { get; set; }

    /// <summary>Raised after a Gantt column resize.</summary>
    [Parameter]
    public EventCallback<GanttColumnResizedEventArgs> ColumnResized { get; set; }

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
        ArgumentNullException.ThrowIfNull(GanttSchema);
        ArgumentNullException.ThrowIfNull(EditorSchema);
    }

    private async Task HandleTaskClickAsync(TItem item)
    {
        if (TaskClick.HasDelegate) await TaskClick.InvokeAsync(item);
        if (_editor is not null) await _editor.BeginEditAsync(item);
    }

    private async Task HandleRowClickAsync(TItem item)
    {
        if (RowClick.HasDelegate) await RowClick.InvokeAsync(item);
    }

    private async Task HandleOperationCompletedAsync(EntityEditorOperationEventArgs<TItem, TKey> args)
    {
        if (_gantt is not null) await _gantt.Reload();
        if (OperationCompleted.HasDelegate) await OperationCompleted.InvokeAsync(args);
    }
}
