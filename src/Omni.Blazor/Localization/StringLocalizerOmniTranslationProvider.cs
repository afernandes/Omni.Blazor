using System.Globalization;
using Microsoft.Extensions.Localization;

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
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            // IStringLocalizer intentionally reads the ambient UI culture. The
            // assignment is scoped to this synchronous lookup and CultureInfo is
            // backed by the async execution context, so concurrent Blazor circuits
            // do not share mutable process-wide state.
            CultureInfo.CurrentUICulture = request.Culture;

            if (request.PluralCategory is { } category)
            {
                LocalizedString plural = _localizer[$"{request.Key}.{category}"];
                if (!plural.ResourceNotFound)
                {
                    translation = plural.Value;
                    return true;
                }

                LocalizedString other = _localizer[$"{request.Key}.{OmniPluralCategory.Other}"];
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
