using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public class OmniDiagramCanvasTests : TestContextBase
{
    private static DiagramNode Node(string id, double x = 0, double y = 0, params DiagramPort[] outPorts) => new()
    {
        Id = id,
        X = x,
        Y = y,
        Title = "Nó " + id,
        Subtitle = "Omni.Test",
        Icon = "box",
        OutPorts = outPorts.Length > 0 ? outPorts : [new DiagramPort("Done")],
    };

    private static readonly IReadOnlyList<DiagramNode> TwoNodes =
    [
        Node("a", 0, 0, new DiagramPort("Done")),
        Node("b", 300, 0, new DiagramPort("Verdadeiro", DiagramPortKind.Success), new DiagramPort("Falso", DiagramPortKind.Failure)),
    ];

    private static readonly IReadOnlyList<DiagramEdge> OneEdge =
        [new DiagramEdge("e1", "a", "Done", "b")];

    [Fact]
    public void DefaultRender_HasRootClassAndLayers()
    {
        var cut = RenderComponent<OmniDiagramCanvas>();

        var root = cut.Find(".omni-diagram");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find(".omni-diagram-grid"));
        Assert.NotNull(cut.Find(".omni-diagram-world"));
        Assert.NotNull(cut.Find(".omni-diagram-svg"));
    }

    [Fact]
    public void ConsumerClass_IsAppendedToRoot()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Class, "minha-classe"));
        var root = cut.Find(".omni-diagram");
        Assert.Contains("minha-classe", root.ClassList);
    }

    [Fact]
    public void ConsumerStyle_IsForwardedToRoot()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Style, "height:400px"));
        var root = cut.Find(".omni-diagram");
        Assert.Contains("height:400px", root.GetAttribute("style"));
    }

    [Fact]
    public void UnmatchedAttributes_SplatOnRoot()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.AddUnmatched("data-testid", "diagram"));
        var root = cut.Find(".omni-diagram");
        Assert.Equal("diagram", root.GetAttribute("data-testid"));
    }

    [Fact]
    public void Nodes_RenderWithTitleSubtitleAndDataAttributes()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Nodes, TwoNodes));

        var nodes = cut.FindAll("[data-dgnode]");
        Assert.Equal(2, nodes.Count);
        var a = cut.Find("[data-dgnode='a']");
        Assert.Equal("0", a.GetAttribute("data-x"));
        Assert.Contains("Nó a", a.TextContent);
        Assert.Contains("Omni.Test", a.TextContent);
    }

    [Fact]
    public void Node_OutPorts_RenderNamesAndSemanticClasses()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Nodes, TwoNodes));

        var b = cut.Find("[data-dgnode='b']");
        var portTrue = b.QuerySelector("[data-port='Verdadeiro']");
        var portFalse = b.QuerySelector("[data-port='Falso']");
        Assert.NotNull(portTrue);
        Assert.NotNull(portFalse);
        Assert.Contains("omni-diagram-port-true", portTrue!.ClassList);
        Assert.Contains("omni-diagram-port-false", portFalse!.ClassList);
        Assert.Contains("Verdadeiro", b.TextContent);
    }

    [Fact]
    public void Node_InPort_RenderedOnlyWhenHasInPort()
    {
        var trigger = Node("t") with { HasInPort = false };
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Nodes, [trigger, Node("n")]));

        Assert.Null(cut.Find("[data-dgnode='t']").QuerySelector("[data-port-in]"));
        Assert.NotNull(cut.Find("[data-dgnode='n']").QuerySelector("[data-port-in]"));
    }

    [Fact]
    public void Edges_RenderBezierPathWithExpectedGeometry()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.Edges, OneEdge));

        var path = cut.Find("path[data-dgedge='e1']");
        // out anchor of "a": x=0+220=220, y=0+52+13=65; in anchor of "b": x=300, y=26
        var d = path.GetAttribute("d")!;
        Assert.StartsWith("M 220 65 C", d);
        Assert.EndsWith("300 26", d);
    }

    [Fact]
    public void Edge_SuccessPort_GetsTrueClass()
    {
        var edges = new[] { new DiagramEdge("e2", "b", "Verdadeiro", "a") };
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.Edges, edges));

        var path = cut.Find("path[data-dgedge='e2']");
        Assert.Contains("omni-diagram-edge-true", path.ClassList);
    }

    [Fact]
    public void Selection_AppliesSelectedClassToNodeAndEdge()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.Edges, OneEdge)
            .Add(x => x.Selection, new DiagramSelection(["a"], "e1")));

        Assert.Contains("omni-diagram-node-selected", cut.Find("[data-dgnode='a']").ClassList);
        Assert.Contains("omni-diagram-edge-selected", cut.Find("path[data-dgedge='e1']").ClassList);
    }

    [Fact]
    public void RunState_AppliesNodeAndEdgeStateClasses()
    {
        var run = new DiagramRunState
        {
            Current = "a",
            Done = ["b"],
            TakenEdges = ["e1"],
        };
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.Edges, OneEdge)
            .Add(x => x.RunState, run));

        Assert.Contains("omni-diagram-node-current", cut.Find("[data-dgnode='a']").ClassList);
        Assert.Contains("omni-diagram-node-done", cut.Find("[data-dgnode='b']").ClassList);
        Assert.Contains("omni-diagram-edge-run-done", cut.Find("path[data-dgedge='e1']").ClassList);
        // badges: loader no atual, check no concluído
        Assert.NotNull(cut.Find("[data-dgnode='a']").QuerySelector(".omni-diagram-node-badge"));
        Assert.NotNull(cut.Find("[data-dgnode='b']").QuerySelector(".omni-diagram-node-badge"));
    }

    [Fact]
    public void RunState_ActiveEdge_RendersToken()
    {
        var run = new DiagramRunState { ActiveEdge = "e1" };
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.Edges, OneEdge)
            .Add(x => x.RunState, run));

        Assert.NotNull(cut.Find("circle.omni-diagram-token"));
        Assert.Contains("omni-diagram-edge-run-active", cut.Find("path[data-dgedge='e1']").ClassList);
    }

    [Fact]
    public void ReadOnly_AddsModifierClass_AndHidesAutoLayout()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.ReadOnly, true));

        Assert.Contains("omni-diagram-readonly", cut.Find(".omni-diagram").ClassList);
        Assert.Empty(cut.FindAll("[title='Auto-layout (organizar)']"));
    }

    [Fact]
    public void EmptyContent_ShownOnlyWithoutNodes()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.EmptyContent, b => b.AddMarkupContent(0, "<span id='vazio'>Canvas vazio</span>")));
        Assert.NotNull(cut.Find("#vazio"));

        cut.SetParametersAndRender(p => p.Add(x => x.Nodes, TwoNodes));
        Assert.Empty(cut.FindAll("#vazio"));
    }

    [Fact]
    public void NodeTemplate_ReplacesDefaultCardContent_PortsRemain()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.NodeTemplate, node => b => b.AddMarkupContent(0, $"<em class='tpl'>{node.Title}</em>")));

        var a = cut.Find("[data-dgnode='a']");
        Assert.NotNull(a.QuerySelector("em.tpl"));
        Assert.Null(a.QuerySelector(".omni-diagram-node-head"));
        Assert.NotNull(a.QuerySelector("[data-port='Done']"));
    }

    [Fact]
    public void Minimap_RendersNodesAndViewRect()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p.Add(x => x.Nodes, TwoNodes));

        var mm = cut.Find(".omni-diagram-minimap svg");
        Assert.Equal(2, mm.QuerySelectorAll(".omni-diagram-mmnode").Length);
        Assert.NotNull(mm.QuerySelector(".omni-diagram-mmview"));
        Assert.NotNull(mm.GetAttribute("data-scale"));
    }

    [Fact]
    public void Minimap_HiddenWhenDisabledOrEmpty()
    {
        var cut = RenderComponent<OmniDiagramCanvas>();
        Assert.Empty(cut.FindAll(".omni-diagram-minimap"));

        cut.SetParametersAndRender(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.ShowMinimap, false));
        Assert.Empty(cut.FindAll(".omni-diagram-minimap"));
    }

    [Fact]
    public void ZoomLabel_ReflectsViewportZoom()
    {
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Viewport, new DiagramViewport(0, 0, 1.0)));
        Assert.Equal("100%", cut.Find(".omni-diagram-zoomlabel").TextContent.Trim());
    }

    [Fact]
    public async Task JsSelect_RaisesSelectionChanged()
    {
        DiagramSelection? received = null;
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.SelectionChanged, s => received = s));

        await cut.InvokeAsync(() => cut.Instance.JsSelect(["a", "b"], null));

        Assert.NotNull(received);
        Assert.Equal(2, received!.NodeIds.Count);
        Assert.Contains("omni-diagram-node-selected", cut.Find("[data-dgnode='a']").ClassList);
    }

    [Fact]
    public async Task JsMoveNodes_RaisesNodesMoved()
    {
        IReadOnlyList<DiagramNodeMove>? moves = null;
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.NodesMoved, m => moves = m));

        await cut.InvokeAsync(() => cut.Instance.JsMoveNodes([new DiagramNodeMove("a", 10, 20)]));

        Assert.NotNull(moves);
        Assert.Equal(10, moves![0].X);
    }

    [Fact]
    public async Task JsConnect_RaisesOnConnect()
    {
        DiagramConnectEventArgs? args = null;
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.OnConnect, a => args = a));

        await cut.InvokeAsync(() => cut.Instance.JsConnect("a", "Done", "b"));

        Assert.NotNull(args);
        Assert.Equal("a", args!.SourceId);
        Assert.Equal("In", args.TargetPort);
    }

    [Fact]
    public async Task JsDeleteRequested_OnlyFiresWithSelection()
    {
        var fired = 0;
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.Nodes, TwoNodes)
            .Add(x => x.OnDeleteRequested, _ => fired++));

        await cut.InvokeAsync(() => cut.Instance.JsDeleteRequested());
        Assert.Equal(0, fired);

        await cut.InvokeAsync(() => cut.Instance.JsSelect(["a"], null));
        await cut.InvokeAsync(() => cut.Instance.JsDeleteRequested());
        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task JsExternalDrop_RaisesOnExternalDropWithWorldCoords()
    {
        DiagramExternalDropEventArgs? args = null;
        var cut = RenderComponent<OmniDiagramCanvas>(p => p
            .Add(x => x.OnExternalDrop, a => args = a));

        await cut.InvokeAsync(() => cut.Instance.JsExternalDrop("Omni.SendEmail", 123.4, 56.7));

        Assert.NotNull(args);
        Assert.Equal("Omni.SendEmail", args!.Payload);
        Assert.Equal(123.4, args.X);
    }

    // ───────────────────────── DiagramGeometry ─────────────────────────

    [Fact]
    public void Geometry_NodeHeight_FollowsPortCount()
    {
        Assert.Equal(52 + 2 * 26 + 10, DiagramGeometry.NodeHeight(TwoNodes[1]));
        Assert.Equal(52 + 12, DiagramGeometry.NodeHeight(Node("x") with { OutPorts = [] }));
    }

    [Fact]
    public void Geometry_BezierPath_UsesMinimum45Tangent()
    {
        var d = DiagramGeometry.BezierPath(0, 0, 10, 0);
        Assert.Equal("M 0 0 C 45 0, -35 0, 10 0", d);
    }

    [Fact]
    public void Geometry_AutoLayout_LayersLeftToRight()
    {
        var nodes = new[]
        {
            Node("start") with { HasInPort = false },
            Node("mid"),
            Node("end"),
        };
        var edges = new[]
        {
            new DiagramEdge("e1", "start", "Done", "mid"),
            new DiagramEdge("e2", "mid", "Done", "end"),
        };

        var moves = DiagramGeometry.AutoLayout(nodes, edges).ToDictionary(m => m.Id);

        Assert.Equal(60, moves["start"].X);
        Assert.Equal(60 + 300, moves["mid"].X);
        Assert.Equal(60 + 600, moves["end"].X);
        Assert.All(moves.Values, m => Assert.Equal(60, m.Y));
    }

    [Fact]
    public void Geometry_AutoLayout_StacksSameLayerByRow()
    {
        var nodes = new[] { Node("a") with { HasInPort = false }, Node("b") with { HasInPort = false } };
        var moves = DiagramGeometry.AutoLayout(nodes, []).ToDictionary(m => m.Id);

        Assert.Equal(60, moves["a"].Y);
        Assert.Equal(60 + 150, moves["b"].Y);
    }
}
