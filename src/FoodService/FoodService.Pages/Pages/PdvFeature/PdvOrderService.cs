namespace FoodService.Pages.Pages.PdvFeature;

/// <summary>Single source of truth do PDV FoodService. Mantém o pedido atual,
/// lista de pedidos pausados, cliente, modo, detalhes contextuais, modificadores
/// e cupons. Dispara <see cref="OnChange"/> em cada mutação.</summary>
public class PdvOrderService
{
    public event Action? OnChange;

    // ─── Current order ─────────────────────────────────────────────
    public List<CartItem> Cart { get; private set; } = new();
    public Customer? Customer { get; private set; }
    public OrderMode Mode { get; private set; } = OrderMode.Balcao;
    public ModeDetails ModeDetails { get; private set; } = new();

    public string Identification { get; set; } = "";
    public string LoyaltyCode { get; set; } = "";
    public bool LoyaltyPointsApplied { get; set; }
    public bool LoyaltyCouponApplied { get; set; }

    /// <summary>Audit log de remoções com reason.</summary>
    public List<(string ItemName, RemoveReason Reason, string? Note, DateTime When)> RemoveLog { get; } = new();

    /// <summary>Ajuste aplicado ao total do pedido inteiro (pode ser % ou fixo).</summary>
    public Adjustment? OrderAdjustment { get; set; }

    /// <summary>Pedidos pausados (multi-order).</summary>
    public List<PausedOrder> Paused { get; } = new();

    /// <summary>Recebe foco no input do omnibox via JS interop. Componente Pdv.razor seta.</summary>
    public Func<Task>? OmniFocus { get; set; }

    // ─── Derived totals ────────────────────────────────────────────
    public int ItemCount => Cart.Sum(i => i.Qty);

    public decimal Subtotal => Cart.Sum(GrossLineTotal);

    public decimal LineDiscounts => Cart
        .Where(i => i.Adjustment is not null)
        .Sum(i => Math.Abs(i.Adjustment!.AppliedTo(i.UnitPrice * i.Qty)));

    public decimal DeliveryFee => Mode == OrderMode.Delivery
        ? (ModeDetails.Hood?.Fee ?? 0m)
        : 0m;

    public decimal ServiceFee => Mode == OrderMode.Mesa
        ? Math.Round(Subtotal * 0.10m, 2)
        : 0m;

    public decimal OrderAdjustmentImpact => OrderAdjustment is null
        ? 0m
        : OrderAdjustment.AppliedTo(Subtotal);

    public decimal CouponDiscount
    {
        get
        {
            if (!LoyaltyCouponApplied) return 0m;
            var coupon = PdvMockData.FindCoupon(LoyaltyCode);
            if (coupon is null) return 0m;
            // FRETE-FREE zeros delivery fee instead of subtotal discount
            if (coupon.Code.Equals("FRETE-FREE", StringComparison.OrdinalIgnoreCase))
                return DeliveryFee;
            return coupon.AppliedTo(Subtotal);
        }
    }

    /// <summary>Maximum potential savings from points + coupon (for display).</summary>
    public decimal LoyaltyMaxSavings
    {
        get
        {
            var pts = LoyaltyMaxFromPoints;
            var couponAmt = 0m;
            var coupon = PdvMockData.FindCoupon(LoyaltyCode);
            if (coupon is not null && !coupon.Code.Equals("FRETE-FREE", StringComparison.OrdinalIgnoreCase))
                couponAmt = coupon.AppliedTo(Subtotal);
            return Math.Min(pts + couponAmt, Subtotal);
        }
    }

    public decimal PointsDiscount
    {
        get
        {
            if (!LoyaltyPointsApplied || Customer is null) return 0m;
            // 1 ponto = R$ 0,01. Aplica até o subtotal disponível.
            var raw = Math.Round(Customer.Points / 100m, 2);
            return Math.Min(raw, Subtotal);
        }
    }

    /// <summary>Discount agregado (cupom + pontos + ajustes).</summary>
    public decimal Discount => CouponDiscount + PointsDiscount;

    public decimal Total => Math.Max(0,
        Subtotal
        + DeliveryFee
        + ServiceFee
        - Discount
        - OrderAdjustmentImpact);

    public decimal LoyaltyMaxFromPoints =>
        Customer is null ? 0m : Math.Round(Customer.Points / 100m, 2);

    private decimal GrossLineTotal(CartItem i)
    {
        var line = i.UnitPrice * i.Qty;
        if (i.Adjustment is not null)
            line -= Math.Abs(i.Adjustment.AppliedTo(line));
        return line;
    }

    // ─── Cart mutations ────────────────────────────────────────────
    public void AddItem(Product p, int qty = 1)
    {
        var existing = Cart.FirstOrDefault(i => i.ProductId == p.Id && i.Notes is null && i.Halves is null);
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

    /// <summary>Adiciona um item de produto pelo código (omnibox).</summary>
    public bool TryAddByCode(string code, int qty = 1)
    {
        var p = PdvMockData.Products.FirstOrDefault(x => x.Code == code);
        if (p is null) return false;
        AddItem(p, qty);
        return true;
    }

    /// <summary>Adiciona uma pizza (meio-a-meio ou inteira) montada via omnibox/UI.</summary>
    public void AddPizza(PizzaSize size, PizzaFlavor flavor1, PizzaFlavor? flavor2 = null, BordaOption? borda = null, int qty = 1)
    {
        // Preço = média dos sabores (regra padrão pizzaria) + borda
        var p1 = flavor1.Prices.For(size);
        var p2 = flavor2?.Prices.For(size) ?? p1;
        var basePrice = Math.Max(p1, p2);  // cobra a metade mais cara
        var unit = basePrice + (borda?.PriceDelta ?? 0m);

        var halves = flavor2 is null
            ? new[] { new PizzaHalf(flavor1.Code, flavor1.Name, flavor1.Ingredients) }
            : new[]
              {
                  new PizzaHalf(flavor1.Code, flavor1.Name, flavor1.Ingredients),
                  new PizzaHalf(flavor2.Code, flavor2.Name, flavor2.Ingredients),
              };

        Cart.Add(new CartItem(
            Id: Guid.NewGuid().ToString("N"),
            ProductId: $"pizza-{size}".ToLowerInvariant(),
            Name: PizzaSizeLabel(size),
            UnitPrice: unit,
            Qty: qty,
            Halves: halves,
            PizzaSize: size,
            Borda: borda));
        Notify();
    }

    public void UpdateQty(string itemId, int newQty)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        var idx = Cart.IndexOf(item);
        Cart[idx] = item with { Qty = Math.Max(1, newQty) };
        Notify();
    }

    public void SetItemNotes(string itemId, string? notes)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        var idx = Cart.IndexOf(item);
        Cart[idx] = item with { Notes = string.IsNullOrWhiteSpace(notes) ? null : notes };
        Notify();
    }

    public void SetItemBorda(string itemId, BordaOption borda)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        var idx = Cart.IndexOf(item);
        // Ajusta preço unitário: remove borda antiga, soma borda nova
        var oldDelta = item.Borda?.PriceDelta ?? 0m;
        var newDelta = borda.Id == "none" ? 0m : borda.PriceDelta;
        Cart[idx] = item with
        {
            Borda = borda.Id == "none" ? null : borda,
            UnitPrice = item.UnitPrice - oldDelta + newDelta,
        };
        Notify();
    }

    public void SetHalfMods(string itemId, int halfIndex, IReadOnlyList<FlavorMod> mods)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null || item.Halves is null || halfIndex < 0 || halfIndex >= item.Halves.Count) return;

        var halves = item.Halves.ToList();
        halves[halfIndex] = halves[halfIndex] with { Mods = mods };
        var idx = Cart.IndexOf(item);

        // Ajusta preço com extras (positive deltas) aplicado uma vez à pizza inteira.
        var extrasDelta = halves
            .SelectMany(h => h.Mods ?? Array.Empty<FlavorMod>())
            .Where(m => m.Kind == FlavorModKind.Extra)
            .Sum(m => m.PriceDelta);

        var basePrice = item.UnitPrice - PreviousExtrasDelta(item);
        Cart[idx] = item with { Halves = halves, UnitPrice = basePrice + extrasDelta };
        Notify();
    }

    private static decimal PreviousExtrasDelta(CartItem i) =>
        (i.Halves ?? Array.Empty<PizzaHalf>())
            .SelectMany(h => h.Mods ?? Array.Empty<FlavorMod>())
            .Where(m => m.Kind == FlavorModKind.Extra)
            .Sum(m => m.PriceDelta);

    public void ApplyItemAdjustment(string itemId, Adjustment? adj)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        var idx = Cart.IndexOf(item);
        Cart[idx] = item with { Adjustment = adj };
        Notify();
    }

    public void ApplyOrderAdjustment(Adjustment? adj)
    {
        OrderAdjustment = adj;
        Notify();
    }

    /// <summary>Remove item registrando a razão no audit log.</summary>
    public void RemoveWithReason(string itemId, RemoveReason reason, string? note = null)
    {
        var item = Cart.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        RemoveLog.Add((item.Name, reason, note, DateTime.UtcNow));
        Cart.Remove(item);
        Notify();
    }

    /// <summary>Remove imediato sem reason (uso interno).</summary>
    public void Remove(string itemId)
    {
        Cart.RemoveAll(i => i.Id == itemId);
        Notify();
    }

    public void Clear()
    {
        Cart.Clear();
        Customer = null;
        OrderAdjustment = null;
        Identification = "";
        LoyaltyCode = "";
        LoyaltyPointsApplied = false;
        ResetModeDetails();
        Mode = OrderMode.Balcao;
        Notify();
    }

    // ─── Customer & Mode ───────────────────────────────────────────
    public void SetCustomer(Customer? c)
    {
        Customer = c;
        // Pré-carrega endereço default se ficou em delivery
        if (Mode == OrderMode.Delivery && c?.DefaultAddress is not null && string.IsNullOrEmpty(ModeDetails.Address))
            ModeDetails.Address = c.DefaultAddress;
        Notify();
    }

    public void SetMode(OrderMode m)
    {
        Mode = m;
        ResetModeDetails();
        // Restaura endereço default do cliente em delivery
        if (m == OrderMode.Delivery && Customer?.DefaultAddress is not null)
            ModeDetails.Address = Customer.DefaultAddress;
        Notify();
    }

    public void NotifyModeDetailsChanged() => Notify();

    private void ResetModeDetails()
    {
        ModeDetails = new ModeDetails();
    }

    // ─── Multi-order (pause / resume) ──────────────────────────────
    public void PauseCurrent()
    {
        if (Cart.Count == 0) return;
        var label = !string.IsNullOrWhiteSpace(Identification)
            ? Identification
            : Customer?.Name ?? $"Pedido {Paused.Count + 1}";
        Paused.Add(new PausedOrder(
            Id: Guid.NewGuid().ToString("N"),
            Label: label,
            PausedAt: DateTime.UtcNow,
            ItemCount: ItemCount,
            Total: Total,
            Customer: Customer,
            Mode: Mode,
            Items: Cart.ToList(),
            Mode_Details: ModeDetails.Snapshot(),
            Identification: Identification,
            LoyaltyCode: LoyaltyCode,
            LoyaltyPointsApplied: LoyaltyPointsApplied));
        ClearCurrent();
        Notify();
    }

    public void Resume(string pausedId)
    {
        var po = Paused.FirstOrDefault(p => p.Id == pausedId);
        if (po is null) return;

        // Move atual para pausados antes (não-destrutivo) só se houver itens.
        if (Cart.Count > 0) PauseCurrent();

        Cart = po.Items.ToList();
        Customer = po.Customer;
        Mode = po.Mode;
        ModeDetails = new ModeDetails();
        ModeDetails.Restore(po.Mode_Details);
        Identification = po.Identification;
        LoyaltyCode = po.LoyaltyCode;
        LoyaltyPointsApplied = po.LoyaltyPointsApplied;
        Paused.Remove(po);
        Notify();
    }

    public void DiscardPaused(string pausedId)
    {
        var po = Paused.FirstOrDefault(p => p.Id == pausedId);
        if (po is null) return;
        Paused.Remove(po);
        Notify();
    }

    private void ClearCurrent()
    {
        Cart.Clear();
        Customer = null;
        OrderAdjustment = null;
        Identification = "";
        LoyaltyCode = "";
        LoyaltyPointsApplied = false;
        ResetModeDetails();
        Mode = OrderMode.Balcao;
    }

    // ─── Finalize ──────────────────────────────────────────────────
    public void FinalizeOrder()
    {
        // Mock: limpa current order. Production: send to backend.
        ClearCurrent();
        Notify();
    }

    private void Notify() => OnChange?.Invoke();

    // ─── Helpers ───────────────────────────────────────────────────
    public static string PizzaSizeLabel(PizzaSize size) => size switch
    {
        PizzaSize.Broto   => "Broto",
        PizzaSize.Grande  => "Grande",
        PizzaSize.Familia => "Família",
        _                 => "Grande",
    };

    public static string ModeLabel(OrderMode m) => m switch
    {
        OrderMode.Balcao   => "Balcão",
        OrderMode.Delivery => "Delivery",
        OrderMode.Retirada => "Retirada",
        OrderMode.Mesa     => "Mesa",
        OrderMode.Comanda  => "Comanda",
        _                  => "Balcão",
    };
}
