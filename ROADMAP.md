# Roadmap — Omni.Blazor

> Estado em **2026-07-28** · versão publicada **v0.3.0** (`AndersonN.Omni.Blazor`, `.Ai`, `.Mcp`)
> 174 componentes · **2004 testes** · cobertura **83,1%** (lib) / 100% (Ai) · lib packable sem warnings (`TreatWarningsAsErrors`)

Consolida três fontes: a **auditoria de hardening** (2026-07-01, 7 PRs), a **análise de biblioteca + templates** (2026-07-28) e as **pendências de roadmaps anteriores** (gaps de componentes/telas, AI-Ready).

**Leitura do momento.** As três travas que a análise apontou — o Quick-start que não compilava, os templates invisíveis/não-consumíveis e a ausência de i18n — **foram endereçadas** (PRs #28, #29, #30). O kit segue maduro no nível de API; o que resta é **cobertura** (templates para os componentes-flagship, showcase), **consistência de API** e **features pontuais**. Nada mais é quebra-confiança.

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

**Dois bugs de biblioteca descobertos no caminho** (afetavam todo consumidor, não estavam na análise original):

- **Utilitários CSS que não shipavam** — `.omni-mono`, `.omni-muted`, `.omni-soft`, `.omni-flex-1` existiam só no CSS do app de demo, mas **6 componentes da lib** as usam (`OmniCard`, `OmniTextBox`, `OmniQtyStepper`, `OmniStepper`, Alert/ConfirmDialog) → renderizavam sem estilo em qualquer app consumidor. Corrigido em #29.
- **Agregados do grid com cultura fixa** — rodapé em `pt-BR` ao lado de células em `CurrentCulture`. Corrigido em #28.

Artefatos novos que passam a valer como convenção: [`scripts/check_template_code.py`](scripts/check_template_code.py) (guarda de drift, na CI) e o catálogo do README **gerado** de `docs/components.json`.

---

## 1. Lista priorizada (pendente)

### P1 — Fundações

| # | Item | Área | Esforço |
|---|---|---|---|
| 5b | Apontar os ~30 defaults de `[Parameter]` para o `OmniTexts` | Lib | M |

### P2 — Cobertura e consistência

| # | Item | Área | Esforço |
|---|---|---|---|
| 9 | Templates para os componentes-flagship (Kanban/Scheduler/Chat/Wizard/Tabs) | Templates | L |
| 10 | Cobertura de showcase (~40 componentes sem página) | Docs | M |
| 11 | Consistência de API pública (`Label`/`Content` → `Text`, `Open`/`IsOpen`, Tabs) | Lib | M |
| 12 | Contrato único de binding multi-valor | Lib | L |
| 13 | Acessibilidade sistêmica dos templates (`label for`, headings) | Templates | M |
| 24 | Limpar as 4 utilitárias duplicadas no `_demo.scss` | Demo | S |

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

### P1 · 5b. Apontar os defaults de `[Parameter]` para o `OmniTexts`

**Contexto.** O seam existe e funciona (#30): `[Parameter]` → `Texts` registrado → default embutido. As **17 strings que não tinham override nenhum** já estão ligadas.

**O que falta.** Cerca de **30 parâmetros** cujo default ainda é uma string pt-BR literal — `OmniChat.SendLabel`/`Placeholder`/`EmptyMessage`, `OmniStepper.NextText`/`PrevText`/`CompleteText`, `OmniScheduler.TodayText`/`NextText`/`PrevText`, `OmniDataFilter.*Text`, `OmniDataGrid.SearchPlaceholder`/`EmptyText`, `OmniDateRangePicker.*Text`, `OmniLayout.SkipLabel`, `OmniCommandPalette.Placeholder`, `OmniConfirmPrompt.ButtonText`, … Eles **já aceitam override por instância**, então o gap é menor — mas quem registra `OmniTexts.English()` ainda vê esses defaults em pt-BR, o que é incoerente.

**Como corrigir.** Por parâmetro: tornar nullable (`public string? X`), remover o inicializador e resolver no uso (`X ?? Texts.Chave`). São ~30 declarações e ~40 pontos de uso, todos simples (`@Param` em atributo/conteúdo). As chaves correspondentes **já existem** no `OmniTexts` (`Send`, `Next`, `Back`, `Complete`, `Today`, `Apply`, `Cancel`, `ClearAll`, `SearchPlaceholder`, `NoRecords`, …) — hoje sem consumidor.

**Cuidado.** `OmniDataFilter` reencaminha vários desses parâmetros por uma interface (`string IOmniDataFilterOwner.AddFilterText => AddFilterText;`) — a substituição precisa acertar só o lado direito.

**Esforço:** M · **Área:** Lib

---

### P2 · 9. Templates para os componentes-flagship

**Problema.** Os componentes que **mais diferenciam** a lib e que são os **mais trabalhosos de montar numa tela real** têm **zero** cobertura de template.

**Evidência (verificada).** Nos templates, o uso de `OmniTabs`, `OmniStepper`, `OmniKanban`, `OmniScheduler`, `OmniChat`, `OmniGantt`, `OmniPivotGrid`, `OmniDataFilter` é **0**. (O `OmniDrawer`/`OmniSplitView` passaram a aparecer no app-shell de #29.)

**Como corrigir.** Telas realistas: board Kanban, agenda/Scheduler, Chat/Inbox (split), Onboarding com `OmniStepper`, Settings com `OmniTabs`. Cada uma com a página de showcase correspondente (regra do CONTRIBUTING) e respeitando o guarda de drift.

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

### P2 · 24. Limpar as utilitárias duplicadas no `_demo.scss`

**Contexto.** `.omni-mono`, `.omni-muted`, `.omni-soft` e `.omni-flex-1` passaram a shipar no bundle da lib (#29), mas o `_demo.scss` da Forneria.Demo **ainda as define localmente**. Os valores são idênticos, então é inofensivo — só redundante, e o prefixo `omni-` é da biblioteca por convenção.

**Por que não saiu junto.** Remover exige **rebuild em Debug** do `forneria-demo.css`: um build em Release minifica o artefato commitado (5903 linhas → 1), gerando um diff enorme e enganoso.

**Como corrigir.** Remover as 4 regras do `_demo.scss` e rebuildar `Forneria.Demo.Pages` **em Debug** no mesmo commit.

**Esforço:** S · **Área:** Demo

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

1. ~~**Sprint "confiança"** — P0 (itens 1–4).~~ ✅ #28
2. ~~**Sprint "templates consumíveis"** — itens 6 + 7 + 8.~~ ✅ #29
3. ~~**Sprint "i18n"** — seam do item 5.~~ ✅ #30 *(falta o 5b)*
4. **Agora:** fechar o **5b** (coerência do i18n, esforço M) e o **24** (S) — ambos pequenos e fecham frentes já abertas.
5. **Depois:** **9** e **10** (cobertura: templates dos flagship + showcase) — é onde o consumidor mais sente falta hoje.
6. **Conforme demanda:** 11–17.

> Convenções obrigatórias para qualquer item: testes junto (cobertura ≥ 80%), lib packable sem warnings (`TreatWarningsAsErrors`), showcase por componente novo, o guarda de drift dos templates verde, e regenerar o manifesto (`dotnet run --project tools/Omni.Blazor.ManifestGen`) quando a API pública mudar.
