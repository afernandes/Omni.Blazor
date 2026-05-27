namespace Forneria.Demo.Pages.Pages.PdvFeature;

/// <summary>Modo de pedido no PDV. Define como o pedido será entregue/consumido.</summary>
public enum OrderMode
{
    Balcao,
    Delivery,
    Retirada,
    Mesa,
    Comanda,
}

/// <summary>Produto vendável no PDV (item do catálogo).</summary>
public record Product(
    string Id,
    string Name,
    string Category,
    decimal Price,
    string? Tag = null,
    string? Thumb = null);

/// <summary>Cliente cadastrado (mock simples).</summary>
public record Customer(
    string Id,
    string Name,
    string Phone,
    int Points = 0,
    int Orders = 0);

/// <summary>Metade de uma pizza meio-a-meio. <see cref="Name"/> é o sabor
/// (ex.: "Calabresa"), <see cref="Ingredients"/> é a lista descritiva
/// exibida no carrinho.</summary>
public record PizzaHalf(string Name, string Ingredients);

/// <summary>Item no carrinho. <see cref="Id"/> é UUID; <see cref="ProductId"/>
/// aponta para o catálogo. <see cref="Halves"/> opcional descreve uma pizza
/// meio-a-meio (ou inteira de um sabor só, se length=1) — apenas para visual
/// extra; o preço continua em <see cref="UnitPrice"/>.</summary>
public record CartItem(
    string Id,
    string ProductId,
    string Name,
    decimal UnitPrice,
    int Qty,
    string? Notes = null,
    IReadOnlyList<PizzaHalf>? Halves = null);

/// <summary>Bairro de entrega (mock — em produção viria do CEP/zona).</summary>
public record DeliveryHood(string Name, decimal Fee, int EtaMin);

/// <summary>Detalhes contextuais por modo. Apenas os campos do modo ativo
/// são preenchidos; trocar de modo via <c>PdvOrderService.SetMode</c>
/// reseta todos os campos.</summary>
public class ModeDetails
{
    // Delivery
    public DeliveryHood? Hood { get; set; }
    public string? ZipCode { get; set; }          // CEP "00000-000"
    public string? Address { get; set; }          // Rua + bairro descritivo
    public string? AddressNumber { get; set; }    // Nº separado (input curto)
    public string? Complement { get; set; }       // Apto/casa/referência

    // Mesa
    public int? TableNumber { get; set; }
    public string? Waiter { get; set; }

    // Comanda
    public int? TabNumber { get; set; }

    // Retirada
    public string? PickupName { get; set; }
}
