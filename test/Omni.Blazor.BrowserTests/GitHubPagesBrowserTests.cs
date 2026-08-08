using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class GitHubPagesBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Base_path_preserves_landing_navigation_and_deep_routes()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);

        IResponse? landingResponse = await page.GotoAsync($"{fixture.BaseUrl}/");
        Assert.NotNull(landingResponse);
        Assert.True(landingResponse.Ok, $"Landing returned HTTP {landingResponse.Status}.");

        await page.GetByRole(AriaRole.Link, new() { Name = "Browse all components", Exact = true })
            .ClickAsync();
        await page.WaitForURLAsync($"{fixture.BaseUrl}/showcase");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Omni.Blazor", Exact = true })
            .WaitForAsync();

        IResponse? deepRouteResponse = await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid");
        Assert.NotNull(deepRouteResponse);
        Assert.True(
            deepRouteResponse.Ok || deepRouteResponse.Status == 404,
            $"Deep route returned unexpected HTTP {deepRouteResponse.Status}.");
        await page.Locator("input[placeholder='Buscar pedidos']").First.WaitForAsync();

        Assert.Empty(errors);
    }
}
