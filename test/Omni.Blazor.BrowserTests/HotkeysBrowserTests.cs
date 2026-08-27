using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class HotkeysBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Direct_load_waits_for_hydration_before_registering_hotkeys()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        IResponse? response = await page.GotoAsync(
            $"{fixture.BaseUrl}/showcase/hotkeys",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Direct load returned HTTP {response.Status}.");

        await page.GetByRole(AriaRole.Heading, new() { Name = "Hotkeys", Exact = true }).WaitForAsync();
        await BrowserFixture.WaitForNavigationFocusAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Verificar", Exact = true }).ClickAsync();

        ILocator registrationCount = page
            .Locator(".omni-meta", new PageLocatorOptions { HasTextString = "C# RegistrationCount:" })
            .Locator("strong");

        // Four registrations belong to this page; the showcase layout owns the fifth.
        await Assertions.Expect(registrationCount).ToHaveTextAsync("5");
    }
}
