# Roadmap — Omni.Blazor

> Estado em **2026-08-19** · versão publicada **v0.9.0** (`AndersonN.Omni.Blazor`, `.Ai`, `.Mcp`, `.Localization`, `.Localization.Json`, `.Localization.Po`)
> 209 componentes catalogados · **2.514 testes passando** (1 ignorado, 0 falhas — suíte completa: lib, AI/MCP/gerador, localização, browser) · pacotes com validação de compatibilidade e warnings como erro
>
> *(Cobertura por projeto — %, não só contagem — não foi remedida nesta revisão; a última leitura confiável dessa métrica é a da auditoria 2026-07-31 abaixo, tratar como desatualizada.)*

Consolida três fontes: a **auditoria de hardening** (2026-07-01, 7 PRs), a **análise de biblioteca + templates** (2026-07-28) e as **pendências de roadmaps anteriores** (gaps de componentes/telas, AI-Ready).

**Leitura do momento.** As três travas da análise anterior — Quick-start, templates
consumíveis e seam de i18n — foram endereçadas. A auditoria de 2026-07-31 elevou
também o piso de engenharia: catálogo completo, SDK estável, validação assíncrona
latest-wins, descarte cancelável, snapshots seguros e gates de vulnerabilidade e
sync-over-async. O P0 de providers e distribuição também foi concluído: fontes remotas
canceláveis, export CSV limitado/streaming e smoke tests dos pacotes reais. O próximo
salto de maturidade depende de **testes reais de browser e acessibilidade**,
**benchmarks com orçamento de alocação** e **migração gradual do JS global para módulos tipados**.

## Marco de engenharia — auditoria 2026-07-31

| Área | Resultado |
|---|---|
| Estrutura | Componentes complexos podem usar `.razor` + `.razor.cs`; `.razor.css` fica reservado a estilos privados. O bundle global continua sendo a fonte de tokens e variantes. |
| Formulários | Validação assíncrona cancelável e latest-wins; submissão aguarda validações; stores sync/async não se apagam mutuamente; descarte remove eventos e cancela trabalho. |
| Concorrência | Chat de IA, DataGrid, Carousel, Chat e notificações tiveram corridas, callbacks temporizados e tarefas não observadas endurecidos. Eventos não são disparados dentro de locks. |
| Memória | Cache de reflexão usa chave fraca e não armazena consultas inválidas; serviços temporizados cancelam e liberam seus recursos; listas concorrentes são expostas por snapshots estáveis. |
| Catálogo | O gerador passou a descobrir todo `ComponentBase` público apoiado por fonte; tipos internos usam exclusão explícita. Catálogo, `llms*.txt` e MCP concordam em 198 componentes. |
| Toolchain | SDK 10 estável fixado, dependências atualizadas, Package Validation habilitado e CI com gates de vulnerabilidade e segurança assíncrona. |
| Performance | Evitou-se LINQ em caminhos quentes revisados e reduziram-se recomputações/cópias desnecessárias. `Span<T>`, `Memory<T>` e pooling só serão adotados quando benchmark comprovar benefício e o lifetime for seguro. |

**Referência Radzen.** O checkout usado na comparação possui 254 arquivos `.razor`,
190 code-behinds e nenhum `.razor.css`. A conclusão não é copiar a proporção, mas
adotar a separação onde há lifecycle, JS interop ou orquestração assíncrona. A
estratégia de um bundle temático central do Omni continua coerente; CSS isolado deve
ser exceção para detalhes semânticos privados, não para tokens ou overlays.

---

## 0. Concluído

| # | Item | PR |
|---|---|---|
| 1 | Quick-start e catálogo do README não compilavam (8 componentes fantasma + params errados) | [#28](https://github.com/afernandes/Omni.Blazor/pull/28) |
| 2 | `OmniDataGrid` formatava agregados com `pt-BR` cravado (bug de cultura) | [#28](https://github.com/afernandes/Omni.Blazor/pull/28) |
| 3 | Links mortos nos templates de Auth | [#28](https://github.com/afernandes/Omni.Blazor/pull/28) |
| 4 | Contagem de componentes divergente entre docs | [#28](https://github.com/afernandes/Omni.Blazor/pull/28) |
| 6 | Distribuição + descoberta do `Omni.Templates` (README-índice das 26 páginas + link no README raiz) | [#29](https://github.com/afernandes/Omni.Blazor/pull/29) |
| 7 | Drift do código copiável (16 classes inexistentes em 10 templates) + guarda na CI | [#29](https://github.com/afernandes/Omni.Blazor/pull/29) |
| 8 | App-shell real + `FullScreenPath` finalmente vivo | [#29](https://github.com/afernandes/Omni.Blazor/pull/29) |
| 5a | Seam de localização (`OmniTexts` + `AddOmniComponents(o => o.Texts = …)`) e as 17 strings que **não tinham override** | [#30](https://github.com/afernandes/Omni.Blazor/pull/30) |
| 25 | Catálogo completo de componentes públicos + exclusão explícita de implementações internas | auditoria 2026-07-31 |
| 26 | Hardening de validação assíncrona, submissão de formulário e descarte | auditoria 2026-07-31 |
| 27 | IDs DOM estáveis e respeito ao `id` fornecido pelo consumidor | auditoria 2026-07-31 |
| 28 | Gates de dependências vulneráveis e padrões async perigosos | auditoria 2026-07-31 |
| 29 | Cache de reflexão com eviction por chave fraca e serviços temporizados canceláveis | auditoria 2026-07-31 |
| 17 | `OmniTreeGrid`, `OmniGlobalSearch`, `OmniFileManager` e `OmniSignaturePad`, todos com testes e showcase | implementação 2026-07-31 |
| 36 | Motor hierárquico único para `OmniDataGrid` e `OmniTreeGrid`, com cancelamento, deduplicação, limites, proteção contra ciclos, descarte determinístico e API pública coerente | implementação 2026-07-31 |
| 37 | Providers paginados, canceláveis e latest-wins em `OmniDataGrid`, `OmniAutoComplete`, `OmniSelect` e `OmniMultiSelect` | P0 2026-07-31 |
| 38 | Exportação CSV do DataGrid por streaming, em lotes e com limite rígido; sem `int.MaxValue` nem materialização integral | P0 2026-07-31 |
| 39 | Smoke tests dos pacotes base/AI/MCP em consumidores Server/WASM limpos e release protegida pelos mesmos gates da CI | P0 2026-07-31 |
| 40 | Observer central para tarefas destacadas, gate contra descarte async e serialização versionada dos listeners globais do MenuBar | P0 2026-07-31 |

**Dois bugs de biblioteca descobertos no caminho** (afetavam todo consumidor, não estavam na análise original):

- **Utilitários CSS que não shipavam** — `.omni-mono`, `.omni-muted`, `.omni-soft`, `.omni-flex-1` existiam só no CSS do app de demo, mas **6 componentes da lib** as usam (`OmniCard`, `OmniTextBox`, `OmniQtyStepper`, `OmniStepper`, Alert/ConfirmDialog) → renderizavam sem estilo em qualquer app consumidor. Corrigido em #29.
- **Agregados do grid com cultura fixa** — rodapé em `pt-BR` ao lado de células em `CurrentCulture`. Corrigido em #28.

Artefatos novos que passam a valer como convenção: [`scripts/check_template_code.py`](scripts/check_template_code.py) (guarda de drift, na CI) e o catálogo do README **gerado** de `docs/components.json`.

---

## 1. Lista priorizada (pendente)

### P1 — Fundações

| # | Item | Área | Esforço |
|---|---|---|---|
| 5b | ~~Localização completa de defaults e strings internas~~ | Lib | ✅ |
| 30 | Migrar interop complexo para módulos JS tipados/lazy e contratos descartáveis | Lib | L |
| 32 | BenchmarkDotNet para DataGrid, Markdown, CSS builders e gerador, com budgets de alocação | Performance | M |

### P2 — Cobertura e consistência

| # | Item | Área | Esforço |
|---|---|---|---|
| 9 | Templates para os componentes-flagship (Kanban/Scheduler/Chat/Wizard/Tabs) | Templates | L |
| 10 | Cobertura de showcase (~40 componentes sem página) | Docs | M |
| 11 | ~~Consistência de API pública (`Label`/`Content`→`Text`, `Open`/`IsOpen`/`Visible`, binding do Tabs)~~ | Lib | ✅ |
| 12 | Contrato único de binding multi-valor | Lib | L |
| 13 | Acessibilidade sistêmica dos templates (`label for`, headings) | Templates | M |
| 24 | Limpar as 4 utilitárias duplicadas no `_demo.scss` | Demo | S |
| 33 | Testes Playwright de teclado, foco, overlays, descarte e reconexão | QA | L — **parcial**: infra existe (`test/Omni.Blazor.BrowserTests`, Playwright), mas cobre forms/grid/localização/rotas publicadas; teclado/foco/overlay/descarte/reconexão específicos ainda não |
| 34 | Gate de paridade componente ↔ teste ↔ showcase, com allow-list explícita de subcomponentes | CI/Docs | M |

### P3 — Features que faltam

| # | Item | Área | Esforço |
|---|---|---|---|
| 15 | ~~Tipos de gráfico faltando (Stacked/Radar/Scatter/Gauge)~~ | Lib | ✅ |
| 16 | Telas SaaS faltando (Onboarding, Billing, Settings com abas, Search, Notificações, CRUD detalhe) | Templates | L |

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

### ✅ P1 · 5b. Localização completa e extensível

**Concluído.** Defaults e textos internos renderizados passam por `IOmniLocalizer<OmniBlazorResource>`; os catálogos embutidos cobrem pt-BR e inglês, com fallback centralizado, pluralização real, `OmniCultureScope`, troca global Server/WASM, RTL e formatação culture-aware. O motor foi extraído para os pacotes independentes `AndersonN.Omni.Localization`, `.Json` e `.Po`, com recursos tipados, validação de catálogos/placeholders, diagnósticos limitados por recurso, pseudolocales e integração `IStringLocalizer`. O demo usa `DemoResource`, provando o uso sem compartilhar o espaço de chaves dos componentes. Consulte [`docs/localization.md`](docs/localization.md).

---

### P2 · 9. Templates para os componentes-flagship

**Problema.** Os componentes que **mais diferenciam** a lib e que são os **mais trabalhosos de montar numa tela real** têm **zero** cobertura de template.

**Evidência (verificada).** Nos templates, o uso de `OmniTabs`, `OmniStepper`, `OmniKanban`, `OmniScheduler`, `OmniChat`, `OmniGantt`, `OmniPivotGrid`, `OmniDataFilter` é **0**. (O `OmniDrawer`/`OmniSplitView` passaram a aparecer no app-shell de #29.)

**Como corrigir.** Telas realistas: board Kanban, agenda/Scheduler, Chat/Inbox (split), Onboarding com `OmniStepper`, Settings com `OmniTabs`. Cada uma com a página de showcase correspondente (regra do CONTRIBUTING) e respeitando o guarda de drift.

**Esforço:** L · **Área:** Templates

---

### P2 · 10. Cobertura de showcase

**Problema.** O CONTRIBUTING exige uma página de showcase por componente; hoje há
**142 arquivos Razor no showcase para 198 componentes catalogados**. A diferença não
é o gap real porque hosts, validators e subcomponentes são legitimamente demonstrados
pelo pai; falta transformar essa regra em mapeamento verificável para obter a lista
exata e impedir regressão.

**Como corrigir.**
1. Levantar a lista exata (excluindo sub-componentes, usando a allow-list `TestedViaParent` dos convention tests como referência).
2. Priorizar os de alto uso (`OmniDataGrid`, `OmniAutoComplete`, `OmniFileUpload`, `OmniCalendar`, `OmniDateRangePicker`, …).
3. Estender o `ComponentConventionTests` com uma checagem de "tem showcase" (mesma mecânica do "tem teste"), com allow-list — assim o gap não volta a crescer.

**Esforço:** M · **Área:** Docs

---

### ✅ P2 · 11. Consistência de API pública

**Concluído.** Sem alias de compatibilidade em nenhum dos três — a lib é pré-1.0, então toda renomeação foi direta (rename + build/testes pegam qualquer referência esquecida como erro, não warning).

- `OmniFabMenuItem.Label` → `Text`; `OmniMessage.Content` → `Text`.
- `OmniFabMenu.IsOpen`/`IsOpenChanged` → `Open`/`OpenChanged`; `OmniOverlay.Visible`/`VisibleChanged` → `Open`/`OpenChanged` — agora todo overlay usa `Open` (`OmniBottomSheet`, `OmniCommandPalette`, `OmniPopover`, `OmniDrawer` já usavam; `OmniOverlay` tinha 4 consumidores internos na própria lib — `OmniEntityEditorHost`, `OmniDataGridForm`, `OmniEntityPicker`, `OmniCommandPalette` — todos migrados junto).
- `OmniTabs.ActiveIndex`/`ActiveIndexChanged` — bindable via `@bind-ActiveIndex`; uso não controlado (sem passar o parâmetro) preserva o comportamento anterior (primeira aba registrada fica ativa).

  *(`OmniBadge.Visible` e `OmniDataGridColumn.Visible` ficaram de fora de propósito — visibilidade genérica, não estado de overlay.)*

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

### P2 · 24. Limpar as utilitárias duplicadas no `_demo.scss`

**Contexto.** `.omni-mono`, `.omni-muted`, `.omni-soft` e `.omni-flex-1` passaram a shipar no bundle da lib (#29), mas o `_demo.scss` da Forneria.Demo **ainda as define localmente**. Os valores são idênticos, então é inofensivo — só redundante, e o prefixo `omni-` é da biblioteca por convenção.

**Por que não saiu junto.** Remover exige **rebuild em Debug** do `forneria-demo.css`: um build em Release minifica o artefato commitado (5903 linhas → 1), gerando um diff enorme e enganoso.

**Como corrigir.** Remover as 4 regras do `_demo.scss` e rebuildar `Forneria.Demo.Pages` **em Debug** no mesmo commit.

**Esforço:** S · **Área:** Demo

---

### ✅ P3 · 15. Tipos de gráfico faltando

**Concluído** (release 0.7.0). `ChartSeriesType` (`src/Omni.Blazor/Models/Enums.cs`) agora tem 13 valores — os 7 originais mais `StackedColumn`, `StackedBar`, `Scatter`, `Bubble`, `Radar` e `Gauge`, com suporte a `ChartSchema` tipado. Ver CHANGELOG `[0.7.0]`.

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

### Concluído · 17. Novos componentes estruturais

O levantamento de gaps foi atendido com:

- **`OmniTreeGrid`**, com colunas declarativas, expansão lazy cancelável,
  seleção, limites de profundidade/linhas e cache LRU limitado;
- **`OmniGlobalSearch` full-page**, com resultados locais/remotos, teclado e
  provider assíncrono latest-wins;
- **`OmniFileManager`/galeria**, separando o backend de armazenamento da UI;
- **`OmniSignaturePad`**, com saída vetorial/raster, undo e acessibilidade por
  rótulos e estados anunciáveis.

Foram priorizados por serem mais valiosos que ampliar a biblioteca com wrappers muito específicos.
Mapas, PDF viewer e editores colaborativos devem permanecer integrações externas até
haver demanda e um contrato de provider sustentável.

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

1. ~~**Sprint "confiança"** — P0 (itens 1–4).~~ ✅ #28
2. ~~**Sprint "templates consumíveis"** — itens 6 + 7 + 8.~~ ✅ #29
3. ~~**Sprint "i18n"** — seam do item 5.~~ ✅ #30 *(falta o 5b)*
4. **Agora:** fechar o **5b**, o smoke test de pacotes (**31**) e o gate de
   paridade de showcase (**34**).
5. **Depois:** observer de tarefas descartadas (**35**), módulos JS tipados
   (**30**), browser/a11y (**33**) e benchmarks com orçamento (**32**).
6. **Cobertura funcional:** itens **9**, **10**, **14**, **15** e **17**.
7. **Conforme demanda:** demais itens 11–16.

> Convenções obrigatórias para qualquer item: testes junto (cobertura ≥ 80%), lib packable sem warnings (`TreatWarningsAsErrors`), showcase por componente novo, o guarda de drift dos templates verde, e regenerar o manifesto (`dotnet run --project tools/Omni.Blazor.ManifestGen`) quando a API pública mudar.
