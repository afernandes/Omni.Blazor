using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Ai;

/// <summary>DI helpers for the optional Omni.Blazor AI layer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OmniChatClient"/> as a transient service so it can be injected
    /// (<c>@inject OmniChatClient</c>) instead of constructed by hand — mirroring
    /// <c>AddOmniComponents()</c> for the base library.
    /// <para>
    /// Register an <see cref="IChatClient"/> first (e.g. <c>services.AddChatClient(...)</c>);
    /// it is resolved from DI, and a registered <see cref="OmniChatOptions"/> is used if present
    /// (otherwise defaults apply). For explicit per-conversation lifetime control you can still
    /// <c>new OmniChatClient(chatClient, options)</c> directly — both are supported.
    /// </para>
    /// </summary>
    public static IServiceCollection AddOmniAi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<OmniChatClient>();
        return services;
    }
}
