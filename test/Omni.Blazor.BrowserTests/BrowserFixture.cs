using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

public sealed class BrowserFixture : IAsyncLifetime
{
    private readonly List<string> _serverOutput = [];
    private Process? _server;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// True when serving the prepared GitHub Pages artifact instead of running a host.
    /// Some things only exist in that artifact — the workflow assembles them.
    /// </summary>
    public bool ServesPreparedSite { get; private set; }

    public async ValueTask InitializeAsync()
    {
        string repositoryRoot = FindRepositoryRoot();
        string configuration = Environment.GetEnvironmentVariable("OMNI_BROWSER_CONFIGURATION") ?? "Release";
        string hostMode = Environment.GetEnvironmentVariable("OMNI_BROWSER_HOST") ?? "server";
        string pathBase = NormalizePathBase(Environment.GetEnvironmentVariable("OMNI_BROWSER_PATH_BASE"));
        string? staticRoot = Environment.GetEnvironmentVariable("OMNI_BROWSER_STATIC_ROOT");
        ServesPreparedSite = !string.IsNullOrWhiteSpace(staticRoot);
        string project = hostMode.Equals("wasm", StringComparison.OrdinalIgnoreCase)
            ? "src/Forneria.Demo/Forneria.Demo.Wasm/Forneria.Demo.Wasm.csproj"
            : "src/Forneria.Demo/Forneria.Demo/Forneria.Demo.csproj";
        int port = ReservePort();
        string origin = $"http://127.0.0.1:{port}";
        BaseUrl = $"{origin}{pathBase}";

        ProcessStartInfo startInfo = string.IsNullOrWhiteSpace(staticRoot)
            ? CreateDotNetHost(repositoryRoot, project, configuration, origin)
            : CreateStaticHost(repositoryRoot, staticRoot, pathBase, port);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_NOLOGO"] = "true";

        _server = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _server.OutputDataReceived += CaptureServerOutput;
        _server.ErrorDataReceived += CaptureServerOutput;
        if (!_server.Start()) throw new InvalidOperationException("The showcase server did not start.");
        _server.BeginOutputReadLine();
        _server.BeginErrorReadLine();

        try
        {
            await WaitForServerAsync();
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }
        catch
        {
            try { await DisposeAsync(); }
            catch { /* Preserve the initialization exception. */ }
            throw;
        }
    }

    private static ProcessStartInfo CreateDotNetHost(
        string repositoryRoot,
        string project,
        string configuration,
        string origin)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(project);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(origin);
        return startInfo;
    }

    private static ProcessStartInfo CreateStaticHost(
        string repositoryRoot,
        string staticRoot,
        string pathBase,
        int port)
    {
        ProcessStartInfo startInfo = new("python")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("scripts/serve_github_pages.py");
        startInfo.ArgumentList.Add("--directory");
        startInfo.ArgumentList.Add(Path.GetFullPath(staticRoot, repositoryRoot));
        startInfo.ArgumentList.Add("--path-base");
        startInfo.ArgumentList.Add(pathBase);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return startInfo;
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        ObjectDisposedException.ThrowIf(_browser is null, this);
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            ReducedMotion = ReducedMotion.Reduce,
            // Library strings resolve against the browser's UI culture now, so without a
            // fixed locale every assertion on one of them depends on the agent: the CI
            // Chromium is en-US and rendered "Confirm" where the suite expects "Confirmar".
            // Pinning it keeps these tests about behaviour rather than about language.
            Locale = "pt-BR"
        });
        context.SetDefaultTimeout(15_000);
        return context;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_browser is not null) await _browser.DisposeAsync();
        }
        finally
        {
            _playwright?.Dispose();
            try
            {
                if (_server is { HasExited: false })
                {
                    _server.Kill(entireProcessTree: true);
                    await _server.WaitForExitAsync();
                }
            }
            finally
            {
                if (_server is not null)
                {
                    _server.OutputDataReceived -= CaptureServerOutput;
                    _server.ErrorDataReceived -= CaptureServerOutput;
                    _server.Dispose();
                }
            }
        }
    }

    private async Task WaitForServerAsync()
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(2) };
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        while (!timeout.IsCancellationRequested)
        {
            if (_server?.HasExited == true)
                throw new InvalidOperationException($"Showcase server exited early.{Environment.NewLine}{GetServerOutput()}");
            try
            {
                using HttpResponseMessage response = await client.GetAsync(
                    $"{BaseUrl}/",
                    timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                // The host is still starting.
            }
            await Task.Delay(200, timeout.Token);
        }
        throw new TimeoutException($"Showcase server did not become ready.{Environment.NewLine}{GetServerOutput()}");
    }

    private string GetServerOutput()
    {
        lock (_serverOutput) return string.Join(Environment.NewLine, _serverOutput);
    }

    private void CaptureServerOutput(object sender, DataReceivedEventArgs args)
    {
        if (args.Data is null) return;
        lock (_serverOutput)
        {
            if (_serverOutput.Count == 200) _serverOutput.RemoveAt(0);
            _serverOutput.Add(args.Data);
        }
    }

    private static int ReservePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string NormalizePathBase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        string pathBase = value.Trim();
        if (pathBase.Contains('?', StringComparison.Ordinal) || pathBase.Contains('#', StringComparison.Ordinal))
            throw new InvalidOperationException("OMNI_BROWSER_PATH_BASE cannot contain a query or fragment.");
        pathBase = pathBase.Trim('/');
        return pathBase.Length == 0 ? string.Empty : $"/{pathBase}";
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Omni.Blazor repository root.");
    }
}

[CollectionDefinition(Name)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "Omni showcase browser";
}
