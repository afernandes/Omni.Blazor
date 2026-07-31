using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Services;
using BunitTestContext = Bunit.BunitContext;

namespace Omni.Blazor.Tests;

/// <summary>
/// Shared base for every Omni.Blazor component test. Provides a bUnit
/// <c>BunitContext</c> pre-wired with Omni.Blazor's services (BreakpointService,
/// ScrollManager, etc.) plus JSInterop set to loose mode so JS calls don't
/// throw — components under test exercise their C# render paths without a
/// real browser.
///
/// The alias keeps the existing test base name independent from bUnit's
/// concrete context type.
/// </summary>
public abstract class TestContextBase : BunitTestContext
{
    protected TestContextBase()
    {
        // bUnit JSInterop: don't throw on unhandled invocations (we don't run
        // real JS in unit tests). Individual tests can still assert specific
        // JS calls via JSInterop.VerifyInvoke(...).
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register Omni.Blazor's DI surface — same shape as Program.cs in
        // consumer apps. Each component that injects one of these services
        // gets a real (test-scoped) instance, not a mock.
        Services.AddSingleton<BreakpointService>();
        Services.AddSingleton<ScrollManager>();
        Services.AddSingleton<FocusManager>();
        Services.AddSingleton<DataFormEditorRegistry>();
        Services.AddSingleton<ParallaxService>();
        Services.AddSingleton<DialogService>();
        Services.AddSingleton<HotkeyService>();
        Services.AddSingleton<KeyInterceptorService>();
        Services.AddSingleton<CommandHistoryService>();
        Services.AddSingleton<NotificationService>();
        Services.AddSingleton<TooltipService>();
        Services.AddSingleton<ContextMenuService>();
        Services.AddSingleton<TourService>();
        Services.AddSingleton<SignaturePadService>();
        Services.AddSingleton<FileDownloadService>();
        Services.AddSingleton<ClickOutsideService>();
        Services.AddSingleton<DataGridInteropService>();
    }
}
