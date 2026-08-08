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

        int faviconStatus = await page.EvaluateAsync<int>(
            "async () => fetch(document.querySelector('link[rel=\"icon\"]')?.href ?? '').then(response => response.status)");
        Assert.Equal(200, faviconStatus);

        await page.GetByRole(AriaRole.Link, new() { Name = "Browse all components", Exact = true })
            .ClickAsync();
        await page.WaitForURLAsync($"{fixture.BaseUrl}/showcase");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Omni.Blazor", Exact = true })
            .WaitForAsync();

        // Deep links must answer 200, not the 404 that a bare `404.html` fallback returns:
        // every client route is pre-rendered as its own .html by prepare_github_pages.py.
        IResponse? deepRouteResponse = await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid");
        Assert.NotNull(deepRouteResponse);
        Assert.True(
            deepRouteResponse.Ok,
            $"Deep route returned HTTP {deepRouteResponse.Status}; it should be pre-rendered and answer 200.");
        await page.Locator("input[placeholder='Buscar pedidos']").First.WaitForAsync();

        // An unknown route still has to fall back to the app shell (and may answer 404).
        IResponse? unknownRouteResponse = await page.GotoAsync($"{fixture.BaseUrl}/showcase/does-not-exist");
        Assert.NotNull(unknownRouteResponse);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Omni.Blazor", Exact = true })
            .WaitForAsync();

        Assert.Empty(errors);
    }
}
