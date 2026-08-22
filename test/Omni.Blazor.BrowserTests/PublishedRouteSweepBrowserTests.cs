using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

/// <summary>
/// Walk every client route of the WebAssembly showcase and fail on any unhandled
/// component exception during SPA navigation, direct loading, or reload.
///
/// This exists because a published WebAssembly app is a different program from the one
/// the other suites exercise: the library sets IsAotCompatible, so publishing trims its
/// members, and code that works in development can throw only there. The first such
/// break (anonymous types in JS interop payloads losing their constructor parameter
/// names) reached production and was found one page at a time by a human. A per-page
/// sweep turns that whole class of bug into a build failure.
///
/// Run it against the development host with OMNI_BROWSER_HOST=wasm, or against the
/// published artifact by pointing OMNI_BROWSER_STATIC_ROOT at the trimmed output.
/// </summary>
[Collection(BrowserCollection.Name)]
public sealed class PublishedRouteSweepBrowserTests(BrowserFixture fixture)
{
    private static readonly Regex PageDirective = new(
        "^\\s*@page\\s+\"(?<route>[^\"]+)\"",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public async Task Every_route_renders_without_an_unhandled_component_exception()
    {
        string[] routes = DiscoverRoutes();
        AssertRouteCoverage(routes);

        await using IBrowserContext context = await fixture.CreateContextAsync();
        List<string> broken = [];
        TrackedPage? tracked = null;
        foreach (string route in routes)
        {
            try
            {
                tracked ??= await CreateTrackedPageAsync(context, "/");
                string[] startupFailures = tracked.DrainFailures();
                if (startupFailures.Length > 0)
                {
                    broken.Add($"{route} [bootstrap] -> {Collapse(startupFailures[0])}");
                    await tracked.DisposeAsync();
                    tracked = null;
                    continue;
                }

                await tracked.Page.EvaluateAsync(
                    "route => Blazor.navigateTo(route, false, false)",
                    $"{fixture.BaseUrl}{route}");

                // Pages that load their data asynchronously throw well after the first render,
                // so settle long enough to see it — a short wait silently passes those routes.
                await tracked.Page.WaitForTimeoutAsync(900);

                string[] failures = tracked.DrainFailures();
                if (failures.Length == 0) continue;

                broken.Add($"{route} -> {Collapse(failures[0])}");
                await tracked.DisposeAsync();
                tracked = null;
            }
            catch (Exception exception)
            {
                broken.Add($"{route} -> {Collapse(exception.Message)}");
                if (tracked is not null) await tracked.DisposeAsync();
                tracked = null;
            }
        }
        if (tracked is not null) await tracked.DisposeAsync();

        Assert.True(
            broken.Count == 0,
            $"{broken.Count} of {routes.Length} routes failed to render:{Environment.NewLine}"
                + string.Join(Environment.NewLine, broken));
    }

    [Fact]
    public async Task Every_route_supports_direct_load_and_reload()
    {
        string[] routes = DiscoverRoutes();
        AssertRouteCoverage(routes);

        await using IBrowserContext context = await fixture.CreateContextAsync();
        List<string> broken = [];
        foreach (string route in routes)
        {
            await using TrackedPage tracked = await CreateTrackedPageAsync(context, route);

            string[] directFailures = tracked.DrainFailures();
            if (directFailures.Length > 0)
            {
                broken.Add($"{route} [direct] -> {Collapse(directFailures[0])}");
                continue;
            }

            try
            {
                IResponse? response = await tracked.Page.ReloadAsync(
                    new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                if (response is null || !response.Ok)
                {
                    broken.Add($"{route} [reload] -> HTTP {response?.Status.ToString() ?? "no response"}");
                    continue;
                }

                await WaitForBlazorAsync(tracked.Page);
                string[] reloadFailures = tracked.DrainFailures();
                if (reloadFailures.Length > 0)
                    broken.Add($"{route} [reload] -> {Collapse(reloadFailures[0])}");
            }
            catch (Exception exception)
            {
                broken.Add($"{route} [reload] -> {Collapse(exception.Message)}");
            }
        }

        Assert.True(
            broken.Count == 0,
            $"{broken.Count} of {routes.Length} routes failed direct loading or reload:{Environment.NewLine}"
                + string.Join(Environment.NewLine, broken));
    }

    /// <summary>Read the routes from the sources, so the sweep cannot drift from the app.</summary>
    private static string[] DiscoverRoutes()
    {
        string root = FindRepositoryRoot();
        string[] sources =
        [
            Path.Combine(root, "src", "Forneria.Demo", "Forneria.Demo.Pages"),
            Path.Combine(root, "src", "Forneria.Demo", "Forneria.Demo.Wasm"),
        ];

        HashSet<string> routes = [];
        foreach (string source in sources.Where(Directory.Exists))
        {
            foreach (string razor in Directory.EnumerateFiles(source, "*.razor", SearchOption.AllDirectories))
            {
                foreach (Match match in PageDirective.Matches(File.ReadAllText(razor)))
                {
                    string route = match.Groups["route"].Value;
                    // A parameterised route has no enumerable instance to visit.
                    if (!route.Contains('{'))
                        routes.Add(route);
                }
            }
        }

        string[] discovered = [.. routes.OrderBy(route => route, StringComparer.Ordinal)];
        string? configured = Environment.GetEnvironmentVariable("OMNI_BROWSER_ROUTES");
        if (string.IsNullOrWhiteSpace(configured)) return discovered;

        HashSet<string> requested = configured
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. discovered.Where(requested.Contains)];
    }

    private static void AssertRouteCoverage(string[] routes)
    {
        string? configured = Environment.GetEnvironmentVariable("OMNI_BROWSER_ROUTES");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.True(routes.Length > 50, $"Only {routes.Length} routes were discovered; the sweep is not covering the showcase.");
        else
            Assert.NotEmpty(routes);
    }

    private async Task<TrackedPage> CreateTrackedPageAsync(IBrowserContext context, string route)
    {
        IPage page = await context.NewPageAsync();
        TrackedPage tracked = new(page);
        try
        {
            IResponse? response = await page.GotoAsync(
                $"{fixture.BaseUrl}{route}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            if (response is null || !response.Ok)
                tracked.AddFailure($"HTTP {response?.Status.ToString() ?? "no response"}");
            await WaitForBlazorAsync(page);
            return tracked;
        }
        catch (Exception exception)
        {
            tracked.AddFailure($"Blazor did not finish bootstrapping this route: {exception.Message}");
            return tracked;
        }
    }

    private static async Task WaitForBlazorAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => !!window.Blazor");
        await page.WaitForTimeoutAsync(900);
    }

    private static string Collapse(string message)
    {
        string single = Regex.Replace(message, @"\s+", " ").Trim();
        return single.Length <= 500 ? single : single[..500];
    }

    private sealed class TrackedPage : IAsyncDisposable
    {
        private readonly List<string> _failures = [];

        public TrackedPage(IPage page)
        {
            Page = page;
            page.Console += CaptureConsole;
            page.PageError += CapturePageError;
        }

        public IPage Page { get; }

        public void AddFailure(string failure)
        {
            lock (_failures) _failures.Add(failure);
        }

        public string[] DrainFailures()
        {
            lock (_failures)
            {
                string[] snapshot = [.. _failures];
                _failures.Clear();
                return snapshot;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Page.Console -= CaptureConsole;
            Page.PageError -= CapturePageError;
            await Page.CloseAsync();
        }

        private void CaptureConsole(object? sender, IConsoleMessage message)
        {
            // Mono Debug logs the first assertion line as a warning in some browsers,
            // then exits the runtime. Capture it even when it is not console.error.
            if (message.Type == "error" || message.Text.Contains("[MONO]", StringComparison.OrdinalIgnoreCase))
                AddFailure(message.Text);
        }

        private void CapturePageError(object? sender, string error) => AddFailure(error);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
