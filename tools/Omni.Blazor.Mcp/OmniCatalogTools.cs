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

    [McpServerTool(Name = "list_configuration_apis")]
    [Description("List typed Omni.Blazor schemas, fluent builders and provider APIs. Optionally filter by Forms or Data.")]
    public static string ListConfigurationApis(
        ComponentCatalog catalog,
        [Description("Optional category filter: Forms or Data.")] string? category = null)
        => catalog.ListConfigurationApisText(category);

    [McpServerTool(Name = "get_configuration_api")]
    [Description("Get constructors, properties and fluent methods for an Omni.Blazor schema, builder or provider type.")]
    public static string GetConfigurationApi(
        ComponentCatalog catalog,
        [Description("Exact friendly type name, e.g. DataGridFormSchemaBuilder<TItem, TKey>.")] string name)
        => catalog.DescribeConfigurationApi(name);

    [McpServerTool(Name = "search_configuration_apis")]
    [Description("Search Omni.Blazor schema, builder and provider APIs by type, method signature or documentation.")]
    public static string SearchConfigurationApis(
        ComponentCatalog catalog,
        [Description("Search text, e.g. Collection, VisibleWhen, IDataGridFormProvider.")] string query)
        => catalog.SearchConfigurationApisText(query);
}
