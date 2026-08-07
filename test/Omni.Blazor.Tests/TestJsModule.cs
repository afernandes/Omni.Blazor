using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests;

/// <summary>
/// bUnit adapter that preserves the existing global invocation identifiers
/// without adding a test-only branch to the production module loader.
/// </summary>
internal sealed class TestJsModule :
    IOmniCoreJsModule,
    IOmniScrollJsModule,
    IOmniResponsiveJsModule,
    IOmniOverlayJsModule,
    IOmniInputsJsModule,
    IOmniNavigationJsModule,
    IOmniSpeechJsModule,
    IOmniDataJsModule,
    IOmniDisplayJsModule,
    IOmniDiagramJsModule
{
    private const string GlobalPrefix = "omniBlazor.";
    private readonly IJSRuntime _js;

    internal TestJsModule(IJSRuntime js) => _js = js;

    public string ModulePath => string.Empty;

    public ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
        => _js.InvokeVoidAsync(GlobalPrefix + identifier, args ?? []);

    public ValueTask InvokeVoidAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
        => _js.InvokeVoidAsync(GlobalPrefix + identifier, cancellationToken, args ?? []);

    public ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        params object?[]? args)
        => _js.InvokeAsync<TValue>(GlobalPrefix + identifier, args ?? []);

    public ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
        => _js.InvokeAsync<TValue>(GlobalPrefix + identifier, cancellationToken, args ?? []);

    public ValueTask<IJSObjectReference> InitializeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TReceiver>(
        string elementId,
        DotNetObjectReference<TReceiver> receiver,
        object options,
        CancellationToken cancellationToken = default)
        where TReceiver : class
        => _js.InvokeAsync<IJSObjectReference>(
            "init",
            cancellationToken,
            elementId,
            receiver,
            options);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
