using System.Globalization;

namespace FoodService.Pages.Pages.CardapioFeature;

/// <summary>State container do Cardápio Digital — single source of truth para
/// a state machine de 5 telas (Home → Sizes → Flavors → Preview → Cart), seleção
/// corrente da pizza em montagem, carrinho, cupom e modo de entrega.
///
/// Registrado como Scoped (Program.cs), compartilhado por todas as 5 screens +
/// page raiz. Notifica via OnChange (padrão idêntico ao PdvOrderService).</summary>
public sealed class CardapioDigitalState
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // ─── Navegação (state machine) ─────────────────────────────────────────
    public CardapioScreen Screen { get; private set; } = CardapioScreen.Home;

    // ─── Pizza em montagem ─────────────────────────────────────────────────
    public CdPizzaSize? SelectedSize { get; private set; }
    public List<string> SelectedFlavorIds { get; } = new();
    public CdBorda? SelectedBorda { get; private set; }
    public string Observation { get; set; } = "";
    public int PizzaQty { get; private set; } = 1;

    /// <summary>Custom per-flavor (extras + remove ingredients + note) editado via
    /// bottom sheet. Chaveado por flavor.Id.</summary>
    public Dictionary<string, CdFlavorCustom> FlavorCustom { get; } = new();

    // ─── Catálogo (browse) ─────────────────────────────────────────────────
    public string ActiveCategory { get; set; } = "destaques";
    public string FlavorFilter { get; set; } = "Todos";
    public string FlavorQuery { get; set; } = "";

    // ─── Carrinho ──────────────────────────────────────────────────────────
    public List<CdCartItem> Cart { get; } = new();
    public string Coupon { get; set; } = "";
    public CdDeliveryMode Mode { get; set; } = CdDeliveryMode.Delivery;
    public string Mesa { get; set; } = "07";

    // ─── Checkout / Tracking ───────────────────────────────────────────────
    public CdPayMethod PayMethod { get; set; } = CdPayMethod.Pix;
    public string Troco { get; set; } = "";
    public string OrderNumber { get; private set; } = "";

    // ─── Eventos ───────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ─── Formatação ───────────────────────────────────────────────────────
    public static string Brl(int cents) => $"R$ {(cents / 100m).ToString("N2", PtBr)}";

    // ─── Derivados de pizza em montagem ────────────────────────────────────
    public int FlavorPrice(CdFlavor f) =>
        SelectedSize is null ? 0 : f.PriceBySize.GetValueOrDefault(SelectedSize.Id, 0);

    /// <summary>Strategy: o sabor mais caro define o preço da pizza meio-a-meio.</summary>
    public int CombinedFlavorsPrice
    {
        get
        {
            if (SelectedSize is null || SelectedFlavorIds.Count == 0) return 0;
            return SelectedFlavorIds
                .Select(id => CardapioMockData.Flavors.First(f => f.Id == id))
                .Max(f => f.PriceBySize.GetValueOrDefault(SelectedSize.Id, 0));
        }
    }

    /// <summary>Sabor mais caro entre os selecionados (usado para hint visual).</summary>
    public CdFlavor? PriorityFlavor
    {
        get
        {
            if (SelectedSize is null || SelectedFlavorIds.Count == 0) return null;
            return SelectedFlavorIds
                .Select(id => CardapioMockData.Flavors.First(f => f.Id == id))
                .OrderByDescending(f => f.PriceBySize.GetValueOrDefault(SelectedSize.Id, 0))
                .First();
        }
    }

    /// <summary>Soma dos extras aplicados via bottom sheet across all selected flavors.</summary>
    public int ExtrasCents =>
        FlavorCustom.Values.Sum(c => c.ExtraIds.Sum(exId =>
            CardapioMockData.Extras.FirstOrDefault(e => e.Id == exId)?.Cents ?? 0));

    public int PizzaTotalCents =>
        CombinedFlavorsPrice + (SelectedBorda?.Cents ?? 0) + ExtrasCents;
    public int MaxFlavors => SelectedSize?.MaxFlavors ?? 0;
    public bool IsFlavorSlotsFull => SelectedFlavorIds.Count == MaxFlavors;

    public bool IsFlavorVisible(CdFlavor f)
    {
        if (FlavorFilter != "Todos"
            && f.Group != FlavorFilter
            && !(FlavorFilter == "Veganos" && f.Tag == "veg"))
            return false;
        if (!string.IsNullOrEmpty(FlavorQuery)
            && !(f.Name + " " + f.Ingredients).Contains(FlavorQuery, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    // ─── Derivados do carrinho ─────────────────────────────────────────────
    public int CartSubtotal => Cart.Sum(i => i.UnitCents * i.Qty);
    public int CartItemCount => Cart.Sum(i => i.Qty);
    public int CartDelivery => Mode == CdDeliveryMode.Delivery ? 800 : 0;
    public int CartDiscount =>
        Coupon == "PIZZA10" ? (int)Math.Round(CartSubtotal * 0.1) : 0;
    public int CartTotal => CartSubtotal + CartDelivery - CartDiscount;

    // ─── Navegação ─────────────────────────────────────────────────────────
    public void GoSizes()
    {
        SelectedFlavorIds.Clear();
        SelectedSize = null;
        Screen = CardapioScreen.Sizes;
        Notify();
    }

    public void GoFlavors(CdPizzaSize size)
    {
        SelectedSize = size;
        SelectedFlavorIds.Clear();
        Screen = CardapioScreen.Flavors;
        Notify();
    }

    public void GoPreview()
    {
        SelectedBorda = null;
        Observation = "";
        PizzaQty = 1;
        FlavorCustom.Clear();
        Screen = CardapioScreen.Preview;
        Notify();
    }

    public void SetPizzaQty(int qty)
    {
        PizzaQty = Math.Max(1, qty);
        Notify();
    }

    public void GoCart()
    {
        Screen = CardapioScreen.Cart;
        Notify();
    }

    public void GoCheckout()
    {
        Screen = CardapioScreen.Checkout;
        Notify();
    }

    public void GoTracking()
    {
        OrderNumber = "#" + Random.Shared.Next(1000, 9999);
        Screen = CardapioScreen.Tracking;
        Notify();
    }

    public void GoHome(bool resetCart = false)
    {
        if (resetCart)
        {
            Cart.Clear();
            Coupon = "";
            Mode = CdDeliveryMode.Delivery;
            PayMethod = CdPayMethod.Pix;
            Troco = "";
        }
        Screen = CardapioScreen.Home;
        Notify();
    }

    public void Back()
    {
        Screen = Screen switch
        {
            CardapioScreen.Sizes    => CardapioScreen.Home,
            CardapioScreen.Flavors  => CardapioScreen.Sizes,
            CardapioScreen.Preview  => CardapioScreen.Flavors,
            CardapioScreen.Cart     => CardapioScreen.Home,
            CardapioScreen.Checkout => CardapioScreen.Cart,
            CardapioScreen.Tracking => CardapioScreen.Home,
            _                       => CardapioScreen.Home,
        };
        Notify();
    }

    // ─── seleção de tamanho/sabor/borda ───────────────────────────────────
    public void SetSize(CdPizzaSize size)
    {
        SelectedSize = size;
        Notify();
    }

    public void ToggleFlavor(CdFlavor f)
    {
        if (SelectedFlavorIds.Contains(f.Id))
            SelectedFlavorIds.Remove(f.Id);
        else if (SelectedSize is not null && SelectedFlavorIds.Count < SelectedSize.MaxFlavors)
            SelectedFlavorIds.Add(f.Id);
        Notify();
    }

    public bool IsFlavorSelected(CdFlavor f) => SelectedFlavorIds.Contains(f.Id);

    public void SetBorda(CdBorda b)
    {
        SelectedBorda = b;
        Notify();
    }

    // ─── Per-flavor customization (extras + remove ingredients + note) ─────
    public CdFlavorCustom GetCustom(string flavorId) =>
        FlavorCustom.TryGetValue(flavorId, out var c)
            ? c
            : new CdFlavorCustom(new HashSet<string>(), new HashSet<string>(), "");

    public void SetCustom(string flavorId, CdFlavorCustom custom)
    {
        if (custom.IsCustomized) FlavorCustom[flavorId] = custom;
        else FlavorCustom.Remove(flavorId);
        Notify();
    }

    // ─── Checkout setters ──────────────────────────────────────────────────
    public void SetPayMethod(CdPayMethod m) { PayMethod = m; Notify(); }
    public void SetTroco(string v)          { Troco = v ?? ""; Notify(); }
    public void SetMesa(string v)           { Mesa = v ?? ""; Notify(); }

    public void SetCategory(string id)
    {
        ActiveCategory = id;
        Notify();
    }

    public void SetFlavorFilter(string filter)
    {
        FlavorFilter = filter;
        Notify();
    }

    public void SetFlavorQuery(string query)
    {
        FlavorQuery = query ?? "";
        Notify();
    }

    public void SetObservation(string obs)
    {
        Observation = obs ?? "";
        Notify();
    }

    // ─── Carrinho ──────────────────────────────────────────────────────────
    public void AddPizzaToCart()
    {
        if (SelectedSize is null || SelectedFlavorIds.Count == 0 || SelectedBorda is null) return;

        var flavorNames = string.Join(" + ",
            SelectedFlavorIds.Select(id => CardapioMockData.Flavors.First(f => f.Id == id).Name));

        var detail = $"{SelectedSize.Name} · {flavorNames}";
        if (SelectedBorda.Cents > 0)
            detail += $" · Borda {SelectedBorda.Name.Replace("Recheada com ", "")}";
        foreach (var (flavorId, custom) in FlavorCustom)
        {
            if (!custom.IsCustomized) continue;
            var flavor = CardapioMockData.Flavors.FirstOrDefault(f => f.Id == flavorId);
            if (flavor is null) continue;
            foreach (var exId in custom.ExtraIds)
            {
                var ex = CardapioMockData.Extras.FirstOrDefault(e => e.Id == exId);
                if (ex is not null) detail += $" · Extra {ex.Name} na {flavor.Name}";
            }
            foreach (var ing in custom.RemovedIngredients)
                detail += $" · sem {ing} ({flavor.Name})";
        }

        Cart.Add(new CdCartItem(
            Key: Guid.NewGuid().ToString("N"),
            Display: $"🍕 {SelectedSize.Name}",
            Detail: detail,
            Obs: string.IsNullOrWhiteSpace(Observation) ? null : Observation,
            UnitCents: PizzaTotalCents,
            Qty: PizzaQty));

        Screen = CardapioScreen.Cart;
        Notify();
    }

    public void AddDrinkToCart(CdDrink d)
    {
        var existing = Cart.FirstOrDefault(c => c.Key == d.Id);
        if (existing is not null)
        {
            var idx = Cart.IndexOf(existing);
            Cart[idx] = existing with { Qty = existing.Qty + 1 };
        }
        else
        {
            Cart.Add(new CdCartItem(
                Key: d.Id,
                Display: $"🥤 {d.Name}",
                Detail: d.Desc,
                Obs: null,
                UnitCents: d.Cents,
                Qty: 1));
        }
        Notify();
    }

    public void SetCartItemQty(CdCartItem item, int newQty)
    {
        var idx = Cart.IndexOf(item);
        if (idx < 0) return;
        Cart[idx] = item with { Qty = Math.Max(1, newQty) };
        Notify();
    }

    public void RemoveCartItem(CdCartItem item)
    {
        Cart.Remove(item);
        Notify();
    }

    public void SetCoupon(string coupon)
    {
        Coupon = (coupon ?? "").Trim().ToUpperInvariant();
        Notify();
    }

    public void SetMode(CdDeliveryMode mode)
    {
        Mode = mode;
        Notify();
    }
}


