using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>
/// Contract shared by one feature-scoped JavaScript module. Consumers depend
/// on a narrower domain interface such as <see cref="IOmniScrollJsModule"/>.
/// </summary>
internal interface IOmniFeatureJsModule : IAsyncDisposable
{
    string ModulePath { get; }

    ValueTask InvokeVoidAsync(string identifier, params object?[]? args);

    ValueTask InvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args);

    ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        params object?[]? args);

    ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args);
}

internal interface IOmniCoreJsModule : IOmniFeatureJsModule;
internal interface IOmniScrollJsModule : IOmniFeatureJsModule;
internal interface IOmniResponsiveJsModule : IOmniFeatureJsModule;
internal interface IOmniOverlayJsModule : IOmniFeatureJsModule;
internal interface IOmniInputsJsModule : IOmniFeatureJsModule;
internal interface IOmniNavigationJsModule : IOmniFeatureJsModule;
internal interface IOmniSpeechJsModule : IOmniFeatureJsModule;
internal interface IOmniDataJsModule : IOmniFeatureJsModule;
internal interface IOmniDisplayJsModule : IOmniFeatureJsModule;

internal interface IOmniDiagramJsModule : IAsyncDisposable
{
    string ModulePath { get; }

    ValueTask<IJSObjectReference> InitializeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TReceiver>(
        string elementId,
        DotNetObjectReference<TReceiver> receiver,
        object options,
        CancellationToken cancellationToken = default)
        where TReceiver : class;
}

/// <summary>
/// Owns the import, concurrent calls and deterministic disposal of exactly one
/// ECMAScript module. Module selection is a composition-root concern, not a
/// runtime string-routing responsibility.
/// </summary>
internal abstract class OmniJsModule : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly object _sync = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<IJSObjectReference>? _moduleTask;
    private Task? _disposeTask;
    private int _activeCalls;
    private int _disposeState;

    protected OmniJsModule(IJSRuntime js, string modulePath)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);
        ModulePath = modulePath;
    }

    public string ModulePath { get; }

    protected async ValueTask InvokeCoreVoidAsync(
        string moduleIdentifier,
        object?[] moduleArguments,
        CancellationToken cancellationToken)
        => await InvokeCoreAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            moduleIdentifier,
            moduleArguments,
            cancellationToken);

    protected async ValueTask<TValue> InvokeCoreAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string moduleIdentifier,
        object?[] moduleArguments,
        CancellationToken cancellationToken)
    {
        EnterCall();
        CancellationTokenSource? linkedCancellation = null;
        CancellationToken effectiveCancellation = GetEffectiveCancellationToken(
            cancellationToken,
            ref linkedCancellation);

        try
        {
            IJSObjectReference module = await GetModuleAsync().WaitAsync(effectiveCancellation);
            return await module.InvokeAsync<TValue>(
                moduleIdentifier,
                effectiveCancellation,
                moduleArguments);
        }
        finally
        {
            linkedCancellation?.Dispose();
            ExitCall();
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposeTask is not null)
                return new ValueTask(_disposeTask);

            Volatile.Write(ref _disposeState, 1);
            Task? drainTask = _activeCalls == 0 ? null : _drained.Task;
            Task<IJSObjectReference>? moduleTask = _moduleTask;
            _moduleTask = null;
            _disposeTask = DisposeCoreAsync(drainTask, moduleTask, _lifetimeCancellation);
            return new ValueTask(_disposeTask);
        }
    }

    private static async Task DisposeCoreAsync(
        Task? drainTask,
        Task<IJSObjectReference>? moduleTask,
        CancellationTokenSource lifetimeCancellation)
    {
        // Run callbacks after the caller's lock is released. A cancellation
        // callback can complete an invocation and re-enter ExitCall.
        await Task.Yield();
        lifetimeCancellation.Cancel();

        if (drainTask is not null)
        {
            try
            {
                await drainTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (moduleTask is not null)
            await DisposeModuleAsync(moduleTask);

        lifetimeCancellation.Dispose();
    }

    private static async Task DisposeModuleAsync(Task<IJSObjectReference> moduleTask)
    {
        try
        {
            IJSObjectReference module = await moduleTask;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
            // The import was attempted while prerendering and never produced
            // a browser-side reference.
        }
        catch (JSException)
        {
            // Observe a failed import during teardown so it never becomes an
            // unobserved task exception.
        }
    }

    private Task<IJSObjectReference> GetModuleAsync()
    {
        lock (_sync)
        {
            // Dispose can start after EnterCall and before this lookup. Never
            // publish an import after the disposal snapshot was captured.
            ThrowIfDisposed();
            if (_moduleTask is null || _moduleTask.IsCanceled || _moduleTask.IsFaulted)
                _moduleTask = ImportCoreAsync();
            return _moduleTask;
        }
    }

    private async Task<IJSObjectReference> ImportCoreAsync()
        => await _js.InvokeAsync<IJSObjectReference>(
            "import",
            _lifetimeCancellation.Token,
            ModulePath);

    private void EnterCall()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            _activeCalls++;
        }
    }

    private void ExitCall()
    {
        lock (_sync)
        {
            _activeCalls--;
            if (_activeCalls == 0 && _disposeState != 0)
                _drained.TrySetResult();
        }
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);

    private CancellationToken GetEffectiveCancellationToken(
        CancellationToken cancellationToken,
        ref CancellationTokenSource? linkedCancellation)
    {
        if (!cancellationToken.CanBeCanceled) return _lifetimeCancellation.Token;
        linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        return linkedCancellation.Token;
    }
}

internal abstract class OmniFeatureJsModule : OmniJsModule, IOmniFeatureJsModule
{
    protected OmniFeatureJsModule(IJSRuntime js, string modulePath)
        : base(js, modulePath)
    {
    }

    public ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
        => InvokeVoidAsync(identifier, CancellationToken.None, args);

    public ValueTask InvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        object?[] arguments = args ?? [];
        string moduleIdentifier = ValidateIdentifier(identifier);
        return InvokeCoreVoidAsync(
            "invoke",
            [moduleIdentifier, arguments],
            cancellationToken);
    }

    public ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        params object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        object?[] arguments = args ?? [];
        string moduleIdentifier = ValidateIdentifier(identifier);
        return InvokeCoreAsync<TValue>(
            "invoke",
            [moduleIdentifier, arguments],
            cancellationToken);
    }

    private static string ValidateIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return identifier;
    }
}

internal sealed class OmniCoreJsModule : OmniFeatureJsModule, IOmniCoreJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-core.js";
    internal OmniCoreJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniScrollJsModule : OmniFeatureJsModule, IOmniScrollJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-scroll.js";
    internal OmniScrollJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniResponsiveJsModule : OmniFeatureJsModule, IOmniResponsiveJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-responsive.js";
    internal OmniResponsiveJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniOverlayJsModule : OmniFeatureJsModule, IOmniOverlayJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-overlay.js";
    internal OmniOverlayJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniInputsJsModule : OmniFeatureJsModule, IOmniInputsJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-inputs.js";
    internal OmniInputsJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniNavigationJsModule : OmniFeatureJsModule, IOmniNavigationJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-navigation.js";
    internal OmniNavigationJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniSpeechJsModule : OmniFeatureJsModule, IOmniSpeechJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-speech.js";
    internal OmniSpeechJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniDataJsModule : OmniFeatureJsModule, IOmniDataJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-data.js";
    internal OmniDataJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniDisplayJsModule : OmniFeatureJsModule, IOmniDisplayJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/modules/omni-display.js";
    internal OmniDisplayJsModule(IJSRuntime js) : base(js, Path) { }
}

internal sealed class OmniDiagramJsModule : OmniJsModule, IOmniDiagramJsModule
{
    internal const string Path = "./_content/Omni.Blazor/js/omni-diagram.js";

    internal OmniDiagramJsModule(IJSRuntime js)
        : base(js, Path)
    {
    }

    public ValueTask<IJSObjectReference> InitializeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TReceiver>(
        string elementId,
        DotNetObjectReference<TReceiver> receiver,
        object options,
        CancellationToken cancellationToken = default)
        where TReceiver : class
    {
        object?[] arguments = [elementId, receiver, options];
        return InvokeCoreAsync<IJSObjectReference>(
            "init",
            arguments,
            cancellationToken);
    }
}
