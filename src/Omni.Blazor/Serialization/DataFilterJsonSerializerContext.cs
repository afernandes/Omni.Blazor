using System.Text.Json.Serialization;
using Omni.Blazor.Models;

namespace Omni.Blazor.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    MaxDepth = 64)]
[JsonSerializable(typeof(DataFilterQueryDocument))]
internal sealed partial class DataFilterJsonSerializerContext : JsonSerializerContext;
