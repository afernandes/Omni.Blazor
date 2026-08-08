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

        // The demo's own stylesheet has to be linked by the host page, not just published.
        // Assert the effect, not the file: .omni-showcase-body owns `overflow:auto`, and the
        // library clips .omni-split-main, so losing it leaves the page content unreachable.
        string showcaseOverflow = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('.omni-showcase-body')).overflowY");
        Assert.Equal("auto", showcaseOverflow);

        // The agent-facing catalog ships with the site, so an agent can read it over HTTP
        // with nothing installed. Nothing in the app links these, so only a check keeps them.
        foreach (string asset in (string[])["llms.txt", "llms-full.txt", "components.json"])
        {
            IAPIResponse assetResponse = await page.APIRequest.GetAsync($"{fixture.BaseUrl}/{asset}");
            Assert.True(assetResponse.Ok, $"{asset} returned HTTP {assetResponse.Status}; it must ship with the site.");
        }

        // An unknown route still has to fall back to the app shell, which boots and lets the
        // router render its own not-found page (the response status may legitimately be 404).
        IResponse? unknownRouteResponse = await page.GotoAsync($"{fixture.BaseUrl}/showcase/does-not-exist");
        Assert.NotNull(unknownRouteResponse);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Page not found", Exact = true })
            .WaitForAsync();

        Assert.Empty(errors);
    }
}
