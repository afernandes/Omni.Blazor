# FoodService

PDV (Frente de Caixa) standalone construído sobre o design system **Omni.Blazor**.

Projeto isolado e independente do `Forneria.Demo` — pode evoluir e ser publicado separadamente.

## Estrutura

```
src/FoodService/
├── FoodService.Pages/        ← RCL (Razor Class Library)
│   ├── _Imports.razor        Imports globais (Omni.Blazor + namespaces locais)
│   ├── Routes.razor          Router (default layout = AppShellPdv)
│   ├── Layout/
│   │   └── AppShellPdv.razor Top bar + sidebar (OmniPanelMenu) + body
│   └── Pages/
│       ├── Pdv.razor                    Página /(/pdv) — frente de caixa
│       └── PdvFeature/
│           ├── PdvModels.cs             OrderMode, Product, Customer, CartItem, PizzaHalf, ModeDetails
│           ├── PdvMockData.cs           Catálogo, clientes, bairros, garçons
│           ├── PdvOrderService.cs       Scoped DI service + OnChange event
│           └── Components/              9 sub-componentes (Cart, ModeTabs, etc.)
│
└── FoodService/              ← Server host (Blazor Server interactive)
    ├── Components/
    │   ├── App.razor         HTML root + OmniTheme + assets
    │   └── _Imports.razor
    ├── FoodService.csproj
    └── Program.cs            DI + render mode + assemblies map
```

## Como rodar

```bash
dotnet run --project src/FoodService/FoodService
# Server: https://localhost:7301
```

A rota raiz (`/`) renderiza diretamente o PDV.

## Por que separado do Forneria.Demo?

- **Independência:** ambos podem evoluir em ritmos diferentes; ex.: FoodService pode receber
  features de cozinha/expedição sem afetar o Forneria.
- **Mesmo design system:** ambos referenciam `Omni.Blazor` — todo CSS/componente é compartilhado
  via NuGet/ProjectReference, zero duplicação visual.
- **Namespace isolado:** `FoodService.*` não colide com `Forneria.Demo.*`.

## Convenções herdadas

- `PdvOrderService` é **Scoped** (por-circuito) + dispara `OnChange` em cada mutação.
- Componentes que assinam o serviço fazem `OnInitialized: Order.OnChange += StateHasChanged`
  e `Dispose: Order.OnChange -= StateHasChanged` — convenção `@implements IDisposable`
  leak-safe.
- Visual 100% via classes `.omni-pdv-*` que vivem em `Omni.Blazor/Themes/_demo.scss` →
  compiladas para `omni.css` e servidas via `_content/Omni.Blazor/css/omni.css`.
