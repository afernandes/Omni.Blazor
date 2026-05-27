namespace FoodService.Pages.Pages.PdvFeature;

/// <summary>Catálogo mock para o PDV FoodService.</summary>
public static class PdvMockData
{
    // ─── Bebidas, lanches, sobremesas ──────────────────────────────────
    public static readonly Product[] Products =
    {
        new("p-coca350",  "201", "Coca-Cola 350ml",       "bebidas",   7.50m, "LATA"),
        new("p-coca600",  "202", "Coca-Cola 600ml",       "bebidas",   9.50m, "GARRAFA"),
        new("p-coca2l",   "203", "Coca-Cola 2L",          "bebidas",  14.00m, "PET"),
        new("p-guarana",  "204", "Guaraná 350ml",         "bebidas",   7.00m, "LATA"),
        new("p-suco",     "205", "Suco de Laranja 500ml", "bebidas",  12.50m),
        new("p-agua",     "206", "Água Mineral 500ml",    "bebidas",   6.00m, "S/GÁS"),
        new("p-hein",     "207", "Heineken Long Neck",    "bebidas",  12.00m, "355ML"),
        new("p-brahma",   "208", "Brahma 600ml",          "bebidas",  11.00m, "GARRAFA"),

        new("p-bg-cls",   "301", "Cheeseburger",          "burgers",  28.90m, "BURGER"),
        new("p-bg-dup",   "302", "Cheeseburger Duplo",    "burgers",  28.00m, "BURGER"),
        new("p-bg-veg",   "303", "Veggie Burger",         "burgers",  32.90m, "VEG"),
        new("p-bg-chick", "304", "Chicken Burger",        "burgers",  31.90m, "BURGER"),
        new("p-fritas-m", "305", "Batata Frita M",        "burgers",  16.00m),
        new("p-fritas-g", "306", "Batata Frita G",        "burgers",  22.00m),

        new("p-pet",      "401", "Petit Gâteau",          "sobremesa",24.90m),
        new("p-pud",      "402", "Pudim de Leite",        "sobremesa",18.90m),
        new("p-mousse",   "403", "Mousse de Chocolate",   "sobremesa",16.90m),
    };

    // ─── Sabores de pizza (código 101+ = salgados, 201+ = doces) ───────
    public static readonly PizzaFlavor[] Flavors =
    {
        new("101", "Calabresa",          "Calabresa fatiada, muçarela, cebola, orégano",
             new(38.90m, 64.00m, 78.90m)),
        new("102", "Quatro Queijos",     "Muçarela, provolone, gorgonzola, parmesão",
             new(46.90m, 68.00m, 98.90m)),
        new("103", "Margherita",         "Muçarela, tomate fresco, manjericão, orégano",
             new(40.90m, 58.90m, 78.90m)),
        new("104", "Portuguesa",         "Presunto, ovo, cebola, pimentão, ervilha, azeitona",
             new(44.90m, 64.90m, 86.90m)),
        new("105", "Pepperoni",          "Pepperoni, muçarela, orégano",
             new(48.90m, 68.90m, 92.90m)),
        new("106", "Frango c/ Catupiry", "Frango desfiado, catupiry, milho",
             new(42.90m, 62.90m, 84.90m)),
        new("107", "Carbonara",          "Bacon, ovo, parmesão, pimenta-do-reino",
             new(50.90m, 70.90m, 94.90m)),
        new("108", "Vegetariana",        "Brócolis, abobrinha, tomate, muçarela",
             new(40.90m, 60.90m, 82.90m)),
        new("201", "Brigadeiro",         "Chocolate ao leite, granulado, leite condensado",
             new(34.90m, 48.90m, 68.90m)),
        new("202", "Romeu & Julieta",    "Goiabada, muçarela, canela",
             new(32.90m, 46.90m, 66.90m)),
        new("203", "Banana c/ Canela",   "Banana, canela, leite condensado",
             new(30.90m, 44.90m, 62.90m)),
    };

    // ─── Bordas ────────────────────────────────────────────────────────
    public static readonly BordaOption[] Bordas =
    {
        new("none",      "Sem borda",     0m),
        new("catupiry",  "Catupiry",      8.00m),
        new("cheddar",   "Cheddar",       8.00m),
        new("chocolate", "Chocolate",    10.00m),
        new("cream",     "Cream cheese",  9.00m),
    };

    // ─── Extras / adicionais ───────────────────────────────────────────
    public static readonly ExtraOption[] Extras =
    {
        new("bacon",    "Bacon",          5.00m),
        new("catupiry", "Catupiry extra", 4.50m),
        new("milho",    "Milho",          3.00m),
        new("queijo",   "Queijo extra",   6.00m),
        new("cebola",   "Cebola",         2.00m),
        new("azeitona", "Azeitona",       2.50m),
        new("ovo",      "Ovo",            3.00m),
        new("oregano",  "Orégano extra",  0m),
    };

    // ─── Clientes ──────────────────────────────────────────────────────
    public static readonly Customer[] Customers =
    {
        new("c1", "Marina Toledo",   "(11) 98823-1204", Points: 842,  Orders: 18, DefaultAddress: "R. das Laranjeiras, 412 — Centro"),
        new("c2", "Carlos Mendes",   "(11) 98745-3322", Points: 312,  Orders:  7, DefaultAddress: "Av. Brigadeiro, 1900 — Jardins"),
        new("c3", "João Silva",      "(11) 99012-4455", Points: 105,  Orders:  3),
        new("c4", "Maria Fernandes", "(11) 98231-7788", Points: 567,  Orders: 12, DefaultAddress: "R. Augusta, 1200 — Consolação"),
        new("c5", "Pedro Lopes",     "(11) 99345-8821", Points:   0,  Orders:  0),
        new("c6", "Família Lima",    "(11) 98882-3344", Points: 1240, Orders: 31, DefaultAddress: "Av. Paulista, 555 — Bela Vista"),
        new("c7", "Carla Mendes",    "(11) 99876-1122", Points: 488,  Orders:  9),
        new("c8", "Lucas Andrade",   "(11) 98765-4321", Points: 220,  Orders:  5),
    };

    // ─── Bairros (delivery) ────────────────────────────────────────────
    public static readonly DeliveryHood[] Hoods =
    {
        new("Centro",          7.00m, 25),
        new("Vila Madalena",   8.00m, 30),
        new("Pinheiros",       8.00m, 30),
        new("Itaim Bibi",      9.50m, 35),
        new("Jardins",         9.50m, 35),
        new("Moema",          10.00m, 40),
        new("Vila Mariana",    9.00m, 35),
        new("Brooklin",       11.00m, 45),
        new("Lapa",           10.00m, 40),
        new("Perdizes",        9.00m, 35),
        new("Tatuapé",        12.00m, 50, Blocked: true),
    };

    // ─── Mesas do salão (40, 5×8 grid, algumas ocupadas) ───────────────
    public static readonly MesaInfo[] Mesas = Enumerable.Range(1, 40)
        .Select(n => new MesaInfo(n, Busy: n is 3 or 7 or 12 or 18 or 22 or 29 or 35))
        .ToArray();

    public static readonly string[] Waiters = { "Ricardo M.", "Felipe S.", "Ana P.", "Bruno A.", "Letícia O." };
    public static readonly string[] TabLocations = { "Salão", "Varanda", "Bar", "Mezanino" };
    public static readonly int[]   PickupEtas    = { 15, 30, 45, 60 };

    // ─── Cupons válidos ────────────────────────────────────────────────
    public static readonly Coupon[] Coupons =
    {
        new("BEM-VINDO15", 15m, IsPercent: false),
        new("PIZZA10",     10m, IsPercent: true),
        new("FRETE-FREE",   0m, IsPercent: false),
    };

    // ─── Favorites (rail topo do PDV) ──────────────────────────────────
    public static readonly FavoriteRecent[] Favorites =
    {
        new("p-coca350",  "Coca-Cola 350ml",     7.50m, 124),
        new("p-bg-dup",   "Cheeseburger Duplo", 38.90m,  87),
        new("p-fritas-m", "Batata Frita M",     16.00m,  76),
        new("p-pet",      "Petit Gâteau",       24.90m,  52),
        new("p-suco",     "Suco de Laranja",    12.50m,  41),
        new("p-hein",     "Heineken",           12.00m,  38),
    };

    public static Coupon? FindCoupon(string code) =>
        Coupons.FirstOrDefault(c => c.Code.Equals(code?.Trim(), StringComparison.OrdinalIgnoreCase));
}
