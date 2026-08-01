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

    public async ValueTask InitializeAsync()
    {
        string repositoryRoot = FindRepositoryRoot();
        string configuration = Environment.GetEnvironmentVariable("OMNI_BROWSER_CONFIGURATION") ?? "Release";
        int port = ReservePort();
        BaseUrl = $"http://127.0.0.1:{port}";

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
        startInfo.ArgumentList.Add("src/Forneria.Demo/Forneria.Demo/Forneria.Demo.csproj");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(BaseUrl);
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

    public async Task<IBrowserContext> CreateContextAsync()
    {
        ObjectDisposedException.ThrowIf(_browser is null, this);
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            ReducedMotion = ReducedMotion.Reduce
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
                    $"{BaseUrl}/showcase/data-form",
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
    public const string Name = "DataForm browser";
}
