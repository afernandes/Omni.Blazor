using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor;
using Omni.Blazor.Localization;
using Omni.Localization;

namespace Omni.Blazor.Tests.Localization;

/// <summary>
/// The localization seam: components fall back to <see cref="OmniTexts"/> for the strings
/// they render themselves. Registering a set translates the whole library at once; passing
/// nothing must keep the historical pt-BR output byte-for-byte.
/// </summary>
public class OmniTextsTests : TestContextBase
{
    // ── The text set itself ───────────────────────────────────────────────

    [Fact]
    public void Default_is_pt_br()
    {
        Assert.Equal("Fechar", OmniTexts.Default.Close);
        Assert.Equal("Limpar", OmniTexts.Default.Clear);
        Assert.Equal("Ações", OmniTexts.Default.Actions);
    }

    [Fact]
    public void English_defines_every_key()
    {
        var en = OmniTexts.English();

        foreach (var p in typeof(OmniTexts).GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            var enValue = (string?)p.GetValue(en);
            Assert.False(string.IsNullOrWhiteSpace(enValue), $"{p.Name} has no English value");
        }

        Assert.Equal("Close", en.Close);
        Assert.Equal("No records found.", en.NoRecords);
        Assert.Equal("Grand total", en.GrandTotal);
    }

    [Fact]
    public void Stable_keys_cover_every_public_text_property()
    {
        string[] properties = typeof(OmniTexts).GetProperties()
            .Where(static property => property.PropertyType == typeof(string))
            .Select(static property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] keys = OmniTranslationKeys.All.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(properties, keys);
    }

    [Fact]
    public void Embedded_catalogs_cover_every_stable_key()
    {
        var services = new ServiceCollection();
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();

        foreach (string cultureName in new[] { "pt-BR", "en-US" })
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            foreach (string key in OmniTranslationKeys.AllCatalogKeys)
            {
                OmniLocalizedString value = localizer.Localize(key, culture);
                Assert.False(value.ResourceNotFound, $"{cultureName} is missing {key}");
                Assert.False(string.IsNullOrWhiteSpace(value.Value), $"{cultureName}:{key} is empty");
            }
        }
    }

    [Fact]
    public void English_returns_a_fresh_instance_each_call()
    {
        var a = OmniTexts.English();
        a.Close = "mutated";
        Assert.Equal("Close", OmniTexts.English().Close); // not shared state
    }

    [Fact]
    public void Fixed_builtin_text_sets_use_real_singular_and_plural_forms_without_di()
    {
        Assert.Equal(
            "1 linha pronta para importar.",
            OmniTexts.Default.Plural(OmniTranslationKeys.DataImportReady, 1, string.Empty, 1));
        Assert.Equal(
            "2 rows ready to import.",
            OmniTexts.English().Plural(OmniTranslationKeys.DataImportReady, 2, string.Empty, 2));
    }

    // ── DI registration ───────────────────────────────────────────────────

    [Fact]
    public void AddOmniComponents_registers_the_default_texts()
    {
        // The registered set now resolves through the localizer, so it follows
        // CurrentUICulture: this same assertion returned "Close" on an en-US agent while
        // passing on a pt-BR one. The UI culture is pinned because what this test is about
        // is the DI registration, not which language a given machine happens to be in.
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture =
                new System.Globalization.CultureInfo("pt-BR");

            var services = new ServiceCollection();
            services.AddOmniComponents();
            using var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            Assert.Equal("Fechar", scope.ServiceProvider.GetRequiredService<OmniTexts>().Close);
            Assert.IsAssignableFrom<IOmniLocalizer<OmniBlazorResource>>(
                scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void AddOmniComponents_registers_the_configured_texts()
    {
        var en = OmniTexts.English();
        var services = new ServiceCollection();
        services.AddOmniComponents(o => o.Texts = en);
        using var provider = services.BuildServiceProvider();

        Assert.Same(en, provider.GetService<OmniTexts>());
    }

    [Fact]
    public void A_previously_registered_OmniTexts_wins()
    {
        // e.g. a consumer registering a scoped set fed by their own IStringLocalizer
        var mine = new OmniTexts { Close = "Dismiss" };
        var services = new ServiceCollection();
        services.AddSingleton(mine);
        services.AddOmniComponents(o => o.Texts = OmniTexts.English());
        using var provider = services.BuildServiceProvider();

        Assert.Same(mine, provider.GetService<OmniTexts>());
    }

    // ── Components actually use it ────────────────────────────────────────

    [Fact]
    public void Component_uses_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniAlert>(p => p.Add(c => c.Dismissible, true));

        Assert.Equal("Close", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    [Fact]
    public void Component_falls_back_to_pt_br_when_nothing_is_registered()
    {
        // Historical behaviour must be untouched for apps that never configure texts.
        var cut = Render<OmniAlert>(p => p.Add(c => c.Dismissible, true));

        Assert.Equal("Fechar", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    // ── [Parameter] defaults now come from the seam too ───────────────────

    [Fact]
    public void Parameter_default_follows_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniLayout>(p => p.Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Skip to content", cut.Find("a.omni-skip-link").TextContent);
    }

    [Fact]
    public void Parameter_default_is_pt_br_without_registration()
    {
        var cut = Render<OmniLayout>(p => p.Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Pular para o conteúdo", cut.Find("a.omni-skip-link").TextContent);
    }

    [Fact]
    public void An_explicit_parameter_still_wins_over_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniLayout>(p => p
            .Add(c => c.SkipLabel, "Ir para o conteúdo")
            .Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Ir para o conteúdo", cut.Find("a.omni-skip-link").TextContent);
    }

    [Fact]
    public void Built_in_resources_follow_current_ui_culture_without_recreating_the_scope()
    {
        var services = new ServiceCollection();
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var texts = scope.ServiceProvider.GetRequiredService<OmniTexts>();
        CultureInfo previous = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
            Assert.Equal("Fechar", texts.Close);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("Close", texts.Close);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Custom_catalog_overrides_one_key_and_preserves_built_in_fallback()
    {
        var services = new ServiceCollection();
        services.AddOmniTranslations("fr", new Dictionary<string, string>
        {
            [OmniTranslationKeys.Close] = "Fermer"
        });
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();
        var culture = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Equal("Fermer", localizer.Localize(OmniTranslationKeys.Close, culture).Value);
        Assert.Equal("Salvar", localizer.Localize(OmniTranslationKeys.Save, culture).Value);
    }

    [Fact]
    public void Pluralization_supports_language_specific_categories()
    {
        var services = new ServiceCollection();
        services.AddOmniTranslations("ar", new Dictionary<string, string>
        {
            ["Items.Zero"] = "لا عناصر",
            ["Items.One"] = "عنصر واحد",
            ["Items.Two"] = "عنصران",
            ["Items.Few"] = "{0} عناصر",
            ["Items.Many"] = "{0} عنصرًا",
            ["Items.Other"] = "{0} عنصر"
        });
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();
        var culture = CultureInfo.GetCultureInfo("ar");

        Assert.Equal("لا عناصر", localizer.Plural("Items", 0, culture, 0));
        Assert.Equal("عنصران", localizer.Plural("Items", 2, culture, 2));
        Assert.Equal("5 عناصر", localizer.Plural("Items", 5, culture, 5));
    }

    [Fact]
    public void Pluralization_falls_back_to_other_when_a_catalog_omits_a_language_specific_category()
    {
        var services = new ServiceCollection();
        services.AddOmniTranslations("ar", new Dictionary<string, string>
        {
            ["Items.Other"] = "{0} عنصر"
        });
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();

        Assert.Equal("5 عنصر", localizer.Plural("Items", 5, CultureInfo.GetCultureInfo("ar"), 5));
    }

    [Fact]
    public void Culture_scope_sets_language_direction_and_localizes_descendants()
    {
        Services.AddOmniComponents();

        var cut = Render<OmniCultureScope>(parameters => parameters
            .Add(component => component.UICulture, CultureInfo.GetCultureInfo("en-US"))
            .AddChildContent<OmniAlert>(alert => alert.Add(component => component.Dismissible, true)));

        Assert.Equal("en-US", cut.Find(".omni-culture-scope").GetAttribute("lang"));
        Assert.Equal("ltr", cut.Find(".omni-culture-scope").GetAttribute("dir"));
        Assert.Equal("Close", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    [Fact]
    public void Culture_scope_infers_rtl_from_ui_culture()
    {
        var cut = Render<OmniCultureScope>(parameters => parameters
            .Add(component => component.UICulture, CultureInfo.GetCultureInfo("ar-SA"))
            .AddChildContent("<span>مرحبا</span>"));

        Assert.Equal("rtl", cut.Find(".omni-culture-scope").GetAttribute("dir"));
    }
}
