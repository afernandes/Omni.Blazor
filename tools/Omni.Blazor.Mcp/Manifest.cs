namespace Omni.Blazor.Mcp;

// Records mirroring docs/components.json (produced by Omni.Blazor.ManifestGen).
// Deserialized case-insensitively, so PascalCase props map from camelCase JSON;
// unknown JSON properties (e.g. "version") are ignored.

/// <summary>Root of the component manifest.</summary>
public sealed record Manifest(
    string Package,
    string Repository,
    int Count,
    List<Component> Components,
    List<ConfigurationApi>? ConfigurationApis = null);

/// <summary>One component and its public surface.</summary>
public sealed record Component(
    string Name,
    string Category,
    string BaseType,
    bool IsInput,
    bool HasChildContent,
    string? Summary,
    string Source,
    List<Param> Parameters);

/// <summary>A component parameter, event (<c>kind=event</c>) or slot (<c>kind=slot</c>).</summary>
public sealed record Param(
    string Name,
    string Kind,
    string Type,
    string? ContextType,
    List<EnumVal>? EnumValues,
    string? Default,
    bool Required,
    string? Summary,
    string? InheritedFrom);

/// <summary>One value of an enum-typed parameter.</summary>
public sealed record EnumVal(string Name, string? Summary);

/// <summary>One fluent schema, builder or provider design API.</summary>
public sealed record ConfigurationApi(
    string Name,
    string Category,
    string Kind,
    string? Summary,
    string Source,
    List<ApiMember> Members);

/// <summary>One callable or readable member of a configuration API.</summary>
public sealed record ApiMember(string Name, string Kind, string Signature, string? Summary);
