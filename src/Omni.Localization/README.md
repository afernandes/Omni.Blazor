# Omni.Localization

`AndersonN.Omni.Localization` is the UI-framework-independent localization engine used by Omni.Blazor.
It provides typed resource boundaries, deterministic culture fallback, plural forms, catalog validation,
pseudolocales and adapters for the standard .NET `IStringLocalizer<T>` pipeline.

```csharp
public sealed class AppResource;

services.AddOmniLocalization();
services.AddOmniLocalizationResource<AppResource>(
    defaultCulture: "pt-BR",
    referenceCulture: "pt-BR",
    referenceTranslations: new Dictionary<string, string>
    {
        ["Home.Title"] = "Início"
    });
services.AddOmniTranslations<AppResource>("en", new Dictionary<string, string>
{
    ["Home.Title"] = "Home"
});
```

Inject `IOmniLocalizer<AppResource>` in any .NET application. The package has no dependency on
Blazor, Razor, JavaScript or static web assets.
