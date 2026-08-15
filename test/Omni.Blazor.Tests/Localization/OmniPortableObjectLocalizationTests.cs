using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omni.Blazor.Localization;
using Omni.Localization;
using Omni.Localization.Po;

namespace Omni.Blazor.Tests.Localization;

public sealed class OmniPortableObjectLocalizationTests
{
    [Fact]
    public void Po_adapter_resolves_explicit_scope_culture_and_plural_form()
    {
        using IHost host = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddOmniPortableObjectLocalization<OmniBlazorResource, PoResource>("Localization/PoFiles");
                services.AddOmniComponents();
            })
            .Build();
        using IServiceScope scope = host.Services.CreateScope();
        IOmniLocalizer<OmniBlazorResource> localizer =
            scope.ServiceProvider.GetRequiredService<IOmniLocalizer<OmniBlazorResource>>();
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Equal("Fermer via PO", localizer.Localize(OmniTranslationKeys.Close, culture).Value);
        Assert.Equal(
            "2 lignes prêtes à importer.",
            localizer.Plural(OmniTranslationKeys.DataImportReady, 2, culture, 2));
    }
}

public sealed class PoResource
{
}
