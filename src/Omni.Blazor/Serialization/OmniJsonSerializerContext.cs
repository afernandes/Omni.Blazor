using System.Text.Json.Serialization;
using Omni.Blazor.Models;

namespace Omni.Blazor.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DataGridViewState))]
[JsonSerializable(typeof(Dictionary<string, long>))]
internal sealed partial class OmniJsonSerializerContext : JsonSerializerContext;
