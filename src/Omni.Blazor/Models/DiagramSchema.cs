namespace Omni.Blazor.Models;

/// <summary>Immutable graph and canvas metadata for <c>OmniDiagramCanvas</c>.</summary>
public sealed class DiagramSchema
{
    internal DiagramSchema(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges,
        DiagramViewport viewport,
        bool readOnly,
        bool fitOnMount,
        bool showMinimap,
        bool showControls,
        bool showAutoLayout,
        string dropPayloadFormat,
        string autoLayoutText,
        string zoomInText,
        string zoomOutText,
        string fitText)
    {
        Nodes = nodes;
        Edges = edges;
        Viewport = viewport;
        ReadOnly = readOnly;
        FitOnMount = fitOnMount;
        ShowMinimap = showMinimap;
        ShowControls = showControls;
        ShowAutoLayout = showAutoLayout;
        DropPayloadFormat = dropPayloadFormat;
        AutoLayoutText = autoLayoutText;
        ZoomInText = zoomInText;
        ZoomOutText = zoomOutText;
        FitText = fitText;
    }

    /// <summary>Immutable node snapshot.</summary>
    public IReadOnlyList<DiagramNode> Nodes { get; }

    /// <summary>Immutable validated edge snapshot.</summary>
    public IReadOnlyList<DiagramEdge> Edges { get; }

    /// <summary>Initial canvas viewport.</summary>
    public DiagramViewport Viewport { get; }

    /// <summary>Whether graph editing is disabled.</summary>
    public bool ReadOnly { get; }

    /// <summary>Whether the graph is fitted once after mounting.</summary>
    public bool FitOnMount { get; }

    /// <summary>Whether the minimap is visible.</summary>
    public bool ShowMinimap { get; }

    /// <summary>Whether viewport controls are visible.</summary>
    public bool ShowControls { get; }

    /// <summary>Whether the auto-layout control is visible.</summary>
    public bool ShowAutoLayout { get; }

    /// <summary>Accepted HTML drag payload format.</summary>
    public string DropPayloadFormat { get; }

    /// <summary>Auto-layout action text.</summary>
    public string AutoLayoutText { get; }

    /// <summary>Zoom-in action text.</summary>
    public string ZoomInText { get; }

    /// <summary>Zoom-out action text.</summary>
    public string ZoomOutText { get; }

    /// <summary>Fit-to-view action text.</summary>
    public string FitText { get; }

    /// <summary>Creates an immutable diagram schema.</summary>
    public static DiagramSchema Create(Action<DiagramSchemaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DiagramSchemaBuilder builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot diagram builder.</summary>
    public static DiagramSchemaBuilder Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public DiagramSchema Extend(Action<DiagramSchemaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DiagramSchemaBuilder builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed one-shot builder for graph structure and canvas behavior.</summary>
public sealed class DiagramSchemaBuilder
{
    private readonly List<DiagramNode> _nodes = [];
    private readonly HashSet<string> _nodeIds = new(StringComparer.Ordinal);
    private readonly List<DiagramEdge> _edges = [];
    private readonly HashSet<string> _edgeIds = new(StringComparer.Ordinal);
    private DiagramViewport _viewport = DiagramViewport.Default;
    private bool _readOnly;
    private bool _fitOnMount;
    private bool _showMinimap = true;
    private bool _showControls = true;
    private bool _showAutoLayout = true;
    private string _dropPayloadFormat = "application/x-omni-diagram";
    private string _autoLayoutText = "Auto-layout (organizar)";
    private string _zoomInText = "Aumentar zoom";
    private string _zoomOutText = "Diminuir zoom";
    private string _fitText = "Ajustar à tela";
    private DiagramAutoLayoutOptions? _autoLayout;
    private bool _built;

    /// <summary>Includes graph structure and canvas defaults from an immutable schema.</summary>
    public DiagramSchemaBuilder Include(DiagramSchema schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        foreach (DiagramNode node in schema.Nodes)
        {
            if (!_nodeIds.Add(node.Id))
                throw new InvalidOperationException($"Diagram node '{node.Id}' was declared more than once.");
            _nodes.Add(node);
        }
        foreach (DiagramEdge edge in schema.Edges)
        {
            if (!_edgeIds.Add(edge.Id))
                throw new InvalidOperationException($"Diagram edge '{edge.Id}' was declared more than once.");
            _edges.Add(edge);
        }
        _viewport = schema.Viewport;
        _readOnly = schema.ReadOnly;
        _fitOnMount = schema.FitOnMount;
        _showMinimap = schema.ShowMinimap;
        _showControls = schema.ShowControls;
        _showAutoLayout = schema.ShowAutoLayout;
        _dropPayloadFormat = schema.DropPayloadFormat;
        _autoLayoutText = schema.AutoLayoutText;
        _zoomInText = schema.ZoomInText;
        _zoomOutText = schema.ZoomOutText;
        _fitText = schema.FitText;
        return this;
    }

    /// <summary>Adds a graph node.</summary>
    public DiagramSchemaBuilder Node(string id, Action<DiagramNodeBuilder>? configure = null)
        => Node<object?>(id, null, configure);

    /// <summary>Adds a graph node with a strongly typed consumer payload.</summary>
    public DiagramSchemaBuilder Node<TData>(string id, TData data, Action<DiagramNodeBuilder>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_nodeIds.Add(id)) throw new InvalidOperationException($"Diagram node '{id}' was declared more than once.");
        DiagramNodeBuilder builder = new(id, data, EnsureMutable);
        configure?.Invoke(builder);
        _nodes.Add(builder.Build());
        return this;
    }

    /// <summary>Replaces an inherited node while preserving its graph position.</summary>
    public DiagramSchemaBuilder OverrideNode<TData>(
        string id,
        TData data,
        Action<DiagramNodeBuilder>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        int index = _nodes.FindIndex(node => string.Equals(node.Id, id, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"Diagram node '{id}' cannot be overridden because it is not declared.");
        DiagramNodeBuilder builder = new(id, data, EnsureMutable);
        configure?.Invoke(builder);
        _nodes[index] = builder.Build();
        return this;
    }

    /// <summary>Removes inherited graph structure while preserving canvas defaults.</summary>
    public DiagramSchemaBuilder ClearGraph()
    {
        EnsureMutable();
        _nodes.Clear();
        _nodeIds.Clear();
        _edges.Clear();
        _edgeIds.Clear();
        return this;
    }

    /// <summary>Adds a directed edge and validates it when the schema is built.</summary>
    public DiagramSchemaBuilder Connect(
        string id,
        string source,
        string sourcePort,
        string target,
        string targetPort = "In")
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePort);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPort);
        if (!_edgeIds.Add(id)) throw new InvalidOperationException($"Diagram edge '{id}' was declared more than once.");
        _edges.Add(new DiagramEdge(id, source, sourcePort, target, targetPort));
        return this;
    }

    /// <summary>Replaces an inherited edge while preserving its declaration position.</summary>
    public DiagramSchemaBuilder OverrideConnection(
        string id,
        string source,
        string sourcePort,
        string target,
        string targetPort = "In")
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePort);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPort);
        int index = _edges.FindIndex(edge => string.Equals(edge.Id, id, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"Diagram edge '{id}' cannot be overridden because it is not declared.");
        _edges[index] = new DiagramEdge(id, source, sourcePort, target, targetPort);
        return this;
    }

    /// <summary>Configures the initial viewport.</summary>
    public DiagramSchemaBuilder Viewport(double x = 40, double y = 30, double zoom = 0.85)
    {
        EnsureMutable();
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (!double.IsFinite(zoom) || zoom is < DiagramGeometry.MinZoom or > DiagramGeometry.MaxZoom)
            throw new ArgumentOutOfRangeException(nameof(zoom));
        _viewport = new DiagramViewport(x, y, zoom);
        return this;
    }

    /// <summary>Configures editing and canvas controls.</summary>
    public DiagramSchemaBuilder Behavior(
        bool readOnly = false,
        bool fitOnMount = false,
        bool showMinimap = true,
        bool showControls = true,
        bool showAutoLayout = true,
        string dropPayloadFormat = "application/x-omni-diagram")
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(dropPayloadFormat);
        _readOnly = readOnly;
        _fitOnMount = fitOnMount;
        _showMinimap = showMinimap;
        _showControls = showControls;
        _showAutoLayout = showAutoLayout;
        _dropPayloadFormat = dropPayloadFormat;
        return this;
    }

    /// <summary>Configures localized action labels.</summary>
    public DiagramSchemaBuilder Labels(string autoLayout, string zoomIn, string zoomOut, string fit)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(autoLayout);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoomIn);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoomOut);
        ArgumentException.ThrowIfNullOrWhiteSpace(fit);
        _autoLayoutText = autoLayout;
        _zoomInText = zoomIn;
        _zoomOutText = zoomOut;
        _fitText = fit;
        return this;
    }

    /// <summary>Applies the built-in left-to-right layout once while building the schema.</summary>
    public DiagramSchemaBuilder AutoLayout(
        double columnWidth = 300,
        double rowHeight = 150,
        double originX = 60,
        double originY = 60)
    {
        EnsureMutable();
        if (!double.IsFinite(columnWidth) || columnWidth <= 0) throw new ArgumentOutOfRangeException(nameof(columnWidth));
        if (!double.IsFinite(rowHeight) || rowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        if (!double.IsFinite(originX)) throw new ArgumentOutOfRangeException(nameof(originX));
        if (!double.IsFinite(originY)) throw new ArgumentOutOfRangeException(nameof(originY));
        _autoLayout = new DiagramAutoLayoutOptions(columnWidth, rowHeight, originX, originY);
        return this;
    }

    /// <summary>Builds and validates the immutable graph.</summary>
    public DiagramSchema Build()
    {
        EnsureMutable();
        _built = true;
        ValidateEdges();

        DiagramNode[] nodes = _nodes.ToArray();
        DiagramEdge[] edges = _edges.ToArray();
        if (_autoLayout is { } layout)
        {
            IReadOnlyList<DiagramNodeMove> moves = DiagramGeometry.AutoLayout(
                nodes, edges, layout.ColumnWidth, layout.RowHeight, layout.OriginX, layout.OriginY);
            Dictionary<string, DiagramNodeMove> positions = moves.ToDictionary(static move => move.Id, StringComparer.Ordinal);
            nodes = nodes.Select(node => positions.TryGetValue(node.Id, out DiagramNodeMove? move)
                ? node with { X = move.X, Y = move.Y }
                : node).ToArray();
        }

        return new DiagramSchema(
            Array.AsReadOnly(nodes), Array.AsReadOnly(edges), _viewport,
            _readOnly, _fitOnMount, _showMinimap, _showControls,
            _showAutoLayout, _dropPayloadFormat, _autoLayoutText, _zoomInText, _zoomOutText, _fitText);
    }

    private void ValidateEdges()
    {
        Dictionary<string, DiagramNode> nodes = _nodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        foreach (DiagramEdge edge in _edges)
        {
            if (!nodes.TryGetValue(edge.Source, out DiagramNode? source))
                throw new InvalidOperationException($"Diagram edge '{edge.Id}' references missing source node '{edge.Source}'.");
            if (!nodes.TryGetValue(edge.Target, out DiagramNode? target))
                throw new InvalidOperationException($"Diagram edge '{edge.Id}' references missing target node '{edge.Target}'.");
            if (!source.OutPorts.Any(port => string.Equals(port.Name, edge.SourcePort, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Diagram edge '{edge.Id}' references missing source port '{edge.SourcePort}'.");
            if (!target.HasInPort || !string.Equals(edge.TargetPort, "In", StringComparison.Ordinal))
                throw new InvalidOperationException($"Diagram edge '{edge.Id}' references an unavailable target port '{edge.TargetPort}'.");
        }
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("This diagram schema builder has already been built.");
    }

    private sealed record DiagramAutoLayoutOptions(
        double ColumnWidth,
        double RowHeight,
        double OriginX,
        double OriginY);
}

/// <summary>Builder for one diagram node and its output ports.</summary>
public sealed class DiagramNodeBuilder
{
    private readonly string _id;
    private readonly object? _data;
    private readonly Action _ensureMutable;
    private readonly List<DiagramPort> _ports = [];
    private readonly HashSet<string> _portNames = new(StringComparer.Ordinal);
    private double _x;
    private double _y;
    private string _title = string.Empty;
    private string? _subtitle;
    private string? _icon;
    private string? _color;
    private bool _hasInPort = true;

    internal DiagramNodeBuilder(string id, object? data, Action ensureMutable)
    {
        _id = id;
        _data = data;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the world-space position.</summary>
    public DiagramNodeBuilder Position(double x, double y)
    {
        _ensureMutable();
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        _x = x;
        _y = y;
        return this;
    }

    /// <summary>Sets the primary and secondary labels.</summary>
    public DiagramNodeBuilder Text(string title, string? subtitle = null)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _title = title;
        _subtitle = subtitle;
        return this;
    }

    /// <summary>Sets the optional icon and accent color.</summary>
    public DiagramNodeBuilder Appearance(string? icon = null, string? color = null)
    {
        _ensureMutable();
        _icon = icon;
        _color = color;
        return this;
    }

    /// <summary>Controls whether the implicit input port is available.</summary>
    public DiagramNodeBuilder Input(bool enabled = true)
    {
        _ensureMutable();
        _hasInPort = enabled;
        return this;
    }

    /// <summary>Adds a named output port.</summary>
    public DiagramNodeBuilder Output(string name, DiagramPortKind kind = DiagramPortKind.Default)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!_portNames.Add(name)) throw new InvalidOperationException($"Diagram node '{_id}' port '{name}' was declared more than once.");
        _ports.Add(new DiagramPort(name, kind));
        return this;
    }

    internal DiagramNode Build() => new()
    {
        Id = _id,
        X = _x,
        Y = _y,
        Title = _title,
        Subtitle = _subtitle,
        Icon = _icon,
        Color = _color,
        HasInPort = _hasInPort,
        OutPorts = Array.AsReadOnly(_ports.ToArray()),
        Data = _data
    };
}
