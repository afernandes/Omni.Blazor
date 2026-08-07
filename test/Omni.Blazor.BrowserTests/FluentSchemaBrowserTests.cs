using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class FluentSchemaBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Fluent_schema_showcases_render_without_browser_errors()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        PageGotoOptions navigation = new() { WaitUntil = WaitUntilState.DOMContentLoaded };

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid", navigation);
        await page.Locator("input[placeholder='Buscar pedidos']").First.WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/scheduler", navigation);
        ILocator scheduler = page.Locator(".omni-scheduler").First;
        await scheduler.WaitForAsync();
        Assert.Contains("height:720px", await scheduler.GetAttributeAsync("style") ?? string.Empty);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/gantt", navigation);
        await page.GetByText("Projeto", new() { Exact = true }).First.WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/kanban", navigation);
        await page.GetByText("Mesa 12 — Combo", new() { Exact = true }).WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/chart", navigation);
        await page.Locator("svg[aria-label='Receita semanal']").WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/diagram", navigation);
        await page.Locator("[data-dgnode='review']").WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/entity-picker", navigation);
        await page.GetByTestId("entity-picker-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        await page.GetByTestId("entity-picker-local").Locator(".omni-entity-picker-trigger").ClickAsync();
        ILocator pickerHeader = page.GetByTestId("entity-picker-local").Locator("thead");
        await pickerHeader.WaitForAsync();
        Assert.Contains("Fornecedor", await pickerHeader.TextContentAsync());
        Assert.Contains("Cidade", await pickerHeader.TextContentAsync());

        Assert.Empty(errors);
    }
}
