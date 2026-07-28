using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Omni.Blazor.Localization;
using Omni.Blazor.Services;

namespace Omni.Blazor;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all Omni.Blazor services as scoped instances.
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
    public static IServiceCollection AddOmniComponents(this IServiceCollection services, Action<OmniOptions>? configure = null)
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
        services.AddScoped<BreakpointService>();
        services.AddScoped<ParallaxService>();
        services.AddScoped<KeyInterceptorService>();
        services.AddScoped<TourService>();
        return services;
    }
}
