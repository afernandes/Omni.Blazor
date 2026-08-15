using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Omni.Localization;

/// <summary>Adapts a standard .NET localizer as a source for a typed Omni resource.</summary>
/// <typeparam name="TResource">Omni target resource.</typeparam>
/// <typeparam name="TStringResource">Marker used by the host's standard localizer.</typeparam>
public sealed class StringLocalizerOmniTranslationProvider<TResource, TStringResource>
    : IOmniTranslationProvider<TResource>
{
    private readonly IStringLocalizer<TStringResource> _localizer;

    /// <summary>Creates the adapter.</summary>
    public StringLocalizerOmniTranslationProvider(IStringLocalizer<TStringResource> localizer)
        => _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = request.Culture;
            if (request.PluralCategory is { } category)
            {
                LocalizedString plural = _localizer[string.Concat(request.Key, ".", category.ToString())];
                if (!plural.ResourceNotFound)
                {
                    translation = plural.Value;
                    return true;
                }
                LocalizedString other = _localizer[string.Concat(request.Key, ".Other")];
                if (!other.ResourceNotFound)
                {
                    translation = other.Value;
                    return true;
                }
            }

            LocalizedString value = _localizer[request.Key];
            translation = value.Value;
            return !value.ResourceNotFound;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}

internal sealed class OmniStringLocalizerAdapter<TResource> : IStringLocalizer<TResource>
{
    private readonly IOmniLocalizer<TResource> _localizer;
    private readonly OmniLocalizationResource<TResource> _resource;

    public OmniStringLocalizerAdapter(
        IOmniLocalizer<TResource> localizer,
        OmniLocalizationResource<TResource> resource)
    {
        _localizer = localizer;
        _resource = resource;
    }

    public LocalizedString this[string name]
    {
        get
        {
            OmniLocalizedString value = _localizer.Localize(name);
            return new(name, value.Value, value.ResourceNotFound);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            OmniLocalizedString value = _localizer.Localize(name);
            string formatted = arguments.Length == 0
                ? value.Value
                : string.Format(CultureInfo.CurrentCulture, value.Value, arguments);
            return new(name, formatted, value.ResourceNotFound);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (string key in _resource.ReferenceTranslations.Keys)
        {
            OmniLocalizedString value = _localizer.Localize(key);
            yield return new(key, value.Value, value.ResourceNotFound);
        }
    }
}
