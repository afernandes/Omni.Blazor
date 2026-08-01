using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

public sealed class BrowserInteropServiceTests : TestContextBase
{
    [Fact]
    public async Task ClickOutside_handle_unregisters_exactly_once()
    {
        var service = Services.GetRequiredService<ClickOutsideService>();
        using var receiver = DotNetObjectReference.Create(new Receiver());

        var handle = await service.RegisterAsync(
            "test-id",
            "[data-test]",
            receiver,
            nameof(Receiver.OnOutside),
            Xunit.TestContext.Current.CancellationToken);

        Assert.NotNull(handle);
        await handle.DisposeAsync();
        await handle.DisposeAsync();

        Assert.Single(JSInterop.Invocations,
            invocation => invocation.Identifier == "omniBlazor.registerClickOutside");
        Assert.Single(JSInterop.Invocations,
            invocation => invocation.Identifier == "omniBlazor.unregisterClickOutside");
    }

    [Fact]
    public async Task FileDownload_does_not_take_ownership_of_caller_stream()
    {
        var service = Services.GetRequiredService<FileDownloadService>();
        await using var stream = new MemoryStream([1, 2, 3]);

        await service.DownloadAsync(
            "dados.csv",
            stream,
            "text/csv",
            Xunit.TestContext.Current.CancellationToken);

        Assert.True(stream.CanRead);
        JSInterop.VerifyInvoke("omniBlazor.downloadStream");
    }

    private sealed class Receiver
    {
        [JSInvokable]
        public Task OnOutside() => Task.CompletedTask;
    }
}
