namespace Omni.Blazor.Models;

/// <summary>Um ponto de dado em uma série do <c>OmniChart</c>.
/// Para Pie/Donut, <c>Category</c> vira o label da fatia.
/// Para Line/Area/Column/Bar, <c>Category</c> vira o tick no eixo categórico.</summary>
public sealed class ChartDataPoint
{
    /// <summary>Rótulo no eixo categórico (ou label da fatia em Pie/Donut).</summary>
    public string Category { get; set; } = "";

    /// <summary>Valor numérico (eixo de valores em cartesianos; tamanho da fatia em Pie/Donut).</summary>
    public double Value { get; set; }

    /// <summary>Cor opcional só para Pie/Donut (override do scheme/cor da série).</summary>
    public string? Color { get; set; }
}

/// <summary>Uma série de dados do <c>OmniChart</c>. Múltiplas séries cartesianas
/// (Line/Area/Column/Bar) podem ser combinadas no mesmo chart; Pie/Donut aceita
/// apenas uma série.</summary>
public sealed class ChartSeries
{
    /// <summary>Nome exibido na legenda e no tooltip.</summary>
    public string Title { get; set; } = "";

    /// <summary>Tipo visual desta série. Default <c>Line</c>.</summary>
    public ChartSeriesType Type { get; set; } = ChartSeriesType.Line;

    /// <summary>Cor opcional (qualquer CSS color). Quando nula, é puxada do esquema.</summary>
    public string? Color { get; set; }

    /// <summary>Pontos de dados desta série.</summary>
    public IEnumerable<ChartDataPoint> Points { get; set; } = Array.Empty<ChartDataPoint>();

    /// <summary>Interpolação para Line/Area (Linear/Smooth/Step). Default <c>Linear</c>.</summary>
    public ChartInterpolation Interpolation { get; set; } = ChartInterpolation.Linear;

    /// <summary>Espessura do traço (Line/Area). Em unidades de pixel SVG. Default 2.</summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>Mostra marcadores nos pontos de Line/Area. Default false.</summary>
    public bool ShowMarkers { get; set; }

    /// <summary>Opacidade do preenchimento de Area (0..1). Default 0.18.</summary>
    public double AreaOpacity { get; set; } = 0.18;
}
