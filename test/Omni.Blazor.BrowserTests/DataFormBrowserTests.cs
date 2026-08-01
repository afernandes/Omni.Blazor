using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class DataFormBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Labels_and_validation_summary_focus_the_real_editor()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-form");
        await page.GetByTestId("data-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator form = page.Locator(".omni-data-form").First;
        await form.Locator("input[name='Nome']").WaitForAsync();

        ILocator label = form.Locator("label.omni-field-label").Filter(new LocatorFilterOptions
        {
            HasTextString = "Nome"
        }).First;
        string? targetId = await label.GetAttributeAsync("for");
        Assert.False(string.IsNullOrWhiteSpace(targetId));
        await label.ClickAsync();
        Assert.Equal(targetId, await page.EvaluateAsync<string>("document.activeElement?.id"));

        await form.GetByRole(AriaRole.Button, new() { Name = "Salvar cliente" }).ClickAsync();
        ILocator summaryLink = form.Locator(".omni-data-form-validation-link").First;
        await summaryLink.WaitForAsync();
        await summaryLink.ClickAsync();
        await page.WaitForFunctionAsync(
            "expected => document.activeElement?.id === expected",
            targetId);
        Assert.Equal(targetId, await page.EvaluateAsync<string>("document.activeElement?.id"));
    }

    [Fact]
    public async Task Container_layout_and_keyboard_lookup_follow_the_local_width_and_dependencies()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-form");
        await page.GetByTestId("data-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("data-form-browser");
        await scope.WaitForAsync();

        string columns = await scope.Locator(".omni-data-form-grid").First
            .EvaluateAsync<string>("element => getComputedStyle(element).gridTemplateColumns");
        Assert.Single(columns.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        ILocator country = scope.Locator(".omni-select-trigger").Nth(0);
        await country.ClickAsync();
        await scope.GetByRole(AriaRole.Option, new() { Name = "Argentina" }).WaitForAsync();
        await country.FocusAsync();
        await country.PressAsync("ArrowDown");
        await scope.Locator(".omni-select-option.omni-active")
            .Filter(new LocatorFilterOptions { HasTextString = "Argentina" })
            .WaitForAsync();
        await country.PressAsync("Enter");
        await scope.GetByRole(AriaRole.Option, new() { Name = "Argentina" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        ILocator state = scope.Locator(".omni-select-trigger").Nth(1);
        await state.ClickAsync();
        await scope.GetByRole(AriaRole.Option, new() { Name = "Buenos Aires" }).WaitForAsync();
    }

    [Fact]
    public async Task DataForm_advanced_scope_has_no_axe_violations()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-form");
        await page.GetByTestId("data-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("data-form-browser");
        await scope.WaitForAsync();

        AxeResult result = await scope.RunAxe();

        Assert.True(
            result.Violations is null || !result.Violations.Any(),
            result.Violations is null
                ? string.Empty
                : string.Join(Environment.NewLine, result.Violations.Select(violation =>
                    $"{violation.Id}: {violation.HelpUrl}")));
    }

    [Fact]
    public async Task Rapid_model_replacement_and_navigation_do_not_surface_unobserved_browser_errors()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-form");
        await page.GetByTestId("data-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("data-form-browser");
        await scope.WaitForAsync();

        await scope.Locator(".omni-select-trigger").Nth(1).ClickAsync();
        await scope.GetByTestId("replace-data-form-model").ClickAsync();
        await scope.GetByTestId("replace-data-form-model").ClickAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/button");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.Empty(errors);
    }
}
