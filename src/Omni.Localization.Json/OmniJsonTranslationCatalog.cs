using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Omni.Localization.Json;

/// <summary>Parses strict, AOT-safe JSON translation catalogs.</summary>
public static class OmniJsonTranslationCatalog
{
    /// <summary>Parses one UTF-8 catalog with <c>culture</c> and <c>texts</c> properties.</summary>
    public static OmniTranslationCatalog<TResource> Parse<TResource>(
        ReadOnlyMemory<byte> utf8Json,
        int priority = 0)
    {
        using JsonDocument document = JsonDocument.Parse(utf8Json);
        return ParseDocument<TResource>(document.RootElement, priority);
    }

    /// <summary>Parses one catalog stream. The stream remains owned by the caller.</summary>
    public static OmniTranslationCatalog<TResource> Parse<TResource>(
        Stream stream,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using JsonDocument document = JsonDocument.Parse(stream);
        return ParseDocument<TResource>(document.RootElement, priority);
    }

    /// <summary>Adds one JSON catalog to a typed resource.</summary>
    public static IServiceCollection AddOmniJsonTranslations<TResource>(
        this IServiceCollection services,
        ReadOnlyMemory<byte> utf8Json,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.AddSingleton<IOmniTranslationProvider<TResource>>(
            Parse<TResource>(utf8Json, priority));
        return services;
    }

    /// <summary>
    /// Adds split JSON catalogs. Later documents receive a higher priority and override
    /// earlier documents deterministically for duplicate culture/key pairs.
    /// </summary>
    public static IServiceCollection AddOmniJsonTranslations<TResource>(
        this IServiceCollection services,
        IEnumerable<string> jsonDocuments,
        int basePriority = 0)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsonDocuments);
        int index = 0;
        foreach (string json in jsonDocuments)
        {
            ArgumentNullException.ThrowIfNull(json);
            services.AddSingleton<IOmniTranslationProvider<TResource>>(
                Parse<TResource>(System.Text.Encoding.UTF8.GetBytes(json), checked(basePriority + index)));
            index++;
        }
        services.AddOmniLocalization();
        return services;
    }

    private static OmniTranslationCatalog<TResource> ParseDocument<TResource>(
        JsonElement root,
        int priority)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("A localization catalog must be a JSON object.");
        if (!root.TryGetProperty("culture", out JsonElement cultureElement) ||
            cultureElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(cultureElement.GetString()))
            throw new JsonException("A localization catalog requires a non-empty string 'culture'.");
        if (!root.TryGetProperty("texts", out JsonElement textsElement) ||
            textsElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("A localization catalog requires an object 'texts'.");

        var translations = new Dictionary<string, string>(StringComparer.Ordinal);
        Flatten(textsElement, prefix: null, translations);
        return new OmniTranslationCatalog<TResource>(cultureElement.GetString()!, translations, priority);
    }

    private static void Flatten(
        JsonElement element,
        string? prefix,
        Dictionary<string, string> translations)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string key = prefix is null ? property.Name : string.Concat(prefix, "__", property.Name);
            AddValue(property.Value, key, translations);
        }
    }

    private static void AddValue(
        JsonElement value,
        string key,
        Dictionary<string, string> translations)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                if (!translations.TryAdd(key, value.GetString()!))
                    throw new JsonException($"Duplicate flattened translation key '{key}'.");
                return;
            case JsonValueKind.Object:
                Flatten(value, key, translations);
                return;
            case JsonValueKind.Array:
                int index = 0;
                foreach (JsonElement item in value.EnumerateArray())
                {
                    AddValue(item, string.Concat(key, "__", index.ToString(System.Globalization.CultureInfo.InvariantCulture)), translations);
                    index++;
                }
                return;
            default:
                throw new JsonException($"Translation '{key}' must be a string, object or array.");
        }
    }
}
