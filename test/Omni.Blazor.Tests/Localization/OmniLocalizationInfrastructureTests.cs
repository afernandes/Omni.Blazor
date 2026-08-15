using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Omni.Blazor.Localization;
using Omni.Localization;

namespace Omni.Blazor.Tests.Localization;

public sealed class OmniLocalizationInfrastructureTests
{
    [Fact]
    public void Built_in_resource_validator_rejects_placeholder_drift()
    {
        var services = new ServiceCollection();
        services.AddOmniComponents();
        using ServiceProvider provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<OmniTranslationCatalogValidator<OmniBlazorResource>>();

        OmniTranslationCatalogValidationResult result = validator.Validate("fr-FR",
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
    public void Built_in_resource_validator_can_require_every_stable_key()
    {
        var services = new ServiceCollection();
        services.AddOmniComponents();
        using ServiceProvider provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<OmniTranslationCatalogValidator<OmniBlazorResource>>();

        OmniTranslationCatalogValidationResult result = validator.Validate("fr",
            new Dictionary<string, string> { [OmniTranslationKeys.Close] = "Fermer" },
            requireCompleteCatalog: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Key == OmniTranslationKeys.Save);
    }

    [Theory]
    [InlineData("en-XA", false)]
    [InlineData("ar-XB", true)]
    public void Pseudo_locales_expand_built_in_text_and_preserve_placeholders(
        string cultureName,
        bool expectedRtl)
    {
        var services = new ServiceCollection();
        services.AddOmniPseudoLocalization();
        services.AddOmniComponents();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<OmniBlazorResource> localizer =
            provider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();

        string translation = localizer.Plural(
            OmniTranslationKeys.DataImportReady,
            2,
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Contains("{0:N0}", translation, StringComparison.Ordinal);
        Assert.True(translation.Length > "{0:N0} rows ready to import.".Length);
        Assert.Equal(expectedRtl, CultureInfo.GetCultureInfo(cultureName).TextInfo.IsRightToLeft);
    }

    [Fact]
    public void Standard_localizer_source_restores_ambient_culture_when_it_throws()
    {
        var adapter = new StringLocalizerOmniTranslationProvider<OmniBlazorResource, TestResource>(
            new ThrowingStringLocalizer());
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            var request = new OmniTranslationRequest(
                "Throw",
                CultureInfo.GetCultureInfo("ar-SA"),
                null,
                null);
            Assert.Throws<InvalidOperationException>(() => adapter.TryGetTranslation(in request, out _));
            Assert.Equal("pt-BR", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Strict_diagnostics_throw_when_foreign_catalog_reaches_default_resource()
    {
        var services = new ServiceCollection();
        services.AddOmniTranslations("fr", new Dictionary<string, string>
        {
            [OmniTranslationKeys.Close] = "Fermer"
        });
        services.AddOmniComponents(options =>
            options.Localization.MissingTranslationBehavior = OmniMissingTranslationBehavior.Throw);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<OmniBlazorResource> localizer =
            provider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();

        Assert.Equal("Fermer", localizer.Localize(
            OmniTranslationKeys.Close,
            CultureInfo.GetCultureInfo("fr-FR")).Value);
        Assert.Throws<OmniMissingTranslationException>(() => localizer.Localize(
            OmniTranslationKeys.Save,
            CultureInfo.GetCultureInfo("fr-FR")));
    }

    private sealed class TestResource;

    private sealed class ThrowingStringLocalizer : IStringLocalizer<TestResource>
    {
        public LocalizedString this[string name] => throw new InvalidOperationException("Provider failure");
        public LocalizedString this[string name, params object[] arguments] => this[name];
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
