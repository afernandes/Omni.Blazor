using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

// =============================================================================
// Omni.Blazor manifest generator
//
// Reflects over the compiled Omni.Blazor assembly + its XML doc file and emits:
//   docs/components.json  — machine-readable manifest (source of truth)
//   llms.txt              — curated index (llmstxt.org format)
//   llms-full.txt         — full API dump for large-context agents
//
// Usage: dotnet run --project tools/Omni.Blazor.ManifestGen [-- <repoRoot>]
// repoRoot defaults to the nearest ancestor containing Omni.Blazor.slnx.
// =============================================================================

string repoRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : FindRepoRoot();
Console.WriteLine($"[manifest-gen] repo root: {repoRoot}");

Assembly asm = typeof(OmniComponent).Assembly;
string xmlPath = Path.ChangeExtension(asm.Location, ".xml");
Dictionary<string, string> docs = LoadXmlDocs(xmlPath);
Console.WriteLine($"[manifest-gen] xml docs: {(File.Exists(xmlPath) ? $"{docs.Count} members" : "NOT FOUND (summaries will be empty)")}");

string componentsDir = Path.Combine(repoRoot, "src", "Omni.Blazor", "Components");
var (categoryByName, sourceByName, descByName) = ScanRazor(componentsDir, repoRoot);

// Read via the PE version resource, NOT GetCustomAttribute<T>: enumerating the
// assembly's attributes forces resolution of every attribute type, including the
// SassCompiler one (PrivateAssets=all → not copied here) which would throw.
string version = ReadVersion(asm.Location);
const string repository = "https://github.com/afernandes/Omni.Blazor";

Type baseType = typeof(OmniComponent);
List<ComponentInfo> components = [];

foreach (Type t in SafeGetTypes(asm))
{
    if (!t.IsClass || t.IsAbstract || !t.IsPublic) continue;
    if (!baseType.IsAssignableFrom(t)) continue;

    string simpleName = StripArity(t.Name);
    // Only real components: must have a matching .razor file. This filters base
    // classes, helpers and anything code-only, and reconciles the true count.
    if (!sourceByName.TryGetValue(simpleName, out string? source)) continue;

    bool isInput = IsFormInput(t);
    bool hasChildren = typeof(OmniComponentWithChildren).IsAssignableFrom(t);
    string baseLabel = isInput ? "FormComponent<T>" : hasChildren ? "OmniComponentWithChildren" : "OmniComponent";

    object? instance = TryInstantiate(t);

    List<ParamInfo> ps = [];
    foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
    {
        if (p.GetCustomAttribute<ParameterAttribute>() is null) continue;

        Type pt = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        (string kind, string? ctx) = Classify(pt);
        // context type is meaningful only for slots (the `Context` var of a
        // RenderFragment<T>); for events the payload is already in the type.
        string? contextType = kind == "slot" ? ctx : null;

        EnumVal[]? enumValues = null;
        if (kind == "parameter" && pt.IsEnum)
        {
            enumValues = pt.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => new EnumVal(f.Name, Doc(docs, $"F:{XmlId(pt)}.{f.Name}")))
                .ToArray();
        }

        string? def = null;
        if (kind == "parameter" && instance is not null && IsSimple(pt))
        {
            try { def = DefaultToString(p.GetValue(instance)); }
            catch { /* getter threw without DI — leave default unknown */ }
        }

        bool required = p.GetCustomAttribute<EditorRequiredAttribute>() is not null;
        string? inheritedFrom = p.DeclaringType is { } dt && dt != t ? StripArity(dt.Name) : null;
        string? summary = Doc(docs, $"P:{XmlId(p.DeclaringType!)}.{p.Name}");

        ps.Add(new ParamInfo(p.Name, kind, Friendly(p.PropertyType), contextType, enumValues, def, required, summary, inheritedFrom));
    }

    // Stable order: own params first (alpha), then inherited (alpha).
    ps = [.. ps.OrderBy(p => p.InheritedFrom is not null).ThenBy(p => p.Name, StringComparer.Ordinal)];

    components.Add(new ComponentInfo(
        Name: simpleName,
        Category: categoryByName.GetValueOrDefault(simpleName, "Other"),
        BaseType: baseLabel,
        IsInput: isInput,
        HasChildContent: hasChildren,
        // Prefer an explicit XML <summary>; fall back to the component's leading
        // `@* *@` comment (most components already have one). No backfill needed.
        Summary: Doc(docs, $"T:{XmlId(t)}") ?? descByName.GetValueOrDefault(simpleName),
        Source: source,
        Parameters: [.. ps]));
}

components = [.. components.OrderBy(c => c.Category, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal)];
Console.WriteLine($"[manifest-gen] components: {components.Count}");

// ---- Theme tokens (for llms-full.txt) ----
string[] tokens = ScanTokens(Path.Combine(repoRoot, "src", "Omni.Blazor", "Themes", "_tokens.scss"));

// ---- Write components.json ----
var manifest = new Manifest("AndersonN.Omni.Blazor", version, repository, components.Count, components.ToArray());
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
Console.WriteLine($"[manifest-gen] wrote docs/components.json");

// ---- Write llms.txt ----
File.WriteAllText(Path.Combine(repoRoot, "llms.txt"), BuildLlmsIndex(components, version));
Console.WriteLine("[manifest-gen] wrote llms.txt");

// ---- Write llms-full.txt ----
File.WriteAllText(Path.Combine(repoRoot, "llms-full.txt"), BuildLlmsFull(components, tokens, version));
Console.WriteLine("[manifest-gen] wrote llms-full.txt");

Console.WriteLine("[manifest-gen] done.");
return 0;

// =============================================================================
// Helpers
// =============================================================================

static string ReadVersion(string assemblyPath)
{
    try
    {
        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyPath);
        return (fvi.ProductVersion ?? fvi.FileVersion ?? "0.0.0").Split('+')[0];
    }
    catch { return "0.0.0"; }
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Omni.Blazor.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}

static IEnumerable<Type> SafeGetTypes(Assembly a)
{
    try { return a.GetTypes(); }
    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
}

static (Dictionary<string, string> category, Dictionary<string, string> source, Dictionary<string, string> description) ScanRazor(string dir, string repoRoot)
{
    var category = new Dictionary<string, string>(StringComparer.Ordinal);
    var source = new Dictionary<string, string>(StringComparer.Ordinal);
    var description = new Dictionary<string, string>(StringComparer.Ordinal);
    if (!Directory.Exists(dir)) return (category, source, description);

    foreach (string f in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
    {
        string name = Path.GetFileNameWithoutExtension(f);
        string rel = Path.GetRelativePath(dir, f).Replace('\\', '/');
        category[name] = rel.Split('/')[0];                 // first folder under Components/
        source[name] = Path.GetRelativePath(repoRoot, f).Replace('\\', '/');
        if (ExtractLeadComment(f) is { } desc) description[name] = desc;
    }
    return (category, source, description);
}

// Pull a one-sentence description from the leading `@* ... *@` Razor comment that
// most components carry, used as a fallback when no XML <summary> exists.
static string? ExtractLeadComment(string file)
{
    string text;
    try { text = File.ReadAllText(file); } catch { return null; }

    int start = text.IndexOf("@*", StringComparison.Ordinal);
    if (start < 0) return null;
    int markup = text.IndexOf('<');                          // first HTML element
    if (markup >= 0 && start > markup) return null;          // comment isn't at the top
    int end = text.IndexOf("*@", start + 2, StringComparison.Ordinal);
    if (end < 0) return null;

    var sb = new StringBuilder();
    foreach (string raw in text[(start + 2)..end].Split('\n'))
    {
        string l = raw.Trim().TrimStart('*').Trim();
        if (l.Length == 0) { if (sb.Length > 0) break; else continue; }
        if (l.StartsWith("Usage", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("Example", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith('<') || l.StartsWith('@')) break;
        sb.Append(l).Append(' ');
        if (l.EndsWith('.') || l.Contains(". ")) break;      // one sentence is enough
    }

    string desc = string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    int dot = desc.IndexOf(". ", StringComparison.Ordinal);
    if (dot > 0) desc = desc[..(dot + 1)];
    return desc.Length > 0 ? desc : null;
}

static string[] ScanTokens(string tokensScss)
{
    if (!File.Exists(tokensScss)) return [];
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var ordered = new List<string>();
    foreach (string line in File.ReadLines(tokensScss))
    {
        int i = line.IndexOf("--omni-", StringComparison.Ordinal);
        if (i < 0) continue;
        int colon = line.IndexOf(':', i);
        if (colon < 0) continue;
        string name = line[i..colon].Trim();
        // skip var() *references*, keep only declarations (name immediately before ':')
        if (name.Contains(' ') || name.Contains('(')) continue;
        if (seen.Add(name)) ordered.Add(name);
    }
    return [.. ordered];
}

static Dictionary<string, string> LoadXmlDocs(string path)
{
    var d = new Dictionary<string, string>(StringComparer.Ordinal);
    if (!File.Exists(path)) return d;
    XDocument doc = XDocument.Load(path);
    foreach (XElement m in doc.Descendants("member"))
    {
        string? name = m.Attribute("name")?.Value;
        XElement? summary = m.Element("summary");
        if (name is null || summary is null) continue;
        string text = FlattenXml(summary);
        if (text.Length > 0) d[name] = text;
    }
    return d;
}

// Flatten an XML doc element into readable single-line text, resolving <see cref>
// and <c> to their plain content and collapsing whitespace.
static string FlattenXml(XElement el)
{
    var sb = new StringBuilder();
    foreach (XNode node in el.DescendantNodes())
    {
        if (node is XText txt) sb.Append(txt.Value);
        else if (node is XElement e && e.Name.LocalName is "see" or "seealso" or "paramref" or "typeparamref")
        {
            string? cref = e.Attribute("cref")?.Value ?? e.Attribute("name")?.Value;
            if (cref is not null) sb.Append(SimplifyCref(cref));
        }
    }
    return string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

// Turn an XML doc cref (e.g. "M:Omni.Blazor...GroupByAsync(System.String)") into
// a readable member name ("GroupByAsync"): drop the "X:" doc-id prefix, any
// "(args)" signature, and the namespace/type qualification.
static string SimplifyCref(string cref)
{
    if (cref.Length > 1 && cref[1] == ':') cref = cref[2..];
    int paren = cref.IndexOf('(');
    if (paren >= 0) cref = cref[..paren];
    int dot = cref.LastIndexOf('.');
    return dot >= 0 ? cref[(dot + 1)..] : cref;
}

static string? Doc(Dictionary<string, string> docs, string id) => docs.GetValueOrDefault(id);

// XML doc id for a type: namespace.Name`arity, nested '+' → '.'.
static string XmlId(Type t) => (t.FullName ?? t.Name).Replace('+', '.');

static string StripArity(string name)
{
    int i = name.IndexOf('`');
    return i < 0 ? name : name[..i];
}

static bool IsFormInput(Type t)
{
    for (Type? b = t.BaseType; b is not null; b = b.BaseType)
        if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(FormComponent<>)) return true;
    return false;
}

static object? TryInstantiate(Type t)
{
    if (t.IsGenericTypeDefinition) return null;
    try { return Activator.CreateInstance(t); }
    catch { return null; }
}

static (string kind, string? context) Classify(Type t)
{
    if (t == typeof(EventCallback)) return ("event", null);
    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventCallback<>)) return ("event", Friendly(t.GetGenericArguments()[0]));
    if (t == typeof(RenderFragment)) return ("slot", null);
    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(RenderFragment<>)) return ("slot", Friendly(t.GetGenericArguments()[0]));
    return ("parameter", null);
}

static bool IsSimple(Type t) =>
    t.IsEnum || t == typeof(string) || t == typeof(bool) || t == typeof(decimal) ||
    (t.IsPrimitive && t != typeof(IntPtr) && t != typeof(UIntPtr));

static string? DefaultToString(object? v) => v switch
{
    null => null,
    bool b => b ? "true" : "false",
    Enum e => e.ToString(),
    string s => s,
    _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture),
};

static string Friendly(Type t)
{
    t = Nullable.GetUnderlyingType(t) ?? t;
    if (t.IsGenericParameter) return t.Name;
    if (t.IsArray) return Friendly(t.GetElementType()!) + "[]";
    if (t.IsGenericType)
    {
        string name = StripArity(t.GetGenericTypeDefinition().Name);
        return $"{name}<{string.Join(", ", t.GetGenericArguments().Select(Friendly))}>";
    }
    return Keyword(t.Name);
}

// Render BCL primitives as the C# keyword the consumer will actually type.
static string Keyword(string n) => n switch
{
    "String" => "string",
    "Boolean" => "bool",
    "Int32" => "int",
    "Int64" => "long",
    "Int16" => "short",
    "Byte" => "byte",
    "Double" => "double",
    "Single" => "float",
    "Decimal" => "decimal",
    "Object" => "object",
    "Char" => "char",
    _ => n,
};

// ---- llms.txt (curated index) ----
static string BuildLlmsIndex(List<ComponentInfo> comps, string version)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Omni.Blazor");
    sb.AppendLine();
    sb.AppendLine($"> Modern Blazor component library for .NET 10 — {comps.Count} components, warm cream/amber design system, dark mode, runtime accent swap, no Bootstrap/Tailwind. Published on NuGet as `AndersonN.Omni.Blazor` (namespace `Omni.Blazor`). Version {version}.");
    sb.AppendLine();
    sb.AppendLine("Install: `dotnet add package AndersonN.Omni.Blazor`, register `builder.Services.AddOmniComponents();`, put `<OmniTheme />` in `<head>`, and load the bundle from `_content/Omni.Blazor/css/omni.css` + `_content/Omni.Blazor/js/Omni.js`. CSS class prefix `omni-`, design-token prefix `--omni-`, JS namespace `window.omniBlazor`. Theme via `<html data-theme=\"light|dark\">`, `data-accent=\"amber|crimson|emerald|blue|violet|teal|cyan|indigo|fuchsia|lime|orange|rose\">`, `data-density=\"compact|comfortable|spacious\">`.");
    sb.AppendLine();

    foreach (var group in comps.GroupBy(c => c.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        sb.AppendLine($"## {group.Key}");
        sb.AppendLine();
        foreach (var c in group)
        {
            string url = $"{c.BlobUrl}";
            string summary = Trim(c.Summary, 160);
            sb.AppendLine(summary.Length > 0 ? $"- [{c.Name}]({url}): {summary}" : $"- [{c.Name}]({url})");
        }
        sb.AppendLine();
    }

    sb.AppendLine("## Optional");
    sb.AppendLine();
    sb.AppendLine("- [llms-full.txt](https://raw.githubusercontent.com/afernandes/Omni.Blazor/main/llms-full.txt): every component's parameters, events, slots, enum values and theme tokens.");
    sb.AppendLine("- [components.json](https://raw.githubusercontent.com/afernandes/Omni.Blazor/main/docs/components.json): machine-readable manifest (same data, structured).");
    sb.AppendLine("- [CLAUDE.md](https://github.com/afernandes/Omni.Blazor/blob/main/CLAUDE.md): repo architecture & authoring conventions.");
    return sb.ToString();
}

// ---- llms-full.txt (full dump) ----
static string BuildLlmsFull(List<ComponentInfo> comps, string[] tokens, string version)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Omni.Blazor — full API reference");
    sb.AppendLine();
    sb.AppendLine($"> {comps.Count} components. NuGet `AndersonN.Omni.Blazor` v{version}, namespace `Omni.Blazor`. Every component inherits the common Omni surface: `Class` (string), `Style` (string) and `Attributes` (HTML splat) — these are omitted per-component below and apply everywhere. Form inputs (FormComponent<T>) also expose `Value`/`ValueChanged`/`ValueExpression`, `Disabled`, `ReadOnly`, `Name`, `Required`.");
    sb.AppendLine();

    foreach (var group in comps.GroupBy(c => c.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        sb.AppendLine($"## {group.Key}");
        sb.AppendLine();
        foreach (var c in group)
        {
            sb.AppendLine($"### {c.Name}");
            if (!string.IsNullOrEmpty(c.Summary)) sb.AppendLine(c.Summary);
            sb.AppendLine($"_base: {c.BaseType}{(c.HasChildContent ? " · accepts ChildContent" : "")}{(c.IsInput ? " · form input" : "")} · source: {c.Source}_");
            sb.AppendLine();

            Emit(sb, "Parameters", c.Parameters.Where(p => p.Kind == "parameter" && p.InheritedFrom is null));
            Emit(sb, "Events", c.Parameters.Where(p => p.Kind == "event" && p.InheritedFrom is null));
            Emit(sb, "Slots", c.Parameters.Where(p => p.Kind == "slot" && p.InheritedFrom is null));
            sb.AppendLine();
        }
    }

    sb.AppendLine("## Theme tokens");
    sb.AppendLine();
    sb.AppendLine("CSS custom properties (set on `:root`, override anywhere). Full list:");
    sb.AppendLine();
    foreach (string tok in tokens) sb.AppendLine($"- `{tok}`");
    return sb.ToString();

    static void Emit(StringBuilder sb, string heading, IEnumerable<ParamInfo> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;
        sb.AppendLine($"{heading}:");
        foreach (ParamInfo p in list)
        {
            var line = new StringBuilder($"- `{p.Name}`: {p.Type}");
            if (p.ContextType is not null) line.Append($" (context: {p.ContextType})");
            if (p.EnumValues is { Length: > 0 }) line.Append($" {{{string.Join(" | ", p.EnumValues.Select(e => e.Name))}}}");
            if (p.Required) line.Append(" *required*");
            if (p.Default is not null) line.Append($" = {p.Default}");
            if (!string.IsNullOrEmpty(p.Summary)) line.Append($" — {p.Summary}");
            sb.AppendLine(line.ToString());
        }
    }
}

static string Trim(string? s, int max)
{
    s = (s ?? string.Empty).Trim();
    return s.Length <= max ? s : s[..max].TrimEnd() + "…";
}

// =============================================================================
// Models
// =============================================================================

internal record EnumVal(string Name, string? Summary);

internal record ParamInfo(
    string Name, string Kind, string Type, string? ContextType,
    EnumVal[]? EnumValues, string? Default, bool Required, string? Summary, string? InheritedFrom);

internal record ComponentInfo(
    string Name, string Category, string BaseType, bool IsInput, bool HasChildContent,
    string? Summary, string Source, ParamInfo[] Parameters)
{
    [JsonIgnore] public string BlobUrl => $"https://github.com/afernandes/Omni.Blazor/blob/main/{Source}";
}

internal record Manifest(string Package, string Version, string Repository, int Count, ComponentInfo[] Components);
