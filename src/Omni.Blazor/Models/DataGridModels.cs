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
/// callback <c>DataProvider</c> para que o consumidor execute paging/sort/filter
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
    /// <summary>Estado inicial limitado a uma página, sem ordenação ou filtros.</summary>
    public static GridState<TItem> Empty { get; } =
        new(0, 50, null, Array.Empty<SortDescriptor>(),
            Array.Empty<FilterDescriptor>(), Array.Empty<string>());
}

/// <summary>
/// Resposta do callback <c>DataProvider</c>. <c>Items</c> contém apenas a janela
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

/// <summary>Asynchronous, cancellable server-side source for <c>OmniDataGrid</c>.</summary>
public delegate ValueTask<GridLoadResult<TItem>> GridDataProvider<TItem>(
    GridState<TItem> state,
    CancellationToken cancellationToken);

/// <summary>
/// Optional streaming export source. The grid enforces its configured row cap
/// while enumerating the returned sequence.
/// </summary>
public delegate IAsyncEnumerable<TItem> GridExportProvider<TItem>(
    GridState<TItem> state,
    CancellationToken cancellationToken);

/// <summary>
/// Hierarquias de data prontas para <c>OmniDataGridColumn.GroupHierarchy</c>. Existem
/// para o caso comum ficar legível na marcação —
/// <c>GroupHierarchy="@DateGroupHierarchy.YearMonthDay"</c> em vez de um array literal
/// de três elementos. Qualquer outra combinação continua válida: o parâmetro aceita
/// qualquer sequência de <see cref="DateGroupInterval"/>.
/// </summary>
public static class DateGroupHierarchy
{
    // Array.AsReadOnly, e não o array cru: um `IReadOnlyList<T>` que por baixo É um
    // array pode ser convertido de volta e mutado pelo consumidor — e como estes são
    // estáticos, a alteração valeria para todos os grids do processo.
    /// <summary>Ano › Mês › Dia — o desdobramento mais comum de uma coluna de data.</summary>
    public static readonly IReadOnlyList<DateGroupInterval> YearMonthDay =
        Array.AsReadOnly(new[] { DateGroupInterval.Year, DateGroupInterval.Month, DateGroupInterval.Day });

    /// <summary>Ano › Mês.</summary>
    public static readonly IReadOnlyList<DateGroupInterval> YearMonth =
        Array.AsReadOnly(new[] { DateGroupInterval.Year, DateGroupInterval.Month });

    /// <summary>Ano › Trimestre › Mês — o desdobramento que o Excel aplica em tabelas dinâmicas.</summary>
    public static readonly IReadOnlyList<DateGroupInterval> YearQuarterMonth =
        Array.AsReadOnly(new[] { DateGroupInterval.Year, DateGroupInterval.Quarter, DateGroupInterval.Month });

    /// <summary>Ano › Mês › Dia › Hora — para logs e telemetria, onde o dia ainda é grande demais.</summary>
    public static readonly IReadOnlyList<DateGroupInterval> YearMonthDayHour =
        Array.AsReadOnly(new[] { DateGroupInterval.Year, DateGroupInterval.Month, DateGroupInterval.Day, DateGroupInterval.Hour });
}
