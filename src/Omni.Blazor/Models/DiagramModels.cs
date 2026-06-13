namespace Omni.Blazor.Models;

/// <summary>Semantic kind of a diagram port, used only for visual styling.</summary>
public enum DiagramPortKind
{
    /// <summary>Neutral port (default styling).</summary>
    Default,

    /// <summary>Success-flavored port (rendered green).</summary>
    Success,

    /// <summary>Failure/error-flavored port (rendered red).</summary>
    Failure,
}

/// <summary>A named output port on a <see cref="DiagramNode"/>.</summary>
public sealed record DiagramPort(string Name, DiagramPortKind Kind = DiagramPortKind.Default);

/// <summary>
/// A node in an <c>OmniDiagramCanvas</c> graph. The canvas knows nothing about
/// what the node represents — <see cref="Data"/> carries the consumer payload.
/// Coordinates are world-space (pre-zoom) pixels.
/// </summary>
public sealed record DiagramNode
{
    /// <summary>Stable unique id of the node within the graph.</summary>
    public required string Id { get; init; }

    /// <summary>World X coordinate (left edge).</summary>
    public double X { get; init; }

    /// <summary>World Y coordinate (top edge).</summary>
    public double Y { get; init; }

    /// <summary>Main label rendered by the default node template.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Secondary mono label under the title (e.g. a type name).</summary>
    public string? Subtitle { get; init; }

    /// <summary>Optional icon name (Omni icon library) for the default template.</summary>
    public string? Icon { get; init; }

    /// <summary>Optional accent color (any CSS color) for the icon tile.</summary>
    public string? Color { get; init; }

    /// <summary>Whether the node exposes the single implicit input port.</summary>
    public bool HasInPort { get; init; } = true;

    /// <summary>Named output ports, rendered top-to-bottom on the right edge.</summary>
    public IReadOnlyList<DiagramPort> OutPorts { get; init; } = [];

    /// <summary>Opaque consumer payload (never inspected by the canvas).</summary>
    public object? Data { get; init; }
}

/// <summary>A directed connection between two node ports.</summary>
public sealed record DiagramEdge(
    string Id,
    string Source,
    string SourcePort,
    string Target,
    string TargetPort = "In");

/// <summary>
/// Visual execution overlay for the canvas. All members reference node ids or
/// edge ids; the canvas only translates them into CSS states.
/// </summary>
public sealed record DiagramRunState
{
    /// <summary>Node currently executing (pulsing blue).</summary>
    public string? Current { get; init; }

    /// <summary>Nodes already completed (green + check badge).</summary>
    public IReadOnlyList<string> Done { get; init; } = [];

    /// <summary>Node that faulted (red + X badge).</summary>
    public string? Faulted { get; init; }

    /// <summary>Node suspended waiting for an external event (amber + pause badge).</summary>
    public string? Waiting { get; init; }

    /// <summary>Nodes dimmed because the execution path skipped them.</summary>
    public IReadOnlyList<string> DimNodes { get; init; } = [];

    /// <summary>Edges traversed by the execution (green).</summary>
    public IReadOnlyList<string> TakenEdges { get; init; } = [];

    /// <summary>Edges dimmed because they were not traversed.</summary>
    public IReadOnlyList<string> DimEdges { get; init; } = [];

    /// <summary>Edge the token is currently animating along (dashed blue + token).</summary>
    public string? ActiveEdge { get; init; }

    /// <summary>Bump to restart the token animation when <see cref="ActiveEdge"/> repeats.</summary>
    public int TokenTick { get; init; }
}

/// <summary>Canvas viewport: world offset plus zoom factor.</summary>
public sealed record DiagramViewport(double X, double Y, double Zoom)
{
    /// <summary>Default viewport used when none is supplied.</summary>
    public static DiagramViewport Default { get; } = new(40, 30, 0.85);
}

/// <summary>Current selection of the canvas (node ids and/or a single edge).</summary>
public sealed record DiagramSelection(IReadOnlyList<string> NodeIds, string? EdgeId = null)
{
    /// <summary>The empty selection.</summary>
    public static DiagramSelection Empty { get; } = new([]);

    /// <summary>True when nothing is selected.</summary>
    public bool IsEmpty => NodeIds.Count == 0 && EdgeId is null;
}

/// <summary>A committed node move (end of a drag gesture or auto-layout).</summary>
public sealed record DiagramNodeMove(string Id, double X, double Y);

/// <summary>Raised when the user finishes a drag-to-connect gesture.</summary>
public sealed record DiagramConnectEventArgs(
    string SourceId,
    string SourcePort,
    string TargetId,
    string TargetPort = "In");

/// <summary>Raised when an external item (e.g. a palette entry) is dropped on the canvas.</summary>
public sealed record DiagramExternalDropEventArgs(string Payload, double X, double Y);

/// <summary>
/// Shared geometry rules for <c>OmniDiagramCanvas</c>. C# and the JS interop
/// module must agree on these numbers; the JS mirror lives in
/// <c>wwwroot/js/omni-diagram.js</c>.
/// </summary>
public static class DiagramGeometry
{
    /// <summary>Fixed node card width.</summary>
    public const double NodeWidth = 220;

    /// <summary>Height of the node header area.</summary>
    public const double HeadHeight = 52;

    /// <summary>Height of each output-port row.</summary>
    public const double PortRowHeight = 26;

    /// <summary>Bottom padding under the last port row.</summary>
    public const double FootPad = 10;

    /// <summary>Vertical offset of the input anchor from the node top.</summary>
    public const double InAnchorY = 26;

    /// <summary>Minimum zoom factor.</summary>
    public const double MinZoom = 0.25;

    /// <summary>Maximum zoom factor.</summary>
    public const double MaxZoom = 2.2;

    /// <summary>Computed node height from its port count.</summary>
    public static double NodeHeight(DiagramNode node)
        => HeadHeight + (node.OutPorts.Count > 0 ? node.OutPorts.Count * PortRowHeight + FootPad : 12);

    /// <summary>World coordinates of the input anchor (left edge).</summary>
    public static (double X, double Y) InAnchor(DiagramNode node) => (node.X, node.Y + InAnchorY);

    /// <summary>World coordinates of a named output anchor (right edge).</summary>
    public static (double X, double Y) OutAnchor(DiagramNode node, string portName)
    {
        var index = 0;
        for (var i = 0; i < node.OutPorts.Count; i++)
        {
            if (node.OutPorts[i].Name == portName) { index = i; break; }
        }

        return (node.X + NodeWidth, node.Y + HeadHeight + index * PortRowHeight + PortRowHeight / 2);
    }

    /// <summary>Cubic bezier path between two anchors (horizontal tangents).</summary>
    public static string BezierPath(double ax, double ay, double bx, double by)
    {
        var dx = Math.Max(45, Math.Abs(bx - ax) * 0.5);
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"M {ax} {ay} C {ax + dx} {ay}, {bx - dx} {by}, {bx} {by}");
    }

    /// <summary>
    /// Left-to-right longest-path layering layout. Returns one move per node;
    /// the input collections are not mutated.
    /// </summary>
    public static IReadOnlyList<DiagramNodeMove> AutoLayout(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges,
        double columnWidth = 300,
        double rowHeight = 150,
        double originX = 60,
        double originY = 60)
    {
        if (nodes.Count == 0) return [];

        var ids = nodes.Select(n => n.Id).ToHashSet();
        var indegree = nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var e in edges)
        {
            if (ids.Contains(e.Target) && ids.Contains(e.Source)) indegree[e.Target]++;
        }

        var layer = nodes.ToDictionary(n => n.Id, _ => 0);
        var frontier = nodes.Where(n => indegree[n.Id] == 0).Select(n => n.Id).ToList();
        if (frontier.Count == 0) frontier = [nodes[0].Id];

        var queue = new Queue<string>(frontier);
        var guard = 0;
        while (queue.Count > 0 && guard++ < 9999)
        {
            var id = queue.Dequeue();
            foreach (var e in edges)
            {
                if (e.Source != id || !layer.ContainsKey(e.Target)) continue;
                var candidate = layer[id] + 1;
                if (candidate > layer[e.Target])
                {
                    layer[e.Target] = candidate;
                    queue.Enqueue(e.Target);
                }
            }
        }

        var columns = nodes
            .GroupBy(n => layer[n.Id])
            .OrderBy(g => g.Key);

        var moves = new List<DiagramNodeMove>(nodes.Count);
        foreach (var column in columns)
        {
            var row = 0;
            foreach (var node in column)
            {
                moves.Add(new DiagramNodeMove(node.Id, originX + column.Key * columnWidth, originY + row * rowHeight));
                row++;
            }
        }

        return moves;
    }
}
