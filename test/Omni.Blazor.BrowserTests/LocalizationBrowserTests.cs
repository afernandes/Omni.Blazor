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
}
