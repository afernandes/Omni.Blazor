# FoodService

Exemplo vertical independente construído sobre o design system **Omni.Blazor**.
Sua landing apresenta duas experiências completas: o PDV operacional e o cardápio
digital voltado ao cliente.

Projeto isolado e independente do `Forneria.Demo` — pode evoluir e ser publicado separadamente.

## Estrutura

```
src/FoodService/
├── FoodService.Pages/        ← RCL (Razor Class Library)
│   ├── _Imports.razor        Imports globais (Omni.Blazor + namespaces locais)
│   ├── Routes.razor          Router das experiências FoodService
│   ├── Layout/
│   │   ├── FoodServiceLandingLayout.razor  Landing do exemplo
│   │   ├── AppShellPdv.razor               Shell operacional do PDV
│   │   └── AppShellCardapio.razor          Shell mobile-first do cardápio
│   └── Pages/
│       ├── FoodServiceHome.razor        Página / — seletor de experiências
│       ├── Pdv.razor                    Página /pdv — frente de caixa
│       ├── CardapioDigital.razor        Página /cardapio — jornada do cliente
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

A rota raiz (`/`) apresenta o projeto. Use `/pdv` para a frente de caixa e
`/cardapio` para a jornada mobile-first do cliente.

## Por que separado do catálogo Omni.Blazor?

- **Independência:** ambos podem evoluir em ritmos diferentes; o FoodService pode receber
  features de cozinha/expedição sem afetar a documentação dos componentes.
- **Mesmo design system:** ambos referenciam `Omni.Blazor` — todo CSS/componente é compartilhado
  via NuGet/ProjectReference, zero duplicação visual.
- **Namespace isolado:** `FoodService.*` não colide com o host do catálogo.

## Convenções herdadas

- `PdvOrderService` é **Scoped** (por-circuito) + dispara `OnChange` em cada mutação.
- Componentes que assinam o serviço fazem `OnInitialized: Order.OnChange += StateHasChanged`
  e `Dispose: Order.OnChange -= StateHasChanged` — convenção `@implements IDisposable`
  leak-safe.
- Visual 100% via classes `.omni-pdv-*` que vivem em `Omni.Blazor/Themes/_demo.scss` →
  compiladas para `omni.css` e servidas via `_content/Omni.Blazor/css/omni.css`.
