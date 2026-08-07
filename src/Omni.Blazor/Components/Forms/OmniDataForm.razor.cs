using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;
using Omni.Blazor.Services;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Metadata-driven form that composes a reusable strongly typed field schema,
/// generated Omni inputs, Blazor's <see cref="EditContext"/> and data annotation validators.
/// </summary>
public partial class OmniDataForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>
    where TModel : class
{
    private static readonly PropertyInfo[] ModelProperties = typeof(TModel)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public);
    private static readonly HashSet<string> IdentifierPropertyNames = ModelProperties
        .Where(IsIdentifierProperty)
        .Select(static property => property.Name)
        .ToHashSet(StringComparer.Ordinal);

    private readonly ParameterState<DefinitionState> _definitionState;
    private IReadOnlyList<DataFormResolvedField<TModel>> _resolvedFields = [];
    private IReadOnlyList<DataFormResolvedField<TModel>> _allResolvedFields = [];
    private IReadOnlyList<DataFormResolvedGroup<TModel>> _resolvedGroups = [];
    private IReadOnlyList<DataFormDiagnostic> _diagnostics = [];
    private OmniForm<TModel>? _form;
    private TModel? _currentModel;
    private readonly HashSet<string> _touchedFields = new(StringComparer.Ordinal);
    private readonly object _validationSync = new();
    private readonly Dictionary<string, FieldValidationOperation> _fieldValidationOperations = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingFieldStateNotifications = new(StringComparer.Ordinal);
    private ValidationMessageStore? _fieldValidationStore;
    private EditContext? _fieldValidationContext;
    private EditContext? _subscribedEditContext;
    private int _disposeState;
    private int _dependencyUpdateDepth;
    private bool _stateNotificationPending;
    private bool _publishAllFieldStates;
    private bool _hasSchemaRequiredValidation;
    private bool _hasSchemaAsyncValidation;

    [Inject] private DataFormEditorRegistry EditorRegistry { get; set; } = default!;
    [Inject] private ScrollManager ScrollManager { get; set; } = default!;
    [Inject] private FocusManager FocusManager { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        EditContext? context = CurrentEditContext;
        if (ReferenceEquals(context, _subscribedEditContext)) return;
        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnFieldChanged -= OnEditContextFieldChanged;
            _subscribedEditContext.OnValidationStateChanged -= OnEditContextValidationStateChanged;
        }
        _subscribedEditContext = context;
        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnFieldChanged += OnEditContextFieldChanged;
            _subscribedEditContext.OnValidationStateChanged += OnEditContextValidationStateChanged;
        }
    }

    /// <summary>Creates an empty metadata-driven form.</summary>
    public OmniDataForm()
    {
        _definitionState = RegisterParameter<DefinitionState>(nameof(Schema))
            .WithParameter(() => new DefinitionState(
                Model,
                EditContext,
                Schema,
                AddDataAnnotationsValidator,
                Disabled,
                ReadOnly,
                FieldChanged))
            .WithComparer(DefinitionStateComparer.Instance)
            .WithChangeHandler(RebuildFields)
            .Attach();
    }

    /// <summary>
    /// Model instance edited by the generated fields. Supply either Model or
    /// <see cref="EditContext"/>, never both.
    /// </summary>
    [Parameter]
    public TModel? Model { get; set; }

    /// <summary>
    /// Existing Blazor edit context. Supply either EditContext or
    /// <see cref="Model"/>, never both. Its model must be a
    /// <typeparamref name="TModel"/> instance.
    /// </summary>
    [Parameter]
    public EditContext? EditContext { get; set; }

    /// <summary>
    /// Immutable, reusable field schema. When omitted, every supported model
    /// property is inferred from its type and data annotations.
    /// </summary>
    [Parameter]
    public DataFormSchema<TModel>? Schema { get; set; }

    /// <summary>
    /// Optional root column override. When null, the schema responsive layout is used.
    /// Values are clamped from 1 to 12.
    /// </summary>
    [Parameter]
    public int? Columns { get; set; }

    /// <summary>Disables every generated editor and the default submit button.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes every generated editor read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Shows <see cref="OmniValidationSummary"/> above the fields. Default true.</summary>
    [Parameter]
    public bool ShowValidationSummary { get; set; } = true;

    /// <summary>Heading used by the generated validation summary. Null uses the localized OmniTexts value.</summary>
    [Parameter]
    public string? ValidationSummaryTitle { get; set; }

    /// <summary>Shows the built-in submit button when <see cref="Actions"/> is null. Default true.</summary>
    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    /// <summary>Text displayed by the built-in submit button. Null uses the localized OmniTexts value.</summary>
    [Parameter]
    public string? SubmitText { get; set; }

    /// <summary>Displays the loading state on the built-in submit button.</summary>
    [Parameter]
    public bool Submitting { get; set; }

    /// <summary>Additional content rendered inside the underlying form after generated fields.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Custom action area. When supplied, it replaces the built-in submit button.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Renders the semantic HTML form element. Disable for an embedded
    /// collection item subform to avoid nested forms. Default true.
    /// </summary>
    [Parameter]
    public bool RenderFormElement { get; set; } = true;

    /// <summary>Invoked after a generated editor writes a property value.</summary>
    [Parameter]
    public EventCallback<DataFormFieldChangedEventArgs<TModel>> FieldChanged { get; set; }

    /// <summary>Invoked with an immutable snapshot when one generated field state changes.</summary>
    [Parameter]
    public EventCallback<DataFormFieldStateChangedEventArgs<TModel>> FieldStateChanged { get; set; }

    /// <summary>Invoked with the aggregate validation state after any field or message change.</summary>
    [Parameter]
    public EventCallback<DataFormValidationStateChangedEventArgs<TModel>> ValidationStateChanged { get; set; }

    /// <summary>Invoked after schema diagnostics are rebuilt.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<DataFormDiagnostic>> DiagnosticsChanged { get; set; }

    /// <summary>Shows schema diagnostics in the rendered form. Intended for development. Default false.</summary>
    [Parameter]
    public bool ShowDiagnostics { get; set; }

    /// <summary>Custom renderer for schema diagnostics.</summary>
    [Parameter]
    public RenderFragment<IReadOnlyList<DataFormDiagnostic>>? DiagnosticsTemplate { get; set; }

    /// <summary>Invoked when the underlying OmniForm submission is valid.</summary>
    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }

    /// <summary>Invoked when the underlying OmniForm submission is invalid.</summary>
    [Parameter]
    public EventCallback<EditContext> OnInvalidSubmit { get; set; }

    /// <summary>Auto-attaches a DataAnnotationsValidator. Default true.</summary>
    [Parameter]
    public bool AddDataAnnotationsValidator { get; set; } = true;

    /// <summary>Synchronous cross-field validation delegated to the underlying OmniForm.</summary>
    [Parameter]
    public Action<EditContext, ValidationMessageStore>? Validation { get; set; }

    /// <summary>Asynchronous validation delegated to the underlying OmniForm.</summary>
    [Parameter]
    public Func<EditContext, ValidationMessageStore, Task>? ValidationAsync { get; set; }

    /// <summary>Preferred cancellable asynchronous validation callback.</summary>
    [Parameter]
    public Func<EditContext, ValidationMessageStore, CancellationToken, Task>? ValidationAsyncWithCancellation { get; set; }

    /// <summary>Debounce in milliseconds for validation caused by field changes.</summary>
    [Parameter]
    public int ValidationDelay { get; set; }

    /// <summary>Scrolls to and focuses the first invalid generated field after submit. Default true.</summary>
    [Parameter]
    public bool FocusFirstInvalidOnSubmit { get; set; } = true;

    /// <summary>Whether any generated field has changed since the last touched reset.</summary>
    [Parameter]
    public bool IsTouched { get; set; }

    /// <summary>Callback for two-way binding of <see cref="IsTouched"/>.</summary>
    [Parameter]
    public EventCallback<bool> IsTouchedChanged { get; set; }

    /// <summary>Whether the underlying form currently has no validation messages.</summary>
    public bool IsValid => _form?.IsValid ?? false;

    /// <summary>Whether one or more explicit field validations are running.</summary>
    public bool IsValidating
    {
        get
        {
            lock (_validationSync) return _fieldValidationOperations.Count != 0;
        }
    }

    /// <summary>Current EditContext used by the underlying form.</summary>
    public EditContext? CurrentEditContext => _form?.CurrentEditContext;

    /// <summary>Current validation messages from the underlying EditContext.</summary>
    public IEnumerable<string> Errors => _form?.Errors ?? [];

    /// <summary>Diagnostics produced while resolving the current schema.</summary>
    public IReadOnlyList<DataFormDiagnostic> Diagnostics => _diagnostics;

    /// <summary>Runs synchronous EditContext validation.</summary>
    public bool Validate() => _form?.Validate() ?? true;

    /// <summary>Runs synchronous and asynchronous validation.</summary>
    public Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        => _form?.ValidateAsync(cancellationToken) ?? Task.FromResult(true);

    /// <summary>Submits the form programmatically and awaits all validation.</summary>
    public Task SubmitAsync(CancellationToken cancellationToken = default)
        => _form?.SubmitAsync(cancellationToken) ?? Task.CompletedTask;

    /// <summary>Captures a restorable model snapshot through the underlying OmniForm.</summary>
    public void Snapshot() => _form?.Snapshot();

    /// <summary>Restores the last snapshot and clears touched and validation state.</summary>
    public async Task ResetAsync()
    {
        if (_form is not null) await _form.ResetAsync();
        _touchedFields.Clear();
        ClearFieldValidation();
    }

    /// <summary>Clears validation messages without mutating model values.</summary>
    public void ResetValidation()
    {
        _form?.ResetValidation();
        ClearFieldValidation();
    }

    /// <summary>Resets the touched state.</summary>
    public async Task ResetTouchedAsync()
    {
        if (_form is not null) await _form.ResetTouchedAsync();
        _touchedFields.Clear();
    }

    /// <summary>Returns the current EditContext state for a schema property path.</summary>
    public DataFormFieldState? GetFieldState(string propertyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyPath);
        if (_currentModel is null || CurrentEditContext is not { } context) return null;
        DataFormResolvedField<TModel>? field = _allResolvedFields.FirstOrDefault(
            field => StringComparer.Ordinal.Equals(field.Metadata.Property, propertyPath));
        if (field is null) return null;
        FieldIdentifier identifier = field.GetFieldIdentifier(_currentModel);
        string[] errors = context.GetValidationMessages(identifier).ToArray();
        return new DataFormFieldState(
            propertyPath,
            _touchedFields.Contains(propertyPath),
            context.IsModified(identifier),
            IsFieldValidating(propertyPath),
            errors.Length == 0,
            errors);
    }

    /// <summary>Returns field state using a rename-safe strongly typed selector.</summary>
    public DataFormFieldState? GetFieldState<TValue>(Expression<Func<TModel, TValue>> property)
        => GetFieldState(GetPropertyPath(property));

    /// <summary>
    /// Runs EditContext, DataAnnotations, required and schema validators for one
    /// field with latest-wins cancellation.
    /// </summary>
    public async Task<DataFormValidationResult> ValidateFieldAsync(
        string propertyPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyPath);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        if (_currentModel is null || CurrentEditContext is not { } context)
            return new(propertyPath, DataFormValidationStatus.Valid, []);
        DataFormResolvedField<TModel>? field = _allResolvedFields.FirstOrDefault(
            item => StringComparer.Ordinal.Equals(item.Metadata.Property, propertyPath));
        if (field is null)
            throw new ArgumentException($"DataForm field '{propertyPath}' was not found.", nameof(propertyPath));

        FieldValidationOperation operation = new(cancellationToken);
        FieldValidationOperation? previous;
        lock (_validationSync)
        {
            _fieldValidationOperations.Remove(propertyPath, out previous);
            previous?.MarkCancellationReason(FieldValidationCancellationReason.Superseded);
            _fieldValidationOperations[propertyPath] = operation;
        }
        ReportCancellationException(previous?.Cancel(FieldValidationCancellationReason.Superseded));
        QueueStateNotification(propertyPath);

        try
        {
            TModel model = _currentModel;
            FieldIdentifier identifier = field.GetFieldIdentifier(model);
            await InvokeAsync(() =>
            {
                EnsureFieldValidationStore(context);
                _fieldValidationStore!.Clear(identifier);

                // DataAnnotationsValidator and any application-defined EditContext
                // field validators run synchronously from this notification.
                context.NotifyFieldChanged(identifier);
            });

            List<string> schemaErrors = [];
            object? value = field.GetValue(model);
            if (field.Metadata.EnforceRequired
                && field.Metadata.IsRequired(model)
                && field.IsVisible(model)
                && !HasRequiredValue(value))
            {
                schemaErrors.Add(field.Metadata.RequiredError ?? Texts.Required);
            }
            AddCollectionCountErrors(field, value, schemaErrors);

            foreach (IDataFormFieldValidator<TModel> validator in field.Metadata.Validators)
            {
                operation.Token.ThrowIfCancellationRequested();
                string? error = await validator.ValidateAsync(model, value, operation.Token);
                if (!string.IsNullOrWhiteSpace(error)) schemaErrors.Add(error);
            }

            operation.Token.ThrowIfCancellationRequested();

            IReadOnlyList<string> errors = [];
            await InvokeAsync(() =>
            {
                lock (_validationSync)
                {
                    if (!_fieldValidationOperations.TryGetValue(propertyPath, out FieldValidationOperation? current)
                        || !ReferenceEquals(current, operation))
                        return;
                }

                EnsureFieldValidationStore(context);
                _fieldValidationStore!.Clear(identifier);
                foreach (string error in schemaErrors) _fieldValidationStore.Add(identifier, error);
                context.NotifyValidationStateChanged();
                errors = context.GetValidationMessages(identifier)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                StateHasChanged();
            });
            operation.Token.ThrowIfCancellationRequested();

            lock (_validationSync)
            {
                if (!_fieldValidationOperations.TryGetValue(propertyPath, out FieldValidationOperation? current)
                    || !ReferenceEquals(current, operation))
                {
                    return new DataFormValidationResult(
                        propertyPath,
                        operation.CancellationReason == FieldValidationCancellationReason.Superseded
                            ? DataFormValidationStatus.Superseded
                            : DataFormValidationStatus.Canceled,
                        []);
                }
                _fieldValidationOperations.Remove(propertyPath);
            }

            return new DataFormValidationResult(
                propertyPath,
                errors.Count == 0 ? DataFormValidationStatus.Valid : DataFormValidationStatus.Invalid,
                errors);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            return new DataFormValidationResult(
                propertyPath,
                operation.CancellationReason == FieldValidationCancellationReason.Superseded
                    ? DataFormValidationStatus.Superseded
                    : DataFormValidationStatus.Canceled,
                []);
        }
        finally
        {
            lock (_validationSync)
            {
                if (_fieldValidationOperations.TryGetValue(propertyPath, out FieldValidationOperation? current)
                    && ReferenceEquals(current, operation))
                    _fieldValidationOperations.Remove(propertyPath);
            }
            operation.Dispose();
            QueueStateNotification(propertyPath);
        }
    }

    /// <summary>Validates one field using a rename-safe strongly typed selector.</summary>
    public Task<DataFormValidationResult> ValidateFieldAsync<TValue>(
        Expression<Func<TModel, TValue>> property,
        CancellationToken cancellationToken = default)
        => ValidateFieldAsync(GetPropertyPath(property), cancellationToken);

    /// <summary>Focuses a generated field by its full schema property path.</summary>
    public async ValueTask<bool> FocusFieldAsync(string propertyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyPath);
        DataFormResolvedField<TModel>? field = _allResolvedFields.FirstOrDefault(
            field => StringComparer.Ordinal.Equals(field.Metadata.Property, propertyPath));
        if (field is null || _currentModel is null || !field.IsVisible(_currentModel)) return false;
        string selector = $"#{field.EditorId}";
        await ScrollManager.ScrollIntoViewAsync(selector, ScrollBehavior.Smooth, ScrollBlock.Center);
        await FocusManager.FocusAsync(field.EditorId);
        return true;
    }

    /// <summary>Focuses one field using a rename-safe strongly typed selector.</summary>
    public ValueTask<bool> FocusFieldAsync<TValue>(Expression<Func<TModel, TValue>> property)
        => FocusFieldAsync(GetPropertyPath(property));

    /// <summary>Focuses the first visible generated field with validation messages.</summary>
    public async ValueTask<bool> FocusFirstInvalidAsync()
    {
        if (_currentModel is null || CurrentEditContext is not { } context) return false;
        foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
        {
            if (!field.IsVisible(_currentModel)) continue;
            if (!context.GetValidationMessages(field.GetFieldIdentifier(_currentModel)).Any()) continue;
            return await FocusFieldAsync(field.Metadata.Property);
        }
        return false;
    }

    /// <summary>Cancels outstanding field validation and releases owned resources.</summary>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return ValueTask.CompletedTask;
        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnFieldChanged -= OnEditContextFieldChanged;
            _subscribedEditContext.OnValidationStateChanged -= OnEditContextValidationStateChanged;
            _subscribedEditContext = null;
        }
        CancelFieldValidations(FieldValidationCancellationReason.Canceled);
        lock (_validationSync)
        {
            _pendingFieldStateNotifications.Clear();
            _publishAllFieldStates = false;
            _stateNotificationPending = false;
        }
        DetachFieldValidationStore();
        return ValueTask.CompletedTask;
    }

    private bool IsFieldValidating(string propertyPath)
    {
        lock (_validationSync) return _fieldValidationOperations.ContainsKey(propertyPath);
    }

    private void OnEditContextFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        string? changedPath = null;
        if (_currentModel is not null)
        {
            foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
            {
                if (!field.GetFieldIdentifier(_currentModel).Equals(args.FieldIdentifier)) continue;
                _touchedFields.Add(field.Metadata.Property);
                changedPath = field.Metadata.Property;
                break;
            }

            if (changedPath is not null && _dependencyUpdateDepth == 0)
                InvalidateDependentLookups(changedPath, _currentModel, args.FieldIdentifier);
        }
        if (changedPath is not null) QueueStateNotification(changedPath);
        ObserveTask(InvokeAsync(StateHasChanged), "OmniDataForm.EditContextFieldChanged");
    }

    private void OnEditContextValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
        => QueueStateNotification(propertyPath: null, publishAllFields: true);

    private void QueueStateNotification(string? propertyPath, bool publishAllFields = false)
    {
        if (Volatile.Read(ref _disposeState) != 0) return;
        bool schedule;
        lock (_validationSync)
        {
            if (propertyPath is not null) _pendingFieldStateNotifications.Add(propertyPath);
            _publishAllFieldStates |= publishAllFields;
            schedule = !_stateNotificationPending;
            _stateNotificationPending = true;
        }
        if (schedule)
            ObserveTask(InvokeAsync(PublishStateNotificationsAsync), "OmniDataForm.StateChanged");
    }

    private async Task PublishStateNotificationsAsync()
    {
        if (Volatile.Read(ref _disposeState) != 0) return;
        string[] paths;
        bool publishAll;
        lock (_validationSync)
        {
            paths = _pendingFieldStateNotifications.ToArray();
            _pendingFieldStateNotifications.Clear();
            publishAll = _publishAllFieldStates;
            _publishAllFieldStates = false;
            _stateNotificationPending = false;
        }

        if (_currentModel is not { } model || CurrentEditContext is null) return;
        if (publishAll) paths = _allResolvedFields.Select(static field => field.Metadata.Property).ToArray();

        if (FieldStateChanged.HasDelegate)
        {
            foreach (string path in paths.Distinct(StringComparer.Ordinal))
            {
                if (Volatile.Read(ref _disposeState) != 0) return;
                DataFormFieldState? state = GetFieldState(path);
                if (state is not null)
                    await FieldStateChanged.InvokeAsync(new DataFormFieldStateChangedEventArgs<TModel>(model, state));
            }
        }

        if (ValidationStateChanged.HasDelegate)
        {
            if (Volatile.Read(ref _disposeState) != 0) return;
            DataFormFieldState[] fields = _allResolvedFields
                .Select(field => GetFieldState(field.Metadata.Property))
                .Where(static state => state is not null)
                .Cast<DataFormFieldState>()
                .ToArray();
            string[] errors = Errors.Distinct(StringComparer.Ordinal).ToArray();
            await ValidationStateChanged.InvokeAsync(new DataFormValidationStateChangedEventArgs<TModel>(
                model,
                IsValidating,
                errors.Length == 0,
                fields,
                errors));
        }

        if (Volatile.Read(ref _disposeState) == 0) StateHasChanged();
    }

    private void InvalidateDependentLookups(
        string changedPath,
        TModel model,
        FieldIdentifier originalIdentifier)
    {
        Queue<string> pending = new();
        HashSet<string> invalidated = new(StringComparer.Ordinal);
        List<(DataFormResolvedField<TModel> Field, object? Value)> resetFields = [];
        pending.Enqueue(changedPath);

        _dependencyUpdateDepth++;
        try
        {
            while (pending.TryDequeue(out string? dependencyPath))
            {
                foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
                {
                    IDataFormLookupDefinition<TModel>? lookup = field.Metadata.Lookup;
                    if (lookup is null || !invalidated.Add(field.Metadata.Property)) continue;
                    if (!lookup.Dependencies.Any(dependency =>
                            StringComparer.Ordinal.Equals(dependency.Path, dependencyPath)))
                    {
                        invalidated.Remove(field.Metadata.Property);
                        continue;
                    }

                    field.LookupVersion++;
                    field.RendererParameters[nameof(OmniDataFormFieldRenderer<TModel, object>.LookupVersion)] =
                        field.LookupVersion;

                    if (!lookup.ClearValueOnDependencyChange) continue;
                    if (field.Metadata.PropertyPath.Leaf.SetMethod is null) continue;
                    object? current = field.GetValue(model);
                    object? cleared = field.Metadata.DefaultValue;
                    if (Equals(current, cleared)) continue;

                    object owner = field.Metadata.PropertyPath.GetOwner(model);
                    field.Metadata.PropertyPath.Leaf.SetValue(owner, cleared);
                    resetFields.Add((field, cleared));
                    pending.Enqueue(field.Metadata.Property);
                }
            }

            EditContext? context = CurrentEditContext;
            if (context is not null)
            {
                foreach ((DataFormResolvedField<TModel> field, _) in resetFields)
                {
                    FieldIdentifier identifier = field.GetFieldIdentifier(model);
                    if (!identifier.Equals(originalIdentifier)) context.NotifyFieldChanged(identifier);
                }
            }
        }
        finally
        {
            _dependencyUpdateDepth--;
        }

        if (resetFields.Count != 0)
        {
            (DataFormResolvedField<TModel> Field, object? Value)[] changes = resetFields.ToArray();
            ObserveTask(
                InvokeAsync(() => PublishDependencyChangesAsync(model, changes)),
                "OmniDataForm.LookupDependencyChanged");
        }
    }

    private async Task PublishDependencyChangesAsync(
        TModel model,
        IReadOnlyList<(DataFormResolvedField<TModel> Field, object? Value)> changes)
    {
        foreach ((DataFormResolvedField<TModel> field, object? value) in changes)
        {
            await HandleFieldChangedAsync(new DataFormFieldChangedEventArgs<TModel>(
                model,
                field.Metadata.Property,
                value));
        }
    }

    private void ClearFieldValidation()
    {
        CancelFieldValidations(FieldValidationCancellationReason.Canceled);
        _fieldValidationStore?.Clear();
        _fieldValidationContext?.NotifyValidationStateChanged();
    }

    private void DetachFieldValidationStore()
    {
        _fieldValidationStore?.Clear();
        _fieldValidationContext?.NotifyValidationStateChanged();
        _fieldValidationStore = null;
        _fieldValidationContext = null;
    }

    private void EnsureFieldValidationStore(EditContext context)
    {
        if (ReferenceEquals(_fieldValidationContext, context)) return;
        _fieldValidationStore?.Clear();
        _fieldValidationContext = context;
        _fieldValidationStore = new ValidationMessageStore(context);
    }

    private void CancelFieldValidations(FieldValidationCancellationReason reason)
    {
        FieldValidationOperation[] operations;
        lock (_validationSync)
        {
            if (_fieldValidationOperations.Count == 0) return;
            operations = _fieldValidationOperations.Values.ToArray();
            foreach (FieldValidationOperation operation in operations)
                operation.MarkCancellationReason(reason);
            _fieldValidationOperations.Clear();
        }
        foreach (FieldValidationOperation operation in operations)
            ReportCancellationException(operation.Cancel(reason));
    }

    private void ReportCancellationException(Exception? exception)
    {
        if (exception is null) return;
        ObserveTask(ReportExceptionSafelyAsync(exception), "OmniDataForm.CancellationCallback");
    }

    private async Task ReportExceptionSafelyAsync(Exception exception)
    {
        try
        {
            await DispatchExceptionAsync(exception);
        }
        catch when (Volatile.Read(ref _disposeState) != 0)
        {
            // The renderer was released while reporting the callback failure.
        }
    }

    private enum FieldValidationCancellationReason
    {
        None,
        Canceled,
        Superseded
    }

    private sealed class FieldValidationOperation : IDisposable
    {
        private readonly object _sync = new();
        private CancellationTokenSource? _source;
        private int _cancellationReason;

        public FieldValidationOperation(CancellationToken cancellationToken)
            => _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        public CancellationToken Token
        {
            get
            {
                lock (_sync) return _source?.Token ?? new CancellationToken(canceled: true);
            }
        }

        public bool IsCancellationRequested
        {
            get
            {
                lock (_sync) return _source?.IsCancellationRequested ?? true;
            }
        }

        public FieldValidationCancellationReason CancellationReason
            => (FieldValidationCancellationReason)Volatile.Read(ref _cancellationReason);

        public Exception? Cancel(FieldValidationCancellationReason reason)
        {
            MarkCancellationReason(reason);
            CancellationTokenSource? source = TakeSource();
            if (source is null) return null;
            try
            {
                source.Cancel(throwOnFirstException: false);
                return null;
            }
            catch (AggregateException exception)
            {
                return exception;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            finally
            {
                source.Dispose();
            }
        }

        public void MarkCancellationReason(FieldValidationCancellationReason reason)
            => Interlocked.CompareExchange(
                ref _cancellationReason,
                (int)reason,
                (int)FieldValidationCancellationReason.None);

        public void Dispose()
        {
            TakeSource()?.Dispose();
        }

        private CancellationTokenSource? TakeSource()
        {
            lock (_sync)
            {
                CancellationTokenSource? source = _source;
                _source = null;
                return source;
            }
        }
    }

    private string RootCss => CssBuilder.Default("omni-data-form")
        .AddClass("omni-data-form-disabled", Disabled)
        .AddClass(Class)
        .Build();

    private string GridStyle
    {
        get
        {
            DataFormLayout layout = Schema?.Layout ?? DataFormLayout.Default;
            if (Columns is not { } columns) return DataFormCss.LayoutStyle(layout);
            int value = Math.Clamp(columns, 1, 12);
            return StyleBuilder.Default(DataFormCss.LayoutStyle(layout))
                .AddStyle("--omni-data-form-columns", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddStyle("--omni-data-form-columns-sm", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddStyle("--omni-data-form-columns-md", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddStyle("--omni-data-form-columns-lg", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddStyle("--omni-data-form-columns-xl", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddStyle("--omni-data-form-columns-xxl", value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Build();
        }
    }

    private string CellStyle(DataFormResolvedField<TModel> field)
        => DataFormCss.CellStyle(
            field.ColumnSpan,
            field.Metadata.ResponsiveSpans,
            Schema?.Layout ?? DataFormLayout.Default,
            Columns is { } columns ? Math.Clamp(columns, 1, 12) : null);

    private Action<EditContext, ValidationMessageStore>? EffectiveValidation
        => _hasSchemaRequiredValidation || Validation is not null
            ? ValidateSynchronously
            : null;

    private Func<EditContext, ValidationMessageStore, CancellationToken, Task>? EffectiveAsyncValidation
        => _hasSchemaAsyncValidation || ValidationAsync is not null || ValidationAsyncWithCancellation is not null
            ? ValidateAsynchronously
            : null;

    private void RebuildFields()
    {
        CancelFieldValidations(FieldValidationCancellationReason.Canceled);
        DetachFieldValidationStore();
        TModel model = ResolveModel();
        if (!ReferenceEquals(_currentModel, model)) _touchedFields.Clear();
        _currentModel = model;

        Dictionary<string, DataFormField<TModel>> explicitFields = BuildExplicitFieldMap();
        List<DataFormResolvedField<TModel>> all = [];
        List<DataFormDiagnostic> diagnostics = [];
        HashSet<string> consumed = new(StringComparer.Ordinal);
        bool autoGenerateFields = Schema?.AutoGenerateFields ?? true;

        for (int declarationOrder = 0; declarationOrder < ModelProperties.Length; declarationOrder++)
        {
            PropertyInfo property = ModelProperties[declarationOrder];
            explicitFields.TryGetValue(property.Name, out DataFormField<TModel>? metadata);

            if (autoGenerateFields && IsAutoGenerated(property))
            {
                all.Add(CreateResolvedField(model, property, metadata, declarationOrder));
                consumed.Add(property.Name);
            }
            else if (metadata is not null)
            {
                all.Add(CreateResolvedField(model, property, metadata, declarationOrder));
                consumed.Add(property.Name);
            }
            else if (autoGenerateFields && property.GetMethod is not null
                     && property.GetIndexParameters().Length == 0
                     && !IdentifierPropertyNames.Contains(property.Name)
                     && property.GetCustomAttribute<ScaffoldColumnAttribute>() is not { Scaffold: false }
                     && property.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() != false)
            {
                diagnostics.Add(new DataFormDiagnostic(
                    "DF001",
                    $"No built-in editor was inferred for '{property.Name}' ({property.PropertyType.Name}); the property was skipped.",
                    DataFormDiagnosticSeverity.Info,
                    property.Name));
            }
        }

        foreach (DataFormField<TModel> metadata in explicitFields.Values)
        {
            if (consumed.Contains(metadata.Property)) continue;
            all.Add(CreateResolvedField(
                model,
                metadata.PropertyPath.Leaf,
                metadata,
                ModelProperties.Length + all.Count));
            consumed.Add(metadata.Property);
        }

        all.Sort(static (left, right) =>
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0
                ? order
                : StringComparer.Ordinal.Compare(left.Metadata.Property, right.Metadata.Property);
        });

        _allResolvedFields = all;
        _resolvedFields = all.Where(static field => field.Metadata.GroupId is null).ToArray();
        _resolvedGroups = ResolveGroups(Schema?.Groups ?? [], all);
        _diagnostics = diagnostics;
        if (DiagnosticsChanged.HasDelegate)
            ObserveTask(
                InvokeAsync(() => DiagnosticsChanged.InvokeAsync(_diagnostics)),
                "OmniDataForm.DiagnosticsChanged");
        _hasSchemaRequiredValidation = all.Any(static field =>
            field.Metadata.EnforceRequired
            || field.Metadata.Validators.Any(static validator => validator.IsSynchronous));
        _hasSchemaAsyncValidation = all.Any(static field =>
            field.Metadata.Validators.Any(static validator => !validator.IsSynchronous));
    }

    private static IReadOnlyList<DataFormResolvedGroup<TModel>> ResolveGroups(
        IReadOnlyList<DataFormGroup<TModel>> groups,
        IReadOnlyList<DataFormResolvedField<TModel>> fields,
        Func<TModel, bool>? ancestorVisible = null)
    {
        if (groups.Count == 0) return [];
        DataFormResolvedGroup<TModel>[] result = new DataFormResolvedGroup<TModel>[groups.Count];
        for (int index = 0; index < groups.Count; index++)
        {
            DataFormGroup<TModel> group = groups[index];
            bool IsGroupVisible(TModel model)
                => (ancestorVisible?.Invoke(model) ?? true) && group.IsVisible(model);
            DataFormResolvedField<TModel>[] groupFields = fields
                .Where(field => StringComparer.Ordinal.Equals(field.Metadata.GroupId, group.Id))
                .ToArray();
            foreach (DataFormResolvedField<TModel> field in groupFields)
                field.ContainerVisible = IsGroupVisible;
            result[index] = new DataFormResolvedGroup<TModel>(
                group,
                groupFields,
                ResolveGroups(group.Groups, fields, IsGroupVisible));
        }
        Array.Sort(result, static (left, right) => left.Metadata.Order.CompareTo(right.Metadata.Order));
        return result;
    }

    private void ValidateSynchronously(EditContext context, ValidationMessageStore store)
    {
        if (ReferenceEquals(_fieldValidationContext, context)) _fieldValidationStore?.Clear();
        if (_currentModel is { } model)
        {
            foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
            {
                if (!field.Metadata.EnforceRequired
                    || !field.Metadata.IsRequired(model)
                    || !field.IsVisible(model)) continue;
                object? value = field.GetValue(model);
                if (HasRequiredValue(value)) continue;

                store.Add(
                    field.GetFieldIdentifier(model),
                    field.Metadata.RequiredError ?? Texts.Required);
            }

            foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
            {
                if (!field.IsVisible(model) || field.Metadata.Validators.Count == 0) continue;
                object? value = field.GetValue(model);
                foreach (IDataFormFieldValidator<TModel> validator in field.Metadata.Validators)
                {
                    if (!validator.IsSynchronous) continue;
                    string? error = validator.Validate(model, value);
                    if (!string.IsNullOrWhiteSpace(error))
                        store.Add(field.GetFieldIdentifier(model), error);
                }
            }

        }

        Validation?.Invoke(context, store);
    }

    private async Task ValidateAsynchronously(
        EditContext context,
        ValidationMessageStore store,
        CancellationToken cancellationToken)
    {
        if (ReferenceEquals(_fieldValidationContext, context)) _fieldValidationStore?.Clear();
        if (_currentModel is { } model)
        {
            foreach (DataFormResolvedField<TModel> field in _allResolvedFields)
            {
                if (!field.IsVisible(model) || field.Metadata.Validators.Count == 0) continue;
                object? value = field.GetValue(model);
                foreach (IDataFormFieldValidator<TModel> validator in field.Metadata.Validators)
                {
                    if (validator.IsSynchronous) continue;
                    cancellationToken.ThrowIfCancellationRequested();
                    string? error = await validator.ValidateAsync(model, value, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(error))
                        store.Add(field.GetFieldIdentifier(model), error);
                }
            }
        }

        if (ValidationAsyncWithCancellation is not null)
            await ValidationAsyncWithCancellation(context, store, cancellationToken);
        else if (ValidationAsync is not null)
            await ValidationAsync(context, store);
    }

    private async Task HandleFieldChangedAsync(DataFormFieldChangedEventArgs<TModel> args)
    {
        _touchedFields.Add(args.Property);
        if (FieldChanged.HasDelegate) await FieldChanged.InvokeAsync(args);
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleInvalidSubmitAsync(EditContext context)
    {
        if (FocusFirstInvalidOnSubmit) await FocusFirstInvalidAsync();
        if (OnInvalidSubmit.HasDelegate) await OnInvalidSubmit.InvokeAsync(context);
    }

    private static bool HasRequiredValue(object? value)
    {
        if (value is null) return false;
        if (value is string text) return text.Length != 0;
        return true;
    }

    private void AddCollectionCountErrors(
        DataFormResolvedField<TModel> field,
        object? value,
        ICollection<string> errors)
    {
        IDataFormCollectionDefinition<TModel>? collection = field.Metadata.Collection;
        if (collection is null) return;
        int count = collection.GetCount(value);
        if (count < collection.MinimumItems)
        {
            errors.Add(collection.MinimumItemsError ?? string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Texts.DataFormMinimumItems,
                collection.MinimumItems));
        }
        if (count > collection.MaximumItems)
        {
            errors.Add(collection.MaximumItemsError ?? string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Texts.DataFormMaximumItems,
                collection.MaximumItems));
        }
    }

    private IReadOnlyList<DataFormValidationSummaryEntry> ValidationSummaryEntries
    {
        get
        {
            if (_currentModel is not { } model || CurrentEditContext is not { } context) return [];
            List<DataFormValidationSummaryEntry> result = [];
            Dictionary<string, int> assignedMessages = new(StringComparer.Ordinal);
            int key = 0;

            foreach (DataFormResolvedField<TModel> resolvedField in _allResolvedFields)
            {
                if (!resolvedField.IsVisible(model)) continue;
                foreach (string message in context.GetValidationMessages(resolvedField.GetFieldIdentifier(model)))
                {
                    result.Add(new DataFormValidationSummaryEntry(
                        key++,
                        resolvedField.Metadata.Property,
                        message));
                    assignedMessages.TryGetValue(message, out int count);
                    assignedMessages[message] = count + 1;
                }
            }

            foreach (string message in context.GetValidationMessages())
            {
                if (assignedMessages.TryGetValue(message, out int count) && count > 0)
                {
                    assignedMessages[message] = count - 1;
                    continue;
                }
                result.Add(new DataFormValidationSummaryEntry(key++, null, message));
            }
            return result;
        }
    }

    private async Task FocusSummaryEntryAsync(string propertyPath)
    {
        await FocusFieldAsync(propertyPath);
    }

    internal sealed record DataFormValidationSummaryEntry(
        int Key,
        string? PropertyPath,
        string Message);

    private Dictionary<string, DataFormField<TModel>> BuildExplicitFieldMap()
    {
        Dictionary<string, DataFormField<TModel>> result = new(StringComparer.Ordinal);
        if (Schema is null) return result;

        foreach (DataFormField<TModel> field in Schema.Fields)
        {
            if (!result.TryAdd(field.Property, field))
                throw new InvalidOperationException($"OmniDataForm field '{field.Property}' was declared more than once.");
        }

        return result;
    }

    private DataFormResolvedField<TModel> CreateResolvedField(
        TModel model,
        PropertyInfo property,
        DataFormField<TModel>? metadata,
        int declarationOrder)
    {
        DataFormEditor inferredEditor = InferEditor(property);
        Type? customEditorType = null;
        DataFormEditor editor;
        if (metadata?.Editor is { } configured && configured != DataFormEditor.Auto)
        {
            editor = configured;
        }
        else if (metadata?.Template is null)
        {
            customEditorType = EditorRegistry.Resolve(new DataFormEditorResolverContext(
                typeof(TModel),
                metadata?.Property ?? property.Name,
                property));
            editor = customEditorType is null ? inferredEditor : DataFormEditor.Custom;
        }
        else
        {
            editor = inferredEditor;
        }

        if (editor == DataFormEditor.Auto && metadata?.Template is null)
        {
            throw new InvalidOperationException(
                $"OmniDataForm does not have a default editor for '{property.Name}' ({property.PropertyType.Name}). " +
                "Configure a supported Editor or a custom Template.");
        }

        ValidateEditorCompatibility(property, editor, metadata?.Template is not null);

        return CreateResolvedFieldCore(
            model,
            property,
            metadata,
            editor,
            customEditorType,
            declarationOrder);
    }

    private DataFormResolvedField<TModel> CreateResolvedFieldCore(
        TModel model,
        PropertyInfo property,
        DataFormField<TModel>? configured,
        DataFormEditor editor,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type? customEditorType,
        int declarationOrder)
    {
        DisplayAttribute? display = property.GetCustomAttribute<DisplayAttribute>();
        RequiredAttribute? requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
        EditableAttribute? editable = property.GetCustomAttribute<EditableAttribute>();
        ReadOnlyAttribute? readOnly = property.GetCustomAttribute<ReadOnlyAttribute>();
        DataFormConventionDefaults convention = ResolveConventionDefaults(property);

        DataFormPropertyPath propertyPath = configured?.PropertyPath ?? ResolveRuntimePropertyPath(property);
        _ = propertyPath.GetOwner(model);
        DataFormField<TModel> effective = new(
            propertyPath.Path,
            propertyPath,
            configured?.GroupId,
            configured?.Label ?? convention.Label ?? display?.GetName() ?? SplitPascalCase(property.Name),
            configured?.Placeholder ?? convention.Placeholder ?? display?.GetPrompt(),
            configured?.Hint ?? convention.Hint ?? display?.GetDescription(),
            configured?.HintRight,
            editor,
            configured?.Order ?? convention.Order ?? display?.GetOrder() ?? declarationOrder,
            Math.Max(1, configured is { HasExplicitColumnSpan: true }
                ? configured.ColumnSpan
                : convention.Span ?? configured?.ColumnSpan ?? 1),
            true,
            configured?.ResponsiveSpans ?? EmptyResponsiveSpans,
            configured is { HasExplicitVisible: true }
                ? configured.Visible
                : convention.Visible ?? configured?.Visible ?? true,
            true,
            configured?.VisibleWhen,
            configured?.EnabledWhen,
            configured?.ReadOnlyWhen,
            configured?.RequiredWhen,
            configured?.Required ?? convention.Required ?? requiredAttribute is not null,
            (configured?.EnforceRequired == true || convention.Required == true)
                && (!AddDataAnnotationsValidator || requiredAttribute is null),
            configured?.RequiredError ?? convention.RequiredError,
            configured?.Disabled ?? convention.Disabled ?? false,
            (configured?.ReadOnly
                ?? convention.ReadOnly
                ?? readOnly?.IsReadOnly
                ?? (editable is not null && !editable.AllowEdit))
                || property.SetMethod is null,
            configured?.Class,
            configured?.Style,
            ResolveOptions(configured, property.PropertyType),
            MergeEditorParameters(property, configured?.EditorParameters),
            configured?.Template,
            configured?.Validators ?? [],
            configured?.Lookup,
            configured?.Collection,
            configured?.DefaultValue,
            configured?.RendererType,
            customEditorType);

        string fieldId = CreateFieldId(Id, propertyPath.Path);
        string editorId = $"{fieldId}-input";

        Dictionary<string, object?> rendererParameters = new(StringComparer.Ordinal)
        {
            [nameof(OmniDataFormFieldRenderer<TModel, object>.Model)] = model,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.Field)] = effective,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.Property)] = property,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.Properties)] = propertyPath.Properties,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.FieldId)] = fieldId,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.Disabled)] = Disabled,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.ReadOnly)] = ReadOnly,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.LookupVersion)] = 0L,
            [nameof(OmniDataFormFieldRenderer<TModel, object>.FieldChanged)] =
                EventCallback.Factory.Create<DataFormFieldChangedEventArgs<TModel>>(this, HandleFieldChangedAsync)
        };

        return new DataFormResolvedField<TModel>
        {
            Metadata = effective,
            Property = property,
            RendererType = effective.RendererType ?? ResolveRuntimeRendererType(property),
            Order = effective.Order ?? declarationOrder,
            ColumnSpan = effective.ColumnSpan,
            RendererParameters = rendererParameters,
            FieldId = fieldId,
            EditorId = editorId,
            LookupVersion = 0
        };
    }

    private static Type ResolveRuntimeRendererType(PropertyInfo property)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException(
                $"O campo '{property.Name}' foi gerado automaticamente, mas Native AOT exige um " +
                "DataFormSchema tipado. Configure o campo com schema.Field(model => model.Propriedade).");
        }

        return CreateRuntimeRendererType(property.PropertyType);
    }

    [RequiresDynamicCode("A geração automática de campos cria um renderer genérico em tempo de execução. Use DataFormSchema tipado em Native AOT.")]
    private static Type CreateRuntimeRendererType(Type propertyType)
        => typeof(OmniDataFormFieldRenderer<,>).MakeGenericType(typeof(TModel), propertyType);

    private DataFormConventionDefaults ResolveConventionDefaults(PropertyInfo property)
    {
        string? label = null;
        string? placeholder = null;
        string? hint = null;
        int? order = null;
        int? span = null;
        bool? visible = null;
        bool? required = null;
        string? requiredError = null;
        bool? disabled = null;
        bool? readOnly = null;

        foreach (DataFormFieldConvention<TModel> convention in Schema?.Conventions ?? [])
        {
            if (!convention.Predicate(property)) continue;
            if (convention.Label is not null) label = convention.Label(property);
            placeholder = convention.Placeholder ?? placeholder;
            hint = convention.Hint ?? hint;
            order = convention.Order ?? order;
            span = convention.Span ?? span;
            visible = convention.Visible ?? visible;
            required = convention.Required ?? required;
            requiredError = convention.RequiredError ?? requiredError;
            disabled = convention.Disabled ?? disabled;
            readOnly = convention.ReadOnly ?? readOnly;
        }

        return new DataFormConventionDefaults(
            label,
            placeholder,
            hint,
            order,
            span,
            visible,
            required,
            requiredError,
            disabled,
            readOnly);
    }

    [RequiresDynamicCode("Automatic field expressions require runtime code generation. Use a typed DataFormSchema in Native AOT.")]
    private static DataFormPropertyPath CreateDirectPropertyPath(PropertyInfo property)
    {
        ParameterExpression model = Expression.Parameter(typeof(TModel), "model");
        MemberExpression access = Expression.Property(model, property);
        return new DataFormPropertyPath(property.Name, [property], Expression.Lambda(access, model));
    }

    private static DataFormPropertyPath ResolveRuntimePropertyPath(PropertyInfo property)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException(
                $"O campo '{property.Name}' foi gerado automaticamente, mas Native AOT exige um DataFormSchema tipado.");
        }
        return CreateDirectPropertyPath(property);
    }

    private static string CreateFieldId(string formId, string propertyPath)
    {
        System.Text.StringBuilder builder = new(formId.Length + propertyPath.Length + 7);
        builder.Append(formId).Append("-field-");
        foreach (char character in propertyPath)
            builder.Append(char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        return builder.ToString();
    }

    private static string GetPropertyPath<TValue>(Expression<Func<TModel, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression? current = expression.Body;
        while (current is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } conversion)
            current = conversion.Operand;

        List<string> names = [];
        while (current is MemberExpression member && member.Member is PropertyInfo property)
        {
            names.Add(property.Name);
            current = member.Expression;
        }
        if (!ReferenceEquals(current, expression.Parameters[0]) || names.Count == 0)
            throw new ArgumentException("The expression must select one model property path.", nameof(expression));
        names.Reverse();
        return string.Join('.', names);
    }

    private static IReadOnlyDictionary<string, object?> MergeEditorParameters(
        PropertyInfo property,
        IReadOnlyDictionary<string, object?>? configured)
    {
        Dictionary<string, object?>? result = null;

        StringLengthAttribute? stringLength = property.GetCustomAttribute<StringLengthAttribute>();
        if (stringLength is not null)
        {
            result = new(StringComparer.Ordinal);
            if (stringLength.MinimumLength > 0) result["MinLength"] = stringLength.MinimumLength;
            if (stringLength.MaximumLength > 0) result["MaxLength"] = stringLength.MaximumLength;
        }
        else if (property.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 } maxLength)
        {
            result = new(StringComparer.Ordinal) { ["MaxLength"] = maxLength.Length };
        }

        if (property.GetCustomAttribute<RangeAttribute>() is { } range
            && TryDecimal(range.Minimum, out decimal min)
            && TryDecimal(range.Maximum, out decimal max))
        {
            result ??= new(StringComparer.Ordinal);
            result["Min"] = min;
            result["Max"] = max;
        }

        if (configured is not null)
        {
            result ??= new(StringComparer.Ordinal);
            foreach ((string key, object? value) in configured)
            {
                if (ReservedEditorParameters.Contains(key)) continue;
                result[key] = value;
            }
        }

        return result is null
            ? EmptyParameters
            : new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(result);
    }

    private static bool TryDecimal(object? value, out decimal result)
    {
        try
        {
            result = Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    private static bool IsAutoGenerated(PropertyInfo property)
    {
        if (property.GetMethod is null || property.GetIndexParameters().Length != 0) return false;
        if (IdentifierPropertyNames.Contains(property.Name)) return false;
        if (property.GetCustomAttribute<ScaffoldColumnAttribute>() is { Scaffold: false }) return false;
        if (property.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() == false) return false;
        return InferEditor(property) != DataFormEditor.Auto;
    }

    private static bool IsIdentifierProperty(PropertyInfo property)
    {
        string name = property.Name;
        if (name.Equals("Id", StringComparison.Ordinal)
            || name.Equals("ID", StringComparison.Ordinal)
            || name.EndsWith("Id", StringComparison.Ordinal)
            || name.EndsWith("ID", StringComparison.Ordinal))
        {
            return true;
        }

        if (property.IsDefined(typeof(KeyAttribute), inherit: true)) return true;

        return property.GetCustomAttributes<DatabaseGeneratedAttribute>(inherit: true)
            .Any(static attribute => attribute.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity);
    }

    private static void ValidateEditorCompatibility(
        PropertyInfo property,
        DataFormEditor editor,
        bool hasTemplate)
    {
        if (hasTemplate) return;

        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        bool compatible = editor switch
        {
            DataFormEditor.Text or DataFormEditor.TextArea or DataFormEditor.Password or
                DataFormEditor.Email or DataFormEditor.Telephone or DataFormEditor.Url
                => type == typeof(string),
            DataFormEditor.Number => IsNumeric(type),
            DataFormEditor.Date or DataFormEditor.DateTime or DataFormEditor.Time
                => type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(TimeOnly),
            DataFormEditor.CheckBox or DataFormEditor.Switch => property.PropertyType == typeof(bool),
            DataFormEditor.Select => true,
            DataFormEditor.Collection => true,
            DataFormEditor.Custom => true,
            _ => false
        };

        if (!compatible)
        {
            throw new InvalidOperationException(
                $"OmniDataForm editor '{editor}' is not compatible with '{property.Name}' " +
                $"({property.PropertyType.Name}). Configure a compatible editor or a custom Template.");
        }
    }

    private static DataFormEditor InferEditor(PropertyInfo property)
    {
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        DataType? dataType = ResolveDataType(property);

        if (type == typeof(string))
        {
            return dataType switch
            {
                DataType.MultilineText => DataFormEditor.TextArea,
                DataType.Password => DataFormEditor.Password,
                DataType.EmailAddress => DataFormEditor.Email,
                DataType.PhoneNumber => DataFormEditor.Telephone,
                DataType.Url => DataFormEditor.Url,
                _ => DataFormEditor.Text
            };
        }

        if (type == typeof(bool))
            return Nullable.GetUnderlyingType(property.PropertyType) is null
                ? DataFormEditor.Switch
                : DataFormEditor.Select;
        if (type.IsEnum) return DataFormEditor.Select;
        if (IsNumeric(type)) return DataFormEditor.Number;
        if (type == typeof(DateOnly)) return DataFormEditor.Date;
        if (type == typeof(TimeOnly)) return DataFormEditor.Time;
        if (type == typeof(DateTime))
            return dataType switch
            {
                DataType.Date => DataFormEditor.Date,
                DataType.Time => DataFormEditor.Time,
                _ => DataFormEditor.DateTime
            };

        return DataFormEditor.Auto;
    }

    private static DataType? ResolveDataType(PropertyInfo property)
    {
        DataType? specialized = null;

        foreach (DataTypeAttribute attribute in
                 property.GetCustomAttributes<DataTypeAttribute>(inherit: true))
        {
            // EmailAddressAttribute, PhoneAttribute, UrlAttribute and other
            // specialized validators derive from DataTypeAttribute. A model
            // can legitimately combine one of them with an explicit
            // [DataType(...)], so GetCustomAttribute<T>() is not safe here.
            // The explicit attribute is the author's editor override and wins.
            if (attribute.GetType() == typeof(DataTypeAttribute))
                return attribute.DataType;

            specialized ??= attribute.DataType;
        }

        return specialized;
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is
        TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070",
        Justification = "This reflection path is only used by auto-generated fields, which are rejected under Native AOT. Typed schemas precompute enum options with rooted enum fields.")]
    private IReadOnlyList<DataFormOption>? BuildRuntimeEnumOptions(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type == typeof(bool) && Nullable.GetUnderlyingType(propertyType) is not null)
            return [new(null, Texts.NotProvided), new(true, Texts.Yes), new(false, Texts.No)];
        if (!type.IsEnum) return null;

        Array values = Enum.GetValuesAsUnderlyingType(type);
        List<DataFormOption> options = new(values.Length);
        foreach (object underlyingValue in values)
        {
            object value = Enum.ToObject(type, underlyingValue);
            string name = Enum.GetName(type, value) ?? value.ToString()!;
            string label = type.GetField(name, BindingFlags.Public | BindingFlags.Static)?
                .GetCustomAttributes<DisplayAttribute>(inherit: false)
                .FirstOrDefault()
                ?.GetName() ?? name;
            options.Add(new DataFormOption(value, label));
        }
        return options;
    }

    private IReadOnlyList<DataFormOption>? ResolveOptions(
        DataFormField<TModel>? configured,
        Type propertyType)
    {
        if (configured is null) return BuildRuntimeEnumOptions(propertyType);
        if (configured.Options is not null) return configured.Options;
        return Nullable.GetUnderlyingType(propertyType) == typeof(bool)
            ? [new(null, Texts.NotProvided), new(true, Texts.Yes), new(false, Texts.No)]
            : null;
    }

    private static string SplitPascalCase(string value)
    {
        if (value.Length < 2) return value;
        System.Text.StringBuilder? builder = null;
        int segmentStart = 0;
        for (int index = 1; index < value.Length; index++)
        {
            if (!char.IsUpper(value[index]) || char.IsUpper(value[index - 1])) continue;
            builder ??= new System.Text.StringBuilder(value.Length + 4);
            builder.Append(value.AsSpan(segmentStart, index - segmentStart));
            builder.Append(' ');
            segmentStart = index;
        }
        if (builder is null) return value;
        builder.Append(value.AsSpan(segmentStart));
        return builder.ToString();
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(0, StringComparer.Ordinal));

    private static readonly IReadOnlyDictionary<Breakpoint, int> EmptyResponsiveSpans =
        new System.Collections.ObjectModel.ReadOnlyDictionary<Breakpoint, int>(
            new Dictionary<Breakpoint, int>());

    private static readonly HashSet<string> ReservedEditorParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "Value", "ValueChanged", "ValueExpression", "Name", "Required",
        "Disabled", "ReadOnly", "Validation", "OnlyValidateIfDirty", "InputId"
    };

    private TModel ResolveModel()
    {
        if (Model is not null && EditContext is not null)
        {
            throw new InvalidOperationException(
                "OmniDataForm requires either Model or EditContext, but not both.");
        }

        if (EditContext is not null)
        {
            if (EditContext.Model is not TModel typedModel)
            {
                throw new InvalidOperationException(
                    $"OmniDataForm EditContext model must be assignable to {typeof(TModel).Name}.");
            }

            return typedModel;
        }

        return Model ?? throw new InvalidOperationException(
            "OmniDataForm requires either a non-null Model or an EditContext.");
    }

    private readonly record struct DefinitionState(
        TModel? Model,
        EditContext? EditContext,
        DataFormSchema<TModel>? Schema,
        bool AddDataAnnotationsValidator,
        bool Disabled,
        bool ReadOnly,
        EventCallback<DataFormFieldChangedEventArgs<TModel>> FieldChanged);

    private sealed class DefinitionStateComparer : IEqualityComparer<DefinitionState>
    {
        public static DefinitionStateComparer Instance { get; } = new();

        public bool Equals(DefinitionState x, DefinitionState y)
            => ReferenceEquals(x.Model, y.Model)
               && ReferenceEquals(x.EditContext, y.EditContext)
               && ReferenceEquals(x.Schema, y.Schema)
               && x.AddDataAnnotationsValidator == y.AddDataAnnotationsValidator
               && x.Disabled == y.Disabled
               && x.ReadOnly == y.ReadOnly
               && x.FieldChanged.Equals(y.FieldChanged);

        public int GetHashCode(DefinitionState obj)
            => HashCode.Combine(
                obj.Model is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Model),
                obj.EditContext is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.EditContext),
                obj.Schema is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Schema),
                obj.AddDataAnnotationsValidator,
                obj.Disabled,
                obj.ReadOnly,
                obj.FieldChanged);
    }
}
