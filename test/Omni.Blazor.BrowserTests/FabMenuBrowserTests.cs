using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

/// <summary>
/// Pins the interplay between OmniFabMenu's JS outside-click dismissal and a consumer
/// control that lives outside the menu. bUnit cannot reach this: the dismissal only
/// exists as a document-level listener registered through JS interop, so the two
/// reactions to a single physical click never meet in a bUnit render tree.
/// </summary>
[Collection(BrowserCollection.Name)]
public sealed class FabMenuBrowserTests(BrowserFixture fixture)
{
    // The "CONTROLE PROGRAMÁTICO" section of /showcase/fab: an @bind-Open menu whose
    // Abrir/Fechar/Toggle buttons are a SIBLING of the menu, so every one of them is an
    // "outside click" as far as the dismissal listener is concerned.
    private const string ControlledMenu = ".omni-fab-menu[aria-label='Controlado externamente']";
    private const string StateReadout = ".fab-controlled-state strong";

    [Fact]
    public async Task External_toggle_button_flips_the_bound_state_on_every_click()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/fab");
        await page.GetByTestId("fab-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator menu = page.Locator(ControlledMenu);
        await menu.WaitForAsync();

        ILocator state = page.Locator(StateReadout);
        ILocator toggle = page.Locator(".fab-controlled-actions button")
            .Filter(new LocatorFilterOptions { HasTextString = "Toggle" });
        await Assertions.Expect(state).ToHaveTextAsync("FECHADO");

        await toggle.ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("ABERTO");
        await Assertions.Expect(menu.Locator(".omni-fab-trigger")).ToHaveAttributeAsync("aria-expanded", "true");

        // The regression only exists while the outside-click listener is armed, so wait for
        // it rather than assume it: OmniFabMenu registers it from OnAfterRenderAsync, after
        // the navigation module has been lazily imported. Without this the second click can
        // land before the listener exists and the test passes for the wrong reason.
        await WaitForOutsideClickListenerAsync(page);

        // The bug: the dismissal used to fire synchronously in the capture phase, writing
        // false into the consumer's bound field BEFORE its own @onclick ran. The consumer's
        // `_menuOpen = !_menuOpen` then read a value the user never saw and flipped it back
        // to true, so the second click on the same button was a net no-op.
        await toggle.ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("FECHADO");
        await Assertions.Expect(menu.Locator(".omni-fab-trigger")).ToHaveAttributeAsync("aria-expanded", "false");

        // A third and fourth click prove it keeps alternating rather than sticking.
        await toggle.ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("ABERTO");
        await WaitForOutsideClickListenerAsync(page);
        await toggle.ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("FECHADO");
    }

    [Fact]
    public async Task Clicking_genuinely_outside_still_dismisses_the_open_menu()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/fab");
        await page.GetByTestId("fab-interactive").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        ILocator menu = page.Locator(ControlledMenu);
        await menu.WaitForAsync();

        ILocator state = page.Locator(StateReadout);
        await page.Locator(".fab-controlled-actions button")
            .Filter(new LocatorFilterOptions { HasTextString = "Abrir" })
            .ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("ABERTO");
        await WaitForOutsideClickListenerAsync(page);

        // Deferring the dismissal must not disable it: inert page chrome carries no Blazor
        // handler, so nothing competes with it and the menu has to close.
        await page.Locator(".fab-log").ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("FECHADO");
    }

    /// <summary>
    /// Waits until the menu's document-level outside-click listener is registered.
    /// <c>__tvsFabMenu</c> is the per-element handler bag that <c>omni-navigation.js</c>
    /// documents as its registration key; if that name ever changes, update it here too.
    /// </summary>
    private static Task WaitForOutsideClickListenerAsync(IPage page) =>
        page.WaitForFunctionAsync(
            "selector => !!document.querySelector(selector)?.__tvsFabMenu?.clickHandler",
            ControlledMenu);
}
