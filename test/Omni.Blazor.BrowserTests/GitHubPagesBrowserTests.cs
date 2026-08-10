using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class GitHubPagesBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Confirm_dialog_is_activatable_in_the_published_WebAssembly()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.Console += (_, message) =>
        {
            if (message.Type == "error") errors.Add(message.Text);
        };
        page.PageError += (_, error) => errors.Add(error);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/dialog");
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar simples", Exact = true })
            .ClickAsync();

        ILocator dialog = page.Locator(".omni-dialog");
        await dialog.WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true })
            .ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        Assert.Empty(errors);
    }

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

        // An unknown route still has to fall back to the app shell, which boots and lets the
        // router render its own not-found page (the response status may legitimately be 404).
        IResponse? unknownRouteResponse = await page.GotoAsync($"{fixture.BaseUrl}/showcase/does-not-exist");
        Assert.NotNull(unknownRouteResponse);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Page not found", Exact = true })
            .WaitForAsync();

        Assert.Empty(errors);
    }

    /// <summary>
    /// The agent-facing catalog ships with the site, so an agent can read it over HTTP with
    /// nothing installed. The Pages workflow copies these in, so this only holds against the
    /// prepared artifact — a plain host run has nothing to check, and says so rather than
    /// passing quietly. Nothing in the app links them; without this they can vanish unnoticed.
    /// </summary>
    [Fact]
    public async Task Agent_catalog_ships_with_the_prepared_site()
    {
        if (!fixture.ServesPreparedSite)
            Assert.Skip("Only the prepared GitHub Pages artifact carries the catalog files.");

        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        foreach (string asset in (string[])["llms.txt", "llms-full.txt", "components.json"])
        {
            IAPIResponse response = await page.APIRequest.GetAsync($"{fixture.BaseUrl}/{asset}");
            Assert.True(response.Ok, $"{asset} returned HTTP {response.Status}; it must ship with the site.");
        }
    }
}
