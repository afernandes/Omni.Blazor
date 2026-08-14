# Omni.Blazor.Localization.Po

Adaptador opcional de PO/Gettext para o pipeline tipado de localização do Omni.Blazor.
O pacote principal permanece sem dependência do Orchard Core.

```csharp
using Omni.Blazor.Localization.Po;

builder.Services.AddOmniPortableObjectLocalization<SharedResources>("Localization");
builder.Services.AddOmniComponents();
```

Crie `Localization/fr.po` e use as chaves estáveis de `OmniTranslationKeys` como
`msgid`. O `msgctxt` deve ser o nome completo do tipo marcador `SharedResources`.

```po
msgctxt "MyApp.SharedResources"
msgid "Close"
msgstr "Fermer"
```
