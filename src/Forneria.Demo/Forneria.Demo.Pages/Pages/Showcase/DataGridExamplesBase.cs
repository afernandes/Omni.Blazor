using System.Globalization;
using Forneria.Demo.Pages.Services;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Forneria.Demo.Pages.Pages.Showcase;

public abstract class DataGridExamplesBase : ComponentBase
{
    private static readonly FakeOrderService SharedOrders = new();

    protected FakeOrderService Orders => SharedOrders;

    protected static CultureInfo PtBr { get; } = CultureInfo.GetCultureInfo("pt-BR");

    protected List<Order> CreateInMemoryOrders()
    {
        var random = new Random(7);
        return Enumerable.Range(1, 24)
            .Select(_ => Orders.ItemAtRandom(random))
            .ToList();
    }

    protected static BadgeVariant StatusVariant(string status) => status switch
    {
        "Pronto" => BadgeVariant.Good,
        "Em preparo" => BadgeVariant.Warn,
        "Pendente" => BadgeVariant.Info,
        "Entregue" => BadgeVariant.Plain,
        "Cancelado" => BadgeVariant.Danger,
        _ => BadgeVariant.Default,
    };
}
