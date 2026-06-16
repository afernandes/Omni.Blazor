namespace Omni.Blazor.Models;

public enum ButtonVariant { Default, Primary, Ghost, Danger, Link }
public enum ComponentSize { Sm, Md, Lg, Xl }
public enum NotificationSeverity { Info, Success, Warning, Error }
public enum NotificationPosition { TopRight, TopLeft, BottomRight, BottomLeft }
public enum SidePosition { Right, Left }
public enum BadgeVariant { Default, Good, Warn, Danger, Info, Accent, Plain, Solid }

/// <summary>
/// Anchor position for a <c>OmniBadge</c> in overlay mode. Start/End naming
/// is RTL-aware (Start = inline-start, End = inline-end). Use TopEnd for
/// the classic "notification dot in top-right" pattern.
/// </summary>
public enum BadgeOrigin { TopEnd, TopStart, BottomEnd, BottomStart }
public enum AvatarSize { Sm, Md, Lg, Xl, XXl }
public enum SortDirection { None, Ascending, Descending }
public enum DialogResultKind { Ok, Cancel, Closed }
public enum SkeletonVariant { Text, Rect, Circle }
public enum Orientation { Horizontal, Vertical }
public enum DataGridEditMode { None, Cell, Row }
public enum AccordionMode { Single, Multi }
public enum PopoverPosition { Top, Bottom, Left, Right }
public enum PaginationVariant { Full, Compact }

/// <summary>Variante visual do <c>OmniCard</c>: superfície padrão, moldura "accent" ou apenas contorno.</summary>
public enum CardVariant { Default, Accent, Outline }

/// <summary>Cor temática do <c>OmniCard</c>. No variante Default pinta a superfície; no Outline, a borda.</summary>
public enum CardTone { None, Accent, Neutral, Info, Success, Warning, Danger }

/// <summary>Posição da mídia dentro de um <c>OmniCardMedia</c>.</summary>
public enum CardMediaPosition { Top, Bottom, Overlay, Start, End }

/// <summary>Como os submenus de um <c>OmniMenuBar</c> são abertos.</summary>
public enum MenuTrigger { Hover, Click }

/// <summary>Cor da pílula de metadados de um item de dropdown do <c>OmniMenuBar</c>.</summary>
public enum MenuMetaKind { Neutral, Accent, Good, Warn, Danger }

/// <summary>Cor/intent semântico de um <c>OmniStatusBadge</c>.
/// As cinco variantes mapeiam para as variáveis CSS <c>--omni-good</c>,
/// <c>--omni-warn</c>, <c>--omni-danger</c>, <c>--omni-accent</c> ou
/// <c>--omni-fg-muted</c> (Neutral).</summary>
public enum StatusBadgeKind { Neutral, Accent, Good, Warn, Danger }

/// <summary>Direção de empilhamento do <c>OmniStack</c>.
/// Column = vertical (default), Row = horizontal.</summary>
public enum StackDirection { Column, Row }

/// <summary>Alinhamento dos filhos no eixo cross de um <c>OmniStack</c>
/// (mapeia para CSS <c>align-items</c>).</summary>
public enum StackAlign { Start, Center, End, Stretch, Baseline }

/// <summary>Distribuição dos filhos no eixo main de um <c>OmniStack</c>
/// (mapeia para CSS <c>justify-content</c>).</summary>
public enum StackJustify { Start, Center, End, Between, Around, Evenly }

/// <summary>Operadores de filtro suportados por coluna no <c>OmniDataGrid</c>.</summary>
public enum FilterOperator
{
    Contains,
    NotContains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterOrEqual,
    LessThan,
    LessOrEqual,
    Between,
    NotBetween,
    IsEmpty,
    IsNotEmpty
}

/// <summary>Tipo de UI/operadores oferecidos por uma coluna filtrável.</summary>
public enum ColumnFilterType { Text, Number, Date, Boolean, Select }

/// <summary>Operador lógico que combina condições/grupos num <c>OmniDataFilter</c>.</summary>
public enum FilterLogic { And, Or }

/// <summary>Função agregadora aplicada numa coluna (footer/group footer).</summary>
public enum AggregateFunction { Sum, Average, Count, Min, Max }

/// <summary>Posição congelada de uma coluna (sticky horizontal).</summary>
public enum FrozenPosition { Left, Right }

/// <summary>Modo de expansão de linhas para master-detail.</summary>
public enum ExpandMode { Single, Multi }

/// <summary>Layout de renderização do DataGrid: tabela clássica ou grid CSS (necessário p/ virtualização).</summary>
public enum DataGridLayoutMode { Table, Grid }

[Flags]
public enum Modifier
{
    None  = 0,
    Ctrl  = 1 << 0,
    Alt   = 1 << 1,
    Shift = 1 << 2,
    Meta  = 1 << 3
}

// ===== Layout / wireframe ===================================================

/// <summary>Posição de ancoragem de um <c>OmniDrawer</c> no <c>OmniLayout</c>.</summary>
public enum DrawerAnchor { Left, Right }

/// <summary>
/// Variante de comportamento de um <c>OmniDrawer</c>:
/// <list type="bullet">
///   <item><term>Persistent</term><description>sempre visível, empurra o main.</description></item>
///   <item><term>Mini</term><description>sempre visível mas icon-only; expande on-hover.</description></item>
///   <item><term>Temporary</term><description>overlay com backdrop, fechado por default.</description></item>
///   <item><term>Responsive</term><description>Persistent acima do <c>Breakpoint</c>, Temporary abaixo (default).</description></item>
/// </list>
/// </summary>
public enum DrawerVariant { Persistent, Mini, Temporary, Responsive }

/// <summary>Largura máxima de um <c>OmniContainer</c> (semelhante a Bootstrap <c>.container-{bp}</c>).</summary>
public enum ContainerMaxWidth { Sm, Md, Lg, Xl, Xxl, Full }

/// <summary>Posição de uma barra (<c>OmniAppBar</c>) no <c>OmniLayout</c>: topo ou rodapé.</summary>
public enum BarPosition { Top, Bottom }

/// <summary>
/// Modo de comparação do <c>OmniHidden</c>:
/// <list type="bullet">
/// <item><term>Down</term><description>esconde quando viewport &lt;= Breakpoint (default — esconde mobile e abaixo)</description></item>
/// <item><term>Up</term><description>esconde quando viewport &gt;= Breakpoint (esconde desktop e acima)</description></item>
/// <item><term>Only</term><description>esconde apenas no breakpoint exato</description></item>
/// </list>
/// </summary>
public enum HiddenMode { Down, Up, Only }

/// <summary>Densidade global do layout — aplica em <c>&lt;html data-density="..."&gt;</c>
/// e troca CSS vars de padding/radius/font-size em cascata.</summary>
public enum LayoutDensity { Compact, Comfortable, Spacious }

/// <summary>
/// Standard responsive breakpoints. Values mirror common conventions
/// (Bootstrap / Tailwind): xs &lt;576, sm &lt;768, md &lt;992, lg &lt;1200,
/// xl &lt;1400, xxl ≥1400. Use with <c>BreakpointService</c> /
/// <c>OmniBreakpointProvider</c>.
/// </summary>
public enum Breakpoint
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
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
public enum SparklineVariant { Line, Area, Column, Bar, Pie }

/// <summary>Modo de aparência pro <c>OmniAppearanceToggle</c>.
/// <list type="bullet">
///   <item><term>Light</term><description>tema claro, explicitamente escolhido pelo user.</description></item>
///   <item><term>Dark</term><description>tema escuro, explicitamente escolhido.</description></item>
///   <item><term>System</term><description>segue o <c>prefers-color-scheme</c> do SO.</description></item>
/// </list>
/// </summary>
public enum ThemeMode { Light, Dark, System }

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
public enum SwipeAreaLiveTransform { None, X, Y, Both }

/// <summary>Variant do <c>OmniBottomSheet</c>.
/// <list type="bullet">
///   <item><term>Modal</term><description>com backdrop, bloqueia interação com o fundo. Padrão pra forms/pickers/confirmações.</description></item>
///   <item><term>Standard</term><description>sem backdrop, página continua interativa. Usado em apps tipo Maps (detalhes coexistem com mapa).</description></item>
/// </list>
/// </summary>
public enum BottomSheetVariant { Modal, Standard }

/// <summary>Comportamento responsivo do <c>OmniBottomSheet</c> em desktop.
/// Material 3 NÃO recomenda bottom sheet em telas grandes — esse enum permite
/// adaptar pra alternativas mais apropriadas.
/// <list type="bullet">
///   <item><term>Always</term><description>sempre bottom sheet (default).</description></item>
///   <item><term>DrawerOnDesktop</term><description>vira drawer lateral (direita) em desktop.</description></item>
///   <item><term>DialogOnDesktop</term><description>vira dialog centralizado em desktop.</description></item>
/// </list>
/// </summary>
public enum BottomSheetAdaptive { Always, DrawerOnDesktop, DialogOnDesktop }

/// <summary>Visual do <c>OmniAppearanceToggle</c>.
/// <list type="bullet">
///   <item><term>Dropdown</term><description>botão único com menu popover de 3 opções (default).</description></item>
///   <item><term>Segmented</term><description>3 botões inline lado a lado.</description></item>
///   <item><term>Cycle</term><description>botão único que cicla pelos 3 modos a cada click.</description></item>
/// </list>
/// </summary>
public enum AppearanceToggleVariant { Dropdown, Segmented, Cycle }

/// <summary>Símbologias de código de barras suportadas pelo <c>OmniBarcode</c>.
/// <list type="bullet">
///   <item><term>Code128</term><description>alfanumérico, alta densidade (padrão).</description></item>
///   <item><term>Code39</term><description>letras maiúsculas, números, alguns símbolos. Auto-checksum.</description></item>
///   <item><term>Ean13</term><description>13 dígitos (12 + checksum). Padrão de varejo internacional.</description></item>
///   <item><term>Ean8</term><description>versão curta do EAN-13 (8 dígitos).</description></item>
///   <item><term>UpcA</term><description>12 dígitos, padrão de varejo norte-americano.</description></item>
/// </list>
/// </summary>
public enum BarcodeType { Code128, Code39, Ean13, Ean8, UpcA }

/// <summary>Nível de correção de erros do QR Code: maior nível tolera mais
/// dano físico mas reduz a capacidade de dados.
/// <list type="bullet">
///   <item><term>Low</term><description>~7% de tolerância (default da maioria das libs).</description></item>
///   <item><term>Medium</term><description>~15% (default Omni — bom para uso geral).</description></item>
///   <item><term>Quartile</term><description>~25%.</description></item>
///   <item><term>High</term><description>~30% (use quando há logo no centro).</description></item>
/// </list>
/// </summary>
public enum QRCodeEcc { Low, Medium, Quartile, High }

/// <summary>Formato dos módulos (pixels) do QR Code.</summary>
public enum QRCodeModuleShape { Square, Rounded, Circle }

/// <summary>Formato dos três marcadores (eyes) de detecção do QR Code.</summary>
public enum QRCodeEyeShape { Square, Rounded, Framed }

/// <summary>Tipos de série suportados pelo <c>OmniChart</c>.</summary>
public enum ChartSeriesType { Line, Area, Column, Bar, Pie, Donut, Waterfall }

/// <summary>Posição da legenda do <c>OmniChart</c>.</summary>
public enum ChartLegendPosition { Top, Right, Bottom, Left, None }

/// <summary>Outcome status for <c>OmniResult</c> — drives icon + color tone.</summary>
public enum ResultStatus { Info, Success, Warning, Error, Forbidden, NotFound, Maintenance }

/// <summary>External authentication provider for <c>OmniSocialButton</c>.</summary>
public enum SocialProvider { Google, Microsoft, Apple, GitHub, Facebook, Passkey }

/// <summary>Esquema de cores das séries do <c>OmniChart</c>:
/// <list type="bullet">
///   <item><term>Palette</term><description>12 cores Tailwind-harmonizadas (default).</description></item>
///   <item><term>Accent</term><description>variações do <c>--omni-accent</c> atual (monocromática quente).</description></item>
///   <item><term>Pastel</term><description>cores suaves de baixa saturação.</description></item>
///   <item><term>Semantic</term><description>good, warn, danger, info, accent (5 cores).</description></item>
/// </list>
/// </summary>
public enum ChartColorScheme { Palette, Accent, Pastel, Semantic }

/// <summary>Tipo de interpolação para séries lineares (Line/Area).</summary>
public enum ChartInterpolation { Linear, Smooth, Step }

/// <summary>Mês do ano (0-based, como o <c>Month</c> do Radzen) — usado pelo
/// <c>StartMonth</c> das views de ano do <c>OmniScheduler</c>.</summary>
public enum Month
{
    January = 0, February = 1, March = 2, April = 3, May = 4, June = 5,
    July = 6, August = 7, September = 8, October = 9, November = 10, December = 11
}

/// <summary>
/// Estilo visual da barra de um <c>OmniSplitter</c>:
/// <list type="bullet">
///   <item><term>Solid</term><description>barra visível com fundo sunken + grip dots no centro (default). Affordance clara — ideal para PDV, dashboards, telas de gestão onde o operador precisa enxergar onde redimensionar.</description></item>
///   <item><term>Line</term><description>fina linha de 1–2px que vira accent ao hover. Visual minimalista — ideal para IDE, editor de relatórios, ferramentas de design onde a barra não deve competir com o conteúdo.</description></item>
///   <item><term>Gap</term><description>espaçamento invisível entre painéis — só o cursor muda no hover. Ideal para landing pages e conteúdo editorial onde o splitter é raramente usado.</description></item>
/// </list>
/// </summary>
public enum SplitterVariant { Solid, Line, Gap }

/// <summary>
/// Direção detectada por um <c>OmniSwipeArea</c>. <c>None</c> é usado quando
/// o eixo correspondente não teve movimento (por exemplo, em
/// <c>MultiDimensionSwipeEventArgs.SwipeDirections</c> uma das duas direções
/// pode ser <c>None</c> se o swipe foi puramente horizontal ou vertical).
/// </summary>
public enum SwipeDirection
{
    None,
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop,
}

/// <summary>Variante visual de um <c>OmniLink</c>:
/// <list type="bullet">
///   <item><term>Default</term><description>cor accent (azul/âmbar conforme tema) — link normal.</description></item>
///   <item><term>Muted</term><description>cor <c>--omni-fg-soft</c> — link secundário/discreto.</description></item>
///   <item><term>Danger</term><description>cor <c>--omni-danger</c> — link destrutivo (ex: "Excluir conta").</description></item>
/// </list>
/// </summary>
public enum LinkVariant { Default, Muted, Danger }

/// <summary>Variante visual de um <c>OmniToggleButton</c>.</summary>
public enum ToggleVariant { Default, Primary, Accent, Ghost }

/// <summary>Modo de edição do <c>OmniHtmlEditor</c>: WYSIWYG (Design) ou HTML cru (Source).</summary>
public enum HtmlEditorMode { Design, Source }

/// <summary>Como a imagem do <c>OmniImage</c> preenche sua caixa (CSS <c>object-fit</c>).</summary>
public enum ObjectFit { Fill, Contain, Cover, None, ScaleDown }

/// <summary>Tipo de entrada do <c>OmniSecurityCode</c>:
/// <list type="bullet">
///   <item><term>Text</term><description>qualquer caractere.</description></item>
///   <item><term>Numeric</term><description>só dígitos (inputmode numérico).</description></item>
///   <item><term>Password</term><description>mascarado (•), qualquer caractere.</description></item>
/// </list></summary>
public enum SecurityCodeType { Text, Numeric, Password }

/// <summary>Cor/intent semântico do ponto (dot) de um <c>OmniTimelineItem</c>.
/// Mapeia para os tokens <c>--omni-fg-muted</c> (Base), <c>--omni-accent</c>,
/// <c>--omni-good</c>, <c>--omni-warn</c>, <c>--omni-danger</c> ou <c>--omni-info</c>.</summary>
public enum TimelinePointStyle { Base, Accent, Good, Warn, Danger, Info }

/// <summary>Variante de preenchimento do ponto de um <c>OmniTimelineItem</c>:
/// <list type="bullet">
///   <item><term>Filled</term><description>fundo sólido na cor do estilo (default).</description></item>
///   <item><term>Outlined</term><description>fundo neutro com anel interno colorido.</description></item>
///   <item><term>Text</term><description>fundo neutro, sem anel — só o conteúdo (ícone/letra) colorido.</description></item>
/// </list></summary>
public enum TimelinePointVariant { Filled, Outlined, Text }

/// <summary>Posição da linha de um <c>OmniTimeline</c> em relação ao conteúdo:
/// <list type="bullet">
///   <item><term>Center</term><description>linha no centro, conteúdo dos dois lados (default).</description></item>
///   <item><term>Start</term><description>linha na borda inicial; conteúdo só do lado final.</description></item>
///   <item><term>End</term><description>linha na borda final; conteúdo só do lado inicial.</description></item>
///   <item><term>Alternate</term><description>linha no centro; itens alternam de lado (zigue-zague).</description></item>
/// </list></summary>
public enum TimelineLinePosition { Center, Start, End, Alternate }

/// <summary>Posição do paginador (dots) de um <c>OmniCarousel</c>:
/// <list type="bullet">
///   <item><term>Top</term><description>dots acima dos slides.</description></item>
///   <item><term>Bottom</term><description>dots abaixo dos slides (default).</description></item>
///   <item><term>TopAndBottom</term><description>dots nas duas extremidades.</description></item>
/// </list></summary>
public enum CarouselPagerPosition { Top, Bottom, TopAndBottom }
