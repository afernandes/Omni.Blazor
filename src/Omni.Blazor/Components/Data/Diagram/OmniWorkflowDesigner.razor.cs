using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

public partial class OmniWorkflowDesigner<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TNode>
    where TNode : class
{
    private readonly List<WorkflowHistoryEntry> _undo = [];
    private readonly List<WorkflowHistoryEntry> _redo = [];
    private IReadOnlyList<DiagramNode> _workingNodes = Array.Empty<DiagramNode>();
    private IReadOnlyList<DiagramEdge> _workingEdges = Array.Empty<DiagramEdge>();
    private IReadOnlyList<DiagramNode>? _lastNodesParameter;
    private IReadOnlyList<DiagramEdge>? _lastEdgesParameter;
    private IReadOnlyList<string> _validationErrors = Array.Empty<string>();
    private DiagramSelection _selection = DiagramSelection.Empty;

    /// <summary>Controlled workflow node snapshot.</summary>
    [Parameter]
    public IReadOnlyList<DiagramNode> Nodes { get; set; } = Array.Empty<DiagramNode>();

    /// <summary>Raised after workflow nodes change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<DiagramNode>> NodesChanged { get; set; }

    /// <summary>Controlled workflow edge snapshot.</summary>
    [Parameter]
    public IReadOnlyList<DiagramEdge> Edges { get; set; } = Array.Empty<DiagramEdge>();

    /// <summary>Raised after workflow edges change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<DiagramEdge>> EdgesChanged { get; set; }

    /// <summary>Controlled Diagram selection.</summary>
    [Parameter]
    public DiagramSelection Selection { get; set; } = DiagramSelection.Empty;

    /// <summary>Raised after selection changes.</summary>
    [Parameter]
    public EventCallback<DiagramSelection> SelectionChanged { get; set; }

    /// <summary>Controlled Diagram viewport.</summary>
    [Parameter]
    public DiagramViewport Viewport { get; set; } = DiagramViewport.Default;

    /// <summary>Raised after the viewport changes.</summary>
    [Parameter]
    public EventCallback<DiagramViewport> ViewportChanged { get; set; }

    /// <summary>Optional execution-state overlay.</summary>
    [Parameter]
    public DiagramRunState? RunState { get; set; }

    /// <summary>Typed inspector schema used when a selected node carries TNode data.</summary>
    [Parameter]
    public DataFormSchema<TNode>? InspectorSchema { get; set; }

    /// <summary>Projects an edited typed payload back into its Diagram node.</summary>
    [Parameter]
    public Func<DiagramNode, TNode, DiagramNode>? UpdateNode { get; set; }

    /// <summary>Creates an edge for a completed connection. Omit to handle OnConnect externally.</summary>
    [Parameter]
    public Func<DiagramConnectEventArgs, DiagramEdge>? ConnectionFactory { get; set; }

    /// <summary>Bounded synchronous graph validator.</summary>
    [Parameter]
    public Func<IReadOnlyList<DiagramNode>, IReadOnlyList<DiagramEdge>, IReadOnlyList<string>>? Validator { get; set; }

    /// <summary>Maximum retained undo entries. Default 50, maximum 200.</summary>
    [Parameter]
    public int MaximumHistory { get; set; } = 50;

    /// <summary>Disables graph and inspector mutations.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Fits the graph on first render.</summary>
    [Parameter]
    public bool FitOnMount { get; set; }

    /// <summary>Shows the canvas minimap.</summary>
    [Parameter]
    public bool ShowMinimap { get; set; } = true;

    /// <summary>Shows viewport controls.</summary>
    [Parameter]
    public bool ShowControls { get; set; } = true;

    /// <summary>Shows the built-in auto-layout action.</summary>
    [Parameter]
    public bool ShowAutoLayout { get; set; } = true;

    /// <summary>Custom Diagram node content.</summary>
    [Parameter]
    public RenderFragment<DiagramNode>? NodeTemplate { get; set; }

    /// <summary>Content rendered when the workflow has no nodes.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Optional left-side workflow palette.</summary>
    [Parameter]
    public RenderFragment? PaletteContent { get; set; }

    /// <summary>Additional toolbar content.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Custom content shown when no typed node is selected.</summary>
    [Parameter]
    public RenderFragment? InspectorEmptyContent { get; set; }

    /// <summary>Raised for every completed connection gesture.</summary>
    [Parameter]
    public EventCallback<DiagramConnectEventArgs> OnConnect { get; set; }

    /// <summary>Raised for external canvas drops.</summary>
    [Parameter]
    public EventCallback<DiagramExternalDropEventArgs> OnExternalDrop { get; set; }

    /// <summary>Raised when an inspector property changes.</summary>
    [Parameter]
    public EventCallback<PropertyGridChangedEventArgs<TNode>> InspectorChanged { get; set; }

    /// <summary>Additional CSS class applied to the Diagram canvas.</summary>
    [Parameter]
    public string? CanvasClass { get; set; }

    /// <summary>Additional inline styles applied to the Diagram canvas.</summary>
    [Parameter]
    public string? CanvasStyle { get; set; }

    /// <summary>Accessible toolbar label.</summary>
    [Parameter]
    public string ToolbarAriaLabel { get; set; } = "Workflow actions";

    /// <summary>Accessible palette label.</summary>
    [Parameter]
    public string PaletteAriaLabel { get; set; } = "Workflow palette";

    /// <summary>Accessible inspector label.</summary>
    [Parameter]
    public string InspectorAriaLabel { get; set; } = "Properties";

    /// <summary>Inspector heading.</summary>
    [Parameter]
    public string InspectorTitle { get; set; } = "Properties";

    /// <summary>Inspector empty-state title.</summary>
    [Parameter]
    public string InspectorEmptyText { get; set; } = "Select a node";

    /// <summary>Undo action text.</summary>
    [Parameter]
    public string UndoText { get; set; } = "Undo";

    /// <summary>Redo action text.</summary>
    [Parameter]
    public string RedoText { get; set; } = "Redo";

    /// <summary>Graph validation heading.</summary>
    [Parameter]
    public string ValidationTitle { get; set; } = "Review the workflow";

    /// <summary>Whether an undo entry is available.</summary>
    public bool CanUndo => _undo.Count != 0;

    /// <summary>Whether a redo entry is available.</summary>
    public bool CanRedo => _redo.Count != 0;

    private TNode? SelectedModel
    {
        get
        {
            if (_selection.NodeIds.Count != 1) return null;
            string id = _selection.NodeIds[0];
            for (int index = 0; index < _workingNodes.Count; index++)
            {
                DiagramNode node = _workingNodes[index];
                if (string.Equals(node.Id, id, StringComparison.Ordinal)) return node.Data as TNode;
            }
            return null;
        }
    }

    private string RootCss => Utilities.CssBuilder.Default("omni-workflow-designer")
        .AddClass("omni-workflow-designer-readonly", ReadOnly)
        .AddClass(Class)
        .Build();

    private string WorkspaceCss => Utilities.CssBuilder.Default("omni-workflow-designer-workspace")
        .AddClass("omni-workflow-designer-with-palette", PaletteContent is not null)
        .Build();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (MaximumHistory is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(MaximumHistory));
        if (!ReferenceEquals(_lastNodesParameter, Nodes))
        {
            _lastNodesParameter = Nodes;
            if (!Nodes.SequenceEqual(_workingNodes))
            {
                _workingNodes = SnapshotNodes(Nodes);
                ClearHistory();
            }
        }
        if (!ReferenceEquals(_lastEdgesParameter, Edges))
        {
            _lastEdgesParameter = Edges;
            if (!Edges.SequenceEqual(_workingEdges))
            {
                _workingEdges = SnapshotEdges(Edges);
                ClearHistory();
            }
        }
        _selection = Selection;
        ValidateGraph();
    }

    /// <summary>Reverts the most recent graph mutation.</summary>
    public async Task UndoAsync()
    {
        if (ReadOnly || _undo.Count == 0) return;
        WorkflowHistoryEntry entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(entry);
        await ApplySnapshotAsync(entry.BeforeNodes, entry.BeforeEdges);
    }

    /// <summary>Reapplies the most recently reverted graph mutation.</summary>
    public async Task RedoAsync()
    {
        if (ReadOnly || _redo.Count == 0) return;
        WorkflowHistoryEntry entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(entry);
        await ApplySnapshotAsync(entry.AfterNodes, entry.AfterEdges);
    }

    /// <summary>Clears retained undo and redo entries.</summary>
    public void ClearHistory()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private async Task NodesMovedAsync(IReadOnlyList<DiagramNodeMove> moves)
    {
        if (ReadOnly || moves.Count == 0) return;
        Dictionary<string, DiagramNodeMove> byId = moves.ToDictionary(static move => move.Id, StringComparer.Ordinal);
        DiagramNode[] next = new DiagramNode[_workingNodes.Count];
        bool changed = false;
        for (int index = 0; index < next.Length; index++)
        {
            DiagramNode node = _workingNodes[index];
            if (byId.TryGetValue(node.Id, out DiagramNodeMove? move)
                && (node.X != move.X || node.Y != move.Y))
            {
                next[index] = node with { X = move.X, Y = move.Y };
                changed = true;
            }
            else
            {
                next[index] = node;
            }
        }
        if (!changed) return;
        await CommitAsync(Array.AsReadOnly(next), _workingEdges);
    }

    private async Task ConnectAsync(DiagramConnectEventArgs args)
    {
        if (OnConnect.HasDelegate) await OnConnect.InvokeAsync(args);
        if (ReadOnly || ConnectionFactory is null) return;
        DiagramEdge edge = ConnectionFactory(args)
            ?? throw new InvalidOperationException("Workflow ConnectionFactory returned null.");
        if (_workingEdges.Any(existing => string.Equals(existing.Id, edge.Id, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Workflow edge '{edge.Id}' already exists.");
        DiagramEdge[] next = new DiagramEdge[_workingEdges.Count + 1];
        for (int index = 0; index < _workingEdges.Count; index++) next[index] = _workingEdges[index];
        next[^1] = edge;
        await CommitAsync(_workingNodes, Array.AsReadOnly(next));
    }

    private async Task DeleteSelectionAsync(DiagramSelection selection)
    {
        if (ReadOnly || selection.IsEmpty) return;
        HashSet<string> nodeIds = selection.NodeIds.ToHashSet(StringComparer.Ordinal);
        DiagramNode[] nodes = _workingNodes.Where(node => !nodeIds.Contains(node.Id)).ToArray();
        DiagramEdge[] edges = _workingEdges.Where(edge =>
            !nodeIds.Contains(edge.Source)
            && !nodeIds.Contains(edge.Target)
            && !string.Equals(edge.Id, selection.EdgeId, StringComparison.Ordinal)).ToArray();
        if (nodes.Length == _workingNodes.Count && edges.Length == _workingEdges.Count) return;
        await CommitAsync(Array.AsReadOnly(nodes), Array.AsReadOnly(edges));
        await SelectionChangedAsync(DiagramSelection.Empty);
    }

    private async Task InspectorChangedAsync(PropertyGridChangedEventArgs<TNode> args)
    {
        if (ReadOnly) return;
        if (UpdateNode is not null && _selection.NodeIds.Count == 1)
        {
            string id = _selection.NodeIds[0];
            DiagramNode[] next = _workingNodes.ToArray();
            int index = Array.FindIndex(next, node => string.Equals(node.Id, id, StringComparison.Ordinal));
            if (index >= 0)
            {
                DiagramNode updated = UpdateNode(next[index], args.Model)
                    ?? throw new InvalidOperationException("Workflow UpdateNode returned null.");
                next[index] = updated;
                _workingNodes = Array.AsReadOnly(next);
                await NotifyNodesChangedAsync();
                ValidateGraph();
            }
        }
        if (InspectorChanged.HasDelegate) await InspectorChanged.InvokeAsync(args);
    }

    private async Task SelectionChangedAsync(DiagramSelection selection)
    {
        _selection = selection;
        if (SelectionChanged.HasDelegate) await SelectionChanged.InvokeAsync(selection);
    }

    private async Task CommitAsync(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        WorkflowHistoryEntry entry = new(
            _workingNodes,
            _workingEdges,
            SnapshotNodes(nodes),
            SnapshotEdges(edges));
        _undo.Add(entry);
        if (_undo.Count > MaximumHistory) _undo.RemoveAt(0);
        _redo.Clear();
        await ApplySnapshotAsync(entry.AfterNodes, entry.AfterEdges);
    }

    private async Task ApplySnapshotAsync(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        _workingNodes = nodes;
        _workingEdges = edges;
        ValidateGraph();
        await NotifyNodesChangedAsync();
        if (EdgesChanged.HasDelegate) await EdgesChanged.InvokeAsync(_workingEdges);
    }

    private Task NotifyNodesChangedAsync()
        => NodesChanged.HasDelegate ? NodesChanged.InvokeAsync(_workingNodes) : Task.CompletedTask;

    private void ValidateGraph()
    {
        if (Validator is null)
        {
            _validationErrors = Array.Empty<string>();
            return;
        }
        IReadOnlyList<string>? errors = Validator(_workingNodes, _workingEdges);
        _validationErrors = errors is null or { Count: 0 }
            ? Array.Empty<string>()
            : Array.AsReadOnly(errors.Where(static error => !string.IsNullOrWhiteSpace(error)).ToArray());
    }

    private static IReadOnlyList<DiagramNode> SnapshotNodes(IEnumerable<DiagramNode> nodes)
        => Array.AsReadOnly(nodes.ToArray());

    private static IReadOnlyList<DiagramEdge> SnapshotEdges(IEnumerable<DiagramEdge> edges)
        => Array.AsReadOnly(edges.ToArray());

    private sealed record WorkflowHistoryEntry(
        IReadOnlyList<DiagramNode> BeforeNodes,
        IReadOnlyList<DiagramEdge> BeforeEdges,
        IReadOnlyList<DiagramNode> AfterNodes,
        IReadOnlyList<DiagramEdge> AfterEdges);
}
