using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Omni.Blazor.Models;
using Omni.Blazor.Services;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Canvas-based signature input with undo, clear and PNG/JPEG/SVG export.</summary>
public partial class OmniSignaturePad
{
    private ElementReference _canvas;
    private DotNetObjectReference<OmniSignaturePad>? _callbackReference;
    private SignaturePadHandle? _handle;
    private SignaturePadOptions? _appliedOptions;
    private ParameterState<string?> _valueState = null!;
    private int _disposeState;

    /// <summary>Canvas height in CSS pixels.</summary>
    [Parameter] public int Height { get; set; } = 220;

    /// <summary>Stroke color accepted by the browser canvas API.</summary>
    [Parameter] public string StrokeColor { get; set; } = "#111827";

    /// <summary>Canvas background color.</summary>
    [Parameter] public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>Stroke width in CSS pixels.</summary>
    [Parameter] public double StrokeWidth { get; set; } = 2;

    /// <summary>Format used by the bound <c>Value</c> and <see cref="ExportAsync"/>.</summary>
    [Parameter] public SignaturePadFormat Format { get; set; } = SignaturePadFormat.Png;

    /// <summary>JPEG quality between 0 and 1. Ignored by PNG and SVG.</summary>
    [Parameter] public double Quality { get; set; } = 0.92;

    /// <summary>Shows the built-in undo, clear and status toolbar.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>Accessible label announced for the drawing canvas.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Área para assinatura";

    /// <summary>Label for the undo action.</summary>
    [Parameter] public string UndoText { get; set; } = "Desfazer";

    /// <summary>Label for the clear action.</summary>
    [Parameter] public string ClearText { get; set; } = "Limpar";

    /// <summary>Status text shown while no signature has been captured.</summary>
    [Parameter] public string EmptyText { get; set; } = "Aguardando assinatura";

    /// <summary>Status text shown after at least one stroke is captured.</summary>
    [Parameter] public string SignedText { get; set; } = "Assinatura capturada";

    /// <summary>Raised after the user completes, removes or clears a stroke.</summary>
    [Parameter] public EventCallback<string?> StrokeCompleted { get; set; }

    /// <summary>Whether the pad currently contains at least one captured stroke.</summary>
    public bool HasSignature { get; private set; }

    private string RootCss => CssBuilder.Default("omni-signature-pad")
        .AddClass("omni-signature-pad-disabled", Disabled)
        .AddClass("omni-signature-pad-readonly", ReadOnly)
        .AddClass(Class)
        .Build();

    protected override void OnInitialized()
    {
        _valueState = RegisterParameter<string?>(nameof(Value))
            .WithParameter(() => Value)
            .WithEventCallback(() => ValueChanged)
            .WithChangeHandler(() => HasSignature = !string.IsNullOrWhiteSpace(Value))
            .Attach();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (Volatile.Read(ref _disposeState) != 0) return;

        var options = CreateOptions();
        if (_handle is null)
        {
            _callbackReference ??= DotNetObjectReference.Create(this);
            _handle = await SignaturePadService.AttachAsync(_canvas, _callbackReference, options);
            if (_handle is not null) _appliedOptions = options;
            return;
        }

        if (!Equals(options, _appliedOptions))
        {
            await _handle.UpdateAsync(options);
            _appliedOptions = options;
        }
    }

    /// <summary>Clears all strokes and updates the bound value.</summary>
    public async Task ClearAsync()
    {
        if (Disabled || ReadOnly) return;
        var snapshot = _handle is null
            ? new SignaturePadSnapshot(null, true)
            : await _handle.ClearAsync();
        await ApplySnapshotAsync(snapshot ?? new SignaturePadSnapshot(null, true));
    }

    /// <summary>Removes the most recently completed stroke.</summary>
    public async Task UndoAsync()
    {
        if (Disabled || ReadOnly || _handle is null) return;
        var snapshot = await _handle.UndoAsync();
        if (snapshot is not null) await ApplySnapshotAsync(snapshot);
    }

    /// <summary>Exports and returns the current signature using <see cref="Format"/>.</summary>
    public async Task<string?> ExportAsync()
    {
        if (_handle is null) return Value;
        var snapshot = await _handle.ExportAsync();
        if (snapshot is not null) await ApplySnapshotAsync(snapshot, notifyStrokeCompleted: false);
        return snapshot?.Value ?? Value;
    }

    /// <summary>Receives a completed stroke from the browser capture engine.</summary>
    [JSInvokable]
    public Task OnSignatureChangedAsync(string? value, bool isEmpty)
        => ApplySnapshotAsync(new SignaturePadSnapshot(value, isEmpty));

    private async Task ApplySnapshotAsync(SignaturePadSnapshot snapshot, bool notifyStrokeCompleted = true)
    {
        HasSignature = !snapshot.IsEmpty;
        await SetValueAsync(snapshot.Value);
        if (notifyStrokeCompleted && StrokeCompleted.HasDelegate)
            await StrokeCompleted.InvokeAsync(snapshot.Value);
        await InvokeAsync(StateHasChanged);
    }

    private SignaturePadOptions CreateOptions() => new(
        StrokeColor,
        BackgroundColor,
        Math.Max(0.5, StrokeWidth),
        Format,
        Math.Clamp(Quality, 0, 1),
        Disabled,
        ReadOnly,
        Value);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;

        if (_handle is not null)
        {
            await _handle.DisposeAsync();
            _handle = null;
        }
        _callbackReference?.Dispose();
        _callbackReference = null;
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
