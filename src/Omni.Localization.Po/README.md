# Omni.Localization.Po

Optional PO/Gettext support for `AndersonN.Omni.Localization`, backed by Orchard Core.
Use it from ASP.NET Core or a .NET Generic Host: Orchard resolves catalogs from
`IHostEnvironment.ContentRootFileProvider`. The localization core and JSON provider do
not require a host and also work with a plain `ServiceCollection`.

```csharp
services.AddOmniLocalizationResource<AppResource>(
    defaultCulture: "pt-BR",
    referenceCulture: "pt-BR",
    referenceTranslations: referenceCatalog);
services.AddOmniPortableObjectLocalization<AppResource>("Localization");
```

When the PO `msgctxt` marker differs from the target resource, use the two-type overload:

```csharp
services.AddOmniPortableObjectLocalization<OmniBlazorResource, SharedResources>();
```
