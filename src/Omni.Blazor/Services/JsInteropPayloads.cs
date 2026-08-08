using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

// Payload types for JS interop calls.
//
// These exist instead of anonymous types on purpose. The library sets
// IsAotCompatible, which implies IsTrimmable, so a published Blazor WebAssembly app
// trims this assembly's members — and trimming strips constructor parameter names.
// System.Text.Json binds a parameterized constructor by parameter name, and an
// anonymous type has nothing else (no parameterless constructor), so serializing one
// throws at runtime in a published app while working fine in development:
//
//     System.NotSupportedException: ConstructorContainsNullParameterNames
//
// Every type below has an implicit parameterless constructor and init-only
// properties, so System.Text.Json never inspects constructor parameters. Call sites
// root the property getters with [DynamicDependency] — a getter read only by the
// serializer is otherwise trimmable.
//
// Do NOT turn these back into anonymous types or records with primary constructors.
// Property names map to the JS contract through the camelCase policy that Blazor's
// interop options apply (Key -> key, CallOnInit -> callOnInit).

/// <summary>One key combination handed to <c>omniBlazor.registerHotkey</c>.</summary>
internal sealed class HotkeyComboPayload
{
    public string Key { get; init; } = "";
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Meta { get; init; }
}

/// <summary>One watched key handed to <c>omniBlazor.attachKeyListener</c>.</summary>
internal sealed class KeyInterceptorPayload
{
    public string Key { get; init; } = "";
    public bool? Ctrl { get; init; }
    public bool? Alt { get; init; }
    public bool? Shift { get; init; }
    public bool? Meta { get; init; }
    public bool PreventDefault { get; init; }
    public bool StopPropagation { get; init; }
}

/// <summary>Target position for <c>omniBlazor.scrollTo</c>.</summary>
internal sealed class ScrollToPayload
{
    public double Top { get; init; }
    public double Left { get; init; }
    public string Behavior { get; init; } = "";
}

/// <summary>Options for <c>omniBlazor.scrollIntoView</c>.</summary>
internal sealed class ScrollIntoViewPayload
{
    public string Behavior { get; init; } = "";
    public string Block { get; init; } = "";
}

/// <summary>Callback wiring for <c>omniBlazor.observeScrollPosition</c>.</summary>
internal sealed class ScrollObserverPayload
{
    public string Method { get; init; } = "";
    public bool CallOnInit { get; init; }
}

/// <summary>Esc-handler wiring for <c>omniBlazor.setupOverlay</c>.</summary>
/// <typeparam name="T">Component that exposes the [JSInvokable] callback.</typeparam>
internal sealed class OverlayEscapePayload<T> where T : class
{
    public DotNetObjectReference<T>? Dotnet { get; init; }
    public string Method { get; init; } = "";
}

/// <summary>Callback wiring for <c>omniBlazor.bottomSheetAttachDrag</c>.</summary>
internal sealed class BottomSheetDragPayload
{
    public string OnSnap { get; init; } = "";
}

/// <summary>Initial state for the diagram canvas engine.</summary>
internal sealed class DiagramInitPayload
{
    public bool ReadOnly { get; init; }
    public string? PayloadFormat { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Zoom { get; init; }
}

/// <summary>Dismiss behaviour for <c>omniBlazor.fabMenuOpen</c>.</summary>
internal sealed class FabMenuPayload
{
    public bool CloseOnOutsideClick { get; init; }
    public bool CloseOnEsc { get; init; }
}

/// <summary>Recognition settings and callback names for <c>omniBlazor.speechToggle</c>.</summary>
internal sealed class SpeechToTextPayload
{
    public string? Language { get; init; }
    public bool Continuous { get; init; }
    public bool InterimResults { get; init; }
    public int MaxAlternatives { get; init; }
    public string StartMethod { get; init; } = "";
    public string EndMethod { get; init; } = "";
    public string ResultMethod { get; init; } = "";
    public string ErrorMethod { get; init; } = "";
    public string UnsupportedMethod { get; init; } = "";
    public string StateMethod { get; init; } = "";
}

/// <summary>
/// Wire shape for <see cref="SignaturePadOptions"/>. The public type is a positional
/// record, so it carries the same trimming hazard as an anonymous type — this is what
/// actually crosses to JS.
/// </summary>
internal sealed class SignaturePadOptionsPayload
{
    public string StrokeColor { get; init; } = "";
    public string BackgroundColor { get; init; } = "";
    public double StrokeWidth { get; init; }
    // Keep the enum type: it goes over the wire as its numeric value, and JS reads it as such.
    public Omni.Blazor.Models.SignaturePadFormat Format { get; init; }
    public double Quality { get; init; }
    public bool Disabled { get; init; }
    public bool ReadOnly { get; init; }
    public string? InitialValue { get; init; }
}
