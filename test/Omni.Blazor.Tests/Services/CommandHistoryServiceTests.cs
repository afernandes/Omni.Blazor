using System.Threading.Tasks;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

/// <summary>
/// In-memory behaviour of <see cref="CommandHistoryService"/> (storage is a
/// loose-mode no-op under bUnit, so this exercises the recency bookkeeping).
/// </summary>
public class CommandHistoryServiceTests : TestContextBase
{
    [Fact]
    public async Task Records_and_orders_by_recency()
    {
        var svc = new CommandHistoryService(JSInterop.JSRuntime);
        await svc.RecordAsync("k", "A");
        await svc.RecordAsync("k", "B");
        var map = await svc.LoadAsync("k");
        Assert.True(map["B"] > map["A"]);
    }

    [Fact]
    public async Task Unknown_namespace_loads_empty()
    {
        var svc = new CommandHistoryService(JSInterop.JSRuntime);
        Assert.Empty(await svc.LoadAsync("nope"));
    }

    [Fact]
    public async Task Re_recording_a_label_bumps_it_and_keeps_one_entry()
    {
        var svc = new CommandHistoryService(JSInterop.JSRuntime);
        await svc.RecordAsync("k", "A");
        await svc.RecordAsync("k", "B");
        await svc.RecordAsync("k", "A");
        var map = await svc.LoadAsync("k");
        Assert.Equal(2, map.Count);
        Assert.True(map["A"] > map["B"]);
    }

    [Fact]
    public async Task Clear_empties_the_namespace()
    {
        var svc = new CommandHistoryService(JSInterop.JSRuntime);
        await svc.RecordAsync("k", "A");
        await svc.ClearAsync("k");
        Assert.Empty(await svc.LoadAsync("k"));
    }

    [Fact]
    public async Task Null_or_empty_label_is_ignored()
    {
        var svc = new CommandHistoryService(JSInterop.JSRuntime);
        await svc.RecordAsync("k", null);
        await svc.RecordAsync("k", "");
        Assert.Empty(await svc.LoadAsync("k"));
    }
}
