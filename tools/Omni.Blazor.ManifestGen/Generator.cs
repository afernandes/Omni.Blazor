using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.ManifestGen;

/// <summary>Type-name and parameter classification helpers (pure, unit-tested).</summary>
public static class TypeNames
{
    /// <summary>Drop the generic-arity backtick suffix: <c>OmniGrid`1</c> → <c>OmniGrid</c>.</summary>
    public static string StripArity(string name)
    {
        int i = name.IndexOf('`');
        return i < 0 ? name : name[..i];
    }

    /// <summary>Render a BCL primitive as the C# keyword the consumer will type.</summary>
    public static string Keyword(string n) => n switch
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

    /// <summary>Readable type name: unwraps Nullable, renders generics/arrays, keywords primitives.</summary>
    public static string Friendly(Type t)
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

    /// <summary>Classify a parameter type as <c>event</c> (EventCallback), <c>slot</c> (RenderFragment) or <c>parameter</c>.</summary>
    public static (string kind, string? context) Classify(Type t)
    {
        if (t == typeof(EventCallback)) return ("event", null);
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventCallback<>)) return ("event", Friendly(t.GetGenericArguments()[0]));
        if (t == typeof(RenderFragment)) return ("slot", null);
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(RenderFragment<>)) return ("slot", Friendly(t.GetGenericArguments()[0]));
        return ("parameter", null);
    }

    /// <summary>True for types whose default value is worth emitting (primitives, string, enum, decimal).</summary>
    public static bool IsSimple(Type t) =>
        t.IsEnum || t == typeof(string) || t == typeof(bool) || t == typeof(decimal) ||
        (t.IsPrimitive && t != typeof(IntPtr) && t != typeof(UIntPtr));

    /// <summary>Stringify a default parameter value for the manifest (invariant culture).</summary>
    public static string? DefaultToString(object? v) => v switch
    {
        null => null,
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        string s => s,
        _ => Convert.ToString(v, CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// XML doc id for a type: <c>namespace.Name`arity</c>, nested <c>+</c> → <c>.</c>.
    /// Closed generics (e.g. <c>FormComponent&lt;string&gt;</c>, the declaring type of
    /// inherited params) are reduced to their open definition so the id matches the
    /// XML doc file (<c>FormComponent`1</c>, not the assembly-qualified closed form).
    /// </summary>
    public static string XmlId(Type t)
    {
        if (t.IsGenericType && !t.IsGenericTypeDefinition) t = t.GetGenericTypeDefinition();
        return (t.FullName ?? t.Name).Replace('+', '.');
    }

    /// <summary>True if the type derives from <c>FormComponent&lt;T&gt;</c> (i.e. a form input).</summary>
    public static bool IsFormInput(Type t)
    {
        for (Type? b = t.BaseType; b is not null; b = b.BaseType)
            if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(FormComponent<>)) return true;
        return false;
    }

    /// <summary>Instantiate a non-generic component to read field-initializer defaults; null on failure.</summary>
    public static object? TryInstantiate(Type t)
    {
        if (t.IsGenericTypeDefinition) return null;
        try { return Activator.CreateInstance(t); }
        catch { return null; }
    }

    /// <summary>All loadable types in an assembly (tolerates partial load failures).</summary>
    public static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}

/// <summary>Reads and flattens C# XML documentation (pure, unit-tested).</summary>
public static class XmlDocText
{
    /// <summary>Member-id → flattened summary text, from a compiler XML doc file.</summary>
    public static Dictionary<string, string> Load(string path)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path)) return d;
        XDocument doc = XDocument.Load(path);
        foreach (XElement m in doc.Descendants("member"))
        {
            string? name = m.Attribute("name")?.Value;
            XElement? summary = m.Element("summary");
            if (name is null || summary is null) continue;
            string text = Flatten(summary);
            if (text.Length > 0) d[name] = text;
        }
        return d;
    }

    /// <summary>Flatten a doc element to single-line text, resolving <c>see/c</c> to plain content.</summary>
    public static string Flatten(XElement el)
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

    /// <summary>Reduce a cref (<c>M:Ns.Type.Method(System.String)</c>) to a readable member name (<c>Method</c>).</summary>
    public static string SimplifyCref(string cref)
    {
        if (cref.Length > 1 && cref[1] == ':') cref = cref[2..];
        int paren = cref.IndexOf('(');
        if (paren >= 0) cref = cref[..paren];
        int dot = cref.LastIndexOf('.');
        return dot >= 0 ? cref[(dot + 1)..] : cref;
    }

    /// <summary>Lookup helper: null when the id is absent.</summary>
    public static string? Get(Dictionary<string, string> docs, string id) => docs.GetValueOrDefault(id);
}

/// <summary>Razor/SCSS source scanning helpers (pure over file content, unit-tested).</summary>
public static class RazorScan
{
    /// <summary>
    /// One-sentence description from a component's leading <c>@* ... *@</c> comment,
    /// used as a fallback when no XML summary exists. Returns null when there is no
    /// top-of-file comment.
    /// </summary>
    public static string? LeadComment(string text)
    {
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

    /// <summary>Distinct <c>--omni-*</c> token declarations from _tokens.scss content, in order.</summary>
    public static string[] Tokens(string scssContent)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        foreach (string line in scssContent.Split('\n'))
        {
            int i = line.IndexOf("--omni-", StringComparison.Ordinal);
            if (i < 0) continue;
            int colon = line.IndexOf(':', i);
            if (colon < 0) continue;
            string name = line[i..colon].Trim();
            if (name.Contains(' ') || name.Contains('(')) continue;   // skip var() references
            if (seen.Add(name)) ordered.Add(name);
        }
        return [.. ordered];
    }
}

/// <summary>Renders the llms.txt / llms-full.txt artifacts from the manifest (pure, unit-tested).</summary>
public static class Writers
{
    /// <summary>Curated index (llmstxt.org format): one bullet per component, grouped by category.</summary>
    public static string LlmsIndex(IReadOnlyList<ComponentInfo> comps)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Omni.Blazor");
        sb.AppendLine();
        sb.AppendLine($"> Modern Blazor component library for .NET 10 — {comps.Count} components, warm cream/amber design system, dark mode, runtime accent swap, no Bootstrap/Tailwind. Published on NuGet as `AndersonN.Omni.Blazor` (namespace `Omni.Blazor`).");
        sb.AppendLine();
        sb.AppendLine("Install: `dotnet add package AndersonN.Omni.Blazor`, register `builder.Services.AddOmniComponents();`, put `<OmniTheme />` in `<head>`, and load the bundle from `_content/Omni.Blazor/css/omni.css` + `_content/Omni.Blazor/js/Omni.js`. CSS class prefix `omni-`, design-token prefix `--omni-`, JS namespace `window.omniBlazor`. Theme via `<html data-theme=\"light|dark\">`, `data-accent=\"amber|crimson|emerald|blue|violet|teal|cyan|indigo|fuchsia|lime|orange|rose\">`, `data-density=\"compact|comfortable|spacious\">`.");
        sb.AppendLine();

        foreach (var group in comps.GroupBy(c => c.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            foreach (ComponentInfo c in group)
            {
                string summary = Trim(c.Summary, 160);
                sb.AppendLine(summary.Length > 0 ? $"- [{c.Name}]({c.BlobUrl}): {summary}" : $"- [{c.Name}]({c.BlobUrl})");
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

    /// <summary>Full API dump: every component's parameters, events, slots, enum values, plus theme tokens.</summary>
    public static string LlmsFull(IReadOnlyList<ComponentInfo> comps, IReadOnlyList<string> tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Omni.Blazor — full API reference");
        sb.AppendLine();
        sb.AppendLine($"> {comps.Count} components. NuGet `AndersonN.Omni.Blazor`, namespace `Omni.Blazor`. Every component inherits the common Omni surface: `Class` (string), `Style` (string) and `Attributes` (HTML splat) — these are omitted per-component below and apply everywhere. Form inputs (FormComponent<T>) also expose `Value`/`ValueChanged`/`ValueExpression`, `Disabled`, `ReadOnly`, `Name`, `Required`.");
        sb.AppendLine();

        foreach (var group in comps.GroupBy(c => c.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            foreach (ComponentInfo c in group)
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
    }

    private static void Emit(StringBuilder sb, string heading, IEnumerable<ParamInfo> items)
    {
        List<ParamInfo> list = items.ToList();
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

    private static string Trim(string? s, int max)
    {
        s = (s ?? string.Empty).Trim();
        return s.Length <= max ? s : s[..max].TrimEnd() + "…";
    }
}
