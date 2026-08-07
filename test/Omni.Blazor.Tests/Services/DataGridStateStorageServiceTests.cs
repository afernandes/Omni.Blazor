using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

public sealed class DataGridStateStorageServiceTests : TestContextBase
{
    private static DataGridViewState State()
        => new(
            [new DataGridColumnViewState("Name", 0, "180px", true, FrozenPosition.Left)],
            [new SortDescriptor("Name", SortDirection.Ascending)],
            [new DataGridFilterViewState("Name", FilterOperator.Contains, "Ana")],
            [],
            "Ana");

    [Fact]
    public async Task Saves_with_a_namespaced_key_and_round_trips_source_generated_json()
    {
        DataGridStateStorageService service = Services.GetRequiredService<DataGridStateStorageService>();
        DataGridViewState expected = State();
        await service.SaveAsync("customers", expected, Xunit.TestContext.Current.CancellationToken);
        var invocation = JSInterop.VerifyInvoke("omniBlazor.storageSet");
        Assert.Equal("omni.grid.customers", invocation.Arguments[0]);
        string json = Assert.IsType<string>(invocation.Arguments[1]);
        JSInterop.Setup<string?>("omniBlazor.storageGet", "omni.grid.customers").SetResult(json);

        DataGridViewState? actual = await service.LoadAsync(
            "customers",
            Xunit.TestContext.Current.CancellationToken);

        Assert.NotNull(actual);
        Assert.Equal("Name", Assert.Single(actual.Columns).Property);
        Assert.Equal("Ana", Assert.Single(actual.Filters).Value);
        Assert.Equal("Ana", actual.Search);
    }

    [Fact]
    public async Task Malformed_browser_state_fails_closed_without_leaking_an_exception()
    {
        JSInterop.Setup<string?>("omniBlazor.storageGet", "omni.grid.corrupt").SetResult("{not-json");
        DataGridStateStorageService service = Services.GetRequiredService<DataGridStateStorageService>();

        DataGridViewState? state = await service.LoadAsync(
            "corrupt",
            Xunit.TestContext.Current.CancellationToken);

        Assert.Null(state);
    }
}
