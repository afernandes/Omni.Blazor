namespace Forneria.Demo.Pages.Pages.PdvFeature;

/// <summary>Single source of truth do PDV. Mantém o carrinho, cliente, modo
/// e detalhes contextuais. Dispara <see cref="OnChange"/> a cada mutação;
/// sub-componentes assinam (`Order.OnChange += StateHasChanged`) e disabilitam
/// no <c>Dispose</c> — convenção <c>@implements IDisposable</c> que protege
/// contra memory leaks (mesmo padrão de <c>ThemeService</c>, <c>BreakpointService</c>).
///
/// Registrar em <c>Program.cs</c> de cada host:
///   <c>builder.Services.AddScoped&lt;PdvOrderService&gt;();</c>
///
/// Scoped (não Singleton) porque o estado é por-circuito em Blazor Server:
/// cada conexão tem seu carrinho separado, e ao desconectar o estado vai
/// embora junto com o scope — zero fuga global.</summary>
public class PdvOrderService
{
    /// <summary>Disparado a cada mutação. Assinantes devem desinscrever no Dispose.</summary>
    public event Action? OnChange;

    public List<CartItem> Cart { get; } = new();
    public Customer? Customer { get; private set; }
    public OrderMode Mode { get; private set; } = OrderMode.Balcao;
    public ModeDetails ModeDetails { get; } = new();

    /// <summary>Identificação opcional do pedido (ex.: "João, senha 42"). Não vincula
    /// cliente cadastrado — apenas anotação para chamar no balcão.</summary>
    public string Identification { get; set; } = "";

    /// <summary>Código de cupom/voucher digitado. Cupons válidos descontam direto no total.</summary>
    public string LoyaltyCode { get; set; } = "";

    // ─── Derived totals ────────────────────────────────────────────
    public decimal Subtotal => Cart.Sum(i => i.UnitPrice * i.Qty);
    public decimal DeliveryFee => Mode == OrderMode.Delivery ? (ModeDetails.Hood?.Fee ?? 0m) : 0m;
    public decimal ServiceFee => Mode == OrderMode.Mesa ? Math.Round(Subtotal * 0.10m, 2) : 0m;
    public decimal Discount => LoyaltyDiscountAmount;
    public decimal Total => Math.Max(0, Subtotal + DeliveryFee + ServiceFee - Discount);
    public int ItemCount => Cart.Sum(i => i.Qty);

    /// <summary>Desconto aplicável dado o código de cupom atual. Tabela mock:
    /// "BEM-VINDO15" = R$ 15 off; "PIZZA10" = 10% off; demais = R$ 0.</summary>
    public decimal LoyaltyDiscountAmount => LoyaltyCode?.Trim().ToUpperInvariant() switch
    {
        "BEM-VINDO15" => Math.Min(15m, Subtotal),
        "PIZZA10"     => Math.Round(Subtotal * 0.10m, 2),
        _             => 0m,
    };

    /// <summary>Máximo desconto possível pelos pontos de fidelidade do cliente
    /// (1 ponto = R$ 0,01, padrão da indústria). Usado só para display
    /// "até -R$ X,XX" no card de fidelidade.</summary>
    public decimal LoyaltyMaxFromPoints =>
        Customer is null ? 0m : Math.Round(Customer.Points / 100m, 2);

    // ─── Cart mutations ────────────────────────────────────────────
    /// <summary>Adiciona produto ao carrinho. Se o mesmo produto já está
    /// presente e sem notes/customização, incrementa a quantidade existente
    /// em vez de duplicar a linha (stack).</summary>
    public void AddItem(Product p, int qty = 1)
    {
        var existing = Cart.FirstOrDefault(i => i.ProductId == p.Id && i.Notes is null);
        if (existing is not null)
        {
            var idx = Cart.IndexOf(existing);
            Cart[idx] = existing with { Qty = existing.Qty + qty };
        }
        else
        {
            Cart.Add(new CartItem(
                Id: Guid.NewGuid().ToString("N"),
                ProductId: p.Id,
                Name: p.Name,
                UnitPrice: p.Price,
                Qty: qty));
        }
        Notify();
    }

    /// <summary>Atualiza a quantidade de um item. Quantidade mínima é 1 —
    /// para remover, use <see cref="Remove"/>.</summary>
    public void UpdateQty(string itemId, int newQty)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        var idx = Cart.IndexOf(item);
        Cart[idx] = item with { Qty = Math.Max(1, newQty) };
        Notify();
    }

    public void Remove(string itemId)
    {
        Cart.RemoveAll(i => i.Id == itemId);
        Notify();
    }

    public void Clear()
    {
        Cart.Clear();
        Customer = null;
        ResetModeDetails();
        Mode = OrderMode.Balcao;
        Notify();
    }

    // ─── Customer ──────────────────────────────────────────────────
    public void SetCustomer(Customer? c) { Customer = c; Notify(); }

    // ─── Mode ──────────────────────────────────────────────────────
    /// <summary>Troca o modo de pedido. Reseta os detalhes contextuais
    /// para evitar estado inconsistente (ex.: bairro de delivery sobrando
    /// quando o usuário trocou para Mesa).</summary>
    public void SetMode(OrderMode m)
    {
        Mode = m;
        ResetModeDetails();
        Notify();
    }

    /// <summary>Sinaliza que <see cref="ModeDetails"/> foi mutado por um
    /// componente externo (ex.: usuário escolheu bairro no dropdown).
    /// O serviço não pode interceptar setters individuais dos campos da
    /// classe; quem mutar deve chamar este método.</summary>
    public void NotifyModeDetailsChanged() => Notify();

    private void ResetModeDetails()
    {
        ModeDetails.Hood = null;
        ModeDetails.ZipCode = null;
        ModeDetails.Address = null;
        ModeDetails.AddressNumber = null;
        ModeDetails.Complement = null;
        ModeDetails.TableNumber = null;
        ModeDetails.Waiter = null;
        ModeDetails.TabNumber = null;
        ModeDetails.PickupName = null;
    }

    /// <summary>Adiciona um item composto (ex.: pizza meio-a-meio) com sabores
    /// e preço explícitos. Não tenta empilhar — itens compostos sempre criam
    /// uma nova linha porque a customização os torna únicos.</summary>
    public void AddComposite(string productId, string name, decimal unitPrice,
                              IReadOnlyList<PizzaHalf> halves, int qty = 1)
    {
        Cart.Add(new CartItem(
            Id: Guid.NewGuid().ToString("N"),
            ProductId: productId,
            Name: name,
            UnitPrice: unitPrice,
            Qty: qty,
            Halves: halves));
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}
