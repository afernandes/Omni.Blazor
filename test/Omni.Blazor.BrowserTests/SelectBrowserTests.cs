using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class SelectBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Paged_provider_load_more_action_uses_the_shared_select_style()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/select");
        await page.GetByTestId("select-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        ILocator sectionTitle = page.Locator(".omni-section-title")
            .Filter(new() { HasTextString = "SELECT — PROVIDER PAGINADO" });
        await sectionTitle.WaitForAsync();

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Categoria remota..." }).ClickAsync();

        ILocator option = page.Locator("button.omni-select-option").First;
        ILocator loadMore = page.Locator("button.omni-select-load-more");
        await loadMore.WaitForAsync();

        Assert.Equal("none", await loadMore.EvaluateAsync<string>(
            "element => getComputedStyle(element).appearance"));
        Assert.Equal("flex", await loadMore.EvaluateAsync<string>(
            "element => getComputedStyle(element).display"));

        double optionWidth = await option.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().width");
        double actionWidth = await loadMore.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().width");
        double actionHeight = await loadMore.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().height");

        Assert.InRange(Math.Abs(actionWidth - optionWidth), 0, 1);
        Assert.True(actionHeight >= 34, $"Expected a 34px action row, but found {actionHeight}px.");
        Assert.Equal("solid", await loadMore.EvaluateAsync<string>(
            "element => getComputedStyle(element).borderTopStyle"));

        string backgroundBeforeHover = await loadMore.EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");
        await loadMore.HoverAsync();
        string backgroundAfterHover = await loadMore.EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");
        Assert.NotEqual(backgroundBeforeHover, backgroundAfterHover);
    }
}
