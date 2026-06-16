using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Services;

namespace Omni.Blazor;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all Omni.Blazor services as scoped instances.
    /// </summary>
    public static IServiceCollection AddOmniComponents(this IServiceCollection services)
    {
        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<HotkeyService>();
        services.AddScoped<CommandHistoryService>();
        services.AddScoped<ScrollManager>();
        services.AddScoped<BreakpointService>();
        services.AddScoped<KeyInterceptorService>();
        return services;
    }
}
