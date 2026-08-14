using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class LocalizationBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Culture_scopes_localize_components_and_emit_language_direction_metadata()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/localization");

        ILocator frenchScope = page.Locator(".omni-culture-scope[lang='fr-FR']");
        await frenchScope.WaitForAsync();
        Assert.Equal("ltr", await frenchScope.GetAttributeAsync("dir"));
        Assert.Equal(
            "Fermer",
            await frenchScope.Locator("button.omni-alert-close").GetAttributeAsync("aria-label"));
        Assert.Equal(
            "Ouvrir le calendrier",
            await frenchScope.Locator("button.omni-datepicker-trigger").GetAttributeAsync("aria-label"));

        ILocator arabicScope = page.Locator(".omni-culture-scope[lang='ar-SA']");
        await arabicScope.WaitForAsync();
        Assert.Equal("rtl", await arabicScope.GetAttributeAsync("dir"));
        Assert.Equal(
            "فتح التقويم",
            await arabicScope.Locator("button.omni-datepicker-trigger").GetAttributeAsync("aria-label"));
    }

    [Fact]
    public async Task Global_culture_switch_persists_pseudo_locale_and_updates_document_direction()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/showcase/localization");

        ILocator selector = page.GetByTestId("global-culture-selector");
        await selector.Locator(".omni-select-trigger").ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Pseudo — RTL", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => document.documentElement.lang === 'ar-XB'");
        await page.GetByTestId("current-culture").WaitForAsync();

        Assert.Equal("ar-XB", await page.Locator("html").GetAttributeAsync("lang"));
        Assert.Equal("rtl", await page.Locator("html").GetAttributeAsync("dir"));
        Assert.Contains("CurrentUICulture: ar-XB", await page.GetByTestId("current-culture").InnerTextAsync());
        Assert.Contains(
            "⟦",
            await page.Locator(".omni-culture-scope[lang='ar-XB'] button.omni-alert-close")
                .GetAttributeAsync("aria-label"));

        string[] overflowingElements = await page.EvaluateAsync<string[]>(
            """
            () => Array.from(document.body.querySelectorAll('*'))
                .filter(element => element instanceof HTMLElement && element.offsetParent !== null)
                .filter(element => !element.matches('.omni-skip-link:not(:focus)'))
                .filter(element => {
                    const rect = element.getBoundingClientRect();
                    return rect.left < -1 || rect.right > document.documentElement.clientWidth + 1;
                })
                .slice(0, 10)
                .map(element => `${element.tagName.toLowerCase()}.${element.className}`)
            """);
        Assert.Empty(overflowingElements);
    }
}
