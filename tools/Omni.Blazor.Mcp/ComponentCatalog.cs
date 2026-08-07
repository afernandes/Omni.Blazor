using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Omni.Blazor.Mcp;

/// <summary>
/// In-memory query layer over the Omni.Blazor component manifest. All the MCP
/// tool behaviour lives here (so it is unit-testable without a transport): list,
/// search, lookup, usage-example synthesis and the human-readable component
/// description returned to the agent.
/// </summary>
public sealed class ComponentCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const StringComparison Ic = StringComparison.OrdinalIgnoreCase;

    private readonly Manifest _manifest;
    private readonly Dictionary<string, Component> _byName;
    private readonly IReadOnlyList<ConfigurationApi> _configurationApis;
    private readonly Dictionary<string, ConfigurationApi> _configurationApiByName;

    private ComponentCatalog(Manifest manifest)
    {
        _manifest = manifest;
        _byName = manifest.Components.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        _configurationApis = manifest.ConfigurationApis ?? [];
        _configurationApiByName = _configurationApis.ToDictionary(
            api => api.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Parse a catalog from manifest JSON.</summary>
    public static ComponentCatalog FromJson(string json)
    {
        Manifest manifest = JsonSerializer.Deserialize<Manifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Manifesto vazio ou inválido.");
        return new ComponentCatalog(manifest);
    }

    /// <summary>
    /// Load from <paramref name="path"/> when given and present; otherwise from
    /// the manifest embedded in this assembly (self-contained server).
    /// </summary>
    public static ComponentCatalog Load(string? path)
    {
        string json = path is not null && File.Exists(path)
            ? File.ReadAllText(path)
            : ReadEmbeddedManifest();
        return FromJson(json);
    }

    private static string ReadEmbeddedManifest()
    {
        Assembly asm = typeof(ComponentCatalog).Assembly;
        using Stream? s = asm.GetManifestResourceStream("components.json")
            ?? throw new InvalidOperationException("Recurso embutido 'components.json' não encontrado.");
        using var reader = new StreamReader(s);
        return reader.ReadToEnd();
    }

    /// <summary>Total component count.</summary>
    public int Count => _manifest.Components.Count;

    /// <summary>Total fluent configuration API count.</summary>
    public int ConfigurationApiCount => _configurationApis.Count;

    /// <summary>All components, optionally filtered by category (case-insensitive).</summary>
    public IReadOnlyList<Component> List(string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? _manifest.Components
            : _manifest.Components.Where(c => string.Equals(c.Category, category.Trim(), Ic)).ToList();

    /// <summary>Exact (case-insensitive) lookup by component name; null if absent.</summary>
    public Component? Get(string? name) =>
        name is not null && _byName.TryGetValue(name.Trim(), out Component? c) ? c : null;

    /// <summary>Lists fluent schemas, builders and providers, optionally filtered by category.</summary>
    public IReadOnlyList<ConfigurationApi> ListConfigurationApis(string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? _configurationApis
            : _configurationApis.Where(api => string.Equals(api.Category, category.Trim(), Ic)).ToList();

    /// <summary>Exact case-insensitive configuration API lookup.</summary>
    public ConfigurationApi? GetConfigurationApi(string? name) =>
        name is not null && _configurationApiByName.TryGetValue(name.Trim(), out ConfigurationApi? api)
            ? api
            : null;

    /// <summary>Searches configuration APIs by type name, member signature or summary.</summary>
    public IReadOnlyList<ConfigurationApi> SearchConfigurationApis(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        string value = query.Trim();
        return _configurationApis
            .Where(api => api.Name.Contains(value, Ic)
                          || (api.Summary?.Contains(value, Ic) ?? false)
                          || api.Members.Any(member => member.Signature.Contains(value, Ic)
                                                       || (member.Summary?.Contains(value, Ic) ?? false)))
            .OrderByDescending(api => api.Name.Contains(value, Ic))
            .ThenBy(api => api.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Fuzzy search over name, category and summary; name matches rank first.</summary>
    public IReadOnlyList<Component> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        string q = query.Trim();
        return _manifest.Components
            .Where(c => c.Name.Contains(q, Ic)
                     || c.Category.Contains(q, Ic)
                     || (c.Summary?.Contains(q, Ic) ?? false))
            .OrderByDescending(c => c.Name.Contains(q, Ic))
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Distinct categories in manifest order.</summary>
    public IReadOnlyList<string> Categories =>
        _manifest.Components.Select(c => c.Category).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Synthesize a minimal copy-paste usage snippet for a component.</summary>
    public string Example(string name)
    {
        Component c = Get(name) ?? throw new KeyNotFoundException($"Componente '{name}' não encontrado.");
        var sb = new StringBuilder("<").Append(c.Name);
        foreach (Param p in c.Parameters.Where(p => p.Required && p.Kind == "parameter"))
            sb.Append(' ').Append(p.Name).Append("=\"").Append(SampleValue(p)).Append('"');
        sb.Append(c.HasChildContent ? $">...</{c.Name}>" : " />");
        return sb.ToString();
    }

    private static string SampleValue(Param p)
    {
        if (p.EnumValues is { Count: > 0 }) return $"{p.Type}.{p.EnumValues[0].Name}";
        return p.Type switch
        {
            "bool" => "true",
            "int" or "long" or "short" or "double" or "float" or "decimal" => "0",
            _ => "...",
        };
    }

    // ─── Text rendered back to the agent ────────────────────────────────────

    /// <summary>Bullet list (name, category, summary) for the list/search tools.</summary>
    public string ListText(string? category)
    {
        IReadOnlyList<Component> items = List(category);
        if (items.Count == 0)
            return string.IsNullOrWhiteSpace(category)
                ? "No components."
                : $"No components in category '{category}'. Categories: {string.Join(", ", Categories)}.";
        return Bullets(items);
    }

    /// <summary>Search results as a bullet list, or a no-match message.</summary>
    public string SearchText(string query)
    {
        IReadOnlyList<Component> items = Search(query);
        return items.Count == 0 ? $"No components match '{query}'." : Bullets(items);
    }

    /// <summary>Full API of one component (or a not-found message with suggestions).</summary>
    public string Describe(string name)
    {
        Component? c = Get(name);
        if (c is null)
        {
            IEnumerable<string> near = Search(name).Take(5).Select(x => x.Name);
            return near.Any()
                ? $"Component '{name}' not found. Did you mean: {string.Join(", ", near)}?"
                : $"Component '{name}' not found.";
        }

        var sb = new StringBuilder();
        sb.Append("# ").Append(c.Name).Append("  (").Append(c.Category).AppendLine(")");
        if (!string.IsNullOrEmpty(c.Summary)) sb.AppendLine(c.Summary);
        sb.Append("Base: ").Append(c.BaseType);
        if (c.HasChildContent) sb.Append(" · accepts ChildContent");
        if (c.IsInput) sb.Append(" · form input");
        sb.Append(" · source: ").AppendLine(c.Source);
        sb.AppendLine();

        Section(sb, "Parameters", c.Parameters.Where(p => p.Kind == "parameter" && p.InheritedFrom is null));
        Section(sb, "Events", c.Parameters.Where(p => p.Kind == "event" && p.InheritedFrom is null));
        Section(sb, "Slots", c.Parameters.Where(p => p.Kind == "slot" && p.InheritedFrom is null));

        sb.AppendLine("## Example").AppendLine(Example(c.Name));
        sb.AppendLine().AppendLine("Plus the common Omni surface: Class, Style and HTML attributes splat on every component"
            + (c.IsInput ? "; inputs also expose Value/ValueChanged/Disabled/ReadOnly/Name." : "."));
        return sb.ToString().TrimEnd();
    }

    /// <summary>Compact fluent configuration API list for MCP responses.</summary>
    public string ListConfigurationApisText(string? category)
    {
        IReadOnlyList<ConfigurationApi> items = ListConfigurationApis(category);
        return items.Count == 0
            ? "No fluent configuration APIs."
            : string.Join("\n", items.Select(api =>
                $"- {api.Name} [{api.Category}/{api.Kind}]" +
                (string.IsNullOrEmpty(api.Summary) ? string.Empty : $": {api.Summary}")));
    }

    /// <summary>Full method/property surface of one fluent configuration API.</summary>
    public string DescribeConfigurationApi(string name)
    {
        ConfigurationApi? api = GetConfigurationApi(name);
        if (api is null)
        {
            string suggestions = string.Join(", ", SearchConfigurationApis(name).Take(5).Select(item => item.Name));
            return suggestions.Length == 0
                ? $"Configuration API '{name}' not found."
                : $"Configuration API '{name}' not found. Did you mean: {suggestions}?";
        }

        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(api.Name);
        if (!string.IsNullOrEmpty(api.Summary)) sb.AppendLine(api.Summary);
        sb.Append(api.Kind).Append(" · ").Append(api.Category).Append(" · source: ").AppendLine(api.Source);
        sb.AppendLine();
        foreach (ApiMember member in api.Members)
        {
            sb.Append("- ").Append(member.Signature);
            if (!string.IsNullOrEmpty(member.Summary)) sb.Append(" — ").Append(member.Summary);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Search result text for fluent configuration APIs.</summary>
    public string SearchConfigurationApisText(string query)
    {
        IReadOnlyList<ConfigurationApi> items = SearchConfigurationApis(query);
        return items.Count == 0
            ? $"No fluent configuration APIs match '{query}'."
            : string.Join("\n", items.Select(api => $"- {api.Name} [{api.Category}/{api.Kind}]"));
    }

    private static string Bullets(IEnumerable<Component> items) =>
        string.Join("\n", items.Select(c =>
            $"- {c.Name} [{c.Category}]{(string.IsNullOrEmpty(c.Summary) ? "" : ": " + c.Summary)}"));

    private static void Section(StringBuilder sb, string heading, IEnumerable<Param> items)
    {
        List<Param> list = items.ToList();
        if (list.Count == 0) return;
        sb.Append("## ").AppendLine(heading);
        foreach (Param p in list)
        {
            sb.Append("- ").Append(p.Name).Append(": ").Append(p.Type);
            if (p.ContextType is not null) sb.Append(" (context: ").Append(p.ContextType).Append(')');
            if (p.EnumValues is { Count: > 0 }) sb.Append(" {").Append(string.Join(" | ", p.EnumValues.Select(e => e.Name))).Append('}');
            if (p.Required) sb.Append(" *required*");
            if (p.Default is not null) sb.Append(" = ").Append(p.Default);
            if (!string.IsNullOrEmpty(p.Summary)) sb.Append(" — ").Append(p.Summary);
            sb.AppendLine();
        }
        sb.AppendLine();
    }
}
