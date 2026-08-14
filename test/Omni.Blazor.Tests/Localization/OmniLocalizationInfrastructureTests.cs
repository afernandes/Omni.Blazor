using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Omni.Blazor.Localization;

namespace Omni.Blazor.Tests.Localization;

public sealed class OmniLocalizationInfrastructureTests
{
    [Fact]
    public void Catalog_validator_rejects_malformed_formats_and_placeholder_drift()
    {
        OmniTranslationCatalogValidationResult result = OmniTranslationCatalogValidator.Validate(
            "fr-FR",
            new Dictionary<string, string>
            {
                [OmniTranslationKeys.DataImportReady] = "{1} lignes",
                [OmniTranslationKeys.Close] = "{"
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Key == OmniTranslationKeys.DataImportReady);
        Assert.Contains(result.Issues, issue => issue.Key == OmniTranslationKeys.Close);
    }

    [Fact]
    public void Catalog_validator_warns_for_extension_keys_without_rejecting_them()
    {
        OmniTranslationCatalogValidationResult result = OmniTranslationCatalogValidator.Validate(
            "fr",
            new Dictionary<string, string> { ["Application.CustomKey"] = "Personnalisé" });

        Assert.True(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(OmniTranslationCatalogIssueSeverity.Warning, result.Issues[0].Severity);
    }

    [Fact]
    public void Catalog_validator_can_require_every_stable_key()
    {
        OmniTranslationCatalogValidationResult result = OmniTranslationCatalogValidator.Validate(
            "fr",
            new Dictionary<string, string> { [OmniTranslationKeys.Close] = "Fermer" },
            requireCompleteCatalog: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Key == OmniTranslationKeys.Save &&
            issue.Message.Contains("missing", StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_constructor_rejects_duplicate_keys_deterministically()
    {
        KeyValuePair<string, string>[] translations =
        [
            new(OmniTranslationKeys.Close, "Fermer"),
            new(OmniTranslationKeys.Close, "Clore")
        ];

        Assert.Throws<ArgumentException>(() => new OmniTranslationCatalog("fr", translations));
    }

    [Theory]
    [InlineData("en-XA", false)]
    [InlineData("ar-XB", true)]
    public void Pseudo_locales_expand_text_and_preserve_placeholders(string cultureName, bool expectedRtl)
    {
        var provider = new OmniPseudoTranslationProvider();
        var request = new OmniTranslationRequest(
            OmniTranslationKeys.DataImportReady,
            CultureInfo.GetCultureInfo(cultureName),
            2,
            OmniPluralCategory.Other);

        Assert.True(provider.TryGetTranslation(in request, out string translation));
        Assert.Contains("{0:N0}", translation, StringComparison.Ordinal);
        Assert.True(translation.Length > "{0:N0} rows ready to import.".Length);
        Assert.Equal(expectedRtl, CultureInfo.GetCultureInfo(cultureName).TextInfo.IsRightToLeft);
    }

    [Fact]
    public void String_localizer_uses_explicit_scope_culture_and_restores_ambient_culture()
    {
        var localizer = new AmbientStringLocalizer();
        var adapter = new StringLocalizerOmniTranslationProvider<TestResource>(localizer);
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");

        try
        {
            var request = new OmniTranslationRequest(
                OmniTranslationKeys.Close,
                CultureInfo.GetCultureInfo("fr-FR"),
                null,
                null);

            Assert.True(adapter.TryGetTranslation(in request, out string translation));
            Assert.Equal("fr-FR:Close", translation);
            Assert.Equal("pt-BR", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void String_localizer_restores_ambient_culture_when_provider_throws()
    {
        var adapter = new StringLocalizerOmniTranslationProvider<TestResource>(new ThrowingStringLocalizer());
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            var request = new OmniTranslationRequest("Throw", CultureInfo.GetCultureInfo("ar-SA"), null, null);
            Assert.Throws<InvalidOperationException>(() => adapter.TryGetTranslation(in request, out _));
            Assert.Equal("pt-BR", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Strict_diagnostics_throw_for_partial_foreign_catalog_fallback()
    {
        var services = new ServiceCollection();
        services.AddOmniTranslations("fr", new Dictionary<string, string>
        {
            [OmniTranslationKeys.Close] = "Fermer"
        });
        services.AddOmniComponents(options =>
            options.Localization.MissingTranslationBehavior = OmniMissingTranslationBehavior.Throw);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IOmniLocalizer localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer>();

        Assert.Equal("Fermer", localizer.Localize(OmniTranslationKeys.Close, CultureInfo.GetCultureInfo("fr-FR")).Value);
        Assert.Throws<OmniMissingTranslationException>(() =>
            localizer.Localize(OmniTranslationKeys.Save, CultureInfo.GetCultureInfo("fr-FR")));
    }

    private sealed class TestResource
    {
    }

    private sealed class AmbientStringLocalizer : IStringLocalizer<TestResource>
    {
        public LocalizedString this[string name]
            => new(name, $"{CultureInfo.CurrentUICulture.Name}:{name}", resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class ThrowingStringLocalizer : IStringLocalizer<TestResource>
    {
        public LocalizedString this[string name] => throw new InvalidOperationException("Provider failure");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
