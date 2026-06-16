using System.Text.RegularExpressions;

namespace FoodService.Pages.Pages.PdvFeature;

/// <summary>Comandos reconhecíveis pelo Omnibox.
/// Discriminated union via record + pattern matching no consumidor.
/// (Nomeado OmniBoxCommand para não colidir com Omni.Blazor.Models.OmniBoxCommand.)</summary>
public abstract record OmniBoxCommand
{
    /// <summary>Adiciona N produtos pelo código curto (ex: "201", "2x201").</summary>
    public sealed record AddProduct(string Code, int Qty) : OmniBoxCommand;

    /// <summary>Monta uma pizza meio-a-meio (ou inteira) com tamanho e borda opcionais.
    /// Ex.: "G101/102.C" → AddPizza(Grande, "101", "102", "C")
    ///      "101/102"    → AddPizza(null,   "101", "102", null)
    ///      "G101"       → AddPizza(Grande, "101", null,  null)</summary>
    public sealed record AddPizza(PizzaSize? Size, string Flavor1, string? Flavor2, string? BordaCode) : OmniBoxCommand;

    /// <summary>Busca textual livre (palavras).</summary>
    public sealed record Search(string Query) : OmniBoxCommand;

    /// <summary>Comando inválido / não reconhecido.</summary>
    public sealed record Unknown(string Raw) : OmniBoxCommand;
}

/// <summary>Parser do Omnibox. Sintaxe (case-insensitive):
/// <list type="bullet">
///   <item><c>201</c> — adiciona produto código 201</item>
///   <item><c>2x201</c> — adiciona 2× produto 201</item>
///   <item><c>G101</c> — pizza Grande sabor 101</item>
///   <item><c>G101/102</c> — pizza Grande meio-a-meio 101/102</item>
///   <item><c>G101/102.C</c> — pizza Grande meio-a-meio com borda Catupiry</item>
///   <item><c>F107</c> — pizza Família sabor 107</item>
///   <item><c>B201</c> — pizza Broto sabor 201</item>
///   <item><c>coca</c> — busca textual</item>
/// </list></summary>
public static class OmniBoxParser
{
    // Pizza: opcional tamanho (B|G|F) + sabor1 + opcional /sabor2 + opcional .borda
    private static readonly Regex PizzaRegex = new(
        @"^(?<size>[BGF])?(?<f1>\d{3})(?:/(?<f2>\d{3}))?(?:\.(?<borda>[A-Z]))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Produto: NxCODE ou apenas CODE (3+ dígitos sem barras)
    private static readonly Regex ProductRegex = new(
        @"^(?:(?<qty>\d{1,3})x)?(?<code>\d{3,4})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static OmniBoxCommand Parse(string? input)
    {
        var raw = (input ?? "").Trim();
        if (string.IsNullOrEmpty(raw)) return new OmniBoxCommand.Unknown("");

        // Pizza tem prioridade (tem barra ou prefixo de tamanho ou ponto)
        var pizzaMatch = PizzaRegex.Match(raw);
        if (pizzaMatch.Success && (raw.Contains('/') || raw.Contains('.')
                                   || "BGF".Contains(char.ToUpperInvariant(raw[0]))))
        {
            var size = pizzaMatch.Groups["size"].Success
                ? SizeFromLetter(pizzaMatch.Groups["size"].Value[0])
                : (PizzaSize?)null;
            var f1 = pizzaMatch.Groups["f1"].Value;
            var f2 = pizzaMatch.Groups["f2"].Success ? pizzaMatch.Groups["f2"].Value : null;
            var b  = pizzaMatch.Groups["borda"].Success
                ? pizzaMatch.Groups["borda"].Value.ToUpperInvariant()
                : null;
            return new OmniBoxCommand.AddPizza(size, f1, f2, b);
        }

        var prodMatch = ProductRegex.Match(raw);
        if (prodMatch.Success)
        {
            var qty = prodMatch.Groups["qty"].Success
                ? int.Parse(prodMatch.Groups["qty"].Value)
                : 1;
            return new OmniBoxCommand.AddProduct(prodMatch.Groups["code"].Value, qty);
        }

        return new OmniBoxCommand.Search(raw);
    }

    public static PizzaSize SizeFromLetter(char c) => char.ToUpperInvariant(c) switch
    {
        'B' => PizzaSize.Broto,
        'F' => PizzaSize.Familia,
        _   => PizzaSize.Grande,
    };

    public static char SizeToLetter(PizzaSize s) => s switch
    {
        PizzaSize.Broto   => 'B',
        PizzaSize.Familia => 'F',
        _                 => 'G',
    };

    public static BordaOption? ResolveBorda(string? bordaCode)
    {
        if (string.IsNullOrEmpty(bordaCode)) return null;
        // Letra simples: C=Catupiry, K=Cheddar, X=Chocolate, R=Cream cheese
        var id = bordaCode.ToUpperInvariant() switch
        {
            "C" => "catupiry",
            "K" => "cheddar",
            "X" => "chocolate",
            "R" => "cream",
            _   => null,
        };
        return id is null
            ? null
            : PdvMockData.Bordas.FirstOrDefault(b => b.Id == id);
    }
}
