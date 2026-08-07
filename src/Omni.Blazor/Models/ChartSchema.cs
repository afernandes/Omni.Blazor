using System.Linq.Expressions;

namespace Omni.Blazor.Models;

/// <summary>Immutable point metadata produced by a typed chart schema.</summary>
public sealed record ChartDataPointSchema(
    string Category,
    double Value,
    string? Color = null,
    bool IsTotal = false,
    double? X = null,
    double? Size = null);

/// <summary>Immutable series metadata produced by a typed chart schema.</summary>
public sealed class ChartSeriesSchema
{
    internal ChartSeriesSchema(
        string title,
        ChartSeriesType type,
        string? color,
        IReadOnlyList<ChartDataPointSchema> points,
        ChartInterpolation interpolation,
        double strokeWidth,
        bool showMarkers,
        double areaOpacity,
        double gaugeMinimum,
        double gaugeMaximum)
    {
        Title = title;
        Type = type;
        Color = color;
        Points = points;
        Interpolation = interpolation;
        StrokeWidth = strokeWidth;
        ShowMarkers = showMarkers;
        AreaOpacity = areaOpacity;
        GaugeMinimum = gaugeMinimum;
        GaugeMaximum = gaugeMaximum;
    }

    /// <summary>Legend and tooltip title.</summary>
    public string Title { get; }

    /// <summary>Visual series type.</summary>
    public ChartSeriesType Type { get; }

    /// <summary>Optional CSS color override.</summary>
    public string? Color { get; }

    /// <summary>Immutable point snapshot.</summary>
    public IReadOnlyList<ChartDataPointSchema> Points { get; }

    /// <summary>Line interpolation mode.</summary>
    public ChartInterpolation Interpolation { get; }

    /// <summary>Line stroke width in SVG pixels.</summary>
    public double StrokeWidth { get; }

    /// <summary>Whether point markers are visible.</summary>
    public bool ShowMarkers { get; }

    /// <summary>Area fill opacity.</summary>
    public double AreaOpacity { get; }

    /// <summary>Minimum Gauge value.</summary>
    public double GaugeMinimum { get; }

    /// <summary>Maximum Gauge value.</summary>
    public double GaugeMaximum { get; }

    internal ChartSeries Materialize() => new()
    {
        Title = Title,
        Type = Type,
        Color = Color,
        Points = Points.Select(static point => new ChartDataPoint
        {
            Category = point.Category,
            Value = point.Value,
            Color = point.Color,
            IsTotal = point.IsTotal,
            X = point.X,
            Size = point.Size
        }).ToArray(),
        Interpolation = Interpolation,
        StrokeWidth = StrokeWidth,
        ShowMarkers = ShowMarkers,
        AreaOpacity = AreaOpacity,
        GaugeMinimum = GaugeMinimum,
        GaugeMaximum = GaugeMaximum
    };
}

/// <summary>Immutable chart data and presentation metadata for <c>OmniChart</c>.</summary>
public sealed class ChartSchema
{
    internal ChartSchema(
        IReadOnlyList<ChartSeriesSchema> series,
        string height,
        string width,
        ChartColorScheme colorScheme,
        ChartLegendPosition legendPosition,
        bool showGrid,
        string? title,
        Func<double, string>? valueFormatter,
        int valueTicks,
        string? donutCenterLabel,
        string? donutCenterValue,
        string? ariaLabel)
    {
        Series = series;
        Height = height;
        Width = width;
        ColorScheme = colorScheme;
        LegendPosition = legendPosition;
        ShowGrid = showGrid;
        Title = title;
        ValueFormatter = valueFormatter;
        ValueTicks = valueTicks;
        DonutCenterLabel = donutCenterLabel;
        DonutCenterValue = donutCenterValue;
        AriaLabel = ariaLabel;
    }

    /// <summary>Immutable series snapshot.</summary>
    public IReadOnlyList<ChartSeriesSchema> Series { get; }

    /// <summary>Chart CSS height.</summary>
    public string Height { get; }

    /// <summary>Chart CSS width.</summary>
    public string Width { get; }

    /// <summary>Fallback color scheme.</summary>
    public ChartColorScheme ColorScheme { get; }

    /// <summary>Legend position.</summary>
    public ChartLegendPosition LegendPosition { get; }

    /// <summary>Whether horizontal grid lines are visible.</summary>
    public bool ShowGrid { get; }

    /// <summary>Optional accessible chart title.</summary>
    public string? Title { get; }

    /// <summary>Optional value-axis formatter.</summary>
    public Func<double, string>? ValueFormatter { get; }

    /// <summary>Requested value-axis tick count.</summary>
    public int ValueTicks { get; }

    /// <summary>Optional donut center label.</summary>
    public string? DonutCenterLabel { get; }

    /// <summary>Optional donut center value.</summary>
    public string? DonutCenterValue { get; }

    /// <summary>Optional accessible SVG label.</summary>
    public string? AriaLabel { get; }

    /// <summary>Creates an immutable chart schema.</summary>
    public static ChartSchema Create(Action<ChartSchemaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ChartSchemaBuilder builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot chart builder.</summary>
    public static ChartSchemaBuilder Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public ChartSchema Extend(Action<ChartSchemaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ChartSchemaBuilder builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }

    internal ChartSeries[] MaterializeSeries()
        => Series.Select(static series => series.Materialize()).ToArray();
}

/// <summary>Strongly typed one-shot builder for chart series and presentation metadata.</summary>
public sealed class ChartSchemaBuilder
{
    private readonly List<ChartSeriesSchema> _series = [];
    private readonly HashSet<string> _titles = new(StringComparer.Ordinal);
    private string _height = "260px";
    private string _width = "100%";
    private ChartColorScheme _colorScheme = ChartColorScheme.Palette;
    private ChartLegendPosition _legendPosition = ChartLegendPosition.Bottom;
    private bool _showGrid = true;
    private string? _title;
    private Func<double, string>? _valueFormatter;
    private int _valueTicks = 5;
    private string? _donutCenterLabel;
    private string? _donutCenterValue;
    private string? _ariaLabel;
    private bool _built;

    /// <summary>Includes series and presentation defaults from an immutable schema.</summary>
    public ChartSchemaBuilder Include(ChartSchema schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        foreach (ChartSeriesSchema series in schema.Series)
        {
            if (!_titles.Add(series.Title))
                throw new InvalidOperationException($"Chart series '{series.Title}' was declared more than once.");
            _series.Add(series);
        }
        _height = schema.Height;
        _width = schema.Width;
        _colorScheme = schema.ColorScheme;
        _legendPosition = schema.LegendPosition;
        _showGrid = schema.ShowGrid;
        _title = schema.Title;
        _valueFormatter = schema.ValueFormatter;
        _valueTicks = schema.ValueTicks;
        _donutCenterLabel = schema.DonutCenterLabel;
        _donutCenterValue = schema.DonutCenterValue;
        _ariaLabel = schema.AriaLabel;
        return this;
    }

    /// <summary>Adds a series from an immutable snapshot of existing chart points.</summary>
    public ChartSchemaBuilder Series(
        string title,
        IEnumerable<ChartDataPoint> points,
        Action<ChartSeriesBuilder<ChartDataPoint>>? configure = null)
        => Series(title, points, static point => point.Category, static point => point.Value, builder =>
        {
            builder.PointColor(static point => point.Color)
                .TotalWhen(static point => point.IsTotal)
                .OptionalX(static point => point.X)
                .OptionalSize(static point => point.Size);
            configure?.Invoke(builder);
        });

    /// <summary>Adds a series using strongly typed category and value selectors.</summary>
    public ChartSchemaBuilder Series<TItem>(
        string title,
        IEnumerable<TItem> items,
        Expression<Func<TItem, string>> category,
        Expression<Func<TItem, double>> value,
        Action<ChartSeriesBuilder<TItem>>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(value);
        if (!_titles.Add(title)) throw new InvalidOperationException($"Chart series '{title}' was declared more than once.");

        ChartSeriesBuilder<TItem> builder = new(items, category.Compile(), value.Compile(), EnsureMutable);
        configure?.Invoke(builder);
        _series.Add(builder.Build(title));
        return this;
    }

    /// <summary>Replaces an inherited series while preserving its legend position.</summary>
    public ChartSchemaBuilder OverrideSeries<TItem>(
        string title,
        IEnumerable<TItem> items,
        Expression<Func<TItem, string>> category,
        Expression<Func<TItem, double>> value,
        Action<ChartSeriesBuilder<TItem>>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(value);
        int index = _series.FindIndex(series => string.Equals(series.Title, title, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"Chart series '{title}' cannot be overridden because it is not declared.");
        ChartSeriesBuilder<TItem> builder = new(items, category.Compile(), value.Compile(), EnsureMutable);
        configure?.Invoke(builder);
        _series[index] = builder.Build(title);
        return this;
    }

    /// <summary>Removes inherited series so a derived schema can declare a new dataset.</summary>
    public ChartSchemaBuilder ClearSeries()
    {
        EnsureMutable();
        _series.Clear();
        _titles.Clear();
        return this;
    }

    /// <summary>Configures chart dimensions.</summary>
    public ChartSchemaBuilder Size(string height = "260px", string width = "100%")
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(width);
        _height = height;
        _width = width;
        return this;
    }

    /// <summary>Configures legend, colors and grid visibility.</summary>
    public ChartSchemaBuilder Appearance(
        ChartColorScheme colorScheme = ChartColorScheme.Palette,
        ChartLegendPosition legendPosition = ChartLegendPosition.Bottom,
        bool showGrid = true)
    {
        EnsureMutable();
        if (!Enum.IsDefined(colorScheme)) throw new ArgumentOutOfRangeException(nameof(colorScheme));
        if (!Enum.IsDefined(legendPosition)) throw new ArgumentOutOfRangeException(nameof(legendPosition));
        _colorScheme = colorScheme;
        _legendPosition = legendPosition;
        _showGrid = showGrid;
        return this;
    }

    /// <summary>Configures title, accessibility and axis formatting.</summary>
    public ChartSchemaBuilder Labels(
        string? title = null,
        string? ariaLabel = null,
        Func<double, string>? valueFormatter = null,
        int valueTicks = 5)
    {
        EnsureMutable();
        if (valueTicks <= 0) throw new ArgumentOutOfRangeException(nameof(valueTicks));
        _title = title;
        _ariaLabel = ariaLabel;
        _valueFormatter = valueFormatter;
        _valueTicks = valueTicks;
        return this;
    }

    /// <summary>Configures donut center content.</summary>
    public ChartSchemaBuilder DonutCenter(string? label, string? value)
    {
        EnsureMutable();
        _donutCenterLabel = label;
        _donutCenterValue = value;
        return this;
    }

    /// <summary>Builds the immutable schema and seals this builder.</summary>
    public ChartSchema Build()
    {
        EnsureMutable();
        _built = true;
        if (_series.Count == 0) throw new InvalidOperationException("Chart schema requires at least one series.");
        bool hasRadial = _series.Any(static series => series.Type is ChartSeriesType.Pie or ChartSeriesType.Donut or ChartSeriesType.Gauge);
        if (hasRadial && _series.Count != 1)
            throw new InvalidOperationException("Pie, donut and gauge schemas support exactly one series.");
        bool hasRadar = _series.Any(static series => series.Type == ChartSeriesType.Radar);
        if (hasRadar && _series.Any(static series => series.Type != ChartSeriesType.Radar))
            throw new InvalidOperationException("Radar schemas can contain only Radar series.");
        bool hasScatter = _series.Any(static series => series.Type is ChartSeriesType.Scatter or ChartSeriesType.Bubble);
        if (hasScatter && _series.Any(static series => series.Type is not (ChartSeriesType.Scatter or ChartSeriesType.Bubble)))
            throw new InvalidOperationException("Scatter and Bubble can be combined only with each other.");
        return new ChartSchema(
            Array.AsReadOnly(_series.ToArray()), _height, _width, _colorScheme, _legendPosition, _showGrid,
            _title, _valueFormatter, _valueTicks, _donutCenterLabel, _donutCenterValue, _ariaLabel);
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("This chart schema builder has already been built.");
    }
}

/// <summary>Strongly typed builder for one chart series.</summary>
public sealed class ChartSeriesBuilder<TItem>
{
    private readonly IEnumerable<TItem> _items;
    private readonly Func<TItem, string> _category;
    private readonly Func<TItem, double> _value;
    private readonly Action _ensureMutable;
    private Func<TItem, string?>? _pointColor;
    private Func<TItem, bool>? _isTotal;
    private ChartSeriesType _type = ChartSeriesType.Line;
    private string? _color;
    private ChartInterpolation _interpolation = ChartInterpolation.Linear;
    private double _strokeWidth = 2;
    private bool _showMarkers;
    private double _areaOpacity = 0.18;
    private Func<TItem, double?>? _x;
    private Func<TItem, double?>? _size;
    private double _gaugeMinimum;
    private double _gaugeMaximum = 100;

    internal ChartSeriesBuilder(
        IEnumerable<TItem> items,
        Func<TItem, string> category,
        Func<TItem, double> value,
        Action ensureMutable)
    {
        _items = items;
        _category = category;
        _value = value;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the visual series type.</summary>
    public ChartSeriesBuilder<TItem> Type(ChartSeriesType type)
    {
        _ensureMutable();
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        _type = type;
        return this;
    }

    /// <summary>Sets an optional CSS series color.</summary>
    public ChartSeriesBuilder<TItem> Color(string? color)
    {
        _ensureMutable();
        _color = color;
        return this;
    }

    /// <summary>Configures line or area rendering.</summary>
    public ChartSeriesBuilder<TItem> Line(
        ChartInterpolation interpolation = ChartInterpolation.Linear,
        double strokeWidth = 2,
        bool showMarkers = false,
        double areaOpacity = 0.18)
    {
        _ensureMutable();
        if (!Enum.IsDefined(interpolation)) throw new ArgumentOutOfRangeException(nameof(interpolation));
        if (!double.IsFinite(strokeWidth) || strokeWidth <= 0) throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        if (!double.IsFinite(areaOpacity) || areaOpacity is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(areaOpacity));
        _interpolation = interpolation;
        _strokeWidth = strokeWidth;
        _showMarkers = showMarkers;
        _areaOpacity = areaOpacity;
        return this;
    }

    /// <summary>Maps an optional color for each point.</summary>
    public ChartSeriesBuilder<TItem> PointColor(Expression<Func<TItem, string?>> selector)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _pointColor = selector.Compile();
        return this;
    }

    /// <summary>Maps waterfall total points.</summary>
    public ChartSeriesBuilder<TItem> TotalWhen(Expression<Func<TItem, bool>> predicate)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        _isTotal = predicate.Compile();
        return this;
    }

    /// <summary>Maps the numeric X coordinate used by Scatter and Bubble series.</summary>
    public ChartSeriesBuilder<TItem> X(Expression<Func<TItem, double>> selector)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TItem, double> compiled = selector.Compile();
        _x = item => compiled(item);
        return this;
    }

    /// <summary>Maps the relative size used by Bubble series.</summary>
    public ChartSeriesBuilder<TItem> BubbleSize(Expression<Func<TItem, double>> selector)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TItem, double> compiled = selector.Compile();
        _size = item => compiled(item);
        return this;
    }

    internal ChartSeriesBuilder<TItem> OptionalX(Func<TItem, double?> selector)
    {
        _ensureMutable();
        _x = selector;
        return this;
    }

    internal ChartSeriesBuilder<TItem> OptionalSize(Func<TItem, double?> selector)
    {
        _ensureMutable();
        _size = selector;
        return this;
    }

    /// <summary>Configures the accepted Gauge range.</summary>
    public ChartSeriesBuilder<TItem> GaugeRange(double minimum = 0, double maximum = 100)
    {
        _ensureMutable();
        if (!double.IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (!double.IsFinite(maximum) || maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
        _gaugeMinimum = minimum;
        _gaugeMaximum = maximum;
        return this;
    }

    internal ChartSeriesSchema Build(string title)
    {
        List<ChartDataPointSchema> points = [];
        foreach (TItem item in _items)
        {
            string category = _category(item) ?? throw new InvalidOperationException($"Chart series '{title}' produced a null category.");
            double value = _value(item);
            if (!double.IsFinite(value)) throw new InvalidOperationException($"Chart series '{title}' produced a non-finite value.");
            double? x = _x?.Invoke(item);
            double? size = _size?.Invoke(item);
            if (x is not null && !double.IsFinite(x.Value))
                throw new InvalidOperationException($"Chart series '{title}' produced a non-finite X value.");
            if (size is not null && (!double.IsFinite(size.Value) || size.Value < 0))
                throw new InvalidOperationException($"Chart series '{title}' produced an invalid bubble size.");
            points.Add(new ChartDataPointSchema(
                category,
                value,
                _pointColor?.Invoke(item),
                _isTotal?.Invoke(item) ?? false,
                x,
                size));
        }

        return new ChartSeriesSchema(
            title, _type, _color, Array.AsReadOnly(points.ToArray()),
            _interpolation, _strokeWidth, _showMarkers, _areaOpacity,
            _gaugeMinimum, _gaugeMaximum);
    }
}
