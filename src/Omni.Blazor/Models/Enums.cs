namespace Omni.Blazor.Models;

/// <summary>Visual variant of an <c>OmniButton</c>.</summary>
public enum ButtonVariant
{
    /// <summary>Neutral surface button (default).</summary>
    Default,
    /// <summary>Filled accent button for the primary action.</summary>
    Primary,
    /// <summary>Transparent button — background only on hover.</summary>
    Ghost,
    /// <summary>Destructive action (filled danger color).</summary>
    Danger,
    /// <summary>Looks like a hyperlink (no border/background).</summary>
    Link
}

/// <summary>Standard size scale shared by many Omni components.</summary>
public enum ComponentSize
{
    /// <summary>Small.</summary>
    Sm,
    /// <summary>Medium (default).</summary>
    Md,
    /// <summary>Large.</summary>
    Lg,
    /// <summary>Extra large.</summary>
    Xl
}

/// <summary>Severity/intent of an <c>OmniNotification</c> — drives icon + color.</summary>
public enum NotificationSeverity
{
    /// <summary>Informational (neutral/accent tone).</summary>
    Info,
    /// <summary>Success (good/green tone).</summary>
    Success,
    /// <summary>Warning (warn/amber tone).</summary>
    Warning,
    /// <summary>Error (danger/red tone).</summary>
    Error
}

/// <summary>Tela onde as notificações do <c>OmniNotification</c> são empilhadas.</summary>
public enum NotificationPosition
{
    /// <summary>Canto superior direito.</summary>
    TopRight,
    /// <summary>Canto superior esquerdo.</summary>
    TopLeft,
    /// <summary>Canto inferior direito.</summary>
    BottomRight,
    /// <summary>Canto inferior esquerdo.</summary>
    BottomLeft
}

/// <summary>Lado da tela em que um painel lateral (ex.: drawer/sidebar) é ancorado.</summary>
public enum SidePosition
{
    /// <summary>Ancorado à direita.</summary>
    Right,
    /// <summary>Ancorado à esquerda.</summary>
    Left
}

/// <summary>Cor/intent semântico de um <c>OmniBadge</c>.</summary>
public enum BadgeVariant
{
    /// <summary>Cinza neutro (default).</summary>
    Default,
    /// <summary>Tom "good"/verde (sucesso).</summary>
    Good,
    /// <summary>Tom "warn"/âmbar (atenção).</summary>
    Warn,
    /// <summary>Tom "danger"/vermelho (erro/crítico).</summary>
    Danger,
    /// <summary>Tom "info"/azul (informativo).</summary>
    Info,
    /// <summary>Cor accent do tema atual.</summary>
    Accent,
    /// <summary>Sem fundo — só texto (plain).</summary>
    Plain,
    /// <summary>Fundo sólido de alto contraste.</summary>
    Solid
}

/// <summary>
/// Anchor position for a <c>OmniBadge</c> in overlay mode. Start/End naming
/// is RTL-aware (Start = inline-start, End = inline-end). Use TopEnd for
/// the classic "notification dot in top-right" pattern.
/// </summary>
public enum BadgeOrigin
{
    /// <summary>Topo, inline-end (top-right em LTR) — o "dot de notificação" clássico.</summary>
    TopEnd,
    /// <summary>Topo, inline-start (top-left em LTR).</summary>
    TopStart,
    /// <summary>Base, inline-end (bottom-right em LTR).</summary>
    BottomEnd,
    /// <summary>Base, inline-start (bottom-left em LTR).</summary>
    BottomStart
}

/// <summary>Tamanho de um <c>OmniAvatar</c>.</summary>
public enum AvatarSize
{
    /// <summary>Pequeno.</summary>
    Sm,
    /// <summary>Médio (default).</summary>
    Md,
    /// <summary>Grande.</summary>
    Lg,
    /// <summary>Extra grande.</summary>
    Xl,
    /// <summary>Gigante (2x extra grande).</summary>
    XXl
}

/// <summary>Direção de ordenação de uma coluna/lista.</summary>
public enum SortDirection
{
    /// <summary>Sem ordenação aplicada.</summary>
    None,
    /// <summary>Ascendente (A→Z, menor→maior).</summary>
    Ascending,
    /// <summary>Descendente (Z→A, maior→menor).</summary>
    Descending
}

/// <summary>Como um <c>OmniDialog</c> foi encerrado.</summary>
public enum DialogResultKind
{
    /// <summary>Confirmado (OK/ação primária).</summary>
    Ok,
    /// <summary>Cancelado pelo usuário.</summary>
    Cancel,
    /// <summary>Fechado sem confirmar (backdrop, Esc, botão X).</summary>
    Closed
}

/// <summary>Forma do placeholder de um <c>OmniSkeleton</c>.</summary>
public enum SkeletonVariant
{
    /// <summary>Linha(s) de texto.</summary>
    Text,
    /// <summary>Bloco retangular.</summary>
    Rect,
    /// <summary>Círculo (ex.: avatar).</summary>
    Circle
}

/// <summary>Eixo de orientação de um componente (ex.: divisor, grupo, slider).</summary>
public enum Orientation
{
    /// <summary>Horizontal.</summary>
    Horizontal,
    /// <summary>Vertical.</summary>
    Vertical
}

/// <summary>Granularidade de edição inline do <c>OmniDataGrid</c>.</summary>
public enum DataGridEditMode
{
    /// <summary>Edição desabilitada.</summary>
    None,
    /// <summary>Edita uma célula por vez.</summary>
    Cell,
    /// <summary>Edita a linha inteira de uma vez.</summary>
    Row
}

/// <summary>Quantos painéis de um <c>OmniAccordion</c> podem ficar abertos ao mesmo tempo.</summary>
public enum AccordionMode
{
    /// <summary>Só um painel aberto por vez (abrir outro fecha o atual).</summary>
    Single,
    /// <summary>Vários painéis abertos simultaneamente.</summary>
    Multi
}

/// <summary>Lado em que um <c>OmniPopover</c> abre em relação ao seu alvo.</summary>
public enum PopoverPosition
{
    /// <summary>Acima do alvo.</summary>
    Top,
    /// <summary>Abaixo do alvo.</summary>
    Bottom,
    /// <summary>À esquerda do alvo.</summary>
    Left,
    /// <summary>À direita do alvo.</summary>
    Right
}

/// <summary>Layout do <c>OmniPagination</c>.</summary>
public enum PaginationVariant
{
    /// <summary>Completo: números de página + setas (default).</summary>
    Full,
    /// <summary>Compacto: só setas anterior/próximo + indicador atual.</summary>
    Compact
}

/// <summary>Variante visual do <c>OmniCard</c>: superfície padrão, moldura "accent" ou apenas contorno.</summary>
public enum CardVariant
{
    /// <summary>Superfície elevada padrão.</summary>
    Default,
    /// <summary>Moldura/realce na cor accent.</summary>
    Accent,
    /// <summary>Apenas contorno, sem fundo preenchido.</summary>
    Outline
}

/// <summary>Cor temática do <c>OmniCard</c>. No variante Default pinta a superfície; no Outline, a borda.</summary>
public enum CardTone
{
    /// <summary>Sem cor temática (neutro padrão).</summary>
    None,
    /// <summary>Cor accent do tema.</summary>
    Accent,
    /// <summary>Tom neutro/cinza.</summary>
    Neutral,
    /// <summary>Tom informativo (azul).</summary>
    Info,
    /// <summary>Tom de sucesso (verde).</summary>
    Success,
    /// <summary>Tom de atenção (âmbar).</summary>
    Warning,
    /// <summary>Tom de perigo (vermelho).</summary>
    Danger
}

/// <summary>Posição da mídia dentro de um <c>OmniCardMedia</c>.</summary>
public enum CardMediaPosition
{
    /// <summary>Mídia no topo do card.</summary>
    Top,
    /// <summary>Mídia na base do card.</summary>
    Bottom,
    /// <summary>Mídia em fundo, com o conteúdo sobreposto.</summary>
    Overlay,
    /// <summary>Mídia ao lado inicial (inline-start).</summary>
    Start,
    /// <summary>Mídia ao lado final (inline-end).</summary>
    End
}

/// <summary>Como os submenus de um <c>OmniMenuBar</c> são abertos.</summary>
public enum MenuTrigger
{
    /// <summary>Abre ao passar o mouse (hover).</summary>
    Hover,
    /// <summary>Abre ao clicar.</summary>
    Click
}

/// <summary>Cor da pílula de metadados de um item de dropdown do <c>OmniMenuBar</c>.</summary>
public enum MenuMetaKind
{
    /// <summary>Neutro/cinza.</summary>
    Neutral,
    /// <summary>Cor accent do tema.</summary>
    Accent,
    /// <summary>Verde (good).</summary>
    Good,
    /// <summary>Âmbar (warn).</summary>
    Warn,
    /// <summary>Vermelho (danger).</summary>
    Danger
}

/// <summary>Eixo da translação de parallax de uma <c>OmniParallaxLayer</c>.
/// Vertical = move no Y (default), Horizontal = no X, Both = nos dois (diagonal),
/// todos dirigidos pelo progresso de scroll da cena.</summary>
public enum ParallaxAxis
{
    /// <summary>Move no eixo Y (default).</summary>
    Vertical,
    /// <summary>Move no eixo X.</summary>
    Horizontal,
    /// <summary>Move nos dois eixos (diagonal).</summary>
    Both
}

/// <summary>Cor/intent semântico de um <c>OmniStatusBadge</c>.
/// As cinco variantes mapeiam para as variáveis CSS <c>--omni-good</c>,
/// <c>--omni-warn</c>, <c>--omni-danger</c>, <c>--omni-accent</c> ou
/// <c>--omni-fg-muted</c> (Neutral).</summary>
public enum StatusBadgeKind
{
    /// <summary>Neutro — mapeia para <c>--omni-fg-muted</c>.</summary>
    Neutral,
    /// <summary>Accent — mapeia para <c>--omni-accent</c>.</summary>
    Accent,
    /// <summary>Sucesso — mapeia para <c>--omni-good</c>.</summary>
    Good,
    /// <summary>Atenção — mapeia para <c>--omni-warn</c>.</summary>
    Warn,
    /// <summary>Perigo — mapeia para <c>--omni-danger</c>.</summary>
    Danger
}

/// <summary>Direção de empilhamento do <c>OmniStack</c>.
/// Column = vertical (default), Row = horizontal.</summary>
public enum StackDirection
{
    /// <summary>Empilhamento vertical (default).</summary>
    Column,
    /// <summary>Empilhamento horizontal.</summary>
    Row
}

/// <summary>Alinhamento dos filhos no eixo cross de um <c>OmniStack</c>
/// (mapeia para CSS <c>align-items</c>).</summary>
public enum StackAlign
{
    /// <summary>Alinha ao início do eixo cross (<c>flex-start</c>).</summary>
    Start,
    /// <summary>Centraliza no eixo cross.</summary>
    Center,
    /// <summary>Alinha ao fim do eixo cross (<c>flex-end</c>).</summary>
    End,
    /// <summary>Estica os filhos para preencher o eixo cross.</summary>
    Stretch,
    /// <summary>Alinha pela linha de base do texto (<c>baseline</c>).</summary>
    Baseline
}

/// <summary>Distribuição dos filhos no eixo main de um <c>OmniStack</c>
/// (mapeia para CSS <c>justify-content</c>).</summary>
public enum StackJustify
{
    /// <summary>Agrupa no início do eixo main (<c>flex-start</c>).</summary>
    Start,
    /// <summary>Centraliza no eixo main.</summary>
    Center,
    /// <summary>Agrupa no fim do eixo main (<c>flex-end</c>).</summary>
    End,
    /// <summary>Espaço igual entre os itens, bordas sem espaço (<c>space-between</c>).</summary>
    Between,
    /// <summary>Espaço igual ao redor de cada item (<c>space-around</c>).</summary>
    Around,
    /// <summary>Espaço perfeitamente uniforme, inclusive nas bordas (<c>space-evenly</c>).</summary>
    Evenly
}

/// <summary>Operadores de filtro suportados por coluna no <c>OmniDataGrid</c>.</summary>
public enum FilterOperator
{
    /// <summary>O valor contém o texto informado.</summary>
    Contains,
    /// <summary>O valor NÃO contém o texto informado.</summary>
    NotContains,
    /// <summary>Igual ao valor informado.</summary>
    Equals,
    /// <summary>Diferente do valor informado.</summary>
    NotEquals,
    /// <summary>Começa com o texto informado.</summary>
    StartsWith,
    /// <summary>Termina com o texto informado.</summary>
    EndsWith,
    /// <summary>Maior que o valor informado.</summary>
    GreaterThan,
    /// <summary>Maior ou igual ao valor informado.</summary>
    GreaterOrEqual,
    /// <summary>Menor que o valor informado.</summary>
    LessThan,
    /// <summary>Menor ou igual ao valor informado.</summary>
    LessOrEqual,
    /// <summary>Dentro do intervalo [min, max].</summary>
    Between,
    /// <summary>Fora do intervalo [min, max].</summary>
    NotBetween,
    /// <summary>Vazio/nulo.</summary>
    IsEmpty,
    /// <summary>Não vazio/não nulo.</summary>
    IsNotEmpty
}

/// <summary>Tipo de UI/operadores oferecidos por uma coluna filtrável.</summary>
public enum ColumnFilterType
{
    /// <summary>Filtro de texto.</summary>
    Text,
    /// <summary>Filtro numérico.</summary>
    Number,
    /// <summary>Filtro de data.</summary>
    Date,
    /// <summary>Filtro booleano (sim/não).</summary>
    Boolean,
    /// <summary>Filtro de seleção (lista de valores).</summary>
    Select
}

/// <summary>Operador lógico que combina condições/grupos num <c>OmniDataFilter</c>.</summary>
public enum FilterLogic
{
    /// <summary>Todas as condições devem ser verdadeiras (E).</summary>
    And,
    /// <summary>Ao menos uma condição deve ser verdadeira (OU).</summary>
    Or
}

/// <summary>Função agregadora aplicada numa coluna (footer/group footer).</summary>
public enum AggregateFunction
{
    /// <summary>Soma dos valores.</summary>
    Sum,
    /// <summary>Média dos valores.</summary>
    Average,
    /// <summary>Contagem de itens.</summary>
    Count,
    /// <summary>Menor valor.</summary>
    Min,
    /// <summary>Maior valor.</summary>
    Max
}

/// <summary>Posição congelada de uma coluna (sticky horizontal).</summary>
public enum FrozenPosition
{
    /// <summary>Congelada à esquerda.</summary>
    Left,
    /// <summary>Congelada à direita.</summary>
    Right
}

/// <summary>Modo de expansão de linhas para master-detail.</summary>
public enum ExpandMode
{
    /// <summary>Só uma linha expandida por vez.</summary>
    Single,
    /// <summary>Várias linhas expandidas simultaneamente.</summary>
    Multi
}

/// <summary>Layout de renderização do DataGrid: tabela clássica ou grid CSS (necessário p/ virtualização).</summary>
public enum DataGridLayoutMode
{
    /// <summary>Tabela HTML clássica (<c>&lt;table&gt;</c>).</summary>
    Table,
    /// <summary>Grid CSS — requerido para virtualização.</summary>
    Grid
}

/// <summary>Teclas modificadoras de um atalho de teclado (combináveis por <c>OR</c>).</summary>
[Flags]
public enum Modifier
{
    /// <summary>Nenhum modificador.</summary>
    None  = 0,
    /// <summary>Tecla Control.</summary>
    Ctrl  = 1 << 0,
    /// <summary>Tecla Alt (Option no macOS).</summary>
    Alt   = 1 << 1,
    /// <summary>Tecla Shift.</summary>
    Shift = 1 << 2,
    /// <summary>Tecla Meta (Cmd no macOS, Win no Windows).</summary>
    Meta  = 1 << 3
}

// ===== Layout / wireframe ===================================================

/// <summary>Posição de ancoragem de um <c>OmniDrawer</c> no <c>OmniLayout</c>.</summary>
public enum DrawerAnchor
{
    /// <summary>Ancorado à esquerda.</summary>
    Left,
    /// <summary>Ancorado à direita.</summary>
    Right
}

/// <summary>
/// Variante de comportamento de um <c>OmniDrawer</c>:
/// <list type="bullet">
///   <item><term>Persistent</term><description>sempre visível, empurra o main.</description></item>
///   <item><term>Mini</term><description>sempre visível mas icon-only; expande on-hover.</description></item>
///   <item><term>Temporary</term><description>overlay com backdrop, fechado por default.</description></item>
///   <item><term>Responsive</term><description>Persistent acima do <c>Breakpoint</c>, Temporary abaixo (default).</description></item>
/// </list>
/// </summary>
public enum DrawerVariant
{
    /// <summary>Sempre visível, empurra o main.</summary>
    Persistent,
    /// <summary>Sempre visível mas icon-only; expande on-hover.</summary>
    Mini,
    /// <summary>Overlay com backdrop, fechado por default.</summary>
    Temporary,
    /// <summary>Persistent acima do <c>Breakpoint</c>, Temporary abaixo (default).</summary>
    Responsive
}

/// <summary>Largura máxima de um <c>OmniContainer</c> (semelhante a Bootstrap <c>.container-{bp}</c>).</summary>
public enum ContainerMaxWidth
{
    /// <summary>Largura máxima do breakpoint sm.</summary>
    Sm,
    /// <summary>Largura máxima do breakpoint md.</summary>
    Md,
    /// <summary>Largura máxima do breakpoint lg.</summary>
    Lg,
    /// <summary>Largura máxima do breakpoint xl.</summary>
    Xl,
    /// <summary>Largura máxima do breakpoint xxl.</summary>
    Xxl,
    /// <summary>Sem limite — ocupa 100% da largura disponível.</summary>
    Full
}

/// <summary>Posição de uma barra (<c>OmniAppBar</c>) no <c>OmniLayout</c>: topo ou rodapé.</summary>
public enum BarPosition
{
    /// <summary>Topo (header).</summary>
    Top,
    /// <summary>Rodapé (footer).</summary>
    Bottom
}

/// <summary>
/// Modo de comparação do <c>OmniHidden</c>:
/// <list type="bullet">
/// <item><term>Down</term><description>esconde quando viewport &lt;= Breakpoint (default — esconde mobile e abaixo)</description></item>
/// <item><term>Up</term><description>esconde quando viewport &gt;= Breakpoint (esconde desktop e acima)</description></item>
/// <item><term>Only</term><description>esconde apenas no breakpoint exato</description></item>
/// </list>
/// </summary>
public enum HiddenMode
{
    /// <summary>Esconde quando viewport &lt;= Breakpoint (default — esconde mobile e abaixo).</summary>
    Down,
    /// <summary>Esconde quando viewport &gt;= Breakpoint (esconde desktop e acima).</summary>
    Up,
    /// <summary>Esconde apenas no breakpoint exato.</summary>
    Only
}

/// <summary>Densidade global do layout — aplica em <c>&lt;html data-density="..."&gt;</c>
/// e troca CSS vars de padding/radius/font-size em cascata.</summary>
public enum LayoutDensity
{
    /// <summary>Compacto — menor padding/altura, mais densidade de informação.</summary>
    Compact,
    /// <summary>Confortável — espaçamento padrão.</summary>
    Comfortable,
    /// <summary>Espaçoso — maior padding/altura, mais respiro.</summary>
    Spacious
}

/// <summary>
/// Standard responsive breakpoints. Values mirror common conventions
/// (Bootstrap / Tailwind): xs &lt;576, sm &lt;768, md &lt;992, lg &lt;1200,
/// xl &lt;1400, xxl ≥1400. Use with <c>BreakpointService</c> /
/// <c>OmniBreakpointProvider</c>.
/// </summary>
public enum Breakpoint
{
    /// <summary>Extra small — &lt;576px.</summary>
    Xs,
    /// <summary>Small — &lt;768px.</summary>
    Sm,
    /// <summary>Medium — &lt;992px.</summary>
    Md,
    /// <summary>Large — &lt;1200px.</summary>
    Lg,
    /// <summary>Extra large — &lt;1400px.</summary>
    Xl,
    /// <summary>Extra extra large — ≥1400px.</summary>
    Xxl
}

// ===== Data visualization ===================================================

/// <summary>Variante visual de um <c>OmniSparkline</c>:
/// <list type="bullet">
///   <item><term>Line</term><description>linha simples conectando pontos.</description></item>
///   <item><term>Area</term><description>linha com preenchimento abaixo.</description></item>
///   <item><term>Column</term><description>barras verticais (uma por ponto).</description></item>
///   <item><term>Bar</term><description>barras horizontais.</description></item>
///   <item><term>Pie</term><description>pizza/donut com slices proporcionais.</description></item>
/// </list>
/// </summary>
public enum SparklineVariant
{
    /// <summary>Linha simples conectando pontos.</summary>
    Line,
    /// <summary>Linha com preenchimento abaixo.</summary>
    Area,
    /// <summary>Barras verticais (uma por ponto).</summary>
    Column,
    /// <summary>Barras horizontais.</summary>
    Bar,
    /// <summary>Pizza/donut com slices proporcionais.</summary>
    Pie
}

/// <summary>Modo de aparência pro <c>OmniAppearanceToggle</c>.
/// <list type="bullet">
///   <item><term>Light</term><description>tema claro, explicitamente escolhido pelo user.</description></item>
///   <item><term>Dark</term><description>tema escuro, explicitamente escolhido.</description></item>
///   <item><term>System</term><description>segue o <c>prefers-color-scheme</c> do SO.</description></item>
/// </list>
/// </summary>
public enum ThemeMode
{
    /// <summary>Tema claro, explicitamente escolhido pelo user.</summary>
    Light,
    /// <summary>Tema escuro, explicitamente escolhido.</summary>
    Dark,
    /// <summary>Segue o <c>prefers-color-scheme</c> do SO.</summary>
    System
}

/// <summary>Eixo(s) que o <c>OmniSwipeArea</c> aplica em modo <c>LiveTransform</c>.
/// Quando setado (não None), o JS atualiza <c>transform: translate(...)</c> direto
/// via CSS variable no elemento — 60fps sem round-trip Blazor por frame. O
/// callback <c>OnSwipeEnd</c> continua disparando normalmente no fim do gesto.
/// <list type="bullet">
///   <item><term>None</term><description>desabilitado (default). Só eventos Blazor.</description></item>
///   <item><term>X</term><description>JS aplica <c>translateX(deltaX)</c> ao vivo.</description></item>
///   <item><term>Y</term><description>JS aplica <c>translateY(deltaY)</c> ao vivo.</description></item>
///   <item><term>Both</term><description>JS aplica <c>translate(deltaX, deltaY)</c>.</description></item>
/// </list>
/// </summary>
public enum SwipeAreaLiveTransform
{
    /// <summary>Desabilitado (default). Só eventos Blazor.</summary>
    None,
    /// <summary>JS aplica <c>translateX(deltaX)</c> ao vivo.</summary>
    X,
    /// <summary>JS aplica <c>translateY(deltaY)</c> ao vivo.</summary>
    Y,
    /// <summary>JS aplica <c>translate(deltaX, deltaY)</c>.</summary>
    Both
}

/// <summary>Variant do <c>OmniBottomSheet</c>.
/// <list type="bullet">
///   <item><term>Modal</term><description>com backdrop, bloqueia interação com o fundo. Padrão pra forms/pickers/confirmações.</description></item>
///   <item><term>Standard</term><description>sem backdrop, página continua interativa. Usado em apps tipo Maps (detalhes coexistem com mapa).</description></item>
/// </list>
/// </summary>
public enum BottomSheetVariant
{
    /// <summary>Com backdrop, bloqueia interação com o fundo. Padrão pra forms/pickers/confirmações.</summary>
    Modal,
    /// <summary>Sem backdrop, página continua interativa. Usado em apps tipo Maps (detalhes coexistem com mapa).</summary>
    Standard
}

/// <summary>Comportamento responsivo do <c>OmniBottomSheet</c> em desktop.
/// Material 3 NÃO recomenda bottom sheet em telas grandes — esse enum permite
/// adaptar pra alternativas mais apropriadas.
/// <list type="bullet">
///   <item><term>Always</term><description>sempre bottom sheet (default).</description></item>
///   <item><term>DrawerOnDesktop</term><description>vira drawer lateral (direita) em desktop.</description></item>
///   <item><term>DialogOnDesktop</term><description>vira dialog centralizado em desktop.</description></item>
/// </list>
/// </summary>
public enum BottomSheetAdaptive
{
    /// <summary>Sempre bottom sheet (default).</summary>
    Always,
    /// <summary>Vira drawer lateral (direita) em desktop.</summary>
    DrawerOnDesktop,
    /// <summary>Vira dialog centralizado em desktop.</summary>
    DialogOnDesktop
}

/// <summary>Visual do <c>OmniAppearanceToggle</c>.
/// <list type="bullet">
///   <item><term>Dropdown</term><description>botão único com menu popover de 3 opções (default).</description></item>
///   <item><term>Segmented</term><description>3 botões inline lado a lado.</description></item>
///   <item><term>Cycle</term><description>botão único que cicla pelos 3 modos a cada click.</description></item>
/// </list>
/// </summary>
public enum AppearanceToggleVariant
{
    /// <summary>Botão único com menu popover de 3 opções (default).</summary>
    Dropdown,
    /// <summary>3 botões inline lado a lado.</summary>
    Segmented,
    /// <summary>Botão único que cicla pelos 3 modos a cada click.</summary>
    Cycle
}

/// <summary>Símbologias de código de barras suportadas pelo <c>OmniBarcode</c>.
/// <list type="bullet">
///   <item><term>Code128</term><description>alfanumérico, alta densidade (padrão).</description></item>
///   <item><term>Code39</term><description>letras maiúsculas, números, alguns símbolos. Auto-checksum.</description></item>
///   <item><term>Ean13</term><description>13 dígitos (12 + checksum). Padrão de varejo internacional.</description></item>
///   <item><term>Ean8</term><description>versão curta do EAN-13 (8 dígitos).</description></item>
///   <item><term>UpcA</term><description>12 dígitos, padrão de varejo norte-americano.</description></item>
/// </list>
/// </summary>
public enum BarcodeType
{
    /// <summary>Alfanumérico, alta densidade (padrão).</summary>
    Code128,
    /// <summary>Letras maiúsculas, números, alguns símbolos. Auto-checksum.</summary>
    Code39,
    /// <summary>13 dígitos (12 + checksum). Padrão de varejo internacional.</summary>
    Ean13,
    /// <summary>Versão curta do EAN-13 (8 dígitos).</summary>
    Ean8,
    /// <summary>12 dígitos, padrão de varejo norte-americano.</summary>
    UpcA
}

/// <summary>Nível de correção de erros do QR Code: maior nível tolera mais
/// dano físico mas reduz a capacidade de dados.
/// <list type="bullet">
///   <item><term>Low</term><description>~7% de tolerância (default da maioria das libs).</description></item>
///   <item><term>Medium</term><description>~15% (default Omni — bom para uso geral).</description></item>
///   <item><term>Quartile</term><description>~25%.</description></item>
///   <item><term>High</term><description>~30% (use quando há logo no centro).</description></item>
/// </list>
/// </summary>
public enum QRCodeEcc
{
    /// <summary>~7% de tolerância (default da maioria das libs).</summary>
    Low,
    /// <summary>~15% (default Omni — bom para uso geral).</summary>
    Medium,
    /// <summary>~25%.</summary>
    Quartile,
    /// <summary>~30% (use quando há logo no centro).</summary>
    High
}

/// <summary>Formato dos módulos (pixels) do QR Code.</summary>
public enum QRCodeModuleShape
{
    /// <summary>Quadrados (padrão).</summary>
    Square,
    /// <summary>Quadrados com cantos arredondados.</summary>
    Rounded,
    /// <summary>Círculos.</summary>
    Circle
}

/// <summary>Formato dos três marcadores (eyes) de detecção do QR Code.</summary>
public enum QRCodeEyeShape
{
    /// <summary>Marcadores quadrados (padrão).</summary>
    Square,
    /// <summary>Marcadores com cantos arredondados.</summary>
    Rounded,
    /// <summary>Marcadores com moldura externa destacada.</summary>
    Framed
}

/// <summary>Tipos de série suportados pelo <c>OmniChart</c>.</summary>
public enum ChartSeriesType
{
    /// <summary>Linha.</summary>
    Line,
    /// <summary>Área (linha preenchida).</summary>
    Area,
    /// <summary>Colunas verticais.</summary>
    Column,
    /// <summary>Barras horizontais.</summary>
    Bar,
    /// <summary>Pizza.</summary>
    Pie,
    /// <summary>Donut (pizza com furo central).</summary>
    Donut,
    /// <summary>Waterfall (cascata de variações acumuladas).</summary>
    Waterfall
}

/// <summary>Posição da legenda do <c>OmniChart</c>.</summary>
public enum ChartLegendPosition
{
    /// <summary>Acima do gráfico.</summary>
    Top,
    /// <summary>À direita do gráfico.</summary>
    Right,
    /// <summary>Abaixo do gráfico.</summary>
    Bottom,
    /// <summary>À esquerda do gráfico.</summary>
    Left,
    /// <summary>Sem legenda.</summary>
    None
}

/// <summary>Outcome status for <c>OmniResult</c> — drives icon + color tone.</summary>
public enum ResultStatus
{
    /// <summary>Informational outcome.</summary>
    Info,
    /// <summary>Operation succeeded.</summary>
    Success,
    /// <summary>Succeeded with a caveat/warning.</summary>
    Warning,
    /// <summary>Operation failed.</summary>
    Error,
    /// <summary>HTTP 403 — access forbidden.</summary>
    Forbidden,
    /// <summary>HTTP 404 — resource not found.</summary>
    NotFound,
    /// <summary>Service under maintenance.</summary>
    Maintenance
}

/// <summary>External authentication provider for <c>OmniSocialButton</c>.</summary>
public enum SocialProvider
{
    /// <summary>Sign in with Google.</summary>
    Google,
    /// <summary>Sign in with Microsoft.</summary>
    Microsoft,
    /// <summary>Sign in with Apple.</summary>
    Apple,
    /// <summary>Sign in with GitHub.</summary>
    GitHub,
    /// <summary>Sign in with Facebook.</summary>
    Facebook,
    /// <summary>Sign in with a passkey (WebAuthn).</summary>
    Passkey
}

/// <summary>Esquema de cores das séries do <c>OmniChart</c>:
/// <list type="bullet">
///   <item><term>Palette</term><description>12 cores Tailwind-harmonizadas (default).</description></item>
///   <item><term>Accent</term><description>variações do <c>--omni-accent</c> atual (monocromática quente).</description></item>
///   <item><term>Pastel</term><description>cores suaves de baixa saturação.</description></item>
///   <item><term>Semantic</term><description>good, warn, danger, info, accent (5 cores).</description></item>
/// </list>
/// </summary>
public enum ChartColorScheme
{
    /// <summary>12 cores Tailwind-harmonizadas (default).</summary>
    Palette,
    /// <summary>Variações do <c>--omni-accent</c> atual (monocromática quente).</summary>
    Accent,
    /// <summary>Cores suaves de baixa saturação.</summary>
    Pastel,
    /// <summary>good, warn, danger, info, accent (5 cores).</summary>
    Semantic
}

/// <summary>Tipo de interpolação para séries lineares (Line/Area).</summary>
public enum ChartInterpolation
{
    /// <summary>Segmentos retos entre pontos.</summary>
    Linear,
    /// <summary>Curva suavizada (spline).</summary>
    Smooth,
    /// <summary>Em degraus (step).</summary>
    Step
}

/// <summary>Mês do ano (0-based, como o <c>Month</c> do Radzen) — usado pelo
/// <c>StartMonth</c> das views de ano do <c>OmniScheduler</c>.</summary>
public enum Month
{
    /// <summary>Janeiro.</summary>
    January = 0,
    /// <summary>Fevereiro.</summary>
    February = 1,
    /// <summary>Março.</summary>
    March = 2,
    /// <summary>Abril.</summary>
    April = 3,
    /// <summary>Maio.</summary>
    May = 4,
    /// <summary>Junho.</summary>
    June = 5,
    /// <summary>Julho.</summary>
    July = 6,
    /// <summary>Agosto.</summary>
    August = 7,
    /// <summary>Setembro.</summary>
    September = 8,
    /// <summary>Outubro.</summary>
    October = 9,
    /// <summary>Novembro.</summary>
    November = 10,
    /// <summary>Dezembro.</summary>
    December = 11
}

/// <summary>
/// Estilo visual da barra de um <c>OmniSplitter</c>:
/// <list type="bullet">
///   <item><term>Solid</term><description>barra visível com fundo sunken + grip dots no centro (default). Affordance clara — ideal para PDV, dashboards, telas de gestão onde o operador precisa enxergar onde redimensionar.</description></item>
///   <item><term>Line</term><description>fina linha de 1–2px que vira accent ao hover. Visual minimalista — ideal para IDE, editor de relatórios, ferramentas de design onde a barra não deve competir com o conteúdo.</description></item>
///   <item><term>Gap</term><description>espaçamento invisível entre painéis — só o cursor muda no hover. Ideal para landing pages e conteúdo editorial onde o splitter é raramente usado.</description></item>
/// </list>
/// </summary>
public enum SplitterVariant
{
    /// <summary>Barra visível com fundo sunken + grip dots no centro (default). Affordance clara para PDV/dashboards.</summary>
    Solid,
    /// <summary>Fina linha de 1–2px que vira accent ao hover. Visual minimalista para IDE/editores.</summary>
    Line,
    /// <summary>Espaçamento invisível entre painéis — só o cursor muda no hover. Para landing pages/conteúdo editorial.</summary>
    Gap
}

/// <summary>
/// Direção detectada por um <c>OmniSwipeArea</c>. <c>None</c> é usado quando
/// o eixo correspondente não teve movimento (por exemplo, em
/// <c>MultiDimensionSwipeEventArgs.SwipeDirections</c> uma das duas direções
/// pode ser <c>None</c> se o swipe foi puramente horizontal ou vertical).
/// </summary>
public enum SwipeDirection
{
    /// <summary>Sem movimento no eixo correspondente.</summary>
    None,
    /// <summary>Swipe da esquerda para a direita.</summary>
    LeftToRight,
    /// <summary>Swipe da direita para a esquerda.</summary>
    RightToLeft,
    /// <summary>Swipe de cima para baixo.</summary>
    TopToBottom,
    /// <summary>Swipe de baixo para cima.</summary>
    BottomToTop,
}

/// <summary>Variante visual de um <c>OmniLink</c>:
/// <list type="bullet">
///   <item><term>Default</term><description>cor accent (azul/âmbar conforme tema) — link normal.</description></item>
///   <item><term>Muted</term><description>cor <c>--omni-fg-soft</c> — link secundário/discreto.</description></item>
///   <item><term>Danger</term><description>cor <c>--omni-danger</c> — link destrutivo (ex: "Excluir conta").</description></item>
/// </list>
/// </summary>
public enum LinkVariant
{
    /// <summary>Cor accent (azul/âmbar conforme tema) — link normal.</summary>
    Default,
    /// <summary>Cor <c>--omni-fg-soft</c> — link secundário/discreto.</summary>
    Muted,
    /// <summary>Cor <c>--omni-danger</c> — link destrutivo (ex: "Excluir conta").</summary>
    Danger
}

/// <summary>Variante visual de um <c>OmniToggleButton</c>.</summary>
public enum ToggleVariant
{
    /// <summary>Superfície neutra padrão.</summary>
    Default,
    /// <summary>Estilo da ação primária.</summary>
    Primary,
    /// <summary>Cor accent do tema.</summary>
    Accent,
    /// <summary>Transparente — fundo só quando ativo/hover.</summary>
    Ghost
}

/// <summary>Modo de edição do <c>OmniHtmlEditor</c>: WYSIWYG (Design) ou HTML cru (Source).</summary>
public enum HtmlEditorMode
{
    /// <summary>WYSIWYG — edição visual.</summary>
    Design,
    /// <summary>HTML cru — edição do markup.</summary>
    Source
}

/// <summary>Como a imagem do <c>OmniImage</c> preenche sua caixa (CSS <c>object-fit</c>).</summary>
public enum ObjectFit
{
    /// <summary>Estica para preencher, ignorando proporção (<c>fill</c>).</summary>
    Fill,
    /// <summary>Cabe inteira na caixa preservando proporção (<c>contain</c>).</summary>
    Contain,
    /// <summary>Cobre toda a caixa preservando proporção, recortando o excesso (<c>cover</c>).</summary>
    Cover,
    /// <summary>Tamanho intrínseco, sem redimensionar (<c>none</c>).</summary>
    None,
    /// <summary>Menor entre <c>none</c> e <c>contain</c> (<c>scale-down</c>).</summary>
    ScaleDown
}

/// <summary>Tipo de entrada do <c>OmniSecurityCode</c>:
/// <list type="bullet">
///   <item><term>Text</term><description>qualquer caractere.</description></item>
///   <item><term>Numeric</term><description>só dígitos (inputmode numérico).</description></item>
///   <item><term>Password</term><description>mascarado (•), qualquer caractere.</description></item>
/// </list></summary>
public enum SecurityCodeType
{
    /// <summary>Qualquer caractere.</summary>
    Text,
    /// <summary>Só dígitos (inputmode numérico).</summary>
    Numeric,
    /// <summary>Mascarado (•), qualquer caractere.</summary>
    Password
}

/// <summary>Cor/intent semântico do ponto (dot) de um <c>OmniTimelineItem</c>.
/// Mapeia para os tokens <c>--omni-fg-muted</c> (Base), <c>--omni-accent</c>,
/// <c>--omni-good</c>, <c>--omni-warn</c>, <c>--omni-danger</c> ou <c>--omni-info</c>.</summary>
public enum TimelinePointStyle
{
    /// <summary>Neutro — <c>--omni-fg-muted</c>.</summary>
    Base,
    /// <summary>Accent — <c>--omni-accent</c>.</summary>
    Accent,
    /// <summary>Sucesso — <c>--omni-good</c>.</summary>
    Good,
    /// <summary>Atenção — <c>--omni-warn</c>.</summary>
    Warn,
    /// <summary>Perigo — <c>--omni-danger</c>.</summary>
    Danger,
    /// <summary>Informativo — <c>--omni-info</c>.</summary>
    Info
}

/// <summary>Variante de preenchimento do ponto de um <c>OmniTimelineItem</c>:
/// <list type="bullet">
///   <item><term>Filled</term><description>fundo sólido na cor do estilo (default).</description></item>
///   <item><term>Outlined</term><description>fundo neutro com anel interno colorido.</description></item>
///   <item><term>Text</term><description>fundo neutro, sem anel — só o conteúdo (ícone/letra) colorido.</description></item>
/// </list></summary>
public enum TimelinePointVariant
{
    /// <summary>Fundo sólido na cor do estilo (default).</summary>
    Filled,
    /// <summary>Fundo neutro com anel interno colorido.</summary>
    Outlined,
    /// <summary>Fundo neutro, sem anel — só o conteúdo (ícone/letra) colorido.</summary>
    Text
}

/// <summary>Posição da linha de um <c>OmniTimeline</c> em relação ao conteúdo:
/// <list type="bullet">
///   <item><term>Center</term><description>linha no centro, conteúdo dos dois lados (default).</description></item>
///   <item><term>Start</term><description>linha na borda inicial; conteúdo só do lado final.</description></item>
///   <item><term>End</term><description>linha na borda final; conteúdo só do lado inicial.</description></item>
///   <item><term>Alternate</term><description>linha no centro; itens alternam de lado (zigue-zague).</description></item>
/// </list></summary>
public enum TimelineLinePosition
{
    /// <summary>Linha no centro, conteúdo dos dois lados (default).</summary>
    Center,
    /// <summary>Linha na borda inicial; conteúdo só do lado final.</summary>
    Start,
    /// <summary>Linha na borda final; conteúdo só do lado inicial.</summary>
    End,
    /// <summary>Linha no centro; itens alternam de lado (zigue-zague).</summary>
    Alternate
}

/// <summary>Posição do paginador (dots) de um <c>OmniCarousel</c>:
/// <list type="bullet">
///   <item><term>Top</term><description>dots acima dos slides.</description></item>
///   <item><term>Bottom</term><description>dots abaixo dos slides (default).</description></item>
///   <item><term>TopAndBottom</term><description>dots nas duas extremidades.</description></item>
/// </list></summary>
public enum CarouselPagerPosition
{
    /// <summary>Dots acima dos slides.</summary>
    Top,
    /// <summary>Dots abaixo dos slides (default).</summary>
    Bottom,
    /// <summary>Dots nas duas extremidades.</summary>
    TopAndBottom
}
