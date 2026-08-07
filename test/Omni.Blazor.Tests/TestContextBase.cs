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
        TestJsModule jsModule = new(JSInterop.JSRuntime);
        Services.AddSingleton<IOmniCoreJsModule>(jsModule);
        Services.AddSingleton<IOmniScrollJsModule>(jsModule);
        Services.AddSingleton<IOmniResponsiveJsModule>(jsModule);
        Services.AddSingleton<IOmniOverlayJsModule>(jsModule);
        Services.AddSingleton<IOmniInputsJsModule>(jsModule);
        Services.AddSingleton<IOmniNavigationJsModule>(jsModule);
        Services.AddSingleton<IOmniSpeechJsModule>(jsModule);
        Services.AddSingleton<IOmniDataJsModule>(jsModule);
        Services.AddSingleton<IOmniDisplayJsModule>(jsModule);
        Services.AddSingleton<IOmniDiagramJsModule>(jsModule);

        // Register Omni.Blazor's DI surface — same shape as Program.cs in
        // consumer apps. Each component that injects one of these services
        // gets a real (test-scoped) instance, not a mock.
        Services.AddSingleton(static provider => new BreakpointService(provider.GetRequiredService<IOmniResponsiveJsModule>()));
        Services.AddSingleton(static provider => new ScrollManager(provider.GetRequiredService<IOmniScrollJsModule>()));
        Services.AddSingleton(static provider => new FocusManager(provider.GetRequiredService<IOmniCoreJsModule>()));
        Services.AddSingleton<DataFormEditorRegistry>();
        Services.AddSingleton(static provider => new ParallaxService(provider.GetRequiredService<IOmniDisplayJsModule>()));
        Services.AddSingleton<DialogService>();
        Services.AddSingleton(static provider => new HotkeyService(provider.GetRequiredService<IOmniNavigationJsModule>()));
        Services.AddSingleton(static provider => new KeyInterceptorService(provider.GetRequiredService<IOmniNavigationJsModule>()));
        Services.AddSingleton(static provider => new CommandHistoryService(provider.GetRequiredService<IOmniCoreJsModule>()));
        Services.AddSingleton<NotificationService>();
        Services.AddSingleton<TooltipService>();
        Services.AddSingleton<ContextMenuService>();
        Services.AddSingleton(static provider => new ContextMenuInteropService(provider.GetRequiredService<IOmniOverlayJsModule>()));
        Services.AddSingleton<IDataGridFormPolicyEvaluator>(
            new DelegateDataGridFormPolicyEvaluator((_, _) => ValueTask.FromResult(true)));
        Services.AddSingleton(static provider => new TourService(provider.GetRequiredService<IOmniCoreJsModule>()));
        Services.AddSingleton(static provider => new SignaturePadService(provider.GetRequiredService<IOmniDisplayJsModule>()));
        Services.AddSingleton(static provider => new FileDownloadService(provider.GetRequiredService<IOmniCoreJsModule>()));
        Services.AddSingleton(static provider => new ClipboardService(provider.GetRequiredService<IOmniCoreJsModule>()));
        Services.AddSingleton(static provider => new ClickOutsideService(provider.GetRequiredService<IOmniOverlayJsModule>()));
        Services.AddSingleton(static provider => new DataGridInteropService(provider.GetRequiredService<IOmniDataJsModule>()));
        Services.AddSingleton(static provider => new DataGridStateStorageService(provider.GetRequiredService<IOmniCoreJsModule>()));
    }
}
