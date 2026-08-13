using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Omni.Blazor.Localization;

/// <summary>
/// Adapts the standard .NET <see cref="IStringLocalizer{T}"/> pipeline to Omni.
/// The host may therefore use RESX, Orchard Core PO files or any custom
/// <c>IStringLocalizerFactory</c> without coupling Omni.Blazor to that source.
/// </summary>
/// <typeparam name="TResource">The consumer's shared resource marker type.</typeparam>
public sealed class StringLocalizerOmniTranslationProvider<TResource> : IOmniTranslationProvider
{
    private readonly IStringLocalizer<TResource> _localizer;

    /// <summary>Creates the adapter.</summary>
    public StringLocalizerOmniTranslationProvider(IStringLocalizer<TResource> localizer)
        => _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        // IStringLocalizer intentionally follows CurrentUICulture and has no
        // explicit-culture lookup API. Never return a value from the wrong culture
        // when an OmniCultureScope requests a different one.
        if (!string.Equals(request.Culture.Name, CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            translation = string.Empty;
            return false;
        }

        if (request.PluralCategory is { } category)
        {
            LocalizedString plural = _localizer[$"{request.Key}.{category}"];
            if (!plural.ResourceNotFound)
            {
                translation = plural.Value;
                return true;
            }
        }

        LocalizedString value = _localizer[request.Key];
        translation = value.Value;
        return !value.ResourceNotFound;
    }
}
