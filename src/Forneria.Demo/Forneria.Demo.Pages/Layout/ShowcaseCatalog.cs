using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Forneria.Demo.Pages.Layout;

/// <summary>One auto-discovered showcase page (built once via reflection).</summary>
public sealed record ShowcaseLink(
    string Category, int CategoryOrder, int ItemOrder,
    string Title, string Icon, string Path,
    string[] Aliases, bool IsNew, bool Featured);

/// <summary>
/// The closed set of showcase nav categories. <see cref="Cat.Auto"/> means "derive from the
/// page's folder" — the default a page gets when it doesn't state a category.
/// </summary>
public enum Cat
{
    Auto,
    Overview, Layout, Navigation, Buttons, Inputs, Selection,
    Forms, Display, Utilities, Feedback, Data, DataViz, Marketing,
}

/// <summary>
/// Per-page curation, co-located with the page — the single source of truth for that page's nav
/// metadata. Everything is optional: with no attribute a page still appears, taking its category
/// from its folder and its title from the type name. Drop it next to <c>@page</c>:
/// <code>@attribute [ShowcaseInfo(Title = "Data Filter", Category = Cat.Data, Icon = "filter", New = true, Aliases = "query,where,sql")]</code>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ShowcaseInfoAttribute : Attribute
{
    /// <summary>Display title. Null → derived from the type name.</summary>
    public string? Title { get; set; }

    /// <summary>Nav category. <see cref="Cat.Auto"/> (default) → derived from the page's folder.</summary>
    public Cat Category { get; set; } = Cat.Auto;

    /// <summary>Icon name. Null → the category's default icon.</summary>
    public string? Icon { get; set; }

    /// <summary>Sort order within the category (lower = first; ties fall back to title).</summary>
    public int Order { get; set; }

    /// <summary>Flags a genuinely-new component with a "Novo" badge (reserved for future additions).</summary>
    public bool New { get; set; }

    /// <summary>Flags a flagship/advanced differentiator (rare or absent in MudBlazor/Radzen) with a "Destaque" badge.</summary>
    public bool Featured { get; set; }

    /// <summary>Comma-separated search synonyms so the page is found by intent, not just its name.</summary>
    public string? Aliases { get; set; }
}

/// <summary>
/// Reflection-built index of every routable <c>/showcase/*</c> page — adding a page makes it
/// appear in the nav automatically. Each page's title/category/icon/order/aliases/"Novo" flag come
/// from its own <see cref="ShowcaseInfoAttribute"/> (the source of truth, sitting beside <c>@page</c>);
/// a page with no attribute falls back to its folder (category) and type name (title). The only
/// central data here is the <see cref="Categories"/> table — the genuinely cross-cutting display
/// order + default icon per category, each declared exactly once.
/// </summary>
public static class ShowcaseCatalog
{
    private static IReadOnlyList<ShowcaseLink>? _cache;
    private static Dictionary<string, ShowcaseLink>? _byPath;

    /// <summary>All discovered pages, ordered by category then item-order then title.</summary>
    public static IReadOnlyList<ShowcaseLink> All => _cache ??= Build();

    private static Dictionary<string, ShowcaseLink> ByPath =>
        _byPath ??= All.ToDictionary(l => l.Path, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Single source of truth for categories: display order (= array position), human name, and
    /// default icon. Add or reorder a category here and nowhere else.
    /// </summary>
    private static readonly (Cat Key, string Name, string Icon)[] Categories =
    {
        (Cat.Overview,   "Visão geral",          "zap"),
        (Cat.Layout,     "Estrutura & layout",   "layout-dashboard"),
        (Cat.Navigation, "Navegação",            "chevron-right"),
        (Cat.Buttons,    "Buttons",              "package"),
        (Cat.Inputs,     "Inputs básicos",       "edit"),
        (Cat.Selection,  "Seleção & avançados",  "list"),
        (Cat.Forms,      "Formulários",          "shield"),
        (Cat.Display,    "Data display",         "grid"),
        (Cat.Utilities,  "Utilitários",          "star"),
        (Cat.Feedback,   "Feedback & overlay",   "alert-triangle"),
        (Cat.Data,       "Data",                 "list"),
        (Cat.DataViz,    "Data viz",             "bar-chart"),
        (Cat.Marketing,  "Marketing",            "star"),
    };

    // Cat -> (display order, name, icon), derived once from the table above.
    private static readonly Dictionary<Cat, (int Order, string Name, string Icon)> CatInfo =
        Categories.Select((c, i) => (c, i))
                  .ToDictionary(x => x.c.Key, x => (Order: x.i, x.c.Name, x.c.Icon));

    // Fallback for pages with no [ShowcaseInfo] category: page folder -> category.
    private static readonly Dictionary<string, Cat> FolderCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Foundations"] = Cat.Overview,
        ["Fundamentos"] = Cat.Overview,
        ["Layout"]      = Cat.Layout,
        ["Wireframes"]  = Cat.Layout,
        ["Navigation"]  = Cat.Navigation,
        ["Buttons"]     = Cat.Buttons,
        ["Inputs"]      = Cat.Inputs,
        ["Forms"]       = Cat.Forms,
        ["Display"]     = Cat.Display,
        ["Feedback"]    = Cat.Feedback,
        ["Overlay"]     = Cat.Feedback,
        ["Overlays"]    = Cat.Feedback,
        ["Data"]        = Cat.Data,
        ["Marketing"]   = Cat.Marketing,
    };

    // camelCase / PascalCase word boundary, shared by DeriveTitle.
    private const string CamelSplit = "(?<=[a-z0-9])(?=[A-Z])";

    /// <summary>Search synonyms for a route (empty when none).</summary>
    public static string[] AliasesOf(string path) =>
        ByPath.TryGetValue(path, out var l) ? l.Aliases : Array.Empty<string>();

    /// <summary>Whether a route is flagged as a genuinely-new component.</summary>
    public static bool IsNew(string path) => ByPath.TryGetValue(path, out var l) && l.IsNew;

    /// <summary>Whether a route is flagged as a flagship/advanced differentiator.</summary>
    public static bool IsFeatured(string path) => ByPath.TryGetValue(path, out var l) && l.Featured;

    private static List<ShowcaseLink> Build()
    {
        var list = new List<ShowcaseLink>();
        foreach (var type in SafeTypes(typeof(ShowcaseCatalog).Assembly))
        {
            if (type is null || !typeof(IComponent).IsAssignableFrom(type)) continue;

            var route = type.GetCustomAttributes<RouteAttribute>()
                            .Select(r => r.Template)
                            .FirstOrDefault(t => t.StartsWith("/showcase", StringComparison.OrdinalIgnoreCase));
            if (route is null) continue;

            var info = type.GetCustomAttribute<ShowcaseInfoAttribute>();

            var cat = info?.Category ?? Cat.Auto;
            if (cat == Cat.Auto) cat = FolderCategory.GetValueOrDefault(FolderOf(type), Cat.Overview);
            var def = CatInfo[cat];

            var title = info?.Title ?? DeriveTitle(type.Name);
            var icon = info?.Icon ?? def.Icon;
            var order = info?.Order ?? 0;
            var aliases = ParseAliases(info?.Aliases);
            var isNew = info?.New ?? false;
            var featured = info?.Featured ?? false;

            list.Add(new ShowcaseLink(def.Name, def.Order, order, title, icon, route, aliases, isNew, featured));
        }
        return list
            .OrderBy(l => l.CategoryOrder)
            .ThenBy(l => l.ItemOrder)
            .ThenBy(l => l.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string[] ParseAliases(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<Type?> SafeTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types; }
    }

    private static string FolderOf(Type type)
    {
        var ns = type.Namespace ?? "";
        const string marker = ".Showcase";
        var i = ns.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "Fundamentos";
        var rest = ns[(i + marker.Length)..].TrimStart('.');
        return rest.Length == 0 ? "Fundamentos" : rest.Split('.')[0];
    }

    private static string DeriveTitle(string typeName)
    {
        var n = typeName;
        if (n.EndsWith("Page", StringComparison.Ordinal)) n = n[..^4];
        if (n.EndsWith("Showcase", StringComparison.Ordinal)) n = n[..^8];
        if (n.StartsWith("Omni", StringComparison.Ordinal) && n.Length > 4) n = n[4..];
        n = Regex.Replace(n, CamelSplit, " ");
        n = Regex.Replace(n, "(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return n.Length == 0 ? typeName : n;
    }
}
