using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Async validation and submission orchestration for <see cref="OmniForm{TModel}"/>.
/// Kept out of the Razor file so cancellation, latest-wins and exception-safety
/// invariants remain reviewable independently from the markup.
/// </summary>
public partial class OmniForm<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>
    where TModel : class
{
    private static readonly PropertyInfo[] SnapshotProperties = typeof(TModel)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(static property => property.GetMethod is not null
            && property.SetMethod is not null
            && property.GetIndexParameters().Length == 0)
        .ToArray();

    private readonly object _validationSync = new();
    private CancellationTokenSource? _validationCts;
    private long _validationVersion;
    private int _disposeState;
    private PropertySnapshot[]? _propertySnapshot;
    private TModel? _typedSnapshot;

    /// <summary>
    /// Optional AOT-safe deep-clone strategy used by <see cref="Snapshot"/>.
    /// Without it, OmniForm captures a shallow snapshot of writable public properties.
    /// </summary>
    [Parameter]
    public Func<TModel, TModel>? SnapshotFactory { get; set; }

    /// <summary>
    /// Optional restore strategy paired with <see cref="SnapshotFactory"/>.
    /// It receives the current model first and the captured snapshot second.
    /// </summary>
    [Parameter]
    public Action<TModel, TModel>? SnapshotRestorer { get; set; }

    /// <summary>Captures the current model state without reflection-based JSON serialization.</summary>
    public void Snapshot()
    {
        TModel? model = CurrentModel;
        if (model is null)
        {
            _propertySnapshot = null;
            _typedSnapshot = null;
            return;
        }

        if (SnapshotFactory is not null)
        {
            TModel snapshot = SnapshotFactory(model)
                ?? throw new InvalidOperationException("SnapshotFactory returned null.");
            _typedSnapshot = snapshot;
            _propertySnapshot = null;
            return;
        }

        PropertySnapshot[] snapshotValues = new PropertySnapshot[SnapshotProperties.Length];
        for (int index = 0; index < SnapshotProperties.Length; index++)
        {
            PropertyInfo property = SnapshotProperties[index];
            snapshotValues[index] = new PropertySnapshot(property, property.GetValue(model));
        }
        _propertySnapshot = snapshotValues;
        _typedSnapshot = null;
    }

    /// <summary>Restores the current model in place so existing bindings keep the same reference.</summary>
    public void Restore()
    {
        TModel? model = CurrentModel;
        if (model is null) return;

        if (_typedSnapshot is not null)
        {
            if (SnapshotRestorer is not null) SnapshotRestorer(model, _typedSnapshot);
            else CopyProperties(_typedSnapshot, model);
            _ctx?.NotifyValidationStateChanged();
            return;
        }

        if (_propertySnapshot is null) return;
        ApplySnapshot(model, _propertySnapshot);
        _ctx?.NotifyValidationStateChanged();
    }

    private static void CopyProperties(TModel source, TModel destination)
    {
        PropertySnapshot[] values = new PropertySnapshot[SnapshotProperties.Length];
        for (int index = 0; index < SnapshotProperties.Length; index++)
        {
            PropertyInfo property = SnapshotProperties[index];
            values[index] = new PropertySnapshot(property, property.GetValue(source));
        }
        ApplySnapshot(destination, values);
    }

    private static void ApplySnapshot(TModel destination, PropertySnapshot[] values)
    {
        PropertySnapshot[] rollback = new PropertySnapshot[values.Length];
        int applied = 0;
        try
        {
            for (; applied < values.Length; applied++)
            {
                PropertySnapshot value = values[applied];
                rollback[applied] = new PropertySnapshot(value.Property, value.Property.GetValue(destination));
                value.Property.SetValue(destination, value.Value);
            }
        }
        catch
        {
            for (int index = applied - 1; index >= 0; index--)
            {
                try { rollback[index].Property.SetValue(destination, rollback[index].Value); }
                catch { /* Preserve the original restore exception. */ }
            }
            throw;
        }
    }

    private readonly record struct PropertySnapshot(PropertyInfo Property, object? Value);

    /// <summary>
    /// Runs standard and synchronous custom validation first, then awaits
    /// async custom validation. Bypasses <see cref="ValidationDelay"/>.
    /// </summary>
    public Task<bool> ValidateAsync() => ValidateAsync(CancellationToken.None);

    /// <summary>
    /// Cancellable validation overload. A newer validation automatically
    /// supersedes this pass and prevents stale messages from becoming current.
    /// </summary>
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken)
    {
        if (_ctx is null) return true;

        // EditContext.Validate raises OnValidationRequested, which refreshes
        // the dedicated synchronous store. Async messages live in a separate
        // store and therefore cannot be erased by this phase.
        _ctx.Validate();
        await RunAsyncValidationAsync(immediate: true, cancellationToken);
        return !_ctx.GetValidationMessages().Any();
    }

    /// <summary>Programmatically submit and await all validation.</summary>
    public Task SubmitAsync() => SubmitAsync(CancellationToken.None);

    /// <summary>Programmatically submit with cancellation support.</summary>
    public async Task SubmitAsync(CancellationToken cancellationToken)
    {
        if (_ctx is null) return;
        var isValid = await ValidateAsync(cancellationToken);
        if (isValid) await OnValidSubmit.InvokeAsync(_ctx);
        else await OnInvalidSubmit.InvokeAsync(_ctx);
    }

    private async Task RunCustomAsync(bool immediate, CancellationToken cancellationToken)
    {
        if (_ctx is null || _syncStore is null) return;

        _syncStore.Clear();
        Validation?.Invoke(_ctx, _syncStore);
        await RunAsyncValidationAsync(immediate, cancellationToken);
    }

    private async Task RunAsyncValidationAsync(bool immediate, CancellationToken cancellationToken)
    {
        if (_ctx is null) return;

        var context = _ctx;
        var localStore = new ValidationMessageStore(context);
        var previousStore = _asyncStore;
        _asyncStore = localStore;
        previousStore?.Clear();

        CancellationTokenSource linkedCts;
        CancellationTokenSource? previousCts;
        long version;
        lock (_validationSync)
        {
            if (IsDisposed) return;

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            version = ++_validationVersion;
            previousCts = _validationCts;
            _validationCts = linkedCts;
        }
        CancelSafely(previousCts);

        try
        {
            if (ValidationAsyncWithCancellation is not null)
            {
                await ValidationAsyncWithCancellation(context, localStore, linkedCts.Token);
            }
            else if (ValidationAsync is not null)
            {
                await ValidationAsync(context, localStore);
            }

            foreach (IOmniFormValidationParticipant participant in SnapshotValidationParticipants())
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                await participant.ValidateAsync(context, localStore, linkedCts.Token);
            }

            bool isCurrent;
            lock (_validationSync)
            {
                isCurrent = !IsDisposed
                    && version == _validationVersion
                    && ReferenceEquals(_validationCts, linkedCts);
            }

            if (!isCurrent
                || linkedCts.IsCancellationRequested
                || !ReferenceEquals(context, _ctx)
                || !ReferenceEquals(localStore, _asyncStore))
            {
                localStore.Clear();
                return;
            }

            context.NotifyValidationStateChanged();
            if (!immediate) await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            localStore.Clear();
        }
        finally
        {
            lock (_validationSync)
            {
                if (ReferenceEquals(_validationCts, linkedCts)) _validationCts = null;
            }
            linkedCts.Dispose();
        }
    }

    private async Task RunFieldValidationSafelyAsync()
    {
        try
        {
            await InvokeAsync(() => RunCustomAsync(immediate: false, CancellationToken.None));
        }
        catch (OperationCanceledException)
        {
            // A newer field change or disposal superseded this pass.
        }
        catch (Exception exception)
        {
            await ReportExceptionSafelyAsync(exception);
        }
    }

    private Task OnSubmitHandler(EditContext context) => SubmitAsync();

    private void CancelPendingValidation()
    {
        CancellationTokenSource? validation;
        lock (_validationSync)
        {
            ++_validationVersion;
            validation = _validationCts;
            _validationCts = null;
        }
        CancelSafely(validation);
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private IOmniFormValidationParticipant[] SnapshotValidationParticipants()
    {
        lock (_validationSync) return [.. _validationParticipants];
    }

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The validation operation owns disposal and may have completed concurrently.
        }
    }

    private async Task ReportExceptionSafelyAsync(Exception exception)
    {
        try
        {
            await DispatchExceptionAsync(exception);
        }
        catch when (IsDisposed)
        {
            // The renderer was released while reporting the original failure.
        }
    }
}
