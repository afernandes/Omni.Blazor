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

        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<HotkeyService>();
        services.AddScoped<CommandHistoryService>();
        services.AddScoped<ScrollManager>();
        services.AddScoped<FocusManager>();
        services.AddScoped<DataFormEditorRegistry>();
        services.AddScoped<BreakpointService>();
        services.AddScoped<ParallaxService>();
        services.AddScoped<KeyInterceptorService>();
        services.AddScoped<TourService>();
        services.AddScoped<SignaturePadService>();
        services.AddScoped<FileDownloadService>();
        services.AddScoped<ClickOutsideService>();
        services.AddScoped<DataGridInteropService>();
        return services;
    }

    /// <summary>
    /// Registers a conventional custom editor for an exact DataForm value type.
    /// The editor must expose Value, ValueChanged and ValueExpression parameters.
    /// </summary>
    public static IServiceCollection AddOmniDataFormEditor<TValue, TComponent>(
        this IServiceCollection services)
        where TComponent : IComponent
        => AddOmniDataFormEditor(services, typeof(TValue), typeof(TComponent));

    /// <summary>
    /// Registers a conventional custom editor. An open component with one generic
    /// argument is closed with <paramref name="valueType"/> when resolved.
    /// </summary>
    public static IServiceCollection AddOmniDataFormEditor(
        this IServiceCollection services,
        Type valueType,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(valueType);
        ArgumentNullException.ThrowIfNull(componentType);
        services.AddSingleton(new DataFormEditorRegistration(valueType, componentType));
        return services;
    }

    /// <summary>
    /// Registers a scoped property-aware DataForm editor resolver. Resolvers are
    /// evaluated in reverse registration order before exact value-type mappings.
    /// </summary>
    public static IServiceCollection AddOmniDataFormEditorResolver<TResolver>(
        this IServiceCollection services)
        where TResolver : class, IDataFormEditorResolver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDataFormEditorResolver, TResolver>();
        return services;
    }
}
