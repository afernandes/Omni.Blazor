# Roadmap — Omni.Blazor

> Estado em **2026-07-28** · versão publicada **v0.3.0** (`AndersonN.Omni.Blazor`, `.Ai`, `.Mcp`)
> 174 componentes · 1992 testes · cobertura 81,3% (lib) / 100% (Ai) · lib packable sem warnings (`TreatWarningsAsErrors`)

Consolida três fontes: a **auditoria de hardening** (2026-07-01, 7 PRs mergeados), a **análise de biblioteca + templates** (2026-07-28) e as **pendências de roadmaps anteriores** (gaps de componentes/telas, iniciativa AI-Ready).

**Leitura do momento:** o kit de componentes está maduro e bem documentado no nível de API. O que trava valor hoje **não é o código da lib** — é a **superfície de consumo**: o Quick-start do README não compila, o `Omni.Templates` é invisível/não-consumível, e não existe história de i18n. Priorize nessa ordem.

---

## 1. Lista priorizada

### P0 — Quebra-confiança (fazer já · esforço S)

| # | Item | Área | Esforço |
|---|---|---|---|
| 1 | Quick-start e catálogo do README não compilam (8 componentes fantasma) | Docs | S |
| 2 | `OmniDataGrid` formata agregados com `pt-BR` cravado (bug de i18n) | Lib | S |
| 3 | Links mortos nos templates de Auth | Templates | S |
| 4 | Contagem de componentes divergente entre docs | Docs | S |

### P1 — Fundações que destravam consumidores

| # | Item | Área | Esforço |
|---|---|---|---|
| 5 | Seam central de localização (i18n) | Lib | L |
| 6 | Distribuição + descoberta do `Omni.Templates` | Templates | M |
| 7 | Fim do drift no código copiável dos templates | Templates | M |

### P2 — Cobertura e consistência

| # | Item | Área | Esforço |
|---|---|---|---|
| 8 | App-shell real + variedade de layouts nos templates | Templates | M |
| 9 | Templates para os componentes-flagship (Kanban/Scheduler/Chat/Wizard/Tabs) | Templates | L |
| 10 | Cobertura de showcase (~40 componentes sem página) | Docs | M |
| 11 | Consistência de API pública (`Label`/`Content` → `Text`, `Open`/`IsOpen`, Tabs) | Lib | M |
| 12 | Contrato único de binding multi-valor | Lib | L |
| 13 | Acessibilidade sistêmica dos templates (`label for`, headings) | Templates | M |

### P3 — Features que faltam

| # | Item | Área | Esforço |
|---|---|---|---|
| 14 | `OmniSelect`/`OmniMultiSelect` com dados server-side | Lib | L |
| 15 | Tipos de gráfico faltando (Stacked/Radar/Scatter/Gauge) | Lib | L |
| 16 | Telas SaaS faltando (Onboarding, Billing, Settings com abas, Search, Notificações, CRUD detalhe) | Templates | L |
| 17 | Componentes faltando (`OmniGlobalSearch` full-page, `OmniFileManager`) | Lib | L |

### P4 — Deferidos (reavaliar antes de executar)

| # | Item | Motivo do deferimento |
|---|---|---|
| 18 | Sanitizador de HTML baseado em parser | O regex atual está endurecido; troca só se `AllowHtml` passar a receber conteúdo hostil |
| 19 | `[GeneratedRegex]` no `MarkdownRenderer` | ROI baixo após a memoização; ~32 patterns em arquivo crítico |
| 20 | Virtualizar as listas de chat | A memoização já removeu o reparse; `Virtualize` + altura variável + auto-scroll é frágil |
| 21 | Unificar os "dois mundos" de chat | Modelos genuinamente diferentes (multiusuário vs IA); alto risco de regressão |
| 22 | Gate `dotnet format` na CI | Exige um pass de formatação nos projetos de demo primeiro |
| 23 | Nice-to-haves de IA (token counter real, `OmniAIForm`, `OmniToolCall`) | Sem demanda concreta |

---

## 2. Detalhamento

### P0 · 1. Quick-start e catálogo do README não compilam

**Problema.** O primeiro trecho de código que um consumidor copia **não compila** (`RZ10012`). Oito componentes citados no README **não existem** no código.

**Evidência (verificada).**
`README.md` referencia `OmniHeader`, `OmniSidebar`, `OmniIconButton`, `OmniConfirmDialog`, `OmniAlertDialog`, `OmniOverlays`, `OmniUpload`, `OmniHotkeys` — nenhum existe em `src/Omni.Blazor/Components`.

**Por que importa.** É o pior tipo de bug de onboarding: quebra a confiança no minuto zero, antes de qualquer avaliação da lib.

**Como corrigir.**
1. Reescrever o Quick-start com os componentes reais de layout: `OmniLayout`, `OmniAppBar`, `OmniDrawer`, `OmniMain`, `OmniPanelMenu`, `OmniBrand` (todos existem).
2. **Gerar a seção de catálogo a partir de `docs/components.json`** (já completo e validado na CI por drift-check) para que README e código não possam mais divergir.
3. Compilar o snippet num projeto-teste (ou numa página do Forneria.Demo) como prova.

**Esforço:** S · **Área:** Docs

---

### P0 · 2. `OmniDataGrid` formata agregados com `pt-BR` cravado

**Problema.** Bug real de correção: o rodapé de agregados usa cultura fixa enquanto as células usam `CurrentCulture` — num app `en-US`/`de-DE` o separador decimal do total sai errado ao lado de valores certos.

**Evidência (verificada).** `src/Omni.Blazor/Components/Data/OmniDataGrid.razor:1397-1399`:
```csharp
decimal d => d.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
double dbl => dbl.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
float f => f.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
```
É o **único** hardcode de cultura em saída visível da lib (`OmniNumeric`/`OmniCalendar` usam `CurrentCulture` corretamente).

**Como corrigir.** Trocar por `CultureInfo.CurrentCulture` nas três linhas + teste de regressão com cultura `en-US`.

**Esforço:** S · **Área:** Lib

---

### P0 · 3. Links mortos nos templates de Auth

**Problema.** CTAs apontam para a própria página, então o fluxo de auth do starter não navega.

**Evidência (verificada).** `src/Omni.Templates/Omni.Templates/Pages/Templates/Auth/LoginTemplate.razor`:
- linha 54 — "Esqueci minha senha" → `/templates/auth/login` (deveria ser `/forgot`)
- linha 73 — "Criar conta" → `/templates/auth/login` (deveria ser `/register`)
- `TwoFactorTemplate` — "Usar outro método" com auto-link.

**Como corrigir.** Apontar para as rotas reais (que já existem) **no markup e no bloco de código copiável**.

**Esforço:** S · **Área:** Templates

---

### P0 · 4. Contagem de componentes divergente

**Problema.** O número de componentes aparece diferente em cada documento (77 / 170+ / ~181), e o manifesto diz **174**.

**Como corrigir.** Derivar de `docs/components.json` em todos os pontos (README, AGENTS.md, CLAUDE.md, `<Description>` do NuGet) — de preferência no mesmo passo do item 1.

**Esforço:** S · **Área:** Docs

---

### P1 · 5. Seam central de localização (i18n)

**Problema.** A lib **não tem nenhuma infraestrutura de localização** e tem texto visível/`aria` cravado em pt-BR sem parâmetro de override em vários componentes — um consumidor não-pt-BR precisa forkar a lib, e leitores de tela anunciam "Fechar"/"Ações" num app em inglês.

**Evidência (verificada).**
- Zero ocorrências de `IStringLocalizer`/`ResourceManager`/`.resx` em `src/Omni.Blazor`.
- **17 arquivos** de componente com string pt-BR visível cravada (`DialogHost`, `TourHost`, `Alert`, `Banner`, `CommandPalette`, `Kanban`, `OmniChat`, …).
- Split incoerente: a lib base tem defaults em pt-BR; a `Omni.Blazor.Ai` tem defaults em inglês.

**Como corrigir.**
1. `OmniLocalizationOptions` / `IOmniTextProvider` registrável em `AddOmniComponents(o => o.Texts = …)`, atuando como **fallback abaixo** dos `[Parameter]` existentes (não quebra nada).
2. Promover as strings cravadas a parâmetros com default vindo do provider.
3. Publicar exemplos `en`/`pt` e alinhar o idioma-padrão entre base e `.Ai`.

**Por que P1.** É mecânico mas amplo, e é o **maior desbloqueio** para adoção internacional.

**Esforço:** L · **Área:** Lib

---

### P1 · 6. Distribuição + descoberta do `Omni.Templates`

**Problema.** O projeto chamado "Templates" **não tem como ser consumido nem descoberto**: não é pacote NuGet (`IsPackable=false`), não é `dotnet new` (sem `PackageType=Template`/`.template.config`), não tem README, e o README raiz **nunca o menciona**. Existe só como preview local na porta 5305.

**Como corrigir (decisão recomendada).** Para 24 páginas fortemente customizadas, **copy-paste documentado** é o modelo certo — não `dotnet new` por página.
1. `src/Omni.Templates/README.md` com **índice** (página → rota → componentes usados) e screenshots.
2. Link a partir do README raiz e do CLAUDE.md, com a frase explícita: *"starters de copy-paste, não um pacote"*.
3. Documentar como rodar o host de preview (porta 5305).
4. *(Opcional, alto valor)* **um** template de solução `dotnet new omni-app` — muito mais útil que templatizar 24 páginas.

**Esforço:** M · **Área:** Templates

---

### P1 · 7. Fim do drift no código copiável dos templates

**Problema.** A promessa da galeria é *"copie o código, cole no seu projeto"*, mas o bloco copiável é um **duplicado mantido à mão que já divergiu** — consumidores colam markup silenciosamente quebrado.

**Evidência.** `UsersTemplate.razor` emite `class="u-cell"`, que **não existe em nenhum SCSS**; vários blocos `_code` omitem `PropertyName`/formatters que o grid ao vivo usa.

**Como corrigir (escolher um).**
- **(a) Fonte única:** renderizar o preview a partir da própria string `_code` (impossível divergir); ou
- **(b) Guarda automática:** teste que compara as classes CSS citadas no `_code` com as que existem no bundle e verifica a paridade de parâmetros.

**Esforço:** M · **Área:** Templates

---

### P2 · 8. App-shell real + variedade de layouts

**Problema.** As **24 páginas usam o mesmo layout de galeria**; não há nenhum exemplo do **frame de aplicação** (topbar + sidebar colapsável + conteúdo) — justamente o artefato mais valioso de um starter. O gancho `TemplateDoc.FullScreenPath` (visualização sem o chrome da galeria) existe e é usado **0 vezes**.

**Como corrigir.** Um layout de app-shell autônomo (`OmniAppBar` + `OmniDrawer` responsivo + `OmniMain`) e um master-detail (`OmniSplitView`), ambos expostos em tela cheia via o `FullScreenPath` já existente.

**Esforço:** M · **Área:** Templates

---

### P2 · 9. Templates para os componentes-flagship

**Problema.** Os componentes que **mais diferenciam** a lib e que são os **mais trabalhosos de montar numa tela real** têm **zero** cobertura de template.

**Evidência (verificada).** Nos 24 templates, o uso de `OmniTabs`, `OmniStepper`, `OmniKanban`, `OmniScheduler`, `OmniChat`, `OmniSplitView`, `OmniDrawer`, `OmniGantt`, `OmniPivotGrid`, `OmniDataFilter` é **0**. Os templates só montam primitivos básicos.

**Como corrigir.** Telas realistas: board Kanban, agenda/Scheduler, Chat/Inbox (split), Onboarding com `OmniStepper`, Settings com `OmniTabs`. Cada uma com a página de showcase correspondente (regra do CONTRIBUTING).

**Esforço:** L · **Área:** Templates

---

### P2 · 10. Cobertura de showcase

**Problema.** O CONTRIBUTING exige uma página de showcase por componente; hoje há **110 páginas para 174 componentes**. Descontando ~20 sub-componentes legitimamente cobertos pelo pai, sobram **~40 sem página própria**.

**Como corrigir.**
1. Levantar a lista exata (excluindo sub-componentes, usando a allow-list `TestedViaParent` dos convention tests como referência).
2. Priorizar os de alto uso (`OmniDataGrid`, `OmniAutoComplete`, `OmniFileUpload`, `OmniCalendar`, `OmniDateRangePicker`, …).
3. Estender o `ComponentConventionTests` com uma checagem de "tem showcase" (mesma mecânica do "tem teste"), com allow-list — assim o gap não volta a crescer.

**Esforço:** M · **Área:** Docs

---

### P2 · 11. Consistência de API pública

**Problema.** Atritos localizados que prejudicam descoberta e troca de componentes.

**Evidência (verificada).**
- `OmniFabMenuItem.Label` (linha 42) e `OmniMessage.Content` (linha 58) destoam da convenção `Text` usada no resto da lib.
- Overlays oscilam entre `Open` / `IsOpen` / `Visible`.
- `OmniTabs` não tem binding de aba ativa.

**Como corrigir.** Renomear para `Text` mantendo alias `[Obsolete]` por uma versão; padronizar o booleano de overlay; adicionar `@bind-ActiveIndex` (ou `ActiveTab`) ao `OmniTabs`.

**Esforço:** M · **Área:** Lib

---

### P2 · 12. Contrato único de binding multi-valor

**Problema.** Três APIs incompatíveis para "seleção múltipla": `OmniMultiSelect` (`Values`/`ValuesChanged`, sem `EditContext`), `OmniListBox` (`Value` **e** `Values`), `OmniCheckBoxList` (`Value` como `IEnumerable`). O consumidor não consegue trocar um pelo outro e nem todos integram com validação de formulário.

**Como corrigir.** Unificar em `FormComponent<IEnumerable<TValue>>` com `Value`/`ValueChanged`, mantendo os nomes antigos como alias `[Obsolete]`. **Breaking change controlado** — agendar para uma minor com nota de migração.

**Esforço:** L · **Área:** Lib

---

### P2 · 13. Acessibilidade sistêmica dos templates

**Problema.** Os templates são o material que o consumidor **copia** — os problemas de a11y se propagam para os apps dele. Falta associação `label`↔input (`for`/`id`) e headings semânticos (títulos de auth renderizados como `div` estilizada em vez de `<h1>`).

**Como corrigir.** Passar `for`/`id` nos formulários e usar `<h1>`/`<h2>` reais **no markup e no bloco copiável**.

**Esforço:** M · **Área:** Templates

---

### P3 · 14. `OmniSelect`/`OmniMultiSelect` com dados server-side

**Problema.** Ambos só bindam uma lista **estática em memória** — sem `ItemsProvider`/`LoadData`/virtualização (verificado: 0 ocorrências). Num kit voltado a apps administrativos com muitos dados, conjuntos grandes ou paginados no servidor não têm caminho, empurrando o consumidor para libs de terceiros (e quebrando o theming por tokens, que é o núcleo de valor).

**Como corrigir.** `ItemsProvider` reaproveitando a paginação do `OmniDataGrid` + dropdown virtualizado com `OmniVirtualize` (ambos já existem).

**Esforço:** L · **Área:** Lib

---

### P3 · 15. Tipos de gráfico faltando

**Problema.** `ChartSeriesType` tem **7 tipos** (`Line`, `Area`, `Column`, `Bar`, `Pie`, `Donut`, `Waterfall`) — faltam `StackedColumn`/`StackedBar`, `Radar`, `Scatter`/`Bubble` e `Gauge` radial, que dashboards (e os próprios templates SAAS/Bento do repo) demandam.

**Como corrigir.** Começar por **StackedColumn** (mais barato, reusa o pipeline cartesiano), depois Gauge/Radar/Scatter.

**Esforço:** L · **Área:** Lib

---

### P3 · 16. Telas SaaS faltando

**Problema.** Faltam categorias inteiras de tela que todo SaaS precisa. Além disso, as páginas ditas "agnósticas de produto" carregam dados de demo do domínio Forneria (`forno`, `R$`, `pedido`, nomes) — o consumidor precisa limpar página por página.

**Prioridade sugerida** (herda do roadmap de gaps anterior):
1. **Onboarding/Wizard** (o `OmniStepper` já existe)
2. **Billing/Checkout** (o Pricing existe, mas o fluxo que ele implica não)
3. **Settings com abas** (hoje é uma pilha de cards)
4. **Detalhe/edição de usuário (CRUD)** — par natural do grid de Users
5. **Search/Results** e **Central de notificações**
6. Convites de usuários, chaves de API, Tenants/impersonation, dashboards de domínio, Landing/Docs

**Como corrigir.** Junto com cada tela, neutralizar os dados de demo (ou isolá-los num `MockData` claramente marcado).

**Esforço:** L · **Área:** Templates

---

### P3 · 17. Componentes faltando

Do levantamento de gaps anterior, ainda pendentes: **`OmniGlobalSearch` full-page** (busca global com resultados agrupados) e **`OmniFileManager`/galeria**. A análise nova confirma que o kit é, no geral, **excepcionalmente completo** — estas são faltas estreitas e honestas, junto com um **Signature pad**.

**Esforço:** L · **Área:** Lib

---

### P4 · Deferidos — contexto para reavaliação

| Item | Quando reconsiderar |
|---|---|
| **18.** Sanitizador por parser (`Ganss.Xss`/AngleSharp; DOMPurify no JS) | O regex foi endurecido (entidades numéricas, control chars, atributo sem aspas, `data:` MIME). Trocar **se** `AllowHtml` passar a receber conteúdo não confiável — regex nunca é garantia. |
| **19.** `[GeneratedRegex]` no `MarkdownRenderer` | Se profiling mostrar o parser como gargalo. A memoização já removeu o custo por render. |
| **20.** Virtualizar listas de chat | Se surgirem chats com centenas de mensagens. Hoje o custo dominante já foi eliminado. |
| **21.** Unificar os "dois mundos" de chat | Se `OmniChat` e a stack de IA passarem a compartilhar requisitos de verdade. Hoje `UserId` (multiusuário) vs `Role` (IA) é separação legítima. |
| **22.** Gate `dotnet format` na CI | Depois de um pass de formatação dedicado nos projetos de demo (a lib já está limpa). |
| **23.** `OmniAIForm`, `OmniToolCall`, token counter real | Quando houver caso de uso concreto. |

---

## 3. Sequência recomendada

1. **Sprint "confiança"** — P0 inteiro (itens 1–4) num único PR pequeno. Alto impacto, baixo risco.
2. **Sprint "templates consumíveis"** — itens 6 + 7 + 8 (README/índice, fim do drift, app-shell). Transforma o ativo mais subutilizado do repo em entregável.
3. **Sprint "i18n"** — item 5. Maior desbloqueio de adoção; mecânico, mas amplo.
4. **Depois:** P2 (consistência/cobertura) e P3 (features), conforme demanda real de consumidores.

> Convenções obrigatórias para qualquer item: testes junto (cobertura ≥ 80%), lib packable sem warnings (`TreatWarningsAsErrors`), showcase por componente novo, e regenerar o manifesto (`dotnet run --project tools/Omni.Blazor.ManifestGen`) quando a API pública mudar.
