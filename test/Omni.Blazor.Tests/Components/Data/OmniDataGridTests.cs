using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDataGrid{TItem}"/>: basic render of
/// headers + rows, empty state, embed mode, and cross-cutting splat. Exhaustive
/// sort/filter/group is covered elsewhere — this only confirms the Class/Style/
/// Attributes surface and that columns are honored.
/// </summary>
public class OmniDataGridTests : TestContextBase
{
    private record Person(string Name, int Age);

    private static readonly Person[] Sample =
    {
        new("Alice", 30),
        new("Bob",   25),
        new("Carol", 41)
    };

    private static RenderFragment ColumnsFragment() => b =>
    {
        b.OpenComponent<OmniDataGridColumn<Person>>(0);
        b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
        b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Person>>(3);
        b.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Title), "Age");
        b.AddAttribute(5, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Age));
        b.CloseComponent();
    };

    [Fact]
    public void Renders_root_div_with_base_class()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var root = cut.Find("div.omni-grid");
        Assert.Contains("omni-grid", root.ClassName);
    }

    [Fact]
    public void Renders_column_headers()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var headers = cut.FindAll("table.omni-grid-table thead th");
        Assert.Contains(headers, h => h.TextContent.Contains("Name"));
        Assert.Contains(headers, h => h.TextContent.Contains("Age"));
    }

    [Fact]
    public void Renders_data_rows()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var bodyText = cut.Find("table.omni-grid-table tbody").TextContent;
        Assert.Contains("Alice", bodyText);
        Assert.Contains("Bob", bodyText);
        Assert.Contains("Carol", bodyText);
    }

    [Fact]
    public void Renders_EmptyText_when_data_is_empty()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Array.Empty<Person>())
            .Add(c => c.EmptyText, "Sem registros")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("Sem registros", cut.Find(".omni-grid-empty").TextContent);
    }

    [Fact]
    public void Embed_adds_modifier_class()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Embed, true)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("omni-grid-embed", cut.Find("div.omni-grid").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Class, "my-grid")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("my-grid", cut.Find("div.omni-grid").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Style, "border: 1px solid red")
            .Add(c => c.Columns, ColumnsFragment()));

        var style = cut.Find("div.omni-grid").GetAttribute("style") ?? "";
        Assert.Contains("border: 1px solid red", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .AddUnmatched("data-testid", "grid1")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Equal("grid1", cut.Find("div.omni-grid").GetAttribute("data-testid"));
    }

    // ─── Column resize ────────────────────────────────────────────────────

    [Fact]
    public void AllowColumnResize_marks_table_and_renders_handles()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("omni-grid-resizable", cut.Find("table.omni-grid-table").ClassName);
        Assert.Equal(2, cut.FindAll(".omni-grid-resizer").Count);
    }

    [Fact]
    public void No_resize_handles_when_disabled()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Empty(cut.FindAll(".omni-grid-resizer"));
        Assert.DoesNotContain("omni-grid-resizable", cut.Find("table.omni-grid-table").ClassName);
    }

    [Fact]
    public void Column_with_Resizable_false_has_no_handle()
    {
        RenderFragment frag = b =>
        {
            b.OpenComponent<OmniDataGridColumn<Person>>(0);
            b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
            b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
            b.CloseComponent();

            b.OpenComponent<OmniDataGridColumn<Person>>(3);
            b.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Title), "Age");
            b.AddAttribute(5, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Age));
            b.AddAttribute(6, nameof(OmniDataGridColumn<Person>.Resizable), false);
            b.CloseComponent();
        };

        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.Columns, frag));

        // Only the first (resizable) column shows a handle.
        Assert.Single(cut.FindAll(".omni-grid-resizer"));
    }

    [Fact]
    public void Renders_colgroup_col_with_id_per_visible_column()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.Columns, ColumnsFragment()));

        // One <col> per data column (each carries an id), plus the trailing
        // width-less filler <col> that absorbs leftover space.
        var dataCols = cut.FindAll("table.omni-grid-table colgroup col:not(.omni-grid-col-filler)");
        Assert.Equal(2, dataCols.Count);
        Assert.All(dataCols, col => Assert.False(string.IsNullOrEmpty(col.GetAttribute("id"))));
        Assert.Single(cut.FindAll("table.omni-grid-table colgroup col.omni-grid-col-filler"));
    }

    [Fact]
    public void No_filler_column_when_resize_disabled()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Empty(cut.FindAll(".omni-grid-col-filler"));
    }

    [Fact]
    public async Task OnColumnResized_updates_col_width_and_fires_event()
    {
        DataGridColumnResizedEventArgs? captured = null;
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.ColumnResized,
                EventCallback.Factory.Create<DataGridColumnResizedEventArgs>(this, e => captured = e))
            .Add(c => c.Columns, ColumnsFragment()));

        await cut.InvokeAsync(() => cut.Instance.OnColumnResized(0, 222));

        Assert.NotNull(captured);
        Assert.Equal(222, captured!.Width);
        var firstCol = cut.FindAll("table.omni-grid-table colgroup col")[0];
        Assert.Contains("width:222px", (firstCol.GetAttribute("style") ?? "").Replace(" ", ""));
    }
}
