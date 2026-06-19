using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Ai;
using Omni.Blazor.Components;
using Omni.Blazor.ManifestGen;

// =============================================================================
// Omni.Blazor manifest generator
//
// Reflects over the compiled Omni.Blazor assembly + its XML doc file and emits:
//   docs/components.json  — machine-readable manifest (source of truth)
//   llms.txt              — curated index (llmstxt.org format)
//   llms-full.txt         — full API dump for large-context agents
//
// The logic lives in testable helpers (Generator.cs, ManifestBuilder.cs); this
// file is just IO orchestration.
//
// Usage: dotnet run --project tools/Omni.Blazor.ManifestGen [-- <repoRoot>]
// repoRoot defaults to the nearest ancestor containing Omni.Blazor.slnx.
// =============================================================================

string repoRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : FindRepoRoot();
Console.WriteLine($"[manifest-gen] repo root: {repoRoot}");

// Reflect over both shipping component assemblies: the base library and the
// optional AI package (Omni.Blazor.Ai ships OmniAiConversation). XML doc files
// from both are merged so summaries resolve regardless of which package a type
// lives in.
Assembly[] assemblies = [typeof(OmniComponent).Assembly, typeof(OmniChatClient).Assembly];

Dictionary<string, string> docs = new(StringComparer.Ordinal);
foreach (Assembly a in assemblies)
{
    string xmlPath = Path.ChangeExtension(a.Location, ".xml");
    bool found = File.Exists(xmlPath);
    if (found)
        foreach (KeyValuePair<string, string> kv in XmlDocText.Load(xmlPath)) docs[kv.Key] = kv.Value;
    Console.WriteLine($"[manifest-gen] xml docs ({a.GetName().Name}): {(found ? "loaded" : "NOT FOUND (summaries will be empty)")}");
}

var (categoryByName, sourceByName, descByName) = ScanRazor(
    [Path.Combine(repoRoot, "src", "Omni.Blazor", "Components"),
     Path.Combine(repoRoot, "src", "Omni.Blazor.Ai", "Components")],
    repoRoot);

const string repository = "https://github.com/afernandes/Omni.Blazor";
List<ComponentInfo> components = [];
foreach (Assembly a in assemblies)
    components.AddRange(ManifestBuilder.Build(a, docs, categoryByName, sourceByName, descByName));
components = [.. components.OrderBy(c => c.Category, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal)];
Console.WriteLine($"[manifest-gen] components: {components.Count}");

string tokensScss = Path.Combine(repoRoot, "src", "Omni.Blazor", "Themes", "_tokens.scss");
string[] tokens = File.Exists(tokensScss) ? RazorScan.Tokens(File.ReadAllText(tokensScss)) : [];

// ---- Write components.json ----
var manifest = new Manifest("AndersonN.Omni.Blazor", repository, components.Count, components.ToArray());
string docsDir = Path.Combine(repoRoot, "docs");
Directory.CreateDirectory(docsDir);
var jsonOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
File.WriteAllText(Path.Combine(docsDir, "components.json"), JsonSerializer.Serialize(manifest, jsonOpts) + "\n");
Console.WriteLine("[manifest-gen] wrote docs/components.json");

// ---- Write llms.txt / llms-full.txt ----
File.WriteAllText(Path.Combine(repoRoot, "llms.txt"), Writers.LlmsIndex(components));
Console.WriteLine("[manifest-gen] wrote llms.txt");
File.WriteAllText(Path.Combine(repoRoot, "llms-full.txt"), Writers.LlmsFull(components, tokens));
Console.WriteLine("[manifest-gen] wrote llms-full.txt");

Console.WriteLine("[manifest-gen] done.");
return 0;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Omni.Blazor.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}

// Scan each Components/ root for category (first folder), source path and a
// fallback description (leading @* *@ comment). The pure parsing lives in
// RazorScan. Multiple roots (base lib + AI package) are merged by component name.
static (Dictionary<string, string> category, Dictionary<string, string> source, Dictionary<string, string> description)
    ScanRazor(IEnumerable<string> dirs, string repoRoot)
{
    var category = new Dictionary<string, string>(StringComparer.Ordinal);
    var source = new Dictionary<string, string>(StringComparer.Ordinal);
    var description = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (string dir in dirs)
    {
        if (!Directory.Exists(dir)) continue;

        foreach (string f in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
        {
            string name = Path.GetFileNameWithoutExtension(f);
            string rel = Path.GetRelativePath(dir, f).Replace('\\', '/');
            string[] parts = rel.Split('/');
            // first folder under Components/, or "Other" for a file directly under it
            category[name] = parts.Length > 1 ? parts[0] : "Other";
            source[name] = Path.GetRelativePath(repoRoot, f).Replace('\\', '/');
            try
            {
                if (RazorScan.LeadComment(File.ReadAllText(f)) is { } desc) description[name] = desc;
            }
            catch { /* unreadable file — skip its description */ }
        }
    }
    return (category, source, description);
}
