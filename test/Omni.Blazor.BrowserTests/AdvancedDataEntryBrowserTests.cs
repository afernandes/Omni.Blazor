using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdvancedDataEntryBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Numeric_filters_invalid_keys_without_relying_on_browser_globals()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/numeric");
        await WaitForNumericInteractivityAsync(page);
        ILocator input = page.Locator(".omni-numeric-input").First;
        await input.WaitForAsync();
        await input.FocusAsync();
        await input.PressAsync("Control+A");
        await input.PressAsync("Backspace");
        await input.PressSequentiallyAsync("fgh1");

        Assert.Equal("1", await input.InputValueAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Numeric_auto_decimal_separator_keeps_the_configured_fixed_scale()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/numeric");
        await WaitForNumericInteractivityAsync(page);

        ILocator twoDecimals = page.GetByTestId("numeric-auto-2").Locator("input");
        await twoDecimals.WaitForAsync();
        await ClearAsync(twoDecimals);
        await twoDecimals.PressSequentiallyAsync("123");
        Assert.Equal("1,23", await twoDecimals.InputValueAsync());
        await twoDecimals.PressAsync("Backspace");
        Assert.Equal("0,12", await twoDecimals.InputValueAsync());
        await twoDecimals.BlurAsync();
        Assert.Equal("0,12", await twoDecimals.InputValueAsync());

        await ClearAsync(twoDecimals);
        await twoDecimals.PressSequentiallyAsync("١٢٣");
        Assert.Equal("1,23", await twoDecimals.InputValueAsync());

        ILocator threeDecimals = page.GetByTestId("numeric-auto-3").Locator("input");
        await ClearAsync(threeDecimals);
        await threeDecimals.PressSequentiallyAsync("1234");
        Assert.Equal("1,234", await threeDecimals.InputValueAsync());

        ILocator fourDecimals = page.GetByTestId("numeric-auto-4").Locator("input");
        await ClearAsync(fourDecimals);
        await fourDecimals.PressSequentiallyAsync("12345");
        Assert.Equal("1,2345", await fourDecimals.InputValueAsync());

        Assert.Empty(errors);
    }

    private static async Task ClearAsync(ILocator input)
    {
        await input.FocusAsync();
        await input.PressAsync("Control+A");
        await input.PressAsync("Backspace");
    }

    private static Task WaitForNumericInteractivityAsync(IPage page) =>
        page.GetByTestId("numeric-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

    [Fact]
    public async Task Typed_data_filter_serializes_and_updates_the_effective_query()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/datafilter");
        await page.GetByTestId("data-filter-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator filter = page.GetByTestId("typed-data-filter");
        await filter.WaitForAsync();

        Assert.Equal(3, await filter.Locator(".omni-datafilter-condition").CountAsync());
        string sectionText = await filter
            .Locator("xpath=ancestor::section[contains(@class,'omni-section')][1]")
            .TextContentAsync() ?? string.Empty;
        Assert.Contains("\"version\":1", sectionText, StringComparison.Ordinal);
        Assert.Contains("\"field\":\"active\"", sectionText, StringComparison.Ordinal);

        ILocator ageCondition = filter.Locator(".omni-datafilter-condition").Nth(2);
        ILocator ageInput = ageCondition.Locator(".omni-numeric-input");
        await ageInput.FillAsync("50");
        await ageInput.BlurAsync();
        await page.WaitForTimeoutAsync(250);
        string resultText = await page.GetByTestId("typed-data-filter-result").InnerTextAsync();
        Assert.Equal("Resultado tipado: 2 de 8", string.Join(' ', resultText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries)));

        AssertNoAxeViolations(await filter.RunAxe());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task EntityPicker_selects_a_local_entity_and_dialog_is_accessible()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/entity-picker");
        await page.GetByTestId("entity-picker-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("entity-picker-local");

        await scope.GetByRole(AriaRole.Button, new() { Name = "Café Aurora — Campinas" }).ClickAsync();
        ILocator dialog = scope.GetByRole(AriaRole.Dialog, new() { Name = "Selecionar fornecedor" });
        await dialog.WaitForAsync();
        AssertNoAxeViolations(await dialog.RunAxe());
        await dialog.Locator("tbody tr")
            .Filter(new LocatorFilterOptions { HasTextString = "Padaria Central" })
            .ClickAsync();

        await scope.GetByRole(AriaRole.Button, new() { Name = "Padaria Central — São Paulo" }).WaitForAsync();
        Assert.Contains("41000000-0000-0000-0000-000000000002", await scope.TextContentAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task DataFormWizard_validates_and_reacts_to_conditional_steps()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-form-wizard");
        await page.GetByTestId("data-form-wizard-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator wizard = page.GetByTestId("data-form-wizard");

        await wizard.GetByRole(AriaRole.Button, new() { Name = "Próximo", Exact = true }).ClickAsync();
        await wizard.Locator(".omni-data-form-validation-summary").WaitForAsync();
        await wizard.Locator("input[name='Nome']").FillAsync("Ana Browser");
        await wizard.Locator("input[name='Email']").FillAsync("ana@exemplo.com");
        await wizard.GetByRole(AriaRole.Button, new() { Name = "Próximo", Exact = true }).ClickAsync();
        await wizard.GetByRole(AriaRole.Heading, new() { Name = "Perfil", Exact = true }).WaitForAsync();
        await wizard.Locator("input[name='PessoaJuridica']").CheckAsync();
        await wizard.GetByRole(AriaRole.Button, new() { Name = "Próximo", Exact = true }).ClickAsync();
        await wizard.GetByRole(AriaRole.Heading, new() { Name = "Empresa", Exact = true }).WaitForAsync();

        AssertNoAxeViolations(await wizard.RunAxe());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task DataImport_previews_validation_and_imports_only_valid_rows()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-import");
        await page.GetByTestId("data-import-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("data-import");

        await scope.GetByRole(AriaRole.Button, new() { Name = "Carregar arquivo de exemplo", Exact = true }).ClickAsync();
        await scope.GetByText("Pré-visualização validada", new() { Exact = true }).WaitForAsync();
        await scope.GetByText(
            "1 linha válida, 1 linha inválida, 2 linhas no total",
            new() { Exact = true }).WaitForAsync();
        AssertNoAxeViolations(await scope.RunAxe());
        await scope.GetByRole(AriaRole.Button, new() { Name = "Importar dados", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Status)
            .GetByText("1 produto(s) importado(s); 1 linha(s) rejeitada(s).", new() { Exact = true })
            .WaitForAsync();

        Assert.Empty(errors);
    }

    private static void AssertNoAxeViolations(AxeResult result)
    {
        Assert.True(
            result.Violations is null || !result.Violations.Any(),
            result.Violations is null
                ? string.Empty
                : string.Join(Environment.NewLine, result.Violations.Select(violation =>
                    $"{violation.Id}: {violation.HelpUrl}{Environment.NewLine}" +
                    string.Join(Environment.NewLine, violation.Nodes.Select(node => node.Html)))));
    }
}
