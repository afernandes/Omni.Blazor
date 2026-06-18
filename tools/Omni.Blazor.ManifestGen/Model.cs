using System.Text.Json.Serialization;

namespace Omni.Blazor.ManifestGen;

// The shape written to docs/components.json. Public so the unit tests (and the
// writers) can construct and assert on it. No Version field on purpose: the
// artifacts must be deterministic from the source so the CI drift-check works
// (a MinVer commit-height version would change every commit).

public sealed record EnumVal(string Name, string? Summary);

public sealed record ParamInfo(
    string Name, string Kind, string Type, string? ContextType,
    EnumVal[]? EnumValues, string? Default, bool Required, string? Summary, string? InheritedFrom);

public sealed record ComponentInfo(
    string Name, string Category, string BaseType, bool IsInput, bool HasChildContent,
    string? Summary, string Source, ParamInfo[] Parameters)
{
    [JsonIgnore] public string BlobUrl => $"https://github.com/afernandes/Omni.Blazor/blob/main/{Source}";
}

public sealed record Manifest(string Package, string Repository, int Count, ComponentInfo[] Components);
