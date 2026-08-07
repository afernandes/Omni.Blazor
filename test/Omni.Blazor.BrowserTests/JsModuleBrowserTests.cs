using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class JsModuleBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Feature_modules_are_loaded_lazily_and_shared_dependency_is_reused()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/test/js-modules");
        await page.GetByTestId("module-probe-status").WaitForAsync();

        string[] initialModules = await GetModuleResourcesAsync(page);
        Assert.Empty(initialModules);

        _ = await page.EvaluateAsync<bool>(
            """
            async () => {
                const module = await import('/_content/Omni.Blazor/js/modules/omni-display.js');
                return module.invoke('parallax.supportsNative', []);
            }
            """);

        string[] displayModules = await GetModuleResourcesAsync(page);
        Assert.Equal(1, Count(displayModules, "omni-display.js"));
        Assert.Equal(1, Count(displayModules, "omni-module.js"));
        Assert.Equal(0, Count(displayModules, "omni-scroll.js"));

        _ = await page.EvaluateAsync<double>(
            """
            async () => {
                const module = await import('/_content/Omni.Blazor/js/modules/omni-scroll.js');
                return module.invoke('scrollOffsetY', ['auto']);
            }
            """);

        string[] loadedModules = await GetModuleResourcesAsync(page);
        Assert.Equal(1, Count(loadedModules, "omni-display.js"));
        Assert.Equal(1, Count(loadedModules, "omni-scroll.js"));
        Assert.Equal(1, Count(loadedModules, "omni-module.js"));
        Assert.DoesNotContain(loadedModules, path => path.EndsWith("/js/Omni.js", StringComparison.Ordinal));
    }

    private static Task<string[]> GetModuleResourcesAsync(IPage page)
        => page.EvaluateAsync<string[]>(
            """
            () => performance.getEntriesByType('resource')
                .map(entry => new URL(entry.name).pathname)
                .filter(path => path.includes('/_content/Omni.Blazor/js/modules/'))
            """);

    private static int Count(IEnumerable<string> paths, string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        return paths.Count(path =>
        {
            string loadedFileName = Path.GetFileName(path);
            return loadedFileName.Equals(fileName, StringComparison.Ordinal) ||
                   loadedFileName.StartsWith($"{stem}.", StringComparison.Ordinal) &&
                   loadedFileName.EndsWith(".js", StringComparison.Ordinal);
        });
    }
}
