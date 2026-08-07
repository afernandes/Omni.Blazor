# Omni.Blazor

Biblioteca de componentes Blazor para **.NET 10** — 206 componentes prontos para
apps de negócio (PDV, CRUD, dashboards): DataGrid completo, DatePicker, Dialog,
Stepper, AutoComplete, FileUpload, e muito mais. Design system cream/amber com
dark mode e troca de acento em runtime.

## Instalação

```bash
dotnet add package AndersonN.Omni.Blazor
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

**4. JavaScript** — nenhum `<script>` manual é necessário. Módulos isolados por
domínio são importados e armazenados em cache somente quando um componente usa
aquela funcionalidade, pelos serviços registrados em `AddOmniComponents()`.

**5. Host de overlays** — uma vez no layout raiz (Dialog, Notification,
Tooltip e ContextMenu compartilham este host):

```razor
<OmniOverlayHosts />
```

**6. Use os componentes:**

```razor
<OmniButton Text="Olá" Variant="ButtonVariant.Primary" OnClick="OnClick" />
```

Os módulos JavaScript são importados automaticamente por capacidade e somente
quando utilizados. Cada módulo possui ciclo de vida e cache próprios no escopo
DI; o consumidor não adiciona scripts nem configura um resolvedor global.

## Native AOT e trimming

O pacote habilita os analisadores de trimming e Native AOT. Em hosts Native AOT,
use schemas tipados, configure `AutoGenerateFields(false)` no DataForm e forneça
uma `.Factory(...)` explícita ao DataImport. A geração automática de campos por
reflection continua disponível apenas quando o runtime suporta código dinâmico.

## Render mode

Os componentes exigem interatividade — use `InteractiveServer` ou
`InteractiveWebAssembly` no host. Render mode fixo por host é recomendado
(evita problemas conhecidos do `InteractiveAuto` no .NET 10).

## Licença

MIT.
