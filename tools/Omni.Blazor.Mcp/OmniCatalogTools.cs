using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Omni.Blazor.Mcp;

/// <summary>
/// MCP tools exposing the Omni.Blazor component catalog to agents. Thin wrappers
/// over <see cref="ComponentCatalog"/> (which holds all the logic); the catalog
/// is injected from DI, the string arguments come from the calling LLM.
/// </summary>
[McpServerToolType]
public static class OmniCatalogTools
{
    [McpServerTool(Name = "list_components")]
    [Description("List Omni.Blazor components (name, category, one-line summary). Optionally filter by category: Buttons, Data, Display, Forms, Inputs, Layout, Marketing, Navigation, Overlay.")]
    public static string ListComponents(
        ComponentCatalog catalog,
        [Description("Optional category filter, e.g. Inputs. Omit for all components.")] string? category = null)
        => catalog.ListText(category);

    [McpServerTool(Name = "get_component")]
    [Description("Full API of one Omni.Blazor component: parameters, events, slots, enum values, defaults and a minimal usage snippet. Use the exact name, e.g. OmniDataGrid.")]
    public static string GetComponent(
        ComponentCatalog catalog,
        [Description("Exact component name, e.g. OmniButton.")] string name)
        => catalog.Describe(name);

    [McpServerTool(Name = "search_components")]
    [Description("Search Omni.Blazor components by name, category or description. Returns matching components (name matches first).")]
    public static string SearchComponents(
        ComponentCatalog catalog,
        [Description("Search text, e.g. 'date', 'chat', 'grid'.")] string query)
        => catalog.SearchText(query);
}
