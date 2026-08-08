using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

/// <summary>
/// Walk every client route of the published showcase and fail on any unhandled
/// component exception.
///
/// This exists because a published WebAssembly app is a different program from the one
/// the other suites exercise: the library sets IsAotCompatible, so publishing trims its
/// members, and code that works in development can throw only there. The first such
/// break (anonymous types in JS interop payloads losing their constructor parameter
/// names) reached production and was found one page at a time by a human. A per-page
/// sweep turns that whole class of bug into a build failure.
///
/// Run it against the published artifact — see the Pages workflow, which points
/// OMNI_BROWSER_STATIC_ROOT at the trimmed publish output.
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
        Assert.True(routes.Length > 50, $"Only {routes.Length} routes were discovered; the sweep is not covering the showcase.");

        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        List<string> current = [];
        page.Console += (_, message) =>
        {
            // Blazor reports a render failure through console.error, so the page keeps
            // responding to clicks and nothing else marks it as broken.
            if (message.Type == "error")
                lock (current) current.Add(message.Text);
        };
        page.PageError += (_, error) => { lock (current) current.Add(error); };

        IResponse? boot = await page.GotoAsync($"{fixture.BaseUrl}/");
        Assert.NotNull(boot);
        await page.WaitForFunctionAsync("() => !!window.Blazor");

        List<string> broken = [];
        foreach (string route in routes)
        {
            lock (current) current.Clear();

            await page.EvaluateAsync("route => Blazor.navigateTo(route, false, false)", $"{fixture.BaseUrl}{route}");

            // Pages that load their data asynchronously throw well after the first render,
            // so settle long enough to see it — a short wait silently passes those routes.
            await page.WaitForTimeoutAsync(900);

            string[] failures;
            lock (current) failures = [.. current];
            if (failures.Length > 0)
                broken.Add($"{route} -> {Collapse(failures[0])}");
        }

        Assert.True(
            broken.Count == 0,
            $"{broken.Count} of {routes.Length} routes failed to render:{Environment.NewLine}"
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

        return [.. routes.OrderBy(route => route, StringComparer.Ordinal)];
    }

    private static string Collapse(string message)
    {
        string single = Regex.Replace(message, @"\s+", " ").Trim();
        return single.Length <= 500 ? single : single[..500];
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
