using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class DemoExperienceBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Landing_presents_the_component_system_and_respects_reduced_motion()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync(fixture.BaseUrl);

        ILocator hero = page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Build Blazor products that feel finished."
        });
        await hero.WaitForAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Browse all components" }).WaitForAsync();

        Assert.Contains("Omni.Blazor", await page.TitleAsync(), StringComparison.Ordinal);
        Assert.Equal(0, await page.Locator(".demo-window").EvaluateAsync<int>(
            "element => element.getAnimations().length"));
        Assert.Equal(0, await page.Locator(".demo-live-dot").EvaluateAsync<int>(
            "element => element.getAnimations().length"));
        Assert.Equal(0, await page.GetByText("Forneria — PDV", new() { Exact = true }).CountAsync());

        AxeResult result = await page.Locator(".demo-landing").RunAxe();
        AssertNoAxeViolations(result);
    }

    [Fact]
    public async Task Showcase_uses_the_catalog_shell_instead_of_the_foodservice_navigation()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> runtimeErrors = [];
        page.Console += (_, message) =>
        {
            if (message.Type == "error" && message.Text.Contains("[MONO]", StringComparison.Ordinal))
                lock (runtimeErrors) runtimeErrors.Add(message.Text);
        };

        await page.GotoAsync($"{fixture.BaseUrl}/showcase");

        await page.GetByRole(AriaRole.Heading, new() { Name = "Omni.Blazor", Exact = true }).WaitForAsync();
        ILocator banner = page.GetByRole(AriaRole.Banner);
        await banner.WaitForAsync();

        Assert.True(await banner.GetByText("Omni.Blazor", new() { Exact = true }).IsVisibleAsync());
        Assert.True(await banner.GetByRole(AriaRole.Link, new() { Name = "Components" }).IsVisibleAsync());
        Assert.Equal(0, await banner.GetByRole(AriaRole.Link, new() { Name = "PDV", Exact = true }).CountAsync());
        Assert.Equal(0, await banner.GetByRole(AriaRole.Link, new() { Name = "Dashboard", Exact = true }).CountAsync());

        // The Mono Debug failure occurs after the first successful render. Keep the page alive
        // long enough to exercise a second interactive render instead of accepting a transient shell.
        await page.WaitForTimeoutAsync(2_000);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Filtrar componentes" }).FillAsync("culture");
        await page.GetByRole(AriaRole.Link, new() { Name = "Culture Picker Novo" }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Culture Picker", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Ver código", Exact = true }).First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Ocultar código", Exact = true }).WaitForAsync();

        lock (runtimeErrors)
            Assert.Empty(runtimeErrors);
    }

    private static void AssertNoAxeViolations(AxeResult result)
    {
        Assert.True(
            result.Violations is null || !result.Violations.Any(),
            result.Violations is null
                ? string.Empty
                : string.Join(Environment.NewLine, result.Violations.Select(violation =>
                    $"{violation.Id}: {violation.HelpUrl}{Environment.NewLine}{string.Join(Environment.NewLine, violation.Nodes.Select(node => $"  {string.Join(", ", node.Target)}"))}")));
    }
}
