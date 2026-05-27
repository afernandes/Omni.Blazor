namespace FoodService.Pages.Pages.PdvFeature;

/// <summary>Modo de pedido no PDV.</summary>
public enum OrderMode { Balcao, Delivery, Retirada, Mesa, Comanda }

/// <summary>Tamanho de pizza.</summary>
public enum PizzaSize { Broto, Grande, Familia }

/// <summary>Tipo de modificador aplicado a um ingrediente de pizza.</summary>
public enum FlavorModKind { Removed, Extra, Note }

/// <summary>Razão de remoção de item (audit trail).</summary>
public enum RemoveReason { Estoque, Preco, Engano, Cliente, Outro }

/// <summary>Resultado do diálogo de remoção (reason + nota opcional).</summary>
public record RemoveResult(RemoveReason Reason, string? Note);

/// <summary>Produto vendável (item do catálogo).</summary>
public record Product(
    string Id,
    string Code,           // código curto pra omnibox (ex.: "201")
    string Name,
    string Category,
    decimal Price,
    string? Tag = null,
    string? Thumb = null);

/// <summary>Sabor de pizza (referenciado pelo código no omnibox: 101, 102...).</summary>
public record PizzaFlavor(
    string Code,
    string Name,
    string Ingredients,
    PizzaSizePrices Prices);

public record PizzaSizePrices(decimal Broto, decimal Grande, decimal Familia)
{
    public decimal For(PizzaSize size) => size switch
    {
        PizzaSize.Broto   => Broto,
        PizzaSize.Grande  => Grande,
        PizzaSize.Familia => Familia,
        _                 => Grande,
    };
}

/// <summary>Opção de borda (recheada/simples).</summary>
public record BordaOption(string Id, string Name, decimal PriceDelta);

/// <summary>Adicional disponível para uma pizza/sabor.</summary>
public record ExtraOption(string Id, string Name, decimal Price);

/// <summary>Modificador aplicado a uma metade (remove ingrediente, adiciona extra, anota).</summary>
public record FlavorMod(FlavorModKind Kind, string Value, decimal PriceDelta = 0m);

/// <summary>Metade da pizza com possíveis modificadores.</summary>
public record PizzaHalf(
    string Code,
    string Name,
    string Ingredients,
    IReadOnlyList<FlavorMod>? Mods = null);

/// <summary>Cliente cadastrado (mock).</summary>
public record Customer(
    string Id,
    string Name,
    string Phone,
    int Points = 0,
    int Orders = 0,
    string? DefaultAddress = null);

/// <summary>Item no carrinho. Regular: <c>Halves = null</c>. Pizza: <c>Halves</c> com 1–2 metades.</summary>
public record CartItem(
    string Id,
    string ProductId,
    string Name,
    decimal UnitPrice,
    int Qty,
    string? Notes = null,
    IReadOnlyList<PizzaHalf>? Halves = null,
    PizzaSize? PizzaSize = null,
    BordaOption? Borda = null,
    Adjustment? Adjustment = null);

/// <summary>Desconto/acréscimo aplicado a um item ou ao pedido inteiro.</summary>
public record Adjustment(
    decimal Value,
    bool IsPercent,
    string? Reason = null)
{
    /// <summary>Calcula o impacto absoluto (negativo = desconto, positivo = acréscimo) sobre uma base.</summary>
    public decimal AppliedTo(decimal baseAmount) =>
        IsPercent
            ? Math.Round(baseAmount * (Value / 100m), 2)
            : Value;
}

/// <summary>Snapshot de um pedido pausado (multi-order).</summary>
public record PausedOrder(
    string Id,
    string Label,
    DateTime PausedAt,
    int ItemCount,
    decimal Total,
    Customer? Customer,
    OrderMode Mode,
    IReadOnlyList<CartItem> Items,
    ModeDetailsSnapshot Mode_Details,
    string Identification,
    string LoyaltyCode,
    bool LoyaltyPointsApplied);

/// <summary>Snapshot imutável de ModeDetails (pra paused orders).</summary>
public record ModeDetailsSnapshot(
    DeliveryHood? Hood,
    string? ZipCode,
    string? Address,
    string? AddressNumber,
    string? Complement,
    int? TableNumber,
    int? Covers,
    string? Waiter,
    int? TabNumber,
    string? TabLocation,
    string? PickupName,
    int? PickupEtaMin);

/// <summary>Bairro de entrega.</summary>
public record DeliveryHood(string Name, decimal Fee, int EtaMin, bool Blocked = false);

/// <summary>Mesa do salão.</summary>
public record MesaInfo(int Number, bool Busy);

/// <summary>Cupom de desconto.</summary>
public record Coupon(string Code, decimal Value, bool IsPercent, DateTime? ExpiresAt = null)
{
    public decimal AppliedTo(decimal subtotal) =>
        IsPercent
            ? Math.Round(subtotal * (Value / 100m), 2)
            : Math.Min(Value, subtotal);
}

/// <summary>Item do "favorites rail" — produto recente/frequente.</summary>
public record FavoriteRecent(string ProductId, string Name, decimal Price, int Times);

/// <summary>Detalhes contextuais do modo selecionado.</summary>
public class ModeDetails
{
    // Delivery
    public DeliveryHood? Hood { get; set; }
    public string? ZipCode { get; set; }
    public string? Address { get; set; }
    public string? AddressNumber { get; set; }
    public string? Complement { get; set; }

    // Mesa
    public int? TableNumber { get; set; }
    public int? Covers { get; set; }
    public string? Waiter { get; set; }

    // Comanda
    public int? TabNumber { get; set; }
    public string? TabLocation { get; set; }

    // Retirada
    public string? PickupName { get; set; }
    public int? PickupEtaMin { get; set; }

    public ModeDetailsSnapshot Snapshot() => new(
        Hood, ZipCode, Address, AddressNumber, Complement,
        TableNumber, Covers, Waiter,
        TabNumber, TabLocation,
        PickupName, PickupEtaMin);

    public void Restore(ModeDetailsSnapshot s)
    {
        Hood = s.Hood;
        ZipCode = s.ZipCode;
        Address = s.Address;
        AddressNumber = s.AddressNumber;
        Complement = s.Complement;
        TableNumber = s.TableNumber;
        Covers = s.Covers;
        Waiter = s.Waiter;
        TabNumber = s.TabNumber;
        TabLocation = s.TabLocation;
        PickupName = s.PickupName;
        PickupEtaMin = s.PickupEtaMin;
    }
}
