namespace Forneria.Demo.Pages.Pages.PdvFeature;

/// <summary>Catálogo, clientes e bairros mock para o /pdv.
/// Dados representativos (pizzaria) — sem persistência. Em produção
/// viria de uma API/EF Core.</summary>
public static class PdvMockData
{
    // ─── Categorias ────────────────────────────────────────────────
    public static readonly (string Id, string Label, string Emoji)[] Categories =
    {
        ("todos",      "Todos",      "🔥"),
        ("pizza-g",    "Pizza G",    "🍕"),
        ("pizza-m",    "Pizza M",    "🍕"),
        ("pizza-doce", "Doces",      "🍫"),
        ("burgers",    "Burgers",    "🍔"),
        ("bebidas",    "Bebidas",    "🥤"),
        ("sobremesa",  "Sobremesas", "🍨"),
    };

    // ─── Catálogo ──────────────────────────────────────────────────
    public static readonly Product[] Products =
    {
        // Pizzas Grandes
        new("pg-cal", "Pizza G · Calabresa",       "pizza-g", 56.90m, "G"),
        new("pg-mar", "Pizza G · Margherita",      "pizza-g", 58.90m, "G", "veg"),
        new("pg-por", "Pizza G · Portuguesa",      "pizza-g", 64.90m, "G"),
        new("pg-pep", "Pizza G · Pepperoni",       "pizza-g", 68.90m, "G", "spicy"),
        new("pg-4q",  "Pizza G · Quatro Queijos",  "pizza-g", 72.90m, "G", "veg"),
        new("pg-fc",  "Pizza G · Frango Catupiry", "pizza-g", 62.90m, "G"),
        new("pg-car", "Pizza G · Carbonara",       "pizza-g", 70.90m, "G"),
        new("pg-veg", "Pizza G · Vegetariana",     "pizza-g", 60.90m, "G", "veg"),

        // Pizzas Médias
        new("pm-cal", "Pizza M · Calabresa",       "pizza-m", 42.90m, "M"),
        new("pm-mar", "Pizza M · Margherita",      "pizza-m", 44.90m, "M", "veg"),
        new("pm-pep", "Pizza M · Pepperoni",       "pizza-m", 51.90m, "M", "spicy"),
        new("pm-4q",  "Pizza M · Quatro Queijos",  "pizza-m", 54.90m, "M", "veg"),

        // Pizzas Doces
        new("pd-brig", "Brigadeiro",  "pizza-doce", 48.90m, "DOCE"),
        new("pd-rj",   "Romeu & Julieta", "pizza-doce", 46.90m, "DOCE"),
        new("pd-bn",   "Banana c/ Canela", "pizza-doce", 44.90m, "DOCE"),

        // Burgers
        new("bg-cls",  "Cheeseburger",        "burgers", 28.90m),
        new("bg-dup",  "Cheeseburger Duplo",  "burgers", 38.90m),
        new("bg-veg",  "Veggie Burger",       "burgers", 32.90m, "VEG", "veg"),

        // Bebidas
        new("bb-coca2l", "Coca-Cola 2L",            "bebidas", 14.00m),
        new("bb-coca350","Coca-Cola 350ml",         "bebidas",  7.50m),
        new("bb-hein",   "Heineken Long Neck",      "bebidas", 12.00m),
        new("bb-suco",   "Suco de Laranja 500ml",   "bebidas", 12.50m),
        new("bb-agua",   "Água Mineral 500ml",      "bebidas",  6.00m),

        // Sobremesas
        new("sb-pet", "Petit Gâteau",         "sobremesa", 24.90m),
        new("sb-pud", "Pudim de Leite",       "sobremesa", 18.90m),
    };

    // ─── Clientes ──────────────────────────────────────────────────
    public static readonly Customer[] Customers =
    {
        new("c1", "Marina Toledo",   "(11) 98823-1204", Points: 842, Orders: 18),
        new("c2", "Carlos Mendes",   "(11) 98745-3322", Points: 312, Orders: 7),
        new("c3", "João Silva",      "(11) 99012-4455", Points: 105, Orders: 3),
        new("c4", "Maria Fernandes", "(11) 98231-7788", Points: 567, Orders: 12),
        new("c5", "Pedro Lopes",     "(11) 99345-8821", Points: 0,   Orders: 0),
        new("c6", "Família Lima",    "(11) 98882-3344", Points: 1240, Orders: 31),
        new("c7", "Carla Mendes",    "(11) 99876-1122", Points: 488, Orders: 9),
        new("c8", "Lucas Andrade",   "(11) 98765-4321", Points: 220, Orders: 5),
    };

    // ─── Bairros (delivery) ────────────────────────────────────────
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
    };

    // ─── Garçons (Mesa) ────────────────────────────────────────────
    public static readonly string[] Waiters =
    {
        "Ricardo M.", "Felipe S.", "Ana P.", "Bruno A.", "Letícia O.",
    };
}
