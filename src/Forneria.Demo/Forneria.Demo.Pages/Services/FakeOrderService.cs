using Omni.Blazor.Models;

namespace Forneria.Demo.Pages.Services;

public sealed record Order(
    int Numero,
    string Cliente,
    string Canal,
    string Status,
    decimal Total,
    int Itens,
    DateTime QuandoUtc);

/// <summary>
/// Backend in-memory simulando server-side. Mantém 10.000 pedidos gerados deterministicamente
/// (seed fixo) e responde a <see cref="GridState{Order}"/> aplicando search/filter/sort/skip/top
/// com <c>Task.Delay</c> pra deixar o loading state visível.
/// </summary>
public sealed class FakeOrderService
{
    private static readonly string[] _clientes =
    {
        "Marina C.", "Lucas P.", "Fernanda S.", "Anônimo · QR", "Rafael S.", "Beatriz M.",
        "Eduardo H.", "Camila A.", "Vinícius L.", "Patrícia O.", "Renato F.", "Júlia T.",
        "Bruno K.", "Larissa N.", "Daniel R.", "Thiago D.", "Aline P.", "Felipe X."
    };
    private static readonly string[] _canais = { "Balcão", "iFood", "QR", "Garçom", "WhatsApp", "Telefone" };
    private static readonly string[] _statuses = { "Pendente", "Em preparo", "Pronto", "Entregue", "Cancelado" };

    private readonly List<Order> _data;

    public FakeOrderService()
    {
        var rng = new Random(42);
        var baseTime = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc);
        _data = Enumerable.Range(1, 10_000)
            .Select(i => new Order(
                Numero: 100000 + i,
                Cliente: _clientes[rng.Next(_clientes.Length)],
                Canal: _canais[rng.Next(_canais.Length)],
                Status: _statuses[rng.Next(_statuses.Length)],
                Total: Math.Round((decimal)(rng.NextDouble() * 280 + 18), 2),
                Itens: rng.Next(1, 9),
                QuandoUtc: baseTime.AddMinutes(-rng.Next(0, 60 * 24 * 90))
            ))
            .ToList();
    }

    public int TotalCount => _data.Count;

    /// <summary>Acesso direto a todos os 10.000 itens — pra demos de virtualize in-memory.</summary>
    public IReadOnlyList<Order> AllOrders => _data;

    public Order ById(int numero) => _data.First(o => o.Numero == numero);

    public Order ItemAtRandom(Random rng) => _data[rng.Next(_data.Count)];

    public IReadOnlyList<string> ItemsOf(Order o)
    {
        var rng = new Random(o.Numero);
        return Enumerable.Range(1, o.Itens).Select(i =>
            new
            {
                Nome = new[] { "Pizza Margherita", "Pizza Calabresa", "Coca-Cola 600ml", "Suco natural",
                               "Pizza Quatro Queijos", "Pão de alho", "Pizza Portuguesa", "Brigadeiro" }[rng.Next(8)],
                Qtd = rng.Next(1, 4),
                Preco = Math.Round((decimal)(rng.NextDouble() * 50 + 12), 2)
            })
            .Select(x => $"{x.Qtd}× {x.Nome} — {x.Preco:C2}".Replace("R$", "R$ "))
            .ToList();
    }

    public async Task<GridLoadResult<Order>> SearchAsync(GridState<Order> state, CancellationToken ct = default)
    {
        // Latência simulada — deixa o loading state visível
        await Task.Delay(220, ct);

        IEnumerable<Order> src = _data;

        // Search global
        if (!string.IsNullOrWhiteSpace(state.Search))
        {
            var q = state.Search.Trim();
            src = src.Where(o =>
                o.Cliente.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Canal.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Status.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Numero.ToString().Contains(q));
        }

        // Filtros per-column
        foreach (var f in state.Filters)
        {
            src = ApplyFilter(src, f);
        }

        // Sort multi-column
        if (state.Sort.Count > 0)
        {
            IOrderedEnumerable<Order>? ordered = null;
            for (int i = 0; i < state.Sort.Count; i++)
            {
                var s = state.Sort[i];
                Func<Order, object?> key = MakeKeySelector(s.Property);
                if (i == 0)
                    ordered = s.Direction == SortDirection.Ascending ? src.OrderBy(key) : src.OrderByDescending(key);
                else
                    ordered = s.Direction == SortDirection.Ascending ? ordered!.ThenBy(key) : ordered!.ThenByDescending(key);
            }
            src = ordered!;
        }

        var materialized = src.ToList();
        var total = materialized.Count;

        // Agregações (sobre o conjunto pós-filtro, antes do paging)
        var aggregates = new Dictionary<string, object?>
        {
            ["Total"]  = materialized.Sum(o => o.Total),
            ["Itens"]  = materialized.Sum(o => o.Itens),
            ["Numero"] = (object)materialized.Count
        };

        var page = materialized.Skip(state.Skip).Take(state.Top).ToList();

        return new GridLoadResult<Order>(page, total, Groups: null, Aggregates: aggregates);
    }

    private static Func<Order, object?> MakeKeySelector(string property) => property switch
    {
        "Numero"    => o => o.Numero,
        "Cliente"   => o => o.Cliente,
        "Canal"     => o => o.Canal,
        "Status"    => o => o.Status,
        "Total"     => o => o.Total,
        "Itens"     => o => o.Itens,
        "QuandoUtc" => o => o.QuandoUtc,
        _ => o => o.Numero
    };

    private static IEnumerable<Order> ApplyFilter(IEnumerable<Order> src, FilterDescriptor f)
    {
        string txt = f.Value?.ToString() ?? string.Empty;
        return f.Property switch
        {
            "Cliente" => src.Where(o => o.Cliente.Contains(txt, StringComparison.OrdinalIgnoreCase)),
            "Canal"   => src.Where(o => o.Canal.Contains(txt, StringComparison.OrdinalIgnoreCase)),
            "Status"  => src.Where(o => o.Status.Contains(txt, StringComparison.OrdinalIgnoreCase)),
            "Numero"  => src.Where(o => o.Numero.ToString().Contains(txt)),
            _ => src
        };
    }
}
