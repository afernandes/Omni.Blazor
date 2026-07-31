using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Async validation and submission orchestration for <see cref="OmniForm{TModel}"/>.
/// Kept out of the Razor file so cancellation, latest-wins and exception-safety
/// invariants remain reviewable independently from the markup.
/// </summary>
public partial class OmniForm<TModel> where TModel : class
{
    private readonly object _validationSync = new();
    private CancellationTokenSource? _validationCts;
    private long _validationVersion;
    private int _disposeState;

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
