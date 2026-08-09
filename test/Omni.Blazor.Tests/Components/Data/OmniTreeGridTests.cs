using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public class OmniTreeGridTests : TestContextBase
{
    [Fact]
    public void Renders_in_memory_hierarchy_columns_and_cross_cutting_attributes()
    {
        var roots = CreateTree();
        var cut = RenderGrid(roots, parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, node => node.Children.Count > 0)
            .Add(component => component.InitiallyExpanded, node => node.Id == "root")
            .Add(component => component.Class, "custom-tree-grid")
            .Add(component => component.Style, "max-height: 30rem")
            .AddUnmatched("data-testid", "tree-grid"));

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find("section.omni-tree-grid");
            Assert.Contains("custom-tree-grid", root.ClassName);
            Assert.Equal("max-height: 30rem", root.GetAttribute("style"));
            Assert.Equal("tree-grid", root.GetAttribute("data-testid"));
            Assert.Equal(3, cut.FindAll(".omni-tree-grid-row").Count);
            Assert.Equal(2, cut.FindAll("thead th").Count);

            // Ausente, aria-selected significaria "linha não selecionável".
            Assert.All(cut.FindAll(".omni-tree-grid-row"),
                linha => Assert.Equal("false", linha.GetAttribute("aria-selected")));
        });
    }

    [Fact]
    public void Toggle_collapses_and_expands_descendants()
    {
        var roots = CreateTree();
        var cut = RenderGrid(roots, parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, node => node.Children.Count > 0)
            .Add(component => component.InitiallyExpanded, node => node.Id == "root"));

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".omni-tree-grid-row").Count));
        cut.Find(".omni-tree-grid-toggle").Click();
        Assert.Single(cut.FindAll(".omni-tree-grid-row"));

        cut.Find(".omni-tree-grid-toggle").Click();
        Assert.Equal(3, cut.FindAll(".omni-tree-grid-row").Count);
    }

    [Fact]
    public void Row_selection_supports_two_way_callback()
    {
        Node? selected = null;
        var cut = RenderGrid(CreateTree(), parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.SelectedItemChanged,
                EventCallback.Factory.Create<Node?>(this, node => selected = node)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".omni-tree-grid-row")));
        cut.Find(".omni-tree-grid-row").Click();

        Assert.Equal("root", selected?.Id);
        Assert.Contains("omni-tree-grid-row-selected", cut.Find(".omni-tree-grid-row").ClassName);
    }

    [Fact]
    public async Task Concurrent_expands_share_one_lazy_request()
    {
        var root = new Node("root", "Raiz");
        var completion = new TaskCompletionSource<IReadOnlyList<Node>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        HierarchyChildrenProvider<Node> provider = (_, _) =>
        {
            calls++;
            return new(completion.Task);
        };

        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.ChildrenProvider, provider));

        Task first = Task.CompletedTask;
        Task second = Task.CompletedTask;
        await cut.InvokeAsync(() =>
        {
            first = cut.Instance.ExpandAsync(root);
            second = cut.Instance.ExpandAsync(root);
        });

        Assert.Equal(1, calls);
        completion.SetResult([new("child", "Filho")]);
        await Task.WhenAll(first, second);

        Assert.Equal(1, calls);
        Assert.Equal(2, cut.Instance.VisibleRowCount);
    }

    [Fact]
    public async Task Collapse_cancels_pending_node_load()
    {
        var root = new Node("root", "Raiz");
        CancellationToken observed = default;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        HierarchyChildrenProvider<Node> provider = async (_, token) =>
        {
            observed = token;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return [];
        };

        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.ChildrenProvider, provider));

        Task expansion = Task.CompletedTask;
        await cut.InvokeAsync(() => { expansion = cut.Instance.ExpandAsync(root); });
        await started.Task;
        await cut.InvokeAsync(() => cut.Instance.CollapseAsync(root));
        await expansion;

        Assert.True(observed.IsCancellationRequested);
        Assert.False(cut.Instance.IsLoading);
    }

    [Fact]
    public void Visible_row_limit_bounds_pathological_hierarchies()
    {
        var children = Enumerable.Range(0, 20)
            .Select(index => new Node(index.ToString(), $"Filho {index}"))
            .ToList();
        var root = new Node("root", "Raiz", children);
        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, node => node.Children.Count > 0)
            .Add(component => component.InitiallyExpanded, node => node.Id == "root")
            .Add(component => component.MaxVisibleRows, 3));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindAll(".omni-tree-grid-row").Count);
            Assert.NotNull(cut.Find(".omni-tree-grid-limit"));
        });
    }

    [Fact]
    public void Controlled_expanded_keys_trigger_lazy_loading_after_render()
    {
        var root = new Node("root", "Raiz");
        var calls = 0;
        HierarchyChildrenProvider<Node> provider = (_, _) =>
        {
            calls++;
            return ValueTask.FromResult<IReadOnlyList<Node>>([new("child", "Filho")]);
        };

        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.ChildrenProvider, provider)
            .Add(component => component.ExpandedKeys, new object[] { "root" }));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, calls);
            Assert.Equal(2, cut.Instance.VisibleRowCount);
        });
    }

    [Fact]
    public async Task Lazy_cache_evicts_least_recently_used_parent()
    {
        var firstRoot = new Node("first", "Primeiro");
        var secondRoot = new Node("second", "Segundo");
        var calls = new Dictionary<string, int>();
        HierarchyChildrenProvider<Node> provider = (parent, _) =>
        {
            calls[parent.Id] = calls.GetValueOrDefault(parent.Id) + 1;
            return ValueTask.FromResult<IReadOnlyList<Node>>(
                [new($"{parent.Id}-child", $"Filho de {parent.Name}")]);
        };

        var cut = RenderGrid([firstRoot, secondRoot], parameters => parameters
            .Add(component => component.HasChildren, node => !node.Id.EndsWith("-child", StringComparison.Ordinal))
            .Add(component => component.ChildrenProvider, provider)
            .Add(component => component.MaxCachedNodes, 1)
            .Add(component => component.MaxCachedItems, 10));

        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(firstRoot));
        await cut.InvokeAsync(() => cut.Instance.CollapseAsync(firstRoot));
        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(secondRoot));
        await cut.InvokeAsync(() => cut.Instance.CollapseAsync(secondRoot));
        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(firstRoot));

        Assert.Equal(2, calls["first"]);
        Assert.Equal(1, cut.Instance.CachedNodeCount);
        Assert.Equal(1, cut.Instance.CachedItemCount);
    }

    [Fact]
    public void Duplicate_keys_and_cycles_do_not_duplicate_visible_rows()
    {
        var root = new Node("root", "Raiz");
        root.Children.Add(root);
        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.InitiallyExpanded, _ => true));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".omni-tree-grid-row"));
            Assert.Equal(1, cut.Instance.VisibleRowCount);
        });
    }

    [Fact]
    public async Task Pending_lazy_loads_respect_the_parallelism_limit()
    {
        Node[] roots = Enumerable.Range(0, 6)
            .Select(index => new Node($"root-{index}", $"Raiz {index}"))
            .ToArray();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var twoStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximum = 0;
        var calls = 0;
        HierarchyChildrenProvider<Node> provider = async (node, token) =>
        {
            Interlocked.Increment(ref calls);
            var current = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximum, current);
            if (current >= 2) twoStarted.TrySetResult();
            try
            {
                await release.Task.WaitAsync(token);
                return [new($"{node.Id}-child", "Filho")];
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        };

        var cut = RenderGrid(roots, parameters => parameters
            .Add(component => component.HasChildren, node => !node.Id.EndsWith("-child", StringComparison.Ordinal))
            .Add(component => component.ChildrenProvider, provider)
            .Add(component => component.ExpandedKeys, roots.Select(node => (object)node.Id).ToArray())
            .Add(component => component.MaxConcurrentLoads, 2));

        await twoStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(2, Volatile.Read(ref maximum));
        Assert.Equal(2, Volatile.Read(ref calls));

        release.TrySetResult();
        cut.WaitForAssertion(() => Assert.Equal(12, cut.Instance.VisibleRowCount));
        Assert.Equal(6, calls);
    }

    [Fact]
    public async Task Dispose_cancels_pending_load_and_makes_late_completion_harmless()
    {
        var root = new Node("root", "Raiz");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        HierarchyChildrenProvider<Node> provider = async (_, token) =>
        {
            observed = token;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return [];
        };
        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.ChildrenProvider, provider));

        Task expansion = Task.CompletedTask;
        await cut.InvokeAsync(() => { expansion = cut.Instance.ExpandAsync(root); });
        await started.Task.WaitAsync(
            TimeSpan.FromSeconds(3),
            Xunit.TestContext.Current.CancellationToken);
        await cut.InvokeAsync(cut.Instance.Dispose);
        await expansion.WaitAsync(
            TimeSpan.FromSeconds(3),
            Xunit.TestContext.Current.CancellationToken);

        Assert.True(observed.IsCancellationRequested);
    }

    [Fact]
    public async Task Failed_load_is_observed_and_retry_replaces_the_error_row()
    {
        var root = new Node("root", "Raiz");
        var calls = 0;
        Exception? captured = null;
        HierarchyChildrenProvider<Node> provider = (_, _) =>
        {
            calls++;
            if (calls == 1) throw new InvalidOperationException("Falha simulada");
            return ValueTask.FromResult<IReadOnlyList<Node>>([new("child", "Filho")]);
        };
        var cut = RenderGrid([root], parameters => parameters
            .Add(component => component.HasChildren, node => node.Id == "root")
            .Add(component => component.ChildrenProvider, provider)
            .Add(component => component.LoadFailed,
                EventCallback.Factory.Create<Exception>(this, exception => captured = exception)));

        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(root));
        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal(1, calls);
        Assert.Equal(1, cut.Instance.ErrorCount);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".omni-tree-grid-error-row")));

        cut.Find(".omni-tree-grid-error-row button").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".omni-tree-grid-error-row"));
            Assert.Equal(2, cut.Instance.VisibleRowCount);
        });
        Assert.Equal(2, calls);
    }

    private IRenderedComponent<OmniTreeGrid<Node>> RenderGrid(
        IEnumerable<Node> items,
        Action<ComponentParameterCollectionBuilder<OmniTreeGrid<Node>>>? configure = null)
    {
        return Render<OmniTreeGrid<Node>>(parameters =>
        {
            parameters
                .Add(component => component.Items, items)
                .Add(component => component.KeySelector, node => node.Id)
                .Add(component => component.Columns, Columns());
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<OmniTreeGridColumn<Node>>(0);
        builder.AddAttribute(1, nameof(OmniTreeGridColumn<Node>.Title), "Nome");
        builder.AddAttribute(2, nameof(OmniTreeGridColumn<Node>.TextSelector),
            (Func<Node, string?>)(node => node.Name));
        builder.AddAttribute(3, nameof(OmniTreeGridColumn<Node>.IsHierarchyAnchor), true);
        builder.CloseComponent();

        builder.OpenComponent<OmniTreeGridColumn<Node>>(4);
        builder.AddAttribute(5, nameof(OmniTreeGridColumn<Node>.Title), "Código");
        builder.AddAttribute(6, nameof(OmniTreeGridColumn<Node>.TextSelector),
            (Func<Node, string?>)(node => node.Id));
        builder.CloseComponent();
    };

    private static Node[] CreateTree() =>
    [
        new("root", "Raiz",
        [
            new("one", "Primeiro"),
            new("two", "Segundo")
        ])
    ];

    private sealed record Node(string Id, string Name, List<Node>? ChildNodes = null)
    {
        public List<Node> Children { get; } = ChildNodes ?? [];
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        var observed = Volatile.Read(ref maximum);
        while (candidate > observed)
        {
            var original = Interlocked.CompareExchange(ref maximum, candidate, observed);
            if (original == observed) return;
            observed = original;
        }
    }
}
