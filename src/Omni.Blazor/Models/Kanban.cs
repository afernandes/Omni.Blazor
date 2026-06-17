namespace Omni.Blazor.Models;

/// <summary>
/// Definição de uma coluna do <c>OmniKanban</c>. É um modelo de dados (mutável)
/// passado via <c>Columns</c>. O estado de "recolhida" é semente — o componente
/// controla o toggle internamente em runtime.
/// </summary>
public sealed class KanbanColumn
{
    /// <summary>Chave da coluna — casada com o valor retornado por <c>ColumnSelector</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Título exibido no cabeçalho. <c>null</c> usa o <see cref="Id"/>.</summary>
    public string? Title { get; set; }

    /// <summary>Ícone Lucide opcional no cabeçalho.</summary>
    public string? Icon { get; set; }

    /// <summary>Tom de cor da coluna (faixa no topo + acentos). Default None.</summary>
    public CardTone Tone { get; set; } = CardTone.None;

    /// <summary>Limite de WIP (work-in-progress). <c>null</c> = sem limite.</summary>
    public int? WipLimit { get; set; }

    /// <summary>Estado inicial de recolhida (o componente controla o toggle depois).</summary>
    public bool Collapsed { get; set; }
}

/// <summary>
/// Definição de uma raia (swimlane) do <c>OmniKanban</c> — categorização horizontal
/// dos cards (por responsável, épico, prioridade, etc.). O estado "recolhida" é semente.
/// </summary>
public sealed class KanbanSwimlane
{
    /// <summary>Chave da raia — casada com o valor de <c>SwimlaneSelector</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Título exibido no cabeçalho da raia. <c>null</c> usa o <see cref="Id"/>.</summary>
    public string? Title { get; set; }

    /// <summary>Ícone Lucide opcional.</summary>
    public string? Icon { get; set; }

    /// <summary>Estado inicial de recolhida.</summary>
    public bool Collapsed { get; set; }
}

/// <summary>
/// Filtro rápido do <c>OmniKanban</c> — um chip clicável que restringe os cards
/// visíveis ao seu <see cref="Predicate"/>. Filtros ativos combinam em E (AND).
/// </summary>
/// <typeparam name="TCard">Tipo do card.</typeparam>
public sealed class KanbanQuickFilter<TCard>
{
    /// <summary>Texto do chip.</summary>
    public string Label { get; set; } = "";

    /// <summary>Ícone Lucide opcional.</summary>
    public string? Icon { get; set; }

    /// <summary>Predicado — cards que retornam <c>false</c> são ocultados quando o filtro está ativo.</summary>
    public Func<TCard, bool> Predicate { get; set; } = _ => true;

    // Igualdade por Label para que o estado "ativo" sobreviva a re-criações da
    // instância (consumidores que recriam os filtros a cada render).
    public override bool Equals(object? obj) =>
        obj is KanbanQuickFilter<TCard> other && string.Equals(Label, other.Label, StringComparison.Ordinal);

    public override int GetHashCode() => Label is null ? 0 : Label.GetHashCode(StringComparison.Ordinal);
}

/// <summary>Prioridade de um card — controla o indicador no card padrão do <c>OmniKanban</c>.</summary>
public enum KanbanPriority
{
    None,
    Low,
    Medium,
    High,
    Urgent,
}

/// <summary>Campo extra exibido no card padrão do <c>OmniKanban</c> (rótulo + valor; até 3).</summary>
public sealed class KanbanField
{
    /// <summary>Rótulo do campo.</summary>
    public string Label { get; set; } = "";

    /// <summary>Valor exibido.</summary>
    public string? Value { get; set; }

    /// <summary>Ícone Lucide opcional.</summary>
    public string? Icon { get; set; }
}

/// <summary>
/// Item do menu de ações ("…") de um card do <c>OmniKanban</c> — dados puros
/// (atribuir, mover, sinalizar, etc.). O componente o converte num item de
/// context-menu e dispara <c>CardAction</c> ao selecionar.
/// </summary>
public sealed class KanbanCardAction
{
    /// <summary>Identificador da ação (usado no <c>switch</c> do handler).</summary>
    public string Id { get; set; } = "";

    /// <summary>Texto exibido no menu.</summary>
    public string? Label { get; set; }

    /// <summary>Ícone Lucide opcional.</summary>
    public string? Icon { get; set; }

    /// <summary>Atalho exibido à direita (apenas visual).</summary>
    public string? Shortcut { get; set; }

    /// <summary>Renderiza em vermelho (ação destrutiva).</summary>
    public bool Danger { get; set; }

    /// <summary>Renderiza como separador (ignora os demais campos).</summary>
    public bool Divider { get; set; }

    /// <summary>Cria um separador.</summary>
    public static KanbanCardAction Separator() => new() { Divider = true };
}

/// <summary>Dados do evento <c>CardAction</c> — ação de menu selecionada num card.</summary>
/// <typeparam name="TCard">Tipo do card.</typeparam>
public sealed class KanbanCardActionEventArgs<TCard>
{
    /// <summary>Card alvo da ação.</summary>
    public required TCard Card { get; init; }

    /// <summary>Ação selecionada.</summary>
    public required KanbanCardAction Action { get; init; }
}

/// <summary>Dados do evento <c>ColumnMoved</c> — emitido após reordenar colunas por drag.</summary>
public sealed class KanbanColumnMovedEventArgs
{
    /// <summary>Coluna movida.</summary>
    public required KanbanColumn Column { get; init; }

    /// <summary>Índice anterior.</summary>
    public required int OldIndex { get; init; }

    /// <summary>Índice novo.</summary>
    public required int NewIndex { get; init; }
}

/// <summary>Como o <c>OmniKanban</c> trata o limite de WIP de uma coluna.</summary>
public enum WipLimitMode
{
    /// <summary>Permite ultrapassar; só sinaliza visualmente (badge vermelho).</summary>
    Warn,

    /// <summary>Bloqueia o drop que ultrapassaria o limite.</summary>
    Enforce,
}

/// <summary>Dados do evento <c>CardMoved</c> — emitido após mover um card (drag ou teclado).</summary>
/// <typeparam name="TCard">Tipo do card.</typeparam>
public sealed class KanbanCardMovedEventArgs<TCard>
{
    /// <summary>O card movido.</summary>
    public required TCard Card { get; init; }

    /// <summary>Id da coluna de origem.</summary>
    public required string FromColumn { get; init; }

    /// <summary>Id da coluna de destino.</summary>
    public required string ToColumn { get; init; }

    /// <summary>Índice do card dentro da coluna de origem (antes do move).</summary>
    public required int OldIndex { get; init; }

    /// <summary>Índice do card dentro da coluna de destino (após o move).</summary>
    public required int NewIndex { get; init; }

    /// <summary>Id da raia de origem (<c>null</c> quando não há swimlanes).</summary>
    public string? FromSwimlane { get; init; }

    /// <summary>Id da raia de destino (<c>null</c> quando não há swimlanes).</summary>
    public string? ToSwimlane { get; init; }
}
