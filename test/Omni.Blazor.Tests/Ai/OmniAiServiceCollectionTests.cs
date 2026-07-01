using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Ai;

namespace Omni.Blazor.Tests.Ai;

public class OmniAiServiceCollectionTests
{
    [Fact]
    public void AddOmniAi_registers_a_resolvable_OmniChatClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChatClient>(new FakeChatClient("ok"));
        services.AddOmniAi();
        using var provider = services.BuildServiceProvider();

        var client = provider.GetService<OmniChatClient>();
        Assert.NotNull(client);
        Assert.Empty(client!.Turns);
    }

    [Fact]
    public void AddOmniAi_returns_the_same_collection()
    {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddOmniAi());
    }

    [Fact]
    public void AddOmniAi_null_services_throws()
        => Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddOmniAi());
}
