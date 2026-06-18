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
    public void Column_headers_have_scope_col()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        // Every column-header <th> in the header row must carry scope="col"
        // so screen readers announce the column for each data cell.
        var headers = cut.FindAll("table.omni-grid-table thead tr:first-child th");
        Assert.NotEmpty(headers);
        Assert.All(headers, th => Assert.Equal("col", th.GetAttribute("scope")));

        // The labelled data columns specifically are scoped.
        var nameHeader = headers.Single(h => h.TextContent.Contains("Name"));
        var ageHeader = headers.Single(h => h.TextContent.Contains("Age"));
        Assert.Equal("col", nameHeader.GetAttribute("scope"));
        Assert.Equal("col", ageHeader.GetAttribute("scope"));
    }

    [Fact]
    public void Utility_column_headers_have_scope_col_and_aria_label()
    {
        var cut = RenderComponent<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowMultiSelection, true)
            .Add(c => c.EditMode, DataGridEditMode.Row)
            .Add(c => c.Columns, ColumnsFragment()));

        // The unlabelled selection column header is still a scoped column header,
        // and carries an aria-label so it is not announced as blank.
        var selectHeader = cut.Find("table.omni-grid-table thead th.omni-grid-th-select");
        Assert.Equal("col", selectHeader.GetAttribute("scope"));
        Assert.False(string.IsNullOrEmpty(selectHeader.GetAttribute("aria-label")));

        // Same for the trailing edit-actions column header.
        var headers = cut.FindAll("table.omni-grid-table thead tr:first-child th");
        Assert.All(headers, th => Assert.Equal("col", th.GetAttribute("scope")));
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

    // ─── Grouping ──────────────────────────────────────────────────────────

    private record Sale(string Region, string Channel, decimal Amount);

    private static readonly Sale[] Sales =
    {
        new("North", "Web",   100m),
        new("North", "Store",  50m),
        new("South", "Web",    30m),
    };

    private static RenderFragment SalesColumns() => b =>
    {
        b.OpenComponent<OmniDataGridColumn<Sale>>(0);
        b.AddAttribute(1, nameof(OmniDataGridColumn<Sale>.Title), "Region");
        b.AddAttribute(2, nameof(OmniDataGridColumn<Sale>.PropertyName), "Region");
        b.AddAttribute(3, nameof(OmniDataGridColumn<Sale>.Property), (Func<Sale, object?>)(s => s.Region));
        b.AddAttribute(4, nameof(OmniDataGridColumn<Sale>.Groupable), true);
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Sale>>(10);
        b.AddAttribute(11, nameof(OmniDataGridColumn<Sale>.Title), "Channel");
        b.AddAttribute(12, nameof(OmniDataGridColumn<Sale>.PropertyName), "Channel");
        b.AddAttribute(13, nameof(OmniDataGridColumn<Sale>.Property), (Func<Sale, object?>)(s => s.Channel));
        b.AddAttribute(14, nameof(OmniDataGridColumn<Sale>.Groupable), true);
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Sale>>(20);
        b.AddAttribute(21, nameof(OmniDataGridColumn<Sale>.Title), "Amount");
        b.AddAttribute(22, nameof(OmniDataGridColumn<Sale>.PropertyName), "Amount");
        b.AddAttribute(23, nameof(OmniDataGridColumn<Sale>.Property), (Func<Sale, object?>)(s => s.Amount));
        b.AddAttribute(24, nameof(OmniDataGridColumn<Sale>.Aggregate), AggregateFunction.Sum);
        b.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Sale>> RenderSalesGrid(
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Sale>>>? extra = null)
        => RenderComponent<OmniDataGrid<Sale>>(p =>
        {
            p.Add(c => c.Data, Sales);
            p.Add(c => c.AllowGrouping, true);
            p.Add(c => c.AllowPaging, false);
            p.Add(c => c.Columns, SalesColumns());
            extra?.Invoke(p);
        });

    [Fact]
    public void AllowGrouping_shows_panel_with_hint_when_no_groups()
    {
        var cut = RenderSalesGrid();
        Assert.NotNull(cut.Find(".omni-grid-group-panel"));
        Assert.NotNull(cut.Find(".omni-grid-group-panel-hint"));
        Assert.Empty(cut.FindAll(".omni-grid-group-row"));
    }

    [Fact]
    public void Groupable_columns_render_a_drag_grip()
    {
        var cut = RenderSalesGrid();
        // Region + Channel are Groupable; Amount is not.
        Assert.Equal(2, cut.FindAll(".omni-grid-col-drag").Count);
    }

    [Fact]
    public async Task GroupByAsync_creates_group_rows_with_counts()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        var groupRows = cut.FindAll(".omni-grid-group-row");
        Assert.Equal(2, groupRows.Count); // North, South
        var text = cut.Find("tbody").TextContent;
        Assert.Contains("North", text);
        Assert.Contains("South", text);
        // North has 2 rows → count chip shows 2.
        Assert.Contains("2", cut.Find(".omni-grid-group-count").TextContent);
        // A chip appears in the panel.
        Assert.Single(cut.FindAll(".omni-grid-group-chip"));
    }

    [Fact]
    public async Task HideGroupedColumn_removes_grouped_column_from_header()
    {
        var cut = RenderSalesGrid(p => p.Add(c => c.HideGroupedColumn, true));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        var headers = cut.FindAll("thead tr:first-child th").Select(h => h.TextContent).ToList();
        Assert.DoesNotContain(headers, h => h.Contains("Region"));
        Assert.Contains(headers, h => h.Contains("Channel"));
    }

    [Fact]
    public async Task Nested_grouping_produces_two_levels()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Channel"));

        // Region groups: North, South (2). Sub-groups by Channel: North→{Web,Store}=2, South→{Web}=1.
        // Total group rows = 2 + 3 = 5.
        Assert.Equal(5, cut.FindAll(".omni-grid-group-row").Count);
        Assert.Equal(2, cut.FindAll(".omni-grid-group-chip").Count);
    }

    [Fact]
    public async Task ShowGroupFooters_renders_aggregate_per_group()
    {
        var cut = RenderSalesGrid(p => p.Add(c => c.ShowGroupFooters, true));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        var footers = cut.FindAll(".omni-grid-group-footer");
        Assert.Equal(2, footers.Count); // one per region
        // North = 100 + 50 = 150.
        Assert.Contains("150", string.Concat(footers.Select(f => f.TextContent)));
    }

    [Fact]
    public async Task ClearGroupingAsync_returns_to_flat_rows()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        Assert.NotEmpty(cut.FindAll(".omni-grid-group-row"));

        await cut.InvokeAsync(() => cut.Instance.ClearGroupingAsync());
        Assert.Empty(cut.FindAll(".omni-grid-group-row"));
        Assert.Empty(cut.FindAll(".omni-grid-group-chip"));
    }

    [Fact]
    public async Task Grouped_event_fires_with_property_names()
    {
        IReadOnlyList<string>? captured = null;
        var cut = RenderSalesGrid(p => p.Add(c => c.Grouped,
            EventCallback.Factory.Create<IReadOnlyList<string>>(this, names => captured = names)));

        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        Assert.NotNull(captured);
        Assert.Equal(new[] { "Region" }, captured!);
    }
}
