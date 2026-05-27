# Omni.Blazor

Biblioteca de componentes Blazor para **.NET 10** — 77 componentes prontos para
apps de negócio (PDV, CRUD, dashboards): DataGrid completo, DatePicker, Dialog,
Stepper, AutoComplete, FileUpload, e muito mais. Design system cream/amber com
dark mode e troca de acento em runtime.

## Instalação

```bash
dotnet add package Omni.Blazor
```

## Primeiros passos

**1. Imports** — em `_Imports.razor`:

```razor
@using Omni.Blazor
@using Omni.Blazor.Components
@using Omni.Blazor.Models
@using Omni.Blazor.Services
```

**2. Serviços** — em `Program.cs`:

```csharp
builder.Services.AddOmniComponents();
```

**3. Tema** — no `<head>` do `App.razor`:

```razor
<OmniTheme Accent="amber" />
```

> Acentos: `amber` (padrão), `emerald`, `blue`, `violet`, `crimson`.
> Use `Dark="true"` para iniciar em dark mode.

**4. JavaScript** — no `App.razor`, antes de `blazor.*.js`:

```html
<script src="_content/Omni.Blazor/js/omni.js"></script>
```

**5. Host de overlays** — uma vez no layout raiz (Dialog, Notification,
Tooltip e ContextMenu compartilham este host):

```razor
<OmniOverlayHosts />
```

**6. Use os componentes:**

```razor
<OmniButton Text="Olá" Variant="ButtonVariant.Primary" OnClick="OnClick" />
```

## Render mode

Os componentes exigem interatividade — use `InteractiveServer` ou
`InteractiveWebAssembly` no host. Render mode fixo por host é recomendado
(evita problemas conhecidos do `InteractiveAuto` no .NET 10).

## Licença

MIT.
