using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniPickList{TItem}"/>: the dual-list
/// layout, item rendering via the embedded listboxes, the four transfer buttons
/// (selected/all × each direction), filtering, orientation, disabled, empty
/// states, and the cross-cutting splat.
/// </summary>
public class OmniPickListTests : TestContextBase
{
    private IRenderedComponent<OmniPickList<string>> Render(
        string[]? source = null, string[]? target = null,
        Action<ComponentParameterCollectionBuilder<OmniPickList<string>>>? extra = null)
        => RenderComponent<OmniPickList<string>>(p =>
        {
            p.Add(c => c.Source, (IEnumerable<string>?)(source ?? new[] { "A", "B", "C" }));
            p.Add(c => c.Target, (IEnumerable<string>?)(target ?? Array.Empty<string>()));
            extra?.Invoke(p);
        });

    [Fact]
    public void Renders_two_panes_and_buttons()
    {
        var cut = Render();
        Assert.NotNull(cut.Find(".omni-picklist"));
        Assert.Equal(2, cut.FindAll(".omni-picklist-pane").Count);
        Assert.NotNull(cut.Find(".omni-picklist-buttons"));
        Assert.Equal(4, cut.FindAll(".omni-picklist-btn").Count); // selected×2 + all×2
    }

    [Fact]
    public void Renders_source_and_target_items()
    {
        var cut = Render(source: new[] { "A", "B", "C" }, target: new[] { "X", "Y" });
        var panes = cut.FindAll(".omni-picklist-pane");
        Assert.Equal(3, panes[0].QuerySelectorAll(".omni-listbox-item").Length);
        Assert.Equal(2, panes[1].QuerySelectorAll(".omni-listbox-item").Length);
    }

    [Fact]
    public void Headers_render_with_titles()
    {
        var cut = Render(extra: p => p.Add(c => c.SourceTitle, "Origem").Add(c => c.TargetTitle, "Destino"));
        var headers = cut.FindAll(".omni-picklist-header");
        Assert.Equal(2, headers.Count);
        Assert.Contains("Origem", headers[0].TextContent);
        Assert.Contains("Destino", headers[1].TextContent);
    }

    [Fact]
    public void Move_all_to_target_empties_source()
    {
        IEnumerable<string>? newSrc = null, newTgt = null;
        var cut = Render(source: new[] { "A", "B", "C" }, target: new[] { "X" }, extra: p => p
            .Add(c => c.SourceChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newSrc = v))
            .Add(c => c.TargetChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newTgt = v)));

        cut.FindAll(".omni-picklist-btn")[2].Click(); // all → target

        Assert.Empty(newSrc!);
        Assert.Equal(new[] { "X", "A", "B", "C" }, newTgt);
    }

    [Fact]
    public void Move_all_to_source_empties_target()
    {
        IEnumerable<string>? newSrc = null, newTgt = null;
        var cut = Render(source: new[] { "A" }, target: new[] { "X", "Y" }, extra: p => p
            .Add(c => c.SourceChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newSrc = v))
            .Add(c => c.TargetChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newTgt = v)));

        cut.FindAll(".omni-picklist-btn")[3].Click(); // all → source

        Assert.Equal(new[] { "A", "X", "Y" }, newSrc);
        Assert.Empty(newTgt!);
    }

    [Fact]
    public void Move_selected_to_target_moves_only_selection()
    {
        IEnumerable<string>? newSrc = null, newTgt = null;
        var cut = Render(source: new[] { "A", "B", "C" }, target: Array.Empty<string>(), extra: p => p
            .Add(c => c.SourceChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newSrc = v))
            .Add(c => c.TargetChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => newTgt = v)));

        // select "B" in the source list
        var srcPane = cut.FindAll(".omni-picklist-pane")[0];
        srcPane.QuerySelectorAll(".omni-listbox-item")[1].Click();

        // selected → target (first button)
        cut.FindAll(".omni-picklist-btn")[0].Click();

        Assert.Equal(new[] { "B" }, newTgt);
        Assert.Equal(new[] { "A", "C" }, newSrc);
    }

    [Fact]
    public void Selected_buttons_disabled_without_selection()
    {
        var cut = Render();
        var btns = cut.FindAll(".omni-picklist-btn");
        Assert.True(btns[0].HasAttribute("disabled")); // selected → target
        Assert.True(btns[1].HasAttribute("disabled")); // selected → source
    }

    [Fact]
    public void AllowMoveAll_false_hides_double_arrow_buttons()
    {
        var cut = Render(extra: p => p.Add(c => c.AllowMoveAll, false));
        Assert.Equal(2, cut.FindAll(".omni-picklist-btn").Count);
    }

    [Fact]
    public void AllowFiltering_renders_filter_inputs()
    {
        Assert.Empty(Render().FindAll(".omni-picklist-filter"));
        Assert.Equal(2, Render(extra: p => p.Add(c => c.AllowFiltering, true)).FindAll(".omni-picklist-filter").Count);
    }

    [Fact]
    public void Filter_narrows_source_items()
    {
        var cut = Render(source: new[] { "Apple", "Apricot", "Banana" },
                         extra: p => p.Add(c => c.AllowFiltering, true));
        cut.FindAll(".omni-picklist-filter")[0].Input("ap");
        var srcPane = cut.FindAll(".omni-picklist-pane")[0];
        Assert.Equal(2, srcPane.QuerySelectorAll(".omni-listbox-item").Length); // Apple, Apricot
    }

    [Fact]
    public void Orientation_vertical_adds_class_and_swaps_icons()
    {
        var cut = Render(extra: p => p.Add(c => c.Orientation, Omni.Blazor.Models.Orientation.Vertical));
        Assert.Contains("omni-picklist-vertical", cut.Find(".omni-picklist").ClassName);
        // vertical uses up/down chevrons (rendered as omni-icon names)
        Assert.NotNull(cut.Find(".omni-picklist-btn"));
    }

    [Fact]
    public void Disabled_adds_class()
    {
        Assert.Contains("omni-picklist-disabled", Render(extra: p => p.Add(c => c.Disabled, true)).Find(".omni-picklist").ClassName);
    }

    [Fact]
    public void Empty_source_shows_empty_text()
    {
        var cut = Render(source: Array.Empty<string>(), target: new[] { "X" },
                         extra: p => p.Add(c => c.SourceEmptyText, "Nada aqui"));
        var empty = cut.Find(".omni-picklist-empty");
        Assert.Contains("Nada aqui", empty.TextContent);
    }

    [Fact]
    public void Move_raises_Moved_with_direction_and_items()
    {
        PickListMoveEventArgs<string>? args = null;
        var cut = Render(source: new[] { "A", "B" }, target: Array.Empty<string>(), extra: p => p
            .Add(c => c.Moved, EventCallback.Factory.Create<PickListMoveEventArgs<string>>(this, a => args = a)));

        cut.FindAll(".omni-picklist-btn")[2].Click(); // all → target

        Assert.NotNull(args);
        Assert.True(args!.ToTarget);
        Assert.Equal(new[] { "A", "B" }, args.Items);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render(extra: p => p
            .Add(c => c.Class, "transfer")
            .Add(c => c.Style, "gap:20px")
            .AddUnmatched("data-testid", "pl1"));
        var root = cut.Find(".omni-picklist");
        Assert.Contains("transfer", root.ClassName);
        Assert.Contains("gap:20px", root.GetAttribute("style") ?? "");
        Assert.Equal("pl1", root.GetAttribute("data-testid"));
    }
}
