using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Omni.Localization.Json;

namespace Omni.Localization.Tests;

public sealed class LocalizationEngineTests
{
    private static readonly Dictionary<string, string> Reference = new(StringComparer.Ordinal)
    {
        ["Greeting"] = "Hello",
        ["Items.One"] = "{0} item",
        ["Items.Other"] = "{0} items"
    };

    [Fact]
    public void Typed_resources_isolate_equal_keys()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        services.AddOmniLocalizationResource<SecondResource>("en", "en",
            new Dictionary<string, string> { ["Greeting"] = "Different" });
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("Hello", provider.GetRequiredService<IOmniLocalizer<FirstResource>>()["Greeting"]);
        Assert.Equal("Different", provider.GetRequiredService<IOmniLocalizer<SecondResource>>()["Greeting"]);
    }

    [Fact]
    public void Exact_regional_catalog_wins_even_when_neutral_catalog_was_registered_first()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        services.AddOmniTranslations<FirstResource>("fr", new Dictionary<string, string>
        {
            ["Greeting"] = "Bonjour"
        });
        services.AddOmniTranslations<FirstResource>("fr-CA", new Dictionary<string, string>
        {
            ["Greeting"] = "Allô"
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<FirstResource> localizer = provider.GetRequiredService<IOmniLocalizer<FirstResource>>();

        OmniLocalizedString exact = localizer.Localize("Greeting", CultureInfo.GetCultureInfo("fr-CA"));
        OmniLocalizedString parent = localizer.Localize("Greeting", CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Equal("Allô", exact.Value);
        Assert.False(exact.UsedFallback);
        Assert.Equal("Bonjour", parent.Value);
        Assert.True(parent.UsedFallback);
        Assert.Equal("fr", parent.ResolvedCulture?.Name);
    }

    [Fact]
    public void Strict_diagnostics_allow_parent_culture_but_reject_default_fallback()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalization(options =>
            options.MissingTranslationBehavior = OmniMissingTranslationBehavior.Throw);
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        services.AddOmniTranslations<FirstResource>("fr", new Dictionary<string, string>
        {
            ["Greeting"] = "Bonjour"
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<FirstResource> localizer = provider.GetRequiredService<IOmniLocalizer<FirstResource>>();

        Assert.Equal("Bonjour", localizer.Localize("Greeting", CultureInfo.GetCultureInfo("fr-FR")).Value);
        Assert.Throws<OmniMissingTranslationException>(() =>
            localizer.Localize("Items.One", CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void Catalog_validation_is_scoped_to_its_resource()
    {
        var resource = new OmniLocalizationResource<FirstResource>("en", "en", Reference);
        var validator = new OmniTranslationCatalogValidator<FirstResource>(resource);
        OmniTranslationCatalogValidationResult result = validator.Validate("fr", new Dictionary<string, string>
        {
            ["OtherResource.Key"] = "Valeur"
        });

        Assert.True(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(OmniTranslationCatalogIssueSeverity.Warning, result.Issues[0].Severity);
    }

    [Fact]
    public void Pseudolocalization_uses_the_resource_reference_catalog()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        services.AddOmniPseudoLocalization<FirstResource>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<FirstResource> localizer = provider.GetRequiredService<IOmniLocalizer<FirstResource>>();

        string result = localizer.Localize("Greeting", CultureInfo.GetCultureInfo("en-XA")).Value;

        Assert.StartsWith("［", result, StringComparison.Ordinal);
        Assert.True(result.Length > "Hello".Length);
    }

    [Fact]
    public void Json_catalog_flattens_nested_values_and_later_documents_override()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<JsonResource>("pt-BR", "pt-BR",
            new Dictionary<string, string> { ["Home__Title"] = "Início" });
        services.AddOmniJsonTranslations<JsonResource>(
        [
            """{"culture":"en","texts":{"Home":{"Title":"Home"}}}""",
            """{"culture":"en","texts":{"Home":{"Title":"Start"}}}"""
        ]);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("Start", provider.GetRequiredService<IOmniLocalizer<JsonResource>>()
            .Localize("Home__Title", CultureInfo.GetCultureInfo("en-US")).Value);
    }

    [Fact]
    public void Localizable_text_round_trips_fixed_and_late_bound_values()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<FirstResource> localizer = provider.GetRequiredService<IOmniLocalizer<FirstResource>>();

        OmniLocalizableText<FirstResource> localized =
            OmniLocalizableText<FirstResource>.Parse("L:Greeting");
        OmniLocalizableText<FirstResource> fixedText =
            OmniLocalizableText<FirstResource>.Parse("F:Literal");

        Assert.Equal("Hello", localized.Resolve(localizer));
        Assert.Equal("Literal", fixedText.Resolve(localizer));
        Assert.Equal("L:Greeting", localized.ToString());
    }

    [Fact]
    public void Standard_localizer_adapter_exposes_the_typed_resource_without_Blazor()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<FirstResource>("en", "en", Reference);
        services.AddOmniStringLocalizerAdapter<FirstResource>();
        using ServiceProvider provider = services.BuildServiceProvider();

        IStringLocalizer<FirstResource> localizer = provider.GetRequiredService<IStringLocalizer<FirstResource>>();

        Assert.Equal("Hello", localizer["Greeting"].Value);
    }

    [Fact]
    public void Typed_base_resource_is_consulted_after_the_derived_default_chain()
    {
        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<BaseResource>("en", "en",
            new Dictionary<string, string> { ["Shared.Action"] = "Continue" });
        services.AddOmniLocalizationResource<DerivedResource>("en", "en",
            new Dictionary<string, string> { ["Own.Title"] = "Checkout" });
        services.AddOmniBaseResource<DerivedResource, BaseResource>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOmniLocalizer<DerivedResource> localizer =
            provider.GetRequiredService<IOmniLocalizer<DerivedResource>>();

        Assert.Equal("Checkout", localizer["Own.Title"]);
        Assert.Equal("Continue", localizer["Shared.Action"]);
    }

    [Fact]
    public void Resource_inheritance_cycles_are_rejected_at_registration()
    {
        var services = new ServiceCollection();
        services.AddOmniBaseResource<FirstResource, SecondResource>();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddOmniBaseResource<SecondResource, FirstResource>());
    }

    private sealed class FirstResource;
    private sealed class SecondResource;
    private sealed class JsonResource;
    private sealed class BaseResource;
    private sealed class DerivedResource;
}
