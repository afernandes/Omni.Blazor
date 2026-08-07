using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class DataGridFormBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Local_crud_and_collection_grid_complete_through_the_generated_data_forms()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-grid-form");
        await page.GetByTestId("data-grid-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        ILocator standalone = page.GetByTestId("data-grid-form-standalone");
        await standalone.GetByRole(AriaRole.Button, new() { Name = "Adicionar", Exact = true }).ClickAsync();
        ILocator editor = standalone.GetByRole(AriaRole.Dialog, new() { Name = "Novo cliente" });
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Nome" }).FillAsync("Cliente browser");
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "E-mail" }).FillAsync("browser@exemplo.com");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();

        ILocator createdRow = standalone.Locator("tbody tr").Filter(
            new LocatorFilterOptions { HasTextString = "Cliente browser" });
        await createdRow.WaitForAsync();
        await createdRow.GetByRole(AriaRole.Button, new() { Name = "Editar", Exact = true }).ClickAsync();
        editor = standalone.GetByRole(AriaRole.Dialog, new() { Name = "Editar — Cliente browser" });
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Nome" }).FillAsync("Cliente browser editado");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();

        ILocator editedRow = standalone.Locator("tbody tr").Filter(
            new LocatorFilterOptions { HasTextString = "Cliente browser editado" });
        await editedRow.WaitForAsync();
        await editedRow.GetByRole(AriaRole.Button, new() { Name = "Mais ações", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Remover", Exact = true }).ClickAsync();
        ILocator confirmation = standalone.GetByRole(AriaRole.Alertdialog);
        await confirmation.GetByText("Excluir o cliente Cliente browser editado?").WaitForAsync();
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
        await editedRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        ILocator collection = page.GetByTestId("data-grid-form-collection");
        await collection.GetByRole(AriaRole.Button, new() { Name = "Adicionar", Exact = true }).ClickAsync();
        editor = collection.GetByRole(AriaRole.Dialog, new() { Name = "Novo contato" });
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Tipo" }).FillAsync("E-mail");
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Contato" }).FillAsync("contato@exemplo.com");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();
        await collection.Locator("tbody tr").Filter(
            new LocatorFilterOptions { HasTextString = "contato@exemplo.com" }).WaitForAsync();

        Assert.Empty(errors);
    }

    [Fact]
    public async Task DataGridForm_showcase_has_no_axe_violations()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-grid-form");
        await page.GetByTestId("data-grid-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator scope = page.GetByTestId("data-grid-form-standalone");
        await scope.WaitForAsync();

        ILocator actionsHeader = scope.Locator("thead th").Filter(
            new LocatorFilterOptions { HasTextString = "Ações" });
        Assert.Equal(1, await actionsHeader.Locator(".omni-grid-resizer").CountAsync());
        Assert.Contains("omni-grid-frozen-right", await actionsHeader.GetAttributeAsync("class") ?? string.Empty);
        Assert.Equal("sticky", await actionsHeader.EvaluateAsync<string>("element => getComputedStyle(element).position"));

        ILocator selectionCells = scope.Locator(".omni-grid-th-select, .omni-grid-td-select");
        Assert.All(await selectionCells.AllTextContentsAsync(), text =>
            Assert.True(string.IsNullOrWhiteSpace(text)));
        Assert.All(
            await selectionCells.EvaluateAllAsync<string[]>(
                "cells => cells.map(cell => getComputedStyle(cell).textOverflow)"),
            textOverflow => Assert.Equal("clip", textOverflow));
        Assert.True(await selectionCells.EvaluateAllAsync<bool>(
            "cells => cells.every(cell => {" +
            " const input = cell.querySelector(\"input[type='checkbox']\");" +
            " if (!input) return true;" +
            " const cellRect = cell.getBoundingClientRect();" +
            " const inputRect = input.getBoundingClientRect();" +
            " return inputRect.left >= cellRect.left && inputRect.right <= cellRect.right;" +
            " })"));

        AssertNoAxeViolations(await scope.RunAxe());

        await scope.GetByRole(AriaRole.Button, new() { Name = "Adicionar", Exact = true }).ClickAsync();
        ILocator editor = scope.GetByRole(AriaRole.Dialog, new() { Name = "Novo cliente" });
        AssertNoAxeViolations(await editor.RunAxe());
        await editor.GetByRole(AriaRole.Button, new() { Name = "Cancelar", Exact = true }).ClickAsync();

        ILocator moreActions = scope.GetByRole(AriaRole.Button, new() { Name = "Mais ações", Exact = true }).First;
        await moreActions.ClickAsync();
        ILocator actionsMenu = page.Locator(".omni-context-menu");
        await actionsMenu.WaitForAsync();
        Assert.Equal("true", await moreActions.GetAttributeAsync("aria-expanded"));
        Assert.True(await actionsMenu.EvaluateAsync<bool>(
            "menu => {" +
            " const m = menu.getBoundingClientRect();" +
            " const trigger = document.querySelector('button[aria-expanded=\"true\"]');" +
            " if (!trigger) return false;" +
            " const t = trigger.getBoundingClientRect();" +
            " return m.left < t.right && m.right > t.left && m.top > 4 && m.left > 4;" +
            "}"));
        await page.WaitForFunctionAsync(
            "() => document.activeElement?.getAttribute('role') === 'menuitem'");
        Assert.Equal(
            "Arquivar",
            await page.EvaluateAsync<string>("document.activeElement?.getAttribute('aria-label')"));
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal(
            "Remover",
            await page.EvaluateAsync<string>("document.activeElement?.getAttribute('aria-label')"));
        await page.Keyboard.PressAsync("Escape");
        await actionsMenu.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.True(await moreActions.EvaluateAsync<bool>("button => document.activeElement === button"));
        Assert.Equal("false", await moreActions.GetAttributeAsync("aria-expanded"));

        await moreActions.ClickAsync();
        await actionsMenu.WaitForAsync();
        AssertNoAxeViolations(await actionsMenu.RunAxe());
        await actionsMenu.GetByRole(AriaRole.Menuitem, new() { Name = "Remover", Exact = true }).ClickAsync();
        ILocator confirmation = scope.GetByRole(AriaRole.Alertdialog);
        AssertNoAxeViolations(await confirmation.RunAxe());
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Cancelar", Exact = true }).ClickAsync();
    }

    [Fact]
    public async Task Server_provider_crud_typed_validation_and_bulk_action_are_executable()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> errors = [];
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/data-grid-form");
        await page.GetByTestId("data-grid-form-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator provider = page.GetByTestId("data-grid-form-provider");
        await provider.GetByText("Alice Provider", new() { Exact = true }).WaitForAsync();

        await provider.GetByRole(AriaRole.Button, new() { Name = "Adicionar", Exact = true }).ClickAsync();
        ILocator editor = provider.GetByRole(AriaRole.Dialog, new() { Name = "Novo cliente" });
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Nome" }).FillAsync("Cliente remoto");
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "E-mail" }).FillAsync("remoto@exemplo.com");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();
        await provider.Locator("tbody tr").Filter(
            new LocatorFilterOptions { HasTextString = "Cliente remoto" }).WaitForAsync();

        await provider.GetByRole(AriaRole.Button, new() { Name = "Adicionar", Exact = true }).ClickAsync();
        editor = provider.GetByRole(AriaRole.Dialog, new() { Name = "Novo cliente" });
        await editor.GetByRole(AriaRole.Textbox, new() { Name = "Nome" }).FillAsync("Alice Provider");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();
        await provider.GetByText("Já existe um cliente com este nome.", new() { Exact = true }).WaitForAsync();
        await editor.GetByRole(AriaRole.Button, new() { Name = "Cancelar", Exact = true }).ClickAsync();
        ILocator discard = provider.GetByRole(AriaRole.Alertdialog, new() { Name = "Descartar alterações?" });
        await discard.GetByRole(AriaRole.Button, new() { Name = "Descartar alterações", Exact = true }).ClickAsync();

        await provider.Locator("tbody input[type='checkbox']").First.CheckAsync();
        await provider.GetByRole(AriaRole.Button, new() { Name = "Processar selecionados", Exact = true }).ClickAsync();
        ILocator bulk = provider.GetByRole(AriaRole.Alertdialog, new() { Name = "Confirmar" });
        await bulk.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
        await provider.GetByText("1 selecionado(s)", new() { Exact = true }).WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

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
                    string.Join(Environment.NewLine, violation.Nodes.Select(node =>
                        $"{node.Html}{Environment.NewLine}" +
                        string.Join(Environment.NewLine, node.Any.Concat(node.All).Concat(node.None)
                            .Select(check => check.Message)))))));
    }
}
