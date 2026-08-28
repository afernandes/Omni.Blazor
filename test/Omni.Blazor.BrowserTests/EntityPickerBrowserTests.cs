using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

/// <summary>
/// Covers the browser's native keyboard activation after an Entity Picker selection.
/// bUnit can observe the component state change, but it cannot reproduce the default
/// Enter action that runs after Blazor's keydown handler returns.
/// </summary>
[Collection(BrowserCollection.Name)]
public sealed class EntityPickerBrowserTests(BrowserFixture fixture)
{
    [Theory]
    [InlineData("entity-picker-local", "Fazenda Verde — Jundiaí")]
    [InlineData("entity-picker-provider", "MEL-030 — Mel orgânico")]
    public async Task Enter_selects_the_cursor_row_without_reopening_the_picker(
        string testId,
        string expectedSelection)
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/entity-picker");
        await page.GetByTestId("entity-picker-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await BrowserFixture.WaitForNavigationFocusAsync(page);

        ILocator example = page.GetByTestId(testId);
        ILocator trigger = example.Locator("button.omni-entity-picker-trigger");
        await trigger.ClickAsync();

        ILocator panel = example.Locator(".omni-entity-picker-panel");
        ILocator search = panel.Locator("input[role='combobox']");
        await search.WaitForAsync();
        await Assertions.Expect(search).ToBeFocusedAsync();
        await panel.Locator("tr[data-omni-grid-cursor='true']").WaitForAsync();

        // End marks a different final row in both showcase sources. Enter selects it and
        // closes the panel while focus returns to the trigger. In WebAssembly, the native
        // default action used to click that newly-focused button in the same keydown and
        // immediately reopen the dialog/drawer.
        await search.PressAsync("End");
        await search.PressAsync("Enter");

        await Assertions.Expect(panel).ToHaveCountAsync(0);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger).ToContainTextAsync(expectedSelection);
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }
}
