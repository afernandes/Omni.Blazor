namespace FoodService.Pages.Pages.CardapioFeature;

/// <summary>State machine das 7 telas do cardápio digital: Home → Sizes →
/// Flavors → Preview → Cart → Checkout → Tracking (volta ao Home).</summary>
public enum CardapioScreen { Home, Sizes, Flavors, Preview, Cart, Checkout, Tracking }

/// <summary>Método de pagamento escolhido no Checkout.</summary>
public enum CdPayMethod { Pix, Card, Cash }

/// <summary>Extra avulso adicionável por sabor (Catupiry, Cheddar, Bacon, etc.).</summary>
public sealed record CdExtra(string Id, string Name, int Cents);

/// <summary>Customização aplicada por sabor (ingredientes removidos + extras
/// adicionados + observação). Aberto via bottom-sheet no Preview.</summary>
public sealed record CdFlavorCustom(
    HashSet<string> RemovedIngredients,
    HashSet<string> ExtraIds,
    string Note)
{
    public bool IsCustomized =>
        RemovedIngredients.Count > 0 || ExtraIds.Count > 0 || !string.IsNullOrWhiteSpace(Note);
}

/// <summary>Tamanho de pizza disponível (Grande/Broto). Prefixo Cd evita
/// colisão de nome com PdvFeature.PizzaSize (que é coisa diferente).</summary>
public sealed record CdPizzaSize(string Id, string Name, string Desc, int MaxFlavors, int FromCents);

/// <summary>Sabor de pizza com preço variável por tamanho.</summary>
public sealed record CdFlavor(
    string Id,
    string Name,
    string Group,
    string Ingredients,
    string Tag,
    Dictionary<string, int> PriceBySize);

/// <summary>Borda opcional da pizza.</summary>
public sealed record CdBorda(string Id, string Name, int Cents);

/// <summary>Bebida do cardápio.</summary>
public sealed record CdDrink(string Id, string Name, string Desc, int Cents);

/// <summary>Categoria de catálogo (sticky chips do home).</summary>
public sealed record CdCategory(string Id, string Name, string Emoji);

/// <summary>Linha do carrinho — pode ser pizza montada ou bebida.</summary>
public sealed record CdCartItem(
    string Key,
    string Display,
    string Detail,
    string? Obs,
    int UnitCents,
    int Qty);

/// <summary>Modo de entrega selecionado no checkout.</summary>
public enum CdDeliveryMode { Delivery, Retirada, Mesa }
