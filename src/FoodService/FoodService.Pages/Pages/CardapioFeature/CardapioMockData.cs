namespace FoodService.Pages.Pages.CardapioFeature;

/// <summary>Dados mock estáticos do cardápio (sabores, bordas, bebidas, categorias).
/// Adaptado de pizza-data.jsx do bundle Anthropic Design.</summary>
public static class CardapioMockData
{
    public static readonly CdPizzaSize[] Sizes =
    {
        new("g", "Pizza Grande", "40cm · 8 fatias", 2, 5900),
        new("b", "Pizza Broto",  "25cm · 4 fatias", 2, 3900),
    };

    public static readonly CdFlavor[] Flavors =
    {
        new("cal", "Calabresa",          "Tradicionais", "Calabresa fatiada, muçarela, cebola, orégano", "",    new(){ ["g"]=5900, ["b"]=3900 }),
        new("mar", "Margherita",         "Tradicionais", "Molho, muçarela, tomate fresco, manjericão",   "veg", new(){ ["g"]=6400, ["b"]=4400 }),
        new("4q",  "4 Queijos",          "Especiais",    "Muçarela, gorgonzola, parmesão, catupiry",     "veg", new(){ ["g"]=7400, ["b"]=5200 }),
        new("fc",  "Frango c/ Catupiry", "Tradicionais", "Frango desfiado, catupiry, orégano",           "",    new(){ ["g"]=6900, ["b"]=4800 }),
        new("por", "Portuguesa",         "Tradicionais", "Presunto, ovos, cebola, azeitona, muçarela",   "",    new(){ ["g"]=6900, ["b"]=4800 }),
        new("pep", "Pepperoni",          "Especiais",    "Pepperoni curado, muçarela, orégano",          "",    new(){ ["g"]=7400, ["b"]=5200 }),
        new("nap", "Napolitana",         "Tradicionais", "Tomate, muçarela, alho, manjericão, azeite",   "veg", new(){ ["g"]=6400, ["b"]=4400 }),
        new("atu", "Atum",               "Especiais",    "Atum, cebola, azeitona, muçarela",             "",    new(){ ["g"]=6400, ["b"]=4400 }),
        new("3qs", "3 Queijos Scala",    "Especiais",    "Muçarela, gorgonzola, parmesão gratinado",     "veg", new(){ ["g"]=7900, ["b"]=5500 }),
        new("bcr", "Bacon Crocante",     "Especiais",    "Bacon, cheddar, muçarela, orégano",            "",    new(){ ["g"]=6900, ["b"]=4800 }),
        new("fch", "Frango c/ Cheddar",  "Especiais",    "Frango, cheddar, cebola caramelizada",         "",    new(){ ["g"]=7200, ["b"]=5000 }),
        new("brb", "Brócolis c/ Bacon",  "Especiais",    "Brócolis, bacon, muçarela, alho",              "",    new(){ ["g"]=6700, ["b"]=4700 }),
    };

    public static readonly CdExtra[] Extras =
    {
        new("cat", "Catupiry",            400),
        new("che", "Cheddar extra",       400),
        new("bac", "Bacon crocante",      500),
        new("muc", "Muçarela extra",      300),
        new("ceb", "Cebola caramelizada", 400),
        new("ore", "Orégano extra",         0),
    };

    public static readonly CdBorda[] Bordas =
    {
        new("no", "Sem borda",                  0),
        new("fi", "Fina",                       0),
        new("tr", "Tradicional",                0),
        new("ca", "Recheada com Catupiry",   1000),
        new("ch", "Recheada com Cheddar",    1000),
        new("cc", "Recheada com Cream Cheese", 1200),
    };

    public static readonly CdDrink[] Drinks =
    {
        new("d1", "Coca-Cola 2L",                  "Gelada, ideal para a pizza grande.", 1400),
        new("d2", "Suco Natural de Laranja 500ml", "Espremido na hora, sem açúcar.",     1250),
        new("d3", "Heineken Long Neck 330ml",      "Long neck gelado.",                  1190),
        new("d4", "Água Mineral c/ Gás 500ml",     "Crystal com gás.",                   700),
    };

    public static readonly CdCategory[] Categories =
    {
        new("destaques",  "Destaques",       "🔥"),
        new("salgadas",   "Pizzas Salgadas", "🍕"),
        new("doces",      "Pizzas Doces",    "🍫"),
        new("veganas",    "Veganas",         "🌱"),
        new("bebidas",    "Bebidas",         "🥤"),
        new("adicionais", "Adicionais",      "🍟"),
    };

    /// <summary>Filtros disponíveis no Flavor picker.</summary>
    public static readonly string[] FlavorFilters = { "Todos", "Tradicionais", "Especiais", "Veganos" };
}


