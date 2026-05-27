namespace Omni.Blazor.Models;

/// <summary>
/// Canto da viewport onde o FAB (Floating Action Button) é ancorado.
/// Equivalente ao posicionamento absoluto do RadzenFab, mas usando presets
/// claros em vez de inset-block-* genérico (mais fácil pra dev típico).
/// </summary>
/// <remarks>
/// O FAB usa <c>position: fixed</c> (não <c>absolute</c> como o Radzen) — fica
/// ancorado à viewport mesmo com scroll. Pra colocar dentro de container
/// específico use <see cref="FabPosition.Static"/> e posicione via CSS no
/// pai (que precisa ter <c>position: relative</c>).
/// </remarks>
public enum FabPosition
{
    /// <summary>Canto inferior direito (padrão Material Design).</summary>
    BottomRight,
    /// <summary>Canto inferior esquerdo.</summary>
    BottomLeft,
    /// <summary>Canto superior direito.</summary>
    TopRight,
    /// <summary>Canto superior esquerdo.</summary>
    TopLeft,
    /// <summary>Centro inferior (apps mobile com bottom-nav central).</summary>
    BottomCenter,
    /// <summary>
    /// Sem posicionamento fixed/absolute — flui no documento. Use quando o FAB
    /// vive dentro de um container customizado já posicionado.
    /// </summary>
    Static,
}

/// <summary>
/// Posição do label flutuante em <c>OmniFabMenuItem</c>.
/// </summary>
public enum FabMenuItemLabelPosition
{
    /// <summary>Decide automaticamente baseado na direção do menu pai:
    /// menu Up/Down = label à esquerda (canto direito) ou à direita (canto esquerdo);
    /// menu Left/Right = sem label (espaço já ocupado pelos próprios items).</summary>
    Auto,
    /// <summary>Label sempre à esquerda do botão.</summary>
    Left,
    /// <summary>Label sempre à direita do botão.</summary>
    Right,
    /// <summary>Sem label visível (usa só tooltip nativo via <c>Title</c>).</summary>
    None,
}

/// <summary>
/// Modo de animação dos itens ao abrir o <c>OmniFabMenu</c>.
/// Cada modo ainda preserva a ordem natural: o item mais próximo do FAB
/// aparece primeiro (cascade "para fora do botão principal").
/// </summary>
public enum FabMenuAnimation
{
    /// <summary>Pop-in com escala 0.4→1 + leve translateY + cascade staggered.
    /// Padrão Material Design ("spring out" feel). Default.</summary>
    Stagger,
    /// <summary>Fade simples (opacity 0→1) sem scale nem translate.
    /// Mais rápido e sutil — equivalente ao animation linear do Radzen.</summary>
    Linear,
    /// <summary>Sem animação. Items aparecem instantaneamente.
    /// Use em UIs density-críticas ou pra usuários com prefers-reduced-motion.</summary>
    None,
}

/// <summary>
/// Direção em que os itens do <c>OmniFabMenu</c> se expandem ao abrir.
/// </summary>
/// <remarks>
/// REGRA PRÁTICA: o FAB fica ancorado à borda; os itens expandem PRA DENTRO
/// da tela. Combine Direction com Position no sentido oposto:
/// <list type="bullet">
///   <item><c>Up</c> + <c>BottomRight</c>/<c>BottomLeft</c>/<c>BottomCenter</c></item>
///   <item><c>Down</c> + <c>TopRight</c>/<c>TopLeft</c></item>
///   <item><c>Left</c> + <c>BottomRight</c>/<c>TopRight</c> (toolbar à direita)</item>
///   <item><c>Right</c> + <c>BottomLeft</c>/<c>TopLeft</c> (toolbar à esquerda)</item>
/// </list>
/// O componente NÃO faz auto-flip — escolha a combinação que faz sentido.
/// </remarks>
public enum FabMenuDirection
{
    /// <summary>Itens expandem PRA CIMA do FAB. Use com Position=<c>Bottom*</c>.</summary>
    Up,
    /// <summary>Itens expandem PRA BAIXO do FAB. Use com Position=<c>Top*</c>.</summary>
    Down,
    /// <summary>Itens expandem PRA ESQUERDA do FAB (FAB ancorado à direita,
    /// items entram na tela). Use com Position=<c>BottomRight</c>/<c>TopRight</c>.</summary>
    Left,
    /// <summary>Itens expandem PRA DIREITA do FAB (FAB ancorado à esquerda,
    /// items entram na tela). Use com Position=<c>BottomLeft</c>/<c>TopLeft</c>.</summary>
    Right,
}
