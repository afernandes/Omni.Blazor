using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Omni.Blazor.Localization;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all Omni.Blazor services as scoped instances.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddOmniComponents(this IServiceCollection services)
        => AddOmniComponents(services, configure: null);

    /// <summary>
    /// Register all Omni.Blazor services as scoped instances and apply startup options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Optional startup configuration. Use it to translate the library in one place:
    /// <code>services.AddOmniComponents(o => o.Texts = OmniTexts.English());</code>
    /// Components still honour a per-instance <c>[Parameter]</c> first; this only replaces
    /// the built-in (pt-BR) defaults. Registering your own <see cref="OmniTexts"/> before
    /// calling this — e.g. scoped, populated from an <c>IStringLocalizer</c> — takes
    /// precedence over <paramref name="configure"/>.
    /// </param>
    public static IServiceCollection AddOmniComponents(this IServiceCollection services, Action<OmniOptions>? configure)
    {
        var options = new OmniOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options.Texts);
        services.TryAddScoped<IOmniCoreJsModule>(static provider => new OmniCoreJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniScrollJsModule>(static provider => new OmniScrollJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniResponsiveJsModule>(static provider => new OmniResponsiveJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniOverlayJsModule>(static provider => new OmniOverlayJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniInputsJsModule>(static provider => new OmniInputsJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniNavigationJsModule>(static provider => new OmniNavigationJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniSpeechJsModule>(static provider => new OmniSpeechJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniDataJsModule>(static provider => new OmniDataJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniDisplayJsModule>(static provider => new OmniDisplayJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        services.TryAddScoped<IOmniDiagramJsModule>(static provider => new OmniDiagramJsModule(
            provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped(static provider => new ContextMenuInteropService(provider.GetRequiredService<IOmniOverlayJsModule>()));
        services.AddScoped(static provider => new ThemeService(
            provider.GetRequiredService<IOmniCoreJsModule>(),
            provider.GetRequiredService<IOmniResponsiveJsModule>()));
        services.AddScoped(static provider => new HotkeyService(provider.GetRequiredService<IOmniNavigationJsModule>()));
        services.AddScoped(static provider => new CommandHistoryService(provider.GetRequiredService<IOmniCoreJsModule>()));
        services.AddScoped(static provider => new ScrollManager(provider.GetRequiredService<IOmniScrollJsModule>()));
        services.AddScoped(static provider => new FocusManager(provider.GetRequiredService<IOmniCoreJsModule>()));
        services.AddScoped<DataFormEditorRegistry>();
        services.AddScoped(static provider => new BreakpointService(provider.GetRequiredService<IOmniResponsiveJsModule>()));
        services.AddScoped(static provider => new ParallaxService(provider.GetRequiredService<IOmniDisplayJsModule>()));
        services.AddScoped(static provider => new KeyInterceptorService(provider.GetRequiredService<IOmniNavigationJsModule>()));
        services.AddScoped(static provider => new TourService(
            provider.GetRequiredService<IOmniCoreJsModule>(),
            provider.GetService<Microsoft.Extensions.Logging.ILogger<TourService>>()));
        services.AddScoped(static provider => new SignaturePadService(provider.GetRequiredService<IOmniDisplayJsModule>()));
        services.AddScoped(static provider => new FileDownloadService(provider.GetRequiredService<IOmniCoreJsModule>()));
        services.AddScoped(static provider => new ClipboardService(provider.GetRequiredService<IOmniCoreJsModule>()));
        services.AddScoped(static provider => new ClickOutsideService(provider.GetRequiredService<IOmniOverlayJsModule>()));
        services.AddScoped(static provider => new DataGridInteropService(provider.GetRequiredService<IOmniDataJsModule>()));
        services.AddScoped(static provider => new DataGridStateStorageService(provider.GetRequiredService<IOmniCoreJsModule>()));
        services.TryAddScoped<IDataGridFormPolicyEvaluator, AspNetCoreDataGridFormPolicyEvaluator>();
        return services;
    }

    /// <summary>
    /// Registers a conventional custom editor for an exact DataForm value type.
    /// The editor must expose Value, ValueChanged and ValueExpression parameters.
    /// </summary>
    public static IServiceCollection AddOmniDataFormEditor<TValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TComponent>(
        this IServiceCollection services)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new DataFormEditorRegistration(typeof(TValue), typeof(TComponent)));
        return services;
    }

    /// <summary>
    /// Registers a scoped property-aware DataForm editor resolver. Resolvers are
    /// evaluated in reverse registration order before exact value-type mappings.
    /// </summary>
    public static IServiceCollection AddOmniDataFormEditorResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResolver>(
        this IServiceCollection services)
        where TResolver : class, IDataFormEditorResolver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDataFormEditorResolver, TResolver>();
        return services;
    }

    /// <summary>Replaces the default ASP.NET Core policy bridge used by OmniDataGridForm.</summary>
    public static IServiceCollection AddOmniDataGridFormPolicyEvaluator<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEvaluator>(
        this IServiceCollection services)
        where TEvaluator : class, IDataGridFormPolicyEvaluator
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Replace(ServiceDescriptor.Scoped<IDataGridFormPolicyEvaluator, TEvaluator>());
        return services;
    }
}
