using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Coordinates explicit DataForm schemas as validated steps over one shared
/// <see cref="EditContext"/> and cancellable navigation state.
/// </summary>
public partial class OmniDataFormWizard<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>
    where TModel : class
{
    private const int MaximumCustomErrorsPerStep = 100;
    private readonly object _operationSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<string, string[]> _stepErrors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _validatedSteps = new(StringComparer.Ordinal);
    private IReadOnlyList<DataFormWizardStep<TModel>> _visibleSteps = Array.Empty<DataFormWizardStep<TModel>>();
    private CancellationTokenSource? _operation;
    private OmniDataForm<TModel>? _form;
    private EditContext? _ownedEditContext;
    private EditContext? _currentEditContext;
    private EditContext? _subscribedEditContext;
    private ValidationMessageStore? _wizardMessages;
    private TModel? _currentModel;
    private DataFormWizardSchema<TModel>? _lastSchema;
    private bool _busy;
    private int _furthestReached;
    private int _wizardDisposeState;

    /// <summary>Immutable ordered wizard schema.</summary>
    [Parameter, EditorRequired]
    public DataFormWizardSchema<TModel> Schema { get; set; } = default!;

    /// <summary>Model used to create an owned EditContext. Supply Model or EditContext, never both.</summary>
    [Parameter]
    public TModel? Model { get; set; }

    /// <summary>Existing EditContext shared with the wizard. Supply EditContext or Model, never both.</summary>
    [Parameter]
    public EditContext? EditContext { get; set; }

    /// <summary>Zero-based visible step index.</summary>
    [Parameter]
    public int ActiveStepIndex { get; set; }

    /// <summary>Two-way binding callback for ActiveStepIndex.</summary>
    [Parameter]
    public EventCallback<int> ActiveStepIndexChanged { get; set; }

    /// <summary>Raised after a successful step transition.</summary>
    [Parameter]
    public EventCallback<DataFormWizardStepChangedEventArgs<TModel>> StepChanged { get; set; }

    /// <summary>Raised after final synchronous, field and step validation succeeds.</summary>
    [Parameter]
    public EventCallback<EditContext> OnCompleted { get; set; }

    /// <summary>Custom typed current-step heading.</summary>
    [Parameter]
    public RenderFragment<DataFormWizardTemplateContext<TModel>>? HeaderTemplate { get; set; }

    /// <summary>Custom typed action area replacing Back, Next and Complete.</summary>
    [Parameter]
    public RenderFragment<DataFormWizardTemplateContext<TModel>>? ActionsTemplate { get; set; }

    /// <summary>Disables generated fields and navigation actions.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Prevents jumping ahead of the furthest validated step. Default true.</summary>
    [Parameter]
    public bool Linear { get; set; } = true;

    /// <summary>Allows clicking accessible step navigation buttons. Default true.</summary>
    [Parameter]
    public bool AllowStepNavigation { get; set; } = true;

    /// <summary>Whether the active DataForm emits a semantic form element. Default true.</summary>
    [Parameter]
    public bool RenderFormElement { get; set; } = true;

    /// <summary>Shows validation messages above the current step. Default true.</summary>
    [Parameter]
    public bool ShowValidationSummary { get; set; } = true;

    /// <summary>Accessible label for the step navigation.</summary>
    [Parameter]
    public string? NavigationLabel { get; set; }

    /// <summary>Overrides the Back action text.</summary>
    [Parameter]
    public string? BackText { get; set; }

    /// <summary>Overrides the Next action text.</summary>
    [Parameter]
    public string? NextText { get; set; }

    /// <summary>Overrides the Complete action text.</summary>
    [Parameter]
    public string? CompleteText { get; set; }

    /// <summary>Shared active EditContext.</summary>
    public EditContext? CurrentEditContext => _currentEditContext;

    /// <summary>Current visible step metadata.</summary>
    public DataFormWizardStep<TModel>? CurrentStep
        => (uint)ActiveStepIndex < (uint)_visibleSteps.Count ? _visibleSteps[ActiveStepIndex] : null;

    /// <summary>Moves to the next visible step after validating the current step.</summary>
    public Task NextAsync() => NavigateToAsync(ActiveStepIndex + 1);

    /// <summary>Moves to the previous visible step without discarding EditContext state.</summary>
    public Task BackAsync() => NavigateToAsync(ActiveStepIndex - 1);

    /// <summary>Validates the final workflow and raises OnCompleted.</summary>
    public Task CompleteAsync() => RunCompleteAsync();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Schema);
        if ((Model is null) == (EditContext is null))
            throw new InvalidOperationException("OmniDataFormWizard requires either Model or EditContext, but not both.");

        TModel model;
        EditContext context;
        if (EditContext is not null)
        {
            model = EditContext.Model as TModel
                ?? throw new InvalidOperationException(
                    $"OmniDataFormWizard EditContext model must be assignable to {typeof(TModel).Name}.");
            context = EditContext;
        }
        else
        {
            model = Model!;
            if (!ReferenceEquals(_currentModel, model) || _ownedEditContext is null)
                _ownedEditContext = new EditContext(model);
            context = _ownedEditContext;
        }

        bool contextChanged = !ReferenceEquals(_currentEditContext, context);
        bool schemaChanged = !ReferenceEquals(_lastSchema, Schema);
        _currentModel = model;
        _currentEditContext = context;
        _lastSchema = Schema;
        if (contextChanged)
        {
            if (_subscribedEditContext is not null)
                _subscribedEditContext.OnFieldChanged -= OnEditContextFieldChanged;
            _subscribedEditContext = context;
            _subscribedEditContext.OnFieldChanged += OnEditContextFieldChanged;
            _wizardMessages = new ValidationMessageStore(context);
            _stepErrors.Clear();
            _validatedSteps.Clear();
            _furthestReached = 0;
        }
        if (schemaChanged)
        {
            _stepErrors.Clear();
            _validatedSteps.Clear();
            _furthestReached = 0;
        }

        RebuildVisibleSteps();
        if (_visibleSteps.Count == 0)
            throw new InvalidOperationException("OmniDataFormWizard has no visible steps for the current model.");
        ActiveStepIndex = Math.Clamp(ActiveStepIndex, 0, _visibleSteps.Count - 1);
        _furthestReached = Math.Max(_furthestReached, ActiveStepIndex);
    }

    private void OnEditContextFieldChanged(object? sender, FieldChangedEventArgs args)
        => ObserveTask(InvokeAsync(() =>
        {
            string? currentStepId = CurrentStep?.Id;
            RebuildVisibleSteps();
            if (_visibleSteps.Count == 0)
                throw new InvalidOperationException("OmniDataFormWizard has no visible steps for the current model.");
            int preservedIndex = FindVisibleStepIndex(currentStepId);
            ActiveStepIndex = preservedIndex >= 0
                ? preservedIndex
                : Math.Clamp(ActiveStepIndex, 0, _visibleSteps.Count - 1);
            StateHasChanged();
        }), "OmniDataFormWizard.FieldChanged");

    private void RebuildVisibleSteps()
        => _visibleSteps = Schema.Steps
            .Where(step => step.VisibleWhen?.Invoke(_currentModel!) ?? true)
            .ToArray();

    private int FindVisibleStepIndex(string? id)
    {
        if (id is null) return -1;
        for (int index = 0; index < _visibleSteps.Count; index++)
            if (StringComparer.Ordinal.Equals(_visibleSteps[index].Id, id)) return index;
        return -1;
    }

    private async Task NavigateToAsync(int targetIndex)
    {
        if (Disabled || (uint)targetIndex >= (uint)_visibleSteps.Count || targetIndex == ActiveStepIndex)
            return;
        if (targetIndex > ActiveStepIndex && Linear && targetIndex > _furthestReached + 1)
            return;
        DataFormWizardStep<TModel> target = _visibleSteps[targetIndex];
        if (target.CanEnter is not null && !target.CanEnter(_currentModel!)) return;

        CancellationTokenSource operation = BeginOperation();
        int sourceIndex = ActiveStepIndex;
        try
        {
            bool movingForward = targetIndex > sourceIndex;
            if (movingForward && !await ValidateCurrentStepAsync(operation.Token)) return;
            operation.Token.ThrowIfCancellationRequested();
            await SetActiveStepAsync(targetIndex, movingForward);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        finally
        {
            EndOperation(operation);
        }
    }

    private async Task RunCompleteAsync()
    {
        if (Disabled || _busy || CurrentEditContext is null || CurrentStep is null) return;
        CancellationTokenSource operation = BeginOperation();
        try
        {
            if (!await ValidateCurrentStepAsync(operation.Token)) return;
            bool annotationsValid = CurrentEditContext.Validate();
            bool rulesValid = await ValidateAllStepRulesAsync(operation.Token);
            if (!annotationsValid || !rulesValid)
            {
                int invalidStep = FindFirstInvalidStep();
                if (invalidStep >= 0 && invalidStep != ActiveStepIndex)
                    await SetActiveStepAsync(invalidStep, movingForward: false);
                return;
            }
            operation.Token.ThrowIfCancellationRequested();
            if (OnCompleted.HasDelegate) await OnCompleted.InvokeAsync(CurrentEditContext);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        finally
        {
            EndOperation(operation);
        }
    }

    private async Task<bool> ValidateCurrentStepAsync(CancellationToken cancellationToken)
    {
        if (_form is null || CurrentStep is not { } step) return true;
        string? firstInvalid = null;
        foreach (DataFormField<TModel> field in step.Schema.Fields)
        {
            DataFormValidationResult result = await _form.ValidateFieldAsync(field.Property, cancellationToken);
            if (result.Status == DataFormValidationStatus.Invalid && firstInvalid is null)
                firstInvalid = field.Property;
        }
        bool ruleValid = await ValidateStepRuleAsync(step, cancellationToken);
        if (firstInvalid is not null)
        {
            await _form.FocusFieldAsync(firstInvalid);
            return false;
        }
        if (!ruleValid) return false;
        _validatedSteps.Add(step.Id);
        return true;
    }

    private async Task<bool> ValidateAllStepRulesAsync(CancellationToken cancellationToken)
    {
        bool valid = true;
        foreach (DataFormWizardStep<TModel> step in _visibleSteps)
            valid &= await ValidateStepRuleAsync(step, cancellationToken);
        return valid;
    }

    private async Task<bool> ValidateStepRuleAsync(
        DataFormWizardStep<TModel> step,
        CancellationToken cancellationToken)
    {
        string[] errors = step.ValidateAsync is null
            ? []
            : (await step.ValidateAsync(_currentModel!, cancellationToken))
                .Where(static error => !string.IsNullOrWhiteSpace(error))
                .Take(MaximumCustomErrorsPerStep)
                .ToArray();
        _stepErrors[step.Id] = errors;
        PublishStepErrors();
        return errors.Length == 0;
    }

    private void PublishStepErrors()
    {
        if (_wizardMessages is null || _currentEditContext is null) return;
        _wizardMessages.Clear();
        FieldIdentifier modelField = new(_currentEditContext.Model, string.Empty);
        foreach (string error in _stepErrors.Values.SelectMany(static errors => errors))
            _wizardMessages.Add(modelField, error);
        _currentEditContext.NotifyValidationStateChanged();
    }

    private int FindFirstInvalidStep()
    {
        if (_currentEditContext is null || _currentModel is null) return -1;
        for (int index = 0; index < _visibleSteps.Count; index++)
        {
            DataFormWizardStep<TModel> step = _visibleSteps[index];
            if (_stepErrors.TryGetValue(step.Id, out string[]? errors) && errors.Length != 0)
                return index;
            foreach (DataFormField<TModel> field in step.Schema.Fields)
            {
                if (_currentEditContext.GetValidationMessages(
                        field.PropertyPath.GetFieldIdentifier(_currentModel)).Any())
                    return index;
            }
        }
        return -1;
    }

    private async Task SetActiveStepAsync(int index, bool movingForward)
    {
        ActiveStepIndex = index;
        _furthestReached = Math.Max(_furthestReached, index);
        if (ActiveStepIndexChanged.HasDelegate) await ActiveStepIndexChanged.InvokeAsync(index);
        if (StepChanged.HasDelegate)
            await StepChanged.InvokeAsync(new DataFormWizardStepChangedEventArgs<TModel>(
                _visibleSteps[index], index, movingForward));
    }

    private bool CanNavigateTo(int index)
    {
        if (!AllowStepNavigation || Disabled || _busy || index == ActiveStepIndex) return false;
        if (Linear && index > _furthestReached) return false;
        return _visibleSteps[index].CanEnter?.Invoke(_currentModel!) ?? true;
    }

    private DataFormWizardTemplateContext<TModel> CreateTemplateContext(
        TModel model,
        EditContext context,
        DataFormWizardStep<TModel> step)
        => new(
            model,
            context,
            step,
            ActiveStepIndex,
            _visibleSteps.Count,
            ActiveStepIndex == 0,
            ActiveStepIndex == _visibleSteps.Count - 1,
            _busy);

    private CancellationTokenSource BeginOperation()
    {
        CancellationTokenSource current = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationTokenSource? previous;
        lock (_operationSync)
        {
            previous = _operation;
            _operation = current;
            _busy = true;
        }
        try { previous?.Cancel(); }
        catch (ObjectDisposedException) { }
        return current;
    }

    private void EndOperation(CancellationTokenSource operation)
    {
        lock (_operationSync)
        {
            if (ReferenceEquals(_operation, operation))
            {
                _operation = null;
                _busy = false;
            }
        }
        operation.Dispose();
    }

    private TModel? CurrentModel => _currentModel;
    private string CurrentTitleId => $"{Id}-step-{CurrentStep?.Id}-title";
    private string RootCss => CssBuilder.Default("omni-data-form-wizard")
        .AddClass("omni-data-form-wizard-busy", _busy)
        .AddClass(Class)
        .Build();
    private string StepCss(int index) => CssBuilder.Default("omni-data-form-wizard-step")
        .AddClass("omni-active", index == ActiveStepIndex)
        .AddClass("omni-complete", index < ActiveStepIndex || _validatedSteps.Contains(_visibleSteps[index].Id))
        .Build();

    /// <summary>Cancels active navigation and releases owned wizard resources.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _wizardDisposeState, 1) != 0) return;
        _lifetime.Cancel();
        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnFieldChanged -= OnEditContextFieldChanged;
            _subscribedEditContext = null;
        }
        CancellationTokenSource? operation;
        lock (_operationSync)
        {
            operation = _operation;
            _operation = null;
            _busy = false;
        }
        try { operation?.Cancel(); }
        catch (ObjectDisposedException) { }
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
