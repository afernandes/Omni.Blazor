using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omni.Blazor.Mcp;

// Stdio MCP server. Tools (OmniCatalogTools) are discovered by attribute and the
// ComponentCatalog they depend on is resolved from DI. The manifest is the one
// embedded at build time unless --manifest <path> / OMNI_COMPONENTS_JSON points
// at a freshly generated docs/components.json (handy in this repo during dev).

var builder = Host.CreateApplicationBuilder(args);

// Stdio carries the JSON-RPC protocol on stdout — every log MUST go to stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

string? manifestPath = GetOption(args, "--manifest") ?? Environment.GetEnvironmentVariable("OMNI_COMPONENTS_JSON");
builder.Services.AddSingleton(ComponentCatalog.Load(manifestPath));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

static string? GetOption(string[] args, string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
