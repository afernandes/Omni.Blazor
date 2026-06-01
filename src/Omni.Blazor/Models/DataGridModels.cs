namespace Omni.Blazor.Models;

/// <summary>
/// Critério de ordenação aplicado a uma coluna. <c>Property</c> é o nome canônico
/// (o consumidor server-side usa para mapear em SQL/OData). <c>Direction</c> nunca
/// é <c>None</c> aqui — uma coluna "sem ordem" simplesmente não aparece na lista.
/// </summary>
public sealed record SortDescriptor(string Property, SortDirection Direction);

/// <summary>
/// Args for <c>OmniDataGrid.ColumnResized</c> — fired after the user finishes
/// dragging a column's resize handle. <c>PropertyName</c> identifies the column;
/// <c>Width</c> is the new width in pixels.
/// </summary>
public sealed record DataGridColumnResizedEventArgs(string? PropertyName, double Width);

/// <summary>
/// Critério de filtro aplicado a uma coluna. <c>SecondValue</c> é usado por
/// operadores binários (<c>Between</c>, <c>NotBetween</c>); ignorado nos demais.
/// </summary>
public sealed record FilterDescriptor(
    string Property,
    FilterOperator Operator,
    object? Value,
    object? SecondValue = null);

/// <summary>
/// Resultado de agregação por grupo. Caso o consumidor server-side retorne
/// dados pré-agrupados, expõe a árvore aqui.
/// </summary>
public sealed record GroupResult<TItem>(
    string Property,
    object? Key,
    IReadOnlyList<TItem> Items,
    IReadOnlyList<GroupResult<TItem>>? Children = null,
    IReadOnlyDictionary<string, object?>? Aggregates = null);

/// <summary>
/// Snapshot do estado do DataGrid no momento de uma busca. Enviado ao
/// callback <c>LoadData</c> para que o consumidor execute paging/sort/filter
/// no servidor (ou backend in-memory).
/// </summary>
public sealed record GridState<TItem>(
    int Skip,
    int Top,
    string? Search,
    IReadOnlyList<SortDescriptor> Sort,
    IReadOnlyList<FilterDescriptor> Filters,
    IReadOnlyList<string> GroupBy)
{
    /// <summary>State vazio (sem paging/sort/filter). Útil para preencher defaults em testes.</summary>
    public static GridState<TItem> Empty { get; } =
        new(0, int.MaxValue, null, Array.Empty<SortDescriptor>(),
            Array.Empty<FilterDescriptor>(), Array.Empty<string>());
}

/// <summary>
/// Resposta do callback <c>LoadData</c>. <c>Items</c> contém apenas a janela
/// retornada (já paginada/ordenada/filtrada). <c>TotalCount</c> é o total
/// pós-filtro (para o paginador exibir "X de Y"). <c>Groups</c> e
/// <c>Aggregates</c> são opcionais — quando o consumidor já tem os valores
/// pré-calculados, evita duplo cálculo client-side.
/// </summary>
public sealed record GridLoadResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    IReadOnlyList<GroupResult<TItem>>? Groups = null,
    IReadOnlyDictionary<string, object?>? Aggregates = null);
