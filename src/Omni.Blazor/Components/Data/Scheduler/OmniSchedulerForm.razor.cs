using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

public partial class OmniSchedulerForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem,
    TKey>
    where TItem : class
    where TKey : notnull
{
    private OmniEntityEditorHost<TItem, TKey>? _editor;
    private OmniScheduler<TItem>? _scheduler;

    /// <summary>Mutable appointment snapshot rendered by the Scheduler.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = default!;

    /// <summary>Raised after a successful local mutation.</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Strongly typed appointment projection and Scheduler defaults.</summary>
    [Parameter, EditorRequired]
    public SchedulerSchema<TItem> SchedulerSchema { get; set; } = default!;

    /// <summary>Reusable DataForm and CRUD definition.</summary>
    [Parameter, EditorRequired]
    public EntityEditorSchema<TItem, TKey> EditorSchema { get; set; } = default!;

    /// <summary>Optional persistence provider. The owner refreshes Items when requested.</summary>
    [Parameter]
    public IOmniEntityMutationProvider<TItem, TKey>? Provider { get; set; }

    /// <summary>Creates a prefilled draft from an empty Scheduler slot.</summary>
    [Parameter]
    public Func<SchedulerSlotSelectEventArgs, TItem>? SlotFactory { get; set; }

    /// <summary>Current Scheduler date.</summary>
    [Parameter]
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>Raised when Scheduler navigation changes the date.</summary>
    [Parameter]
    public EventCallback<DateTime> DateChanged { get; set; }

    /// <summary>Initially selected Scheduler view index.</summary>
    [Parameter]
    public int SelectedIndex { get; set; }

    /// <summary>Scheduler view declarations. Day, Week and Month are generated when omitted.</summary>
    [Parameter]
    public RenderFragment? Views { get; set; }

    /// <summary>Typed appointment content.</summary>
    [Parameter]
    public RenderFragment<TItem>? AppointmentTemplate { get; set; }

    /// <summary>Custom Scheduler navigation content.</summary>
    [Parameter]
    public RenderFragment? NavigationTemplate { get; set; }

    /// <summary>Additional editor toolbar content.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Additional CSS class applied directly to the Scheduler.</summary>
    [Parameter]
    public string? SchedulerClass { get; set; }

    /// <summary>Additional inline styles applied directly to the Scheduler.</summary>
    [Parameter]
    public string? SchedulerStyle { get; set; }

    /// <summary>Disables generated entity operations.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes generated entity operations read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Minimum local appointment count.</summary>
    [Parameter]
    public int MinimumItems { get; set; }

    /// <summary>Maximum local appointment count.</summary>
    [Parameter]
    public int MaximumItems { get; set; } = int.MaxValue;

    /// <summary>Raised after an empty slot is selected.</summary>
    [Parameter]
    public EventCallback<SchedulerSlotSelectEventArgs> SlotSelect { get; set; }

    /// <summary>Raised after an appointment is selected for editing.</summary>
    [Parameter]
    public EventCallback<SchedulerAppointmentSelectEventArgs<TItem>> AppointmentSelect { get; set; }

    /// <summary>Raised when an appointment is moved.</summary>
    [Parameter]
    public EventCallback<SchedulerAppointmentMoveEventArgs> AppointmentMove { get; set; }

    /// <summary>Raised when the pointer enters an appointment.</summary>
    [Parameter]
    public EventCallback<SchedulerAppointmentMouseEventArgs<TItem>> AppointmentMouseEnter { get; set; }

    /// <summary>Raised when the pointer leaves an appointment.</summary>
    [Parameter]
    public EventCallback<SchedulerAppointmentMouseEventArgs<TItem>> AppointmentMouseLeave { get; set; }

    /// <summary>Per-appointment render hook.</summary>
    [Parameter]
    public Action<SchedulerAppointmentRenderEventArgs<TItem>>? AppointmentRender { get; set; }

    /// <summary>Per-slot render hook.</summary>
    [Parameter]
    public Action<SchedulerSlotRenderEventArgs>? SlotRender { get; set; }

    /// <summary>Raised when the visible Scheduler range changes.</summary>
    [Parameter]
    public EventCallback<SchedulerLoadDataEventArgs> LoadData { get; set; }

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
        ArgumentNullException.ThrowIfNull(SchedulerSchema);
        ArgumentNullException.ThrowIfNull(EditorSchema);
    }

    private async Task HandleSlotSelectAsync(SchedulerSlotSelectEventArgs args)
    {
        if (SlotSelect.HasDelegate) await SlotSelect.InvokeAsync(args);
        if (!args.IsDefaultPrevented && SlotFactory is not null && _editor is not null)
            await _editor.BeginCreateAsync(SlotFactory(args));
    }

    private async Task HandleAppointmentSelectAsync(SchedulerAppointmentSelectEventArgs<TItem> args)
    {
        if (AppointmentSelect.HasDelegate) await AppointmentSelect.InvokeAsync(args);
        if (args.Data is not null && _editor is not null)
            await _editor.BeginEditAsync(args.Data);
    }

    private async Task HandleOperationCompletedAsync(EntityEditorOperationEventArgs<TItem, TKey> args)
    {
        if (_scheduler is not null) await _scheduler.ReprojectAsync();
        if (OperationCompleted.HasDelegate) await OperationCompleted.InvokeAsync(args);
    }
}
