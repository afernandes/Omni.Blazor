using System.Text.Json.Serialization;

namespace Forneria.Demo.Pages.Pages.ReportDesigner;

/// <summary>Tipos de banda em um relatório. Inspirado em FastReport / Stimulsoft.</summary>
public enum BandKind
{
    /// <summary>Cabeçalho do relatório — uma vez no topo.</summary>
    ReportHeader,
    /// <summary>Cabeçalho de página — repete em cada página.</summary>
    PageHeader,
    /// <summary>Cabeçalho do grupo (acima dos dados).</summary>
    GroupHeader,
    /// <summary>Faixa de detalhe — repete por linha do dataset.</summary>
    Detail,
    /// <summary>Rodapé do grupo.</summary>
    GroupFooter,
    /// <summary>Rodapé de página — repete em cada página.</summary>
    PageFooter,
    /// <summary>Rodapé do relatório — uma vez no fim.</summary>
    ReportFooter,
}

/// <summary>Tipos de elemento que podem ser inseridos numa banda.</summary>
public enum ElementKind
{
    /// <summary>Texto fixo / título.</summary>
    Label,
    /// <summary>Campo de dado (placeholder para data binding).</summary>
    TextBox,
    /// <summary>Linha horizontal ou vertical.</summary>
    Line,
    /// <summary>Retângulo (bordas + fundo).</summary>
    Rectangle,
    /// <summary>Imagem (URL ou base64).</summary>
    Image,
    /// <summary>Tabela simples (rows × cols).</summary>
    Table,
    /// <summary>Código de barras (placeholder texto).</summary>
    Barcode,
}

/// <summary>Alinhamento horizontal de texto.</summary>
public enum TextAlign { Left, Center, Right, Justify }

/// <summary>Definição completa de um relatório — serializável para JSON.
/// Tudo é mutável (classes, não records) porque o designer precisa editar
/// in-place sem alocar cópias a cada keystroke.</summary>
public sealed class ReportDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Novo relatório";
    public string Description { get; set; } = "";
    public PageSettings Page { get; set; } = new();
    public List<ReportBand> Bands { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Configurações da página (tamanho, margens, orientação).</summary>
public sealed class PageSettings
{
    public string PageSize { get; set; } = "A4";   // A4, Letter, Legal, custom
    public bool Landscape { get; set; }
    public int MarginTop { get; set; } = 40;
    public int MarginRight { get; set; } = 40;
    public int MarginBottom { get; set; } = 40;
    public int MarginLeft { get; set; } = 40;

    /// <summary>Largura útil em pixels (1 unit ≈ 1px no designer, 96dpi).</summary>
    public int Width =>
        Landscape ? PageSizeHeight(PageSize) - MarginLeft - MarginRight
                  : PageSizeWidth(PageSize) - MarginLeft - MarginRight;

    private static int PageSizeWidth(string size) => size switch
    {
        "A4"     => 794,   // 210mm @ 96dpi
        "Letter" => 816,   // 8.5in
        "Legal"  => 816,
        _        => 794,
    };
    private static int PageSizeHeight(string size) => size switch
    {
        "A4"     => 1123,  // 297mm @ 96dpi
        "Letter" => 1056,  // 11in
        "Legal"  => 1344,  // 14in
        _        => 1123,
    };
}

/// <summary>Uma banda do relatório — contém elementos posicionados.</summary>
public sealed class ReportBand
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public BandKind Kind { get; set; }
    public string Title { get; set; } = "";
    public int Height { get; set; } = 80;
    public string BackgroundColor { get; set; } = "transparent";
    public List<ReportElement> Elements { get; set; } = new();
}

/// <summary>Um elemento posicionado livremente dentro de uma banda. Todas as
/// propriedades são polimórficas (Text para Label, Width/Height para
/// retângulo, Src para Image, etc.). Mantemos tudo num único tipo para
/// simplificar serialização e edição inline; o <see cref="Kind"/> diz qual
/// subset interpretar.</summary>
public sealed class ReportElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ElementKind Kind { get; set; }

    // Posição/tamanho (em pixels relativos à banda)
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 120;
    public int Height { get; set; } = 24;

    // Conteúdo
    public string Text { get; set; } = "";
    public string DataField { get; set; } = "";     // para TextBox com binding
    public string Src { get; set; } = "";            // para Image

    // Tipografia
    public int FontSize { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public string Color { get; set; } = "#1f2937";

    // Container styling
    public string BackgroundColor { get; set; } = "transparent";
    public string BorderColor { get; set; } = "transparent";
    public int BorderWidth { get; set; }
    public int BorderRadius { get; set; }

    // Tabela (Kind=Table)
    public int Rows { get; set; } = 3;
    public int Columns { get; set; } = 3;

    /// <summary>Display label compacto usado na árvore de outline.</summary>
    [JsonIgnore]
    public string DisplayLabel => Kind switch
    {
        ElementKind.Label     => string.IsNullOrEmpty(Text) ? "Label" : $"Label · {Truncate(Text, 24)}",
        ElementKind.TextBox   => string.IsNullOrEmpty(DataField) ? "TextBox" : $"[{DataField}]",
        ElementKind.Line      => "Line",
        ElementKind.Rectangle => "Rectangle",
        ElementKind.Image     => string.IsNullOrEmpty(Src) ? "Image" : $"Image · {Truncate(Src, 24)}",
        ElementKind.Table     => $"Table {Rows}×{Columns}",
        ElementKind.Barcode   => string.IsNullOrEmpty(Text) ? "Barcode" : $"Barcode · {Truncate(Text, 20)}",
        _                     => Kind.ToString(),
    };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
