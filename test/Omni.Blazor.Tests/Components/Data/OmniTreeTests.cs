using System.Collections;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniTree"/> / <see cref="OmniTreeItem"/> /
/// <see cref="OmniTreeLevel"/>: inline + data-bound rendering, expand/collapse,
/// single selection, tri-state checkbox cascade, icons, and the cross-cutting splat.
/// </summary>
public class OmniTreeTests : TestContextBase
{
    public record Node(string Name, List<Node>? Children = null);

    private static List<Node> Sample() => new()
    {
        new("Fruits", new() { new("Apple"), new("Banana") }),
        new("Veggies", new() { new("Carrot") }),
    };

    // Two-level data-bound fragment (Name / Children).
    private static RenderFragment Levels(bool expanded = false) => b =>
    {
        b.OpenComponent<OmniTreeLevel>(0);
        b.AddAttribute(1, nameof(OmniTreeLevel.TextProperty), "Name");
        b.AddAttribute(2, nameof(OmniTreeLevel.ChildrenProperty), "Children");
        if (expanded) b.AddAttribute(3, nameof(OmniTreeLevel.Expanded), (Func<object, bool>)(_ => true));
        b.CloseComponent();

        b.OpenComponent<OmniTreeLevel>(10);
        b.AddAttribute(11, nameof(OmniTreeLevel.TextProperty), "Name");
        b.AddAttribute(12, nameof(OmniTreeLevel.HasChildren), (Func<object, bool>)(_ => false));
        b.CloseComponent();
    };

    private IRenderedComponent<OmniTree> RenderDataTree(
        Action<ComponentParameterCollectionBuilder<OmniTree>>? extra = null, bool expanded = false)
        => Render<OmniTree>(p =>
        {
            p.Add(t => t.Data, Sample());
            p.Add(t => t.ChildContent, Levels(expanded));
            extra?.Invoke(p);
        });

    // ─── Cross-cutting ────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_with_tree_role()
    {
        var cut = RenderDataTree();
        var root = cut.Find("div.omni-tree");
        Assert.Equal("tree", root.GetAttribute("role"));
    }

    [Fact]
    public void Appends_consumer_Class_and_splats_attributes()
    {
        var cut = RenderDataTree(p => p
            .Add(t => t.Class, "my-tree")
            .AddUnmatched("data-testid", "tr1"));
        var root = cut.Find("div.omni-tree");
        Assert.Contains("my-tree", root.ClassName);
        Assert.Equal("tr1", root.GetAttribute("data-testid"));
    }

    // ─── Data-bound render ────────────────────────────────────────────────

    [Fact]
    public void Renders_root_nodes_from_data()
    {
        var cut = RenderDataTree();
        var texts = cut.FindAll(".omni-tree-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Fruits", texts);
        Assert.Contains("Veggies", texts);
        // Children collapsed by default.
        Assert.DoesNotContain("Apple", texts);
    }

    [Fact]
    public void Expanded_level_shows_children()
    {
        var cut = RenderDataTree(expanded: true);
        var texts = cut.FindAll(".omni-tree-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Apple", texts);
        Assert.Contains("Carrot", texts);
    }

    [Fact]
    public void Clicking_toggle_expands_a_node()
    {
        var cut = RenderDataTree();
        Assert.DoesNotContain("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));

        // First node (Fruits) toggle.
        cut.FindAll(".omni-tree-toggle").First().Click();

        Assert.Contains("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Leaf_nodes_have_no_toggle_chevron()
    {
        var cut = RenderDataTree(expanded: true);
        // Find the node whose text is "Apple" and assert its content has the empty toggle.
        var appleContent = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Apple");
        Assert.NotNull(appleContent.QuerySelector(".omni-tree-toggle-empty"));
    }

    // ─── Selection ────────────────────────────────────────────────────────

    [Fact]
    public void Clicking_a_node_selects_it_and_fires_events()
    {
        TreeEventArgs? captured = null;
        var cut = RenderDataTree(p => p.Add(t => t.SelectionChanged,
            EventCallback.Factory.Create<TreeEventArgs>(this, e => captured = e)));

        var fruits = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Fruits");
        fruits.Click();

        Assert.NotNull(captured);
        Assert.Equal("Fruits", captured!.Text);
        // The selected class is applied.
        Assert.Contains(cut.FindAll(".omni-tree-content-selected"),
            c => c.QuerySelector(".omni-tree-text")?.TextContent == "Fruits");
    }

    // ─── Checkboxes ───────────────────────────────────────────────────────

    [Fact]
    public void AllowCheckboxes_renders_a_checkbox_per_node()
    {
        var cut = RenderDataTree(p => p.Add(t => t.AllowCheckboxes, true));
        Assert.Contains("omni-tree-checkable", cut.Find(".omni-tree").ClassName);
        Assert.Equal(2, cut.FindAll(".omni-tree-check").Count); // two root nodes
    }

    [Fact]
    public void Checking_a_parent_cascades_to_all_descendants()
    {
        IEnumerable<object>? checkedValues = null;
        var cut = RenderDataTree(p => p
            .Add(t => t.AllowCheckboxes, true)
            .Add(t => t.CheckedValuesChanged,
                EventCallback.Factory.Create<IEnumerable<object>>(this, v => checkedValues = v)));

        // Check the first root (Fruits) — cascade is data-derived, so children
        // count even while collapsed.
        cut.FindAll(".omni-tree-check").First().Click();

        Assert.NotNull(checkedValues);
        var names = checkedValues!.OfType<Node>().Select(n => n.Name).ToList();
        Assert.Contains("Fruits", names);
        Assert.Contains("Apple", names);
        Assert.Contains("Banana", names);
        Assert.DoesNotContain("Veggies", names);
    }

    [Fact]
    public void Parent_is_indeterminate_when_only_some_children_checked()
    {
        var cut = RenderDataTree(p => p.Add(t => t.AllowCheckboxes, true), expanded: true);

        // Check one leaf under Fruits (Apple) → Fruits becomes indeterminate.
        var appleCheck = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Apple")
            .QuerySelector(".omni-tree-check")!;
        appleCheck.Click();

        var fruitsCheck = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Fruits")
            .QuerySelector(".omni-tree-check")!;
        Assert.Contains("omni-indeterminate", fruitsCheck.ClassName);
    }

    // ─── Inline + icons ───────────────────────────────────────────────────

    [Fact]
    public void Inline_items_render_with_icon_and_nest_when_expanded()
    {
        RenderFragment inline = b =>
        {
            b.OpenComponent<OmniTreeItem>(0);
            b.AddAttribute(1, nameof(OmniTreeItem.Text), "Root");
            b.AddAttribute(2, nameof(OmniTreeItem.Icon), "folder");
            b.AddAttribute(3, nameof(OmniTreeItem.Expanded), true);
            b.AddAttribute(4, nameof(OmniTreeItem.ChildContent), (RenderFragment)(cb =>
            {
                cb.OpenComponent<OmniTreeItem>(0);
                cb.AddAttribute(1, nameof(OmniTreeItem.Text), "Child");
                cb.CloseComponent();
            }));
            b.CloseComponent();
        };

        var cut = Render<OmniTree>(p => p.Add(t => t.ChildContent, inline));

        var texts = cut.FindAll(".omni-tree-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Root", texts);
        Assert.Contains("Child", texts); // expanded
        Assert.NotNull(cut.Find(".omni-tree-icon")); // folder icon rendered
    }

    // ─── Accessibility / keyboard navigation ──────────────────────────────

    [Fact]
    public void Uses_roving_tabindex_single_tab_stop()
    {
        var cut = RenderDataTree();
        var contents = cut.FindAll(".omni-tree-content");
        Assert.Equal("0", contents[0].GetAttribute("tabindex"));
        Assert.All(contents.Skip(1), c => Assert.Equal("-1", c.GetAttribute("tabindex")));
    }

    [Fact]
    public void Nodes_expose_aria_level_and_role()
    {
        var cut = RenderDataTree(expanded: true);
        Assert.Equal("tree", cut.Find(".omni-tree").GetAttribute("role"));
        var fruits = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Fruits");
        var apple = cut.FindAll(".omni-tree-content")
            .First(c => c.QuerySelector(".omni-tree-text")?.TextContent == "Apple");
        Assert.Equal("treeitem", fruits.GetAttribute("role"));
        Assert.Equal("1", fruits.GetAttribute("aria-level"));
        Assert.Equal("2", apple.GetAttribute("aria-level"));
    }

    [Fact]
    public void ArrowDown_moves_the_tab_stop_to_the_next_node()
    {
        var cut = RenderDataTree();
        cut.Find(".omni-tree").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        var contents = cut.FindAll(".omni-tree-content");
        Assert.Equal("-1", contents[0].GetAttribute("tabindex"));
        Assert.Equal("0", contents[1].GetAttribute("tabindex")); // Veggies is now the tab stop
    }

    [Fact]
    public void ArrowRight_expands_the_focused_node()
    {
        var cut = RenderDataTree();
        Assert.DoesNotContain("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));
        cut.Find(".omni-tree").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Contains("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));
    }

    [Fact]
    public void ArrowLeft_collapses_the_focused_node()
    {
        var cut = RenderDataTree(expanded: true);
        Assert.Contains("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));
        cut.Find(".omni-tree").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.DoesNotContain("Apple", cut.FindAll(".omni-tree-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Enter_selects_the_focused_node()
    {
        TreeEventArgs? captured = null;
        var cut = RenderDataTree(p => p.Add(t => t.SelectionChanged,
            EventCallback.Factory.Create<TreeEventArgs>(this, e => captured = e)));
        cut.Find(".omni-tree").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.NotNull(captured);
        Assert.Equal("Fruits", captured!.Text); // first node focused by default
    }

    [Fact]
    public void End_moves_the_tab_stop_to_the_last_visible_node()
    {
        var cut = RenderDataTree();
        cut.Find(".omni-tree").KeyDown(new KeyboardEventArgs { Key = "End" });
        var contents = cut.FindAll(".omni-tree-content");
        Assert.Equal("0", contents[^1].GetAttribute("tabindex")); // Veggies (last root)
    }
}
