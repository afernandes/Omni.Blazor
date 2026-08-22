using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class DataGridBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Flat_grid_moves_selection_and_dom_focus_with_arrow_keys_without_trapping_tab()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        CaptureRuntimeErrors(page, errors);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid");
        ILocator demo = page.GetByTestId("datagrid-keyboard-navigation");
        await demo.WaitForAsync();
        await Assertions.Expect(page.Locator(".omni-grid[aria-busy=\"true\"]")).ToHaveCountAsync(0);

        ILocator grids = demo.Locator("table[role=grid]");
        Assert.Equal(2, await grids.CountAsync());
        ILocator rows = grids.Nth(0).Locator("tbody tr[data-omni-grid-row-index]");
        Assert.Equal(6, await rows.CountAsync());
        ILocator first = rows.Nth(0);
        ILocator second = rows.Nth(1);
        string selectedClient = (await second.Locator("td").Nth(1).InnerTextAsync()).Trim();

        await first.PressAsync("ArrowDown");
        await Assertions.Expect(second).ToBeFocusedAsync();

        Assert.Equal("true", await second.GetAttributeAsync("aria-selected"));
        await demo.GetByText($"Selecionado: {selectedClient}", new() { Exact = true }).WaitForAsync();

        await second.PressAsync("Home");
        await Assertions.Expect(first).ToBeFocusedAsync();
        await first.PressAsync("Tab");
        Assert.False(await first.EvaluateAsync<bool>("row => document.activeElement === row"));
        AxeResult axe = await demo.RunAxe();
        Assert.True(
            axe.Violations is null || !axe.Violations.Any(),
            axe.Violations is null
                ? string.Empty
                : string.Join(Environment.NewLine, axe.Violations.Select(violation => violation.Description)));
        lock (errors)
            Assert.Empty(errors);
    }

    [Fact]
    public async Task Explicit_keyboard_selection_keeps_focus_and_selection_separate_until_enter()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        CaptureRuntimeErrors(page, errors);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid");
        ILocator demo = page.GetByTestId("datagrid-keyboard-navigation");
        await demo.WaitForAsync();
        await Assertions.Expect(page.Locator(".omni-grid[aria-busy=\"true\"]")).ToHaveCountAsync(0);

        ILocator grid = demo.Locator("table[role=grid]").Nth(1);
        ILocator rows = grid.Locator("tbody tr[data-omni-grid-row-index]");
        ILocator first = rows.Nth(0);
        ILocator second = rows.Nth(1);
        string selectedClient = (await second.Locator("td").Nth(1).InnerTextAsync()).Trim();

        await first.PressAsync("ArrowDown");
        await Assertions.Expect(second).ToBeFocusedAsync();

        Assert.Equal("false", await second.GetAttributeAsync("aria-selected"));
        await demo.GetByText("Confirmação explícita: nenhum", new() { Exact = true }).WaitForAsync();

        await second.PressAsync("Enter");
        await Assertions.Expect(second).ToHaveAttributeAsync("aria-selected", "true");
        await demo.GetByText($"Confirmação explícita: {selectedClient}", new() { Exact = true }).WaitForAsync();

        Assert.Equal("true", await second.GetAttributeAsync("aria-selected"));
        lock (errors)
            Assert.Empty(errors);
    }

    [Fact]
    public async Task Direct_route_survives_delayed_provider_and_a_second_interactive_render()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        CaptureRuntimeErrors(page, errors);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/datagrid");
        await page.GetByRole(AriaRole.Heading, new() { Name = "OmniDataGrid", Exact = true }).WaitForAsync();
        ILocator busyGrids = page.Locator(".omni-grid[aria-busy=\"true\"]");
        await Assertions.Expect(busyGrids).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ver código", Exact = true }).First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Ocultar código", Exact = true }).First.WaitForAsync();

        lock (errors)
            Assert.Empty(errors);
    }

    private static void CaptureRuntimeErrors(IPage page, List<string> errors)
    {
        page.PageError += (_, error) =>
        {
            lock (errors) errors.Add(error);
        };
        page.Console += (_, message) =>
        {
            if (message.Text.Contains("[MONO]", StringComparison.Ordinal))
                lock (errors) errors.Add(message.Text);
        };
    }
}
