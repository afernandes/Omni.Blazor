using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>Options passed to the browser signature capture engine.</summary>
public sealed record SignaturePadOptions(
    string StrokeColor,
    string BackgroundColor,
    double StrokeWidth,
    SignaturePadFormat Format,
    double Quality,
    bool Disabled,
    bool ReadOnly,
    string? InitialValue);

/// <summary>Current exported signature and its empty state.</summary>
public sealed record SignaturePadSnapshot(string? Value, bool IsEmpty);

/// <summary>
/// Typed façade over <c>window.omniBlazor.signaturePad</c>. Components use this
/// service instead of depending directly on <see cref="IJSRuntime"/>.
/// </summary>
public sealed class SignaturePadService
{
    private readonly IJSRuntime _js;

    public SignaturePadService(IJSRuntime js) => _js = js;

    /// <summary>Attaches the capture engine to a canvas and returns its lifetime handle.</summary>
    public async ValueTask<SignaturePadHandle?> AttachAsync(
        ElementReference canvas,
        DotNetObjectReference<Omni.Blazor.Components.OmniSignaturePad> callback,
        SignaturePadOptions options)
    {
        try
        {
            var reference = await _js.InvokeAsync<IJSObjectReference>(
                "omniBlazor.signaturePad.create",
                canvas,
                callback,
                options);

            return reference is null ? null : new SignaturePadHandle(reference);
        }
        catch (InvalidOperationException)
        {
            // Static SSR/prerender: the component can attach after an interactive render.
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }
}

/// <summary>Owns one browser signature-pad instance and its event listeners.</summary>
public sealed class SignaturePadHandle : IAsyncDisposable
{
    private IJSObjectReference? _reference;

    internal SignaturePadHandle(IJSObjectReference reference) => _reference = reference;

    /// <summary>Updates drawing and interaction options without replacing listeners.</summary>
    public ValueTask UpdateAsync(SignaturePadOptions options)
        => InvokeVoidAsync("update", options);

    /// <summary>Clears all captured strokes.</summary>
    public ValueTask<SignaturePadSnapshot?> ClearAsync()
        => InvokeAsync<SignaturePadSnapshot?>("clear");

    /// <summary>Removes the most recently completed stroke.</summary>
    public ValueTask<SignaturePadSnapshot?> UndoAsync()
        => InvokeAsync<SignaturePadSnapshot?>("undo");

    /// <summary>Exports the current strokes using the configured output format.</summary>
    public ValueTask<SignaturePadSnapshot?> ExportAsync()
        => InvokeAsync<SignaturePadSnapshot?>("exportValue");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var reference = Interlocked.Exchange(ref _reference, null);
        if (reference is null) return;

        try
        {
            await reference.InvokeVoidAsync("dispose");
        }
        catch (JSDisconnectedException)
        {
            // The browser circuit already owns the remaining resources.
        }
        catch (ObjectDisposedException)
        {
            // Idempotent disposal raced with renderer teardown.
        }

        try
        {
            await reference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async ValueTask InvokeVoidAsync(string method, params object?[] arguments)
    {
        var reference = Volatile.Read(ref _reference);
        if (reference is null) return;

        try
        {
            await reference.InvokeVoidAsync(method, arguments);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async ValueTask<T?> InvokeAsync<T>(string method)
    {
        var reference = Volatile.Read(ref _reference);
        if (reference is null) return default;

        try
        {
            return await reference.InvokeAsync<T>(method);
        }
        catch (JSDisconnectedException)
        {
            return default;
        }
        catch (ObjectDisposedException)
        {
            return default;
        }
    }
}

