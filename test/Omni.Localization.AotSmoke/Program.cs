using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Omni.Localization;
using Omni.Localization.Json;

var services = new ServiceCollection();
services.AddOmniLocalizationResource<AotResource>("pt-BR", "en", new Dictionary<string, string>
{
    ["Greeting"] = "Hello",
    ["Items.One"] = "{0} item",
    ["Items.Other"] = "{0} items"
});
services.AddOmniJsonTranslations<AotResource>(
    """{"culture":"pt-BR","texts":{"Greeting":"Olá","Items.One":"{0} item","Items.Other":"{0} itens"}}"""u8.ToArray());
services.AddOmniPseudoLocalization<AotResource>();

using ServiceProvider provider = services.BuildServiceProvider();
IOmniLocalizer<AotResource> localizer = provider.GetRequiredService<IOmniLocalizer<AotResource>>();
bool works = localizer.Localize("Greeting", CultureInfo.GetCultureInfo("pt-BR")).Value == "Olá"
    && localizer.Plural("Items", 2, CultureInfo.GetCultureInfo("pt-BR"), 2) == "2 itens"
    && localizer.Localize("Greeting", CultureInfo.GetCultureInfo("en-XA")).Value.StartsWith('［');

Console.WriteLine(works
    ? "Omni.Localization Native AOT smoke test passed."
    : "Omni.Localization Native AOT smoke test failed.");
return works ? 0 : 1;

internal sealed class AotResource;
