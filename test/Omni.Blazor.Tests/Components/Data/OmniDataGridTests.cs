using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

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
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var root = cut.Find("div.omni-grid");
        Assert.Contains("omni-grid", root.ClassName);
    }

    [Fact]
    public void Renders_column_headers()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var headers = cut.FindAll("table.omni-grid-table thead th");
        Assert.Contains(headers, h => h.TextContent.Contains("Name"));
        Assert.Contains(headers, h => h.TextContent.Contains("Age"));
    }

    [Fact]
    public void Column_headers_have_scope_col()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
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
    public void Utility_column_headers_and_selection_inputs_have_accessible_names()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowMultiSelection, true)
            .Add(c => c.EditMode, DataGridEditMode.Row)
            .Add(c => c.Columns, ColumnsFragment()));

        // The utility header and every generated checkbox own explicit accessible
        // names without adding visual text to the narrow selection cells.
        var selectHeader = cut.Find("table.omni-grid-table thead th.omni-grid-th-select");
        Assert.Equal("col", selectHeader.GetAttribute("scope"));
        Assert.False(string.IsNullOrWhiteSpace(selectHeader.GetAttribute("aria-label")));
        Assert.True(string.IsNullOrWhiteSpace(selectHeader.TextContent));
        Assert.All(cut.FindAll("table.omni-grid-table tbody td.omni-grid-td-select"), cell =>
            Assert.True(string.IsNullOrWhiteSpace(cell.TextContent)));
        Assert.All(cut.FindAll("input[type='checkbox']"), input =>
            Assert.False(string.IsNullOrWhiteSpace(input.GetAttribute("aria-label"))));

        // Same for the trailing edit-actions column header.
        var headers = cut.FindAll("table.omni-grid-table thead tr:first-child th");
        Assert.All(headers, th => Assert.Equal("col", th.GetAttribute("scope")));
    }

    [Fact]
    public void Renders_data_rows()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        var bodyText = cut.Find("table.omni-grid-table tbody").TextContent;
        Assert.Contains("Alice", bodyText);
        Assert.Contains("Bob", bodyText);
        Assert.Contains("Carol", bodyText);
    }

    [Fact]
    public void Monospace_column_applies_the_typography_class_to_header_and_cells()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Person>>(0);
            builder.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
            builder.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
            builder.AddAttribute(3, nameof(OmniDataGridColumn<Person>.Monospace), true);
            builder.CloseComponent();
        };
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, columns));

        Assert.Contains("omni-grid-cell-mono", cut.Find("thead th").ClassName);
        Assert.All(cut.FindAll("tbody td"), cell =>
            Assert.Contains("omni-grid-cell-mono", cell.ClassName));
    }

    [Fact]
    public void Numeric_column_applies_monospace_and_numeric_classes()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Person>>(0);
            builder.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Age");
            builder.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Age));
            builder.AddAttribute(3, nameof(OmniDataGridColumn<Person>.Numeric), true);
            builder.CloseComponent();
        };
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, columns));

        var header = cut.Find("thead th");
        Assert.Contains("omni-grid-cell-mono", header.ClassName);
        Assert.Contains("omni-grid-cell-numeric", header.ClassName);
        Assert.All(cut.FindAll("tbody td"), cell =>
        {
            Assert.Contains("omni-grid-cell-mono", cell.ClassName);
            Assert.Contains("omni-grid-cell-numeric", cell.ClassName);
        });
    }

    [Fact]
    public void Renders_EmptyText_when_data_is_empty()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Array.Empty<Person>())
            .Add(c => c.EmptyText, "Sem registros")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("Sem registros", cut.Find(".omni-grid-empty").TextContent);
    }

    [Fact]
    public void Embed_adds_modifier_class()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Embed, true)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("omni-grid-embed", cut.Find("div.omni-grid").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Class, "my-grid")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("my-grid", cut.Find("div.omni-grid").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Style, "border: 1px solid red")
            .Add(c => c.Columns, ColumnsFragment()));

        var style = cut.Find("div.omni-grid").GetAttribute("style") ?? "";
        Assert.Contains("border: 1px solid red", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .AddUnmatched("data-testid", "grid1")
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Equal("grid1", cut.Find("div.omni-grid").GetAttribute("data-testid"));
    }

    // ─── Column resize ────────────────────────────────────────────────────

    [Fact]
    public void AllowColumnResize_marks_table_and_renders_handles()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Contains("omni-grid-resizable", cut.Find("table.omni-grid-table").ClassName);
        Assert.Equal(2, cut.FindAll(".omni-grid-resizer").Count);
    }

    [Fact]
    public void No_resize_handles_when_disabled()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
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

        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.AllowColumnResize, true)
            .Add(c => c.Columns, frag));

        // Only the first (resizable) column shows a handle.
        Assert.Single(cut.FindAll(".omni-grid-resizer"));
    }

    [Fact]
    public void Renders_colgroup_col_with_id_per_visible_column()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
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
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsFragment()));

        Assert.Empty(cut.FindAll(".omni-grid-col-filler"));
    }

    [Fact]
    public async Task OnColumnResized_updates_col_width_and_fires_event()
    {
        DataGridColumnResizedEventArgs? captured = null;
        var cut = Render<OmniDataGrid<Person>>(p => p
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

    [Fact]
    public async Task Resizing_a_right_frozen_column_recomputes_adjacent_frozen_offsets()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Person>>(0);
            builder.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
            builder.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(person => person.Name));
            builder.AddAttribute(3, nameof(OmniDataGridColumn<Person>.Width), "100px");
            builder.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Frozen), FrozenPosition.Right);
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Person>>(5);
            builder.AddAttribute(6, nameof(OmniDataGridColumn<Person>.Title), "Age");
            builder.AddAttribute(7, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(person => person.Age));
            builder.AddAttribute(8, nameof(OmniDataGridColumn<Person>.Width), "80px");
            builder.AddAttribute(9, nameof(OmniDataGridColumn<Person>.Frozen), FrozenPosition.Right);
            builder.CloseComponent();
        };
        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.Data, Sample)
            .Add(component => component.AllowColumnResize, true)
            .Add(component => component.Columns, columns));

        await cut.InvokeAsync(() => cut.Instance.OnColumnResized(1, 140));

        var nameHeader = cut.FindAll("thead th").Single(header => header.TextContent.Contains("Name"));
        Assert.Contains("right: 140px", nameHeader.GetAttribute("style"));
    }

    [Fact]
    public async Task View_state_round_trips_order_width_visibility_frozen_sort_filter_group_and_search()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Person>>(0);
            builder.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
            builder.AddAttribute(2, nameof(OmniDataGridColumn<Person>.PropertyName), "Name");
            builder.AddAttribute(3, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(person => person.Name));
            builder.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Filterable), true);
            builder.AddAttribute(5, nameof(OmniDataGridColumn<Person>.Groupable), true);
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Person>>(10);
            builder.AddAttribute(11, nameof(OmniDataGridColumn<Person>.Title), "Age");
            builder.AddAttribute(12, nameof(OmniDataGridColumn<Person>.PropertyName), "Age");
            builder.AddAttribute(13, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(person => person.Age));
            builder.CloseComponent();
        };
        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.Data, Sample)
            .Add(component => component.AllowPaging, false)
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.AllowColumnFilter, true)
            .Add(component => component.Columns, columns));
        DataGridViewState state = new(
            [
                new("Age", 0, "164px", true, FrozenPosition.Right),
                new("Name", 1, "220px", true, null)
            ],
            [new SortDescriptor("Age", SortDirection.Descending)],
            [new DataGridFilterViewState("Name", FilterOperator.Contains, "A")],
            [new DataGridGroupViewState("Name")],
            "Ali");

        await cut.InvokeAsync(() => cut.Instance.ApplyViewStateAsync(state));

        DataGridViewState captured = cut.Instance.CaptureViewState();
        Assert.Equal(["Age", "Name"], captured.Columns.Select(column => column.Property));
        Assert.Equal("164px", captured.Columns[0].Width);
        Assert.Equal(FrozenPosition.Right, captured.Columns[0].Frozen);
        Assert.Equal(SortDirection.Descending, Assert.Single(captured.Sort).Direction);
        Assert.Equal("A", Assert.Single(captured.Filters).Value);
        Assert.Equal("Name", Assert.Single(captured.Groups).Property);
        Assert.Equal("Ali", captured.Search);
    }

    [Fact]
    public void Persist_key_restores_state_after_columns_register()
    {
        DataGridViewState persisted = new(
            [
                new("Age", 0, "180px", true, FrozenPosition.Right),
                new("Name", 1, null, true, null)
            ],
            [new SortDescriptor("Age", SortDirection.Descending)],
            [],
            []);
        string json = JsonSerializer.Serialize(
            persisted,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        JSInterop.Setup<string?>("omniBlazor.storageGet", "omni.grid.people").SetResult(json);
        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.Data, Sample)
            .Add(component => component.PersistKey, "people")
            .Add(component => component.Columns, ColumnsFragment()));

        cut.WaitForAssertion(() =>
        {
            DataGridViewState captured = cut.Instance.CaptureViewState();
            Assert.Equal("Age", captured.Columns[0].Property);
            Assert.Equal("180px", captured.Columns[0].Width);
            Assert.Equal(FrozenPosition.Right, captured.Columns[0].Frozen);
        });
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
        => Render<OmniDataGrid<Sale>>(p =>
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

    [Fact]
    public async Task Dragging_a_groupable_column_onto_the_panel_groups_the_rows()
    {
        // O caminho que o usuário percorre de fato — segurar a alça e soltá-la na
        // faixa — hoje por pointer events: o pointerdown arma o gesto no browser
        // (gridStartGroupDrag) e o JS chama OnGroupGripDropped quando o ponteiro
        // solta sobre o painel. HTML5 dragstart/drop saíram: dentro do WebView2
        // (MAUI/Photino no Windows) o drop nunca chegava à página.
        var cut = RenderSalesGrid();

        await cut.FindAll(".omni-grid-col-drag")[0]
            .TriggerEventAsync("onpointerdown", new PointerEventArgs());
        await cut.InvokeAsync(() => cut.Instance.OnGroupGripDropped());

        Assert.NotEmpty(cut.FindAll(".omni-grid-group-row"));
    }

    [Fact]
    public async Task Releasing_the_grip_away_from_the_panel_does_not_group()
    {
        // O JS só invoca OnGroupGripDropped quando o up acontece sobre o painel;
        // soltar longe encerra o gesto sem callback. Um pointerdown que não
        // termina em drop não pode deixar agrupamento armado para o futuro.
        var cut = RenderSalesGrid();

        await cut.FindAll(".omni-grid-col-drag")[0]
            .TriggerEventAsync("onpointerdown", new PointerEventArgs());

        Assert.Empty(cut.FindAll(".omni-grid-group-row"));
    }

    // ─── Aggregate formatting ──────────────────────────────────────────────

    [Theory]
    [InlineData("en-US", "180.00")]
    [InlineData("pt-BR", "180,00")]
    [InlineData("de-DE", "180,00")]
    public void Aggregate_row_formats_with_the_current_culture(string culture, string expected)
    {
        // Regression: the aggregate row used to hardcode pt-BR, so an en-US/de-DE app
        // showed the wrong decimal separator right below correctly-formatted cells.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);

            var cut = RenderSalesGrid(p => p.Add(c => c.ShowAggregateRow, true));

            // Sum of the Amount column: 100 + 50 + 30
            Assert.Contains(expected, cut.Find(".omni-grid-aggregate-value").TextContent);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    // ─── Shared hierarchy engine ──────────────────────────────────────────

    [Fact]
    public void Collapsed_expanders_state_aria_expanded_as_false()
    {
        // Um aria-expanded ausente não é "colapsado": é "não abre nada".
        // Vale para o chevron da hierarquia e para o botão de master-detail.
        var arvore = RenderHierarchyGrid(CreateHierarchy(), parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, node => node.Children.Count > 0));

        arvore.WaitForAssertion(() =>
            Assert.Equal("false", arvore.Find(".omni-grid-tree-chevron").GetAttribute("aria-expanded")));

        var detalhe = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.Data, Sample)
            .Add(component => component.AllowPaging, false)
            .Add(component => component.Columns, ColumnsFragment())
            .Add(component => component.DetailTemplate, person => builder =>
                builder.AddContent(0, $"Detalhe de {person.Name}")));

        Assert.All(detalhe.FindAll(".omni-grid-td-expand button"),
            botao => Assert.Equal("false", botao.GetAttribute("aria-expanded")));

        detalhe.FindAll(".omni-grid-td-expand button")[0].Click();

        detalhe.WaitForAssertion(() =>
            Assert.Equal("true", detalhe.FindAll(".omni-grid-td-expand button")[0].GetAttribute("aria-expanded")));
    }

    [Fact]
    public void Hierarchy_mode_renders_treegrid_semantics_and_stable_levels()
    {
        var cut = RenderHierarchyGrid(CreateHierarchy(), parameters => parameters
            .Add(component => component.Children, node => node.Children)
            .Add(component => component.HasChildren, node => node.Children.Count > 0)
            .Add(component => component.InitiallyExpanded, node => node.Id == "root"));

        cut.WaitForAssertion(() =>
        {
            var table = cut.Find("table.omni-grid-table");
            Assert.Equal("treegrid", table.GetAttribute("role"));
            Assert.Equal("Estrutura", table.GetAttribute("aria-label"));
            var rows = cut.FindAll("tbody tr[role='row']");
            Assert.Equal(3, rows.Count);
            Assert.Equal("1", rows[0].GetAttribute("aria-level"));
            Assert.Equal("2", rows[1].GetAttribute("aria-level"));
            Assert.All(rows, row => Assert.NotEmpty(row.QuerySelectorAll("td[role='gridcell']")));
        });
    }

    [Fact]
    public async Task Concurrent_hierarchy_expands_share_one_lazy_request()
    {
        var root = new HierarchyNode("root", "Raiz");
        var completion = new TaskCompletionSource<IReadOnlyList<HierarchyNode>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        HierarchyChildrenProvider<HierarchyNode> provider = (_, _) =>
        {
            calls++;
            return new(completion.Task);
        };
        var cut = RenderHierarchyGrid([root], parameters => parameters
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
        Assert.Equal(2, cut.Instance.VisibleHierarchyRowCount);
    }

    [Fact]
    public async Task Collapsing_hierarchy_cancels_pending_lazy_request()
    {
        var root = new HierarchyNode("root", "Raiz");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        HierarchyChildrenProvider<HierarchyNode> provider = async (_, token) =>
        {
            observed = token;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return [];
        };
        var cut = RenderHierarchyGrid([root], parameters => parameters
            .Add(component => component.HasChildren, _ => true)
            .Add(component => component.ChildrenProvider, provider));

        Task expansion = Task.CompletedTask;
        await cut.InvokeAsync(() => { expansion = cut.Instance.ExpandAsync(root); });
        await started.Task;
        await cut.InvokeAsync(() => cut.Instance.CollapseAsync(root));
        await expansion;

        Assert.True(observed.IsCancellationRequested);
        Assert.False(cut.Instance.IsHierarchyLoading);
    }

    [Fact]
    public void Controlled_expanded_keys_start_lazy_loading_and_update_aria()
    {
        var root = new HierarchyNode("root", "Raiz");
        var calls = 0;
        HierarchyChildrenProvider<HierarchyNode> provider = (_, _) =>
        {
            calls++;
            return ValueTask.FromResult<IReadOnlyList<HierarchyNode>>([new("child", "Filho")]);
        };
        var cut = RenderHierarchyGrid([root], parameters => parameters
            .Add(component => component.HasChildren, node => node.Id == "root")
            .Add(component => component.ChildrenProvider, provider)
            .Add(component => component.ExpandedKeys, new object[] { "root" }));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, calls);
            Assert.Equal(2, cut.Instance.VisibleHierarchyRowCount);
            Assert.Equal("true", cut.Find("tbody tr[role='row']").GetAttribute("aria-expanded"));
        });
    }

    [Fact]
    public void Hierarchy_mode_preserves_master_detail_column_and_content()
    {
        var root = new HierarchyNode("root", "Raiz");
        var cut = RenderHierarchyGrid([root], parameters => parameters
            .Add(component => component.Children, _ => Array.Empty<HierarchyNode>())
            .Add(component => component.DetailTemplate, node => builder =>
                builder.AddContent(0, $"Detalhe de {node.Name}")));

        var hierarchyRow = cut.Find("tbody tr[role='row']");
        Assert.Equal(2, hierarchyRow.QuerySelectorAll("td").Length);

        // Colapsado, o botão precisa dizer "false": ausente, aria-expanded
        // significaria que ele não abre nada.
        Assert.Equal("false", cut.Find(".omni-grid-td-expand button").GetAttribute("aria-expanded"));

        cut.Find(".omni-grid-td-expand button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Detalhe de Raiz", cut.Find(".omni-grid-detail-row").TextContent);
            Assert.Equal("true", cut.Find(".omni-grid-td-expand button").GetAttribute("aria-expanded"));
        });
    }

    [Fact]
    public async Task DataProvider_cancels_superseded_load_and_latest_result_wins()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;

        async ValueTask<GridLoadResult<Person>> Provider(
            GridState<Person> state,
            CancellationToken cancellationToken)
        {
            if (state.Search == "primeiro")
            {
                firstToken = cancellationToken;
                firstStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            var item = new Person(state.Search ?? "inicial", 1);
            return new GridLoadResult<Person>([item], 1);
        }

        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.DataProvider, Provider)
            .Add(component => component.DebounceMs, 0)
            .Add(component => component.AllowSearch, true)
            .Add(component => component.Columns, ColumnsFragment()));
        var search = cut.Find(".omni-grid-search input");

        var first = search.InputAsync("primeiro");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        var second = search.InputAsync("segundo");

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() =>
        {
            Assert.True(firstToken.IsCancellationRequested);
            var body = cut.Find("tbody").TextContent;
            Assert.Contains("segundo", body);
            Assert.DoesNotContain("primeiro", body);
        });
    }

    [Fact]
    public async Task Export_pages_provider_streams_download_and_reports_truncation()
    {
        var data = Enumerable.Range(1, 5).Select(index => new Person($"Pessoa {index}", index)).ToArray();
        var requests = new List<GridState<Person>>();
        var truncatedAt = 0;

        ValueTask<GridLoadResult<Person>> Provider(
            GridState<Person> state,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            requests.Add(state);
            return ValueTask.FromResult(new GridLoadResult<Person>(
                data.Skip(state.Skip).Take(state.Top).ToArray(),
                data.Length));
        }

        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.DataProvider, Provider)
            .Add(component => component.AllowExport, true)
            .Add(component => component.MaxExportRows, 3)
            .Add(component => component.ExportBatchSize, 2)
            .Add(component => component.ExportTruncated, count => truncatedAt = count)
            .Add(component => component.Columns, ColumnsFragment()));

        await cut.FindAll("button").Single(button => button.TextContent.Contains("Exportar")).ClickAsync(new());

        Assert.True(cut.Instance.LastExportWasTruncated);
        Assert.Equal(3, truncatedAt);
        Assert.Contains(requests, request => request.Skip == 0 && request.Top == 2);
        Assert.Contains(requests, request => request.Skip == 2 && request.Top == 1);
        Assert.DoesNotContain(requests, request => request.Top == int.MaxValue);
        JSInterop.VerifyInvoke("omniBlazor.downloadStream");
    }

    [Fact]
    public async Task Export_observes_stream_failure_and_releases_single_export_gate()
    {
        var failure = new InvalidOperationException("falha de exportação");
        Exception? reported = null;

        async IAsyncEnumerable<Person> FailingExport(
            GridState<Person> state,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return Sample[0];
            throw failure;
        }

        var cut = Render<OmniDataGrid<Person>>(parameters => parameters
            .Add(component => component.Data, Sample)
            .Add(component => component.AllowExport, true)
            .Add(component => component.ExportProvider, FailingExport)
            .Add(component => component.ExportFailed, exception => reported = exception)
            .Add(component => component.Columns, ColumnsFragment()));

        await cut.FindAll("button").Single(button => button.TextContent.Contains("Exportar")).ClickAsync(new());

        Assert.Same(failure, reported);
        Assert.False(cut.Instance.IsExporting);
    }

    private IRenderedComponent<OmniDataGrid<HierarchyNode>> RenderHierarchyGrid(
        IEnumerable<HierarchyNode> items,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<HierarchyNode>>>? configure = null)
    {
        return Render<OmniDataGrid<HierarchyNode>>(parameters =>
        {
            parameters
                .Add(component => component.Data, items)
                .Add(component => component.KeySelector, node => node.Id)
                .Add(component => component.AllowPaging, false)
                .Add(component => component.HierarchyAriaLabel, "Estrutura")
                .Add(component => component.Columns, HierarchyColumns());
            configure?.Invoke(parameters);
        });
    }

    private static RenderFragment HierarchyColumns() => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<HierarchyNode>>(0);
        builder.AddAttribute(1, nameof(OmniDataGridColumn<HierarchyNode>.Title), "Nome");
        builder.AddAttribute(2, nameof(OmniDataGridColumn<HierarchyNode>.Property),
            (Func<HierarchyNode, object?>)(node => node.Name));
        builder.AddAttribute(3, nameof(OmniDataGridColumn<HierarchyNode>.IsHierarchyAnchor), true);
        builder.CloseComponent();
    };

    private static HierarchyNode[] CreateHierarchy() =>
    [
        new("root", "Raiz",
        [
            new("first", "Primeiro"),
            new("second", "Segundo")
        ])
    ];

    private sealed record HierarchyNode(
        string Id,
        string Name,
        List<HierarchyNode>? ChildNodes = null)
    {
        public List<HierarchyNode> Children { get; } = ChildNodes ?? [];
    }

    // ── Agrupamento hierárquico por data ────────────────────────────────────────
    //
    // Uma coluna de data agrupada pelo valor exato rende um grupo por linha. Estes
    // testes cobrem o desdobramento em Ano › Mês › Dia a partir de um único arrasto.

    private record Evento(DateTime Quando, string Texto);

    private static readonly Evento[] Eventos =
    {
        new(new DateTime(2026, 7, 31, 22, 44, 16), "a"),
        new(new DateTime(2026, 7, 31, 10, 0, 0), "b"),   // mesmo dia, hora diferente
        new(new DateTime(2026, 7, 15, 8, 0, 0), "c"),    // mesmo mês, outro dia
        new(new DateTime(2026, 3, 2, 8, 0, 0), "d"),     // outro mês
        new(new DateTime(2025, 12, 31, 23, 0, 0), "e"),  // outro ano
    };

    private static RenderFragment EventoColumns(IReadOnlyList<DateGroupInterval>? hierarquia) => b =>
    {
        b.OpenComponent<OmniDataGridColumn<Evento>>(0);
        b.AddAttribute(1, nameof(OmniDataGridColumn<Evento>.Title), "Quando");
        b.AddAttribute(2, nameof(OmniDataGridColumn<Evento>.PropertyName), "Quando");
        b.AddAttribute(3, nameof(OmniDataGridColumn<Evento>.Property), (Func<Evento, object?>)(e => e.Quando));
        b.AddAttribute(4, nameof(OmniDataGridColumn<Evento>.Groupable), true);
        b.AddAttribute(5, nameof(OmniDataGridColumn<Evento>.GroupHierarchy), hierarquia);
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Evento>>(10);
        b.AddAttribute(11, nameof(OmniDataGridColumn<Evento>.Title), "Texto");
        b.AddAttribute(12, nameof(OmniDataGridColumn<Evento>.PropertyName), "Texto");
        b.AddAttribute(13, nameof(OmniDataGridColumn<Evento>.Property), (Func<Evento, object?>)(e => e.Texto));
        b.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Evento>> RenderEventosGrid(IReadOnlyList<DateGroupInterval>? hierarquia)
        => Render<OmniDataGrid<Evento>>(p =>
        {
            p.Add(c => c.Data, Eventos);
            p.Add(c => c.AllowGrouping, true);
            p.Add(c => c.AllowPaging, false);
            p.Add(c => c.Columns, EventoColumns(hierarquia));
        });

    [Fact]
    public async Task Without_a_hierarchy_a_date_column_groups_by_the_exact_value()
    {
        // Comportamento anterior, preservado: 5 instantes distintos → 5 grupos, que é
        // exatamente o problema que a hierarquia resolve.
        var cut = RenderEventosGrid(null);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        Assert.Equal(5, cut.FindAll(".omni-grid-group-row").Count);
    }

    [Fact]
    public async Task A_date_hierarchy_turns_one_drag_into_year_month_day_levels()
    {
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        // 2 anos + 3 meses (jul/26, mar/26, dez/25) + 4 dias (31/07, 15/07, 02/03, 31/12).
        Assert.Equal(9, cut.FindAll(".omni-grid-group-row").Count);

        // Um chip por nível: cada um removível por conta própria.
        Assert.Equal(3, cut.FindAll(".omni-grid-group-chip").Count);
    }

    [Fact]
    public async Task The_deepest_level_holds_the_rows_that_share_the_day()
    {
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        // 31/07 tem dois eventos; o contador do grupo mais profundo precisa dizer 2.
        var contadores = cut.FindAll(".omni-grid-group-count").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("2", contadores);
    }

    [Fact]
    public async Task Group_labels_are_the_truncated_key_and_not_the_cell_text()
    {
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        var rotulos = cut.FindAll(".omni-grid-group-key").Select(e => e.TextContent.Trim()).ToList();
        var julho = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(7);

        Assert.Contains("2026", rotulos);
        // Sob o ano, o mês vem sozinho — sem repetir "2026".
        Assert.Contains(julho, rotulos);
        // E sob o mês, o dia é só o número.
        Assert.Contains("31", rotulos);
        // Nenhum rótulo carrega o instante da célula: é a chave truncada que nomeia o grupo.
        Assert.DoesNotContain(rotulos, r => r.Contains("22:44"));
    }

    [Fact]
    public async Task Each_chip_names_the_column_and_its_unit()
    {
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        var chips = cut.FindAll(".omni-grid-group-chip").Select(c => c.TextContent.Trim()).ToList();

        // "Quando (Ano)", "Quando (Mês)", "Quando (Dia)": dois campos de data agrupados
        // dariam dois chips "Ano" indistinguíveis sem o nome da coluna.
        Assert.All(chips, c => Assert.Contains("Quando", c));
        Assert.Contains(chips, c => c.Contains(DateGrouping.IntervalName(DateGroupInterval.Year)));
        Assert.Contains(chips, c => c.Contains(DateGrouping.IntervalName(DateGroupInterval.Day)));
    }

    [Fact]
    public async Task Removing_one_chip_keeps_the_other_levels()
    {
        // O ponto da refatoração: tirar o "Dia" não pode desmontar Ano › Mês.
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        var xDoDia = cut.FindAll(".omni-grid-group-chip-x")[2];
        await cut.InvokeAsync(() => xDoDia.Click());

        Assert.Equal(2, cut.FindAll(".omni-grid-group-chip").Count);
        // 2 anos + 3 meses, sem o nível de dia.
        Assert.Equal(5, cut.FindAll(".omni-grid-group-row").Count);
    }

    [Fact]
    public async Task Removing_a_middle_level_stops_shortening_the_label_below_it()
    {
        // Sem o mês acima, "31" sozinho não identifica mais nada: o dia volta à data cheia.
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        var xDoMes = cut.FindAll(".omni-grid-group-chip-x")[1];
        await cut.InvokeAsync(() => xDoMes.Click());

        var rotulos = cut.FindAll(".omni-grid-group-key").Select(e => e.TextContent.Trim()).ToList();
        Assert.DoesNotContain("31", rotulos);
        Assert.Contains(rotulos, r => r.Contains("31") && r.Length > 2);
    }

    [Fact]
    public async Task Removing_every_level_clears_the_grouping()
    {
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        for (var i = 0; i < 3; i++)
        {
            await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-chip-x")[0].Click());
        }

        Assert.Empty(cut.FindAll(".omni-grid-group-chip"));
        Assert.Empty(cut.FindAll(".omni-grid-group-row"));
        Assert.NotNull(cut.Find(".omni-grid-group-panel-hint"));
    }

    [Fact]
    public async Task UngroupByAsync_still_removes_the_whole_column()
    {
        // A API pública continua falando em coluna: quem chama não conhece os níveis.
        var cut = RenderEventosGrid(DateGroupHierarchy.YearMonthDay);
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));
        await cut.InvokeAsync(() => cut.Instance.UngroupByAsync("Quando"));

        Assert.Empty(cut.FindAll(".omni-grid-group-chip"));
    }

    [Fact]
    public async Task A_single_interval_groups_without_nesting()
    {
        var cut = RenderEventosGrid(new[] { DateGroupInterval.Month });
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        // jul/26, mar/26, dez/25.
        Assert.Equal(3, cut.FindAll(".omni-grid-group-row").Count);
    }

    // ── Agrupamento × paginação ─────────────────────────────────────────────────

    // 4 registros em 2 dias, alternando: se o grid agrupasse a PÁGINA, cada página de 2
    // linhas mostraria os dois dias, e cada dia apareceria duas vezes no total — com
    // contador 1 em cada aparição. É o cenário que fatia um grupo entre páginas.
    private static readonly Evento[] Alternados =
    {
        new(new DateTime(2026, 1, 1, 9, 0, 0), "a"),
        new(new DateTime(2026, 1, 5, 9, 0, 0), "b"),
        new(new DateTime(2026, 1, 1, 10, 0, 0), "c"),
        new(new DateTime(2026, 1, 5, 10, 0, 0), "d"),
    };

    private IRenderedComponent<OmniDataGrid<Evento>> RenderPaginadoAgrupado() =>
        Render<OmniDataGrid<Evento>>(p =>
        {
            p.Add(c => c.Data, Alternados);
            p.Add(c => c.AllowGrouping, true);
            p.Add(c => c.AllowPaging, true);
            p.Add(c => c.PageSize, 2);
            p.Add(c => c.Columns, EventoColumns(new[] { DateGroupInterval.Day }));
        });

    [Fact]
    public async Task Grouping_runs_over_the_whole_set_and_never_splits_a_group_across_pages()
    {
        var cut = RenderPaginadoAgrupado();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        // Dois grupos (01/01 e 05/01), cada um COMPLETO — com os 2 itens do dia, mesmo os
        // que a página de 2 linhas não conteria.
        var contadores = cut.FindAll(".omni-grid-group-count").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "2", "2" }, contadores);
    }

    [Fact]
    public async Task Paging_slices_groups_and_not_rows_when_grouped()
    {
        // PageSize 2 com 2 grupos: os dois cabem na primeira página, e o rodapé some.
        var cut = RenderPaginadoAgrupado();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));

        Assert.Equal(2, cut.FindAll(".omni-grid-group-row").Count);
        // As 4 linhas de dados vêm junto: paginar grupos traz os itens inteiros deles.
        Assert.Equal(4, cut.FindAll("tbody tr:not(.omni-grid-group-row):not(.omni-grid-group-footer)").Count);
    }

    [Fact]
    public async Task Ungrouping_restores_row_paging()
    {
        var cut = RenderPaginadoAgrupado();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Quando"));
        await cut.InvokeAsync(() => cut.Instance.UngroupByAsync("Quando"));

        // Sem agrupamento, a página volta a contar linhas: 2 de 4.
        Assert.Empty(cut.FindAll(".omni-grid-group-row"));
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
    }

    // ── Modo agrupado achatado ──────────────────────────────────────────────────
    //
    // O <tbody> agrupado era um foreach RECURSIVO sobre a árvore de grupos que não
    // consultava Virtualize: com o pacote real, agrupar 100 mil linhas emitia 1.800.258
    // frames de render (18 por linha) e retinha 233 MB, contra 243 frames fixos do modo
    // plano virtualizado. A árvore passou a ser achatada numa lista linear antes do
    // render, e é essa lista que alimenta os dois ramos.

    // Conjunto grande de propósito: é o volume que separa "virtualizou" de "não virtualizou".
    private static Sale[] ManySales(int total, int regions) =>
        Enumerable.Range(0, total)
            .Select(i => new Sale($"R{i % regions:D4}", i % 2 == 0 ? "Web" : "Store", i))
            .ToArray();

    private IRenderedComponent<OmniDataGrid<Sale>> RenderManySalesGrid(
        Sale[] data,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Sale>>>? extra = null)
        => Render<OmniDataGrid<Sale>>(p =>
        {
            p.Add(c => c.Data, data);
            p.Add(c => c.AllowGrouping, true);
            p.Add(c => c.AllowPaging, false);
            p.Add(c => c.Columns, SalesColumns());
            extra?.Invoke(p);
        });

    private static List<string> BodyRowKinds<T>(IRenderedComponent<OmniDataGrid<T>> cut) =>
        cut.FindAll("tbody tr")
            .Select(tr => tr.ClassList.Contains("omni-grid-group-row") ? "header"
                        : tr.ClassList.Contains("omni-grid-group-footer") ? "footer"
                        : "data")
            .ToList();

    private async Task<int> GroupedBodyRowCount(int total, bool virtualize)
    {
        var cut = RenderManySalesGrid(ManySales(total, 4), p => p.Add(c => c.Virtualize, virtualize));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        Assert.Equal(total + 4, cut.Instance.VisibleGroupRowCount);
        return cut.FindAll("tbody tr").Count;
    }

    [Fact]
    public async Task Virtualized_grouping_stops_putting_the_whole_set_in_the_DOM()
    {
        // O ramo agrupado não olhava Virtualize: montava um <tr> por linha do conjunto, que
        // é exatamente o que o ramo NÃO virtualizado ainda faz. A comparação abaixo é a
        // medida antes/depois.
        var semVirtualize = await GroupedBodyRowCount(20_000, virtualize: false);
        var comVirtualize = await GroupedBodyRowCount(20_000, virtualize: true);

        Assert.Equal(20_004, semVirtualize);
        Assert.True(comVirtualize < 200,
            $"O <tbody> virtualizado montou {comVirtualize} linhas.");
    }

    [Fact]
    public async Task The_virtualized_group_viewport_does_not_grow_with_the_set()
    {
        // A propriedade que interessa não é "é menor", é "não depende de N".
        var pequeno = await GroupedBodyRowCount(2_000, virtualize: true);
        var grande = await GroupedBodyRowCount(20_000, virtualize: true);

        Assert.Equal(pequeno, grande);
    }

    [Fact]
    public async Task Collapsing_a_group_while_virtualized_reaches_the_rendered_body()
    {
        // A lista achatada é mutada no lugar (Clear + refill preserva o array de apoio) e a
        // MESMA instância segue ligada ao Virtualize. Se a mutação não propagasse, o clique
        // no cabeçalho não mudaria nada na tela.
        var cut = RenderManySalesGrid(ManySales(2_000, 4), p => p.Add(c => c.Virtualize, true));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        var cabecalhosAntes = cut.FindAll(".omni-grid-group-toggle").Count;

        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());

        // O primeiro grupo tem 500 linhas: fechá-lo tira exatamente essas 500 da lista.
        Assert.Equal(1_504, cut.Instance.VisibleGroupRowCount);
        // E o DOM acompanhou — com 500 linhas a menos na frente, mais grupos couberam na janela.
        Assert.True(cut.FindAll(".omni-grid-group-toggle").Count > cabecalhosAntes);
    }

    [Fact]
    public async Task Non_virtualized_grouping_still_renders_every_flattened_row()
    {
        // O ajuste da verificação adversarial: a lista achatada alimenta OS DOIS ramos.
        // Sem Virtualize o comportamento visível não muda — todas as linhas continuam lá.
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        // 2 cabeçalhos + 3 linhas de dados.
        Assert.Equal(5, cut.Instance.VisibleGroupRowCount);
        Assert.Equal(5, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public async Task Collapsing_a_group_removes_its_whole_subtree_from_the_flattened_list()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Channel"));

        // North › {Web, Store} e South › {Web}: 2 + 3 cabeçalhos + 3 linhas = 8.
        Assert.Equal(8, cut.Instance.VisibleGroupRowCount);

        // Fechar "North" leva junto os subgrupos e as linhas dele.
        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());

        // Sobram: North (fechado), South, South›Web e a linha do South.
        Assert.Equal(4, cut.Instance.VisibleGroupRowCount);
        Assert.Equal(new[] { "header", "header", "header", "data" }, BodyRowKinds(cut));
    }

    [Fact]
    public async Task Expanding_a_group_again_only_brings_back_that_group()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        // Fecha os dois, reabre só o primeiro.
        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());
        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[1].Click());
        Assert.Equal(2, cut.Instance.VisibleGroupRowCount);

        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());

        // North volta com suas 2 linhas; South continua fechado.
        Assert.Equal(4, cut.Instance.VisibleGroupRowCount);
        Assert.Equal(new[] { "header", "data", "data", "header" }, BodyRowKinds(cut));
    }

    [Fact]
    public async Task Group_footers_keep_the_post_order_of_the_recursive_renderer()
    {
        // O rodapé fica DENTRO do grupo aberto e DEPOIS dos filhos — o do pai vem depois
        // dos dos filhos. Um achatamento que emitisse o rodapé junto ao cabeçalho passaria
        // no teste de contagem e quebraria a leitura da tabela.
        var cut = RenderSalesGrid(p => p.Add(c => c.ShowGroupFooters, true));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Channel"));

        Assert.Equal(
            new[]
            {
                "header",                       // North
                "header", "data", "footer",     // North › Web
                "header", "data", "footer",     // North › Store
                "footer",                       // North (depois dos filhos)
                "header",                       // South
                "header", "data", "footer",     // South › Web
                "footer"                        // South
            },
            BodyRowKinds(cut));
    }

    [Fact]
    public async Task A_collapsed_group_has_no_footer()
    {
        var cut = RenderSalesGrid(p => p.Add(c => c.ShowGroupFooters, true));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());

        // North fechado: nem linhas, nem rodapé. South intacto.
        Assert.Equal(new[] { "header", "header", "data", "footer" }, BodyRowKinds(cut));
    }

    [Fact]
    public async Task Groups_start_collapsed_above_the_auto_collapse_threshold()
    {
        // OnGroupsChangedAsync limpava os colapsados, então o primeiro render depois de
        // arrastar a coluna era sempre o conjunto inteiro aberto.
        var cut = RenderManySalesGrid(
            ManySales(600, 200),
            p => p.Add(c => c.AutoCollapseGroupsThreshold, 100));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        // 200 grupos, todos fechados: só os cabeçalhos entram na lista achatada.
        Assert.Equal(200, cut.Instance.VisibleGroupRowCount);
        Assert.Equal(200, cut.FindAll(".omni-grid-group-row").Count);
        Assert.Empty(cut.FindAll("tbody tr:not(.omni-grid-group-row)"));
    }

    [Fact]
    public async Task Below_the_threshold_groups_still_start_open()
    {
        var cut = RenderManySalesGrid(
            ManySales(600, 20),
            p => p.Add(c => c.AutoCollapseGroupsThreshold, 100));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        Assert.Equal(620, cut.Instance.VisibleGroupRowCount);
    }

    [Fact]
    public async Task Auto_collapse_does_not_close_groups_the_user_reopened()
    {
        // O auto-colapso é decisão de UMA passada. Reavaliá-lo a cada BuildGroups fecharia
        // de volta o grupo no render seguinte a qualquer mudança de parâmetro.
        var cut = RenderManySalesGrid(
            ManySales(600, 200),
            p => p.Add(c => c.AutoCollapseGroupsThreshold, 100));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        await cut.InvokeAsync(() => cut.FindAll(".omni-grid-group-toggle")[0].Click());
        Assert.Equal(203, cut.Instance.VisibleGroupRowCount);

        // Qualquer re-render (aqui, uma mudança de parâmetro qualquer) refaz os grupos.
        cut.Render(p => p.Add(c => c.Class, "recalcula"));

        Assert.Equal(203, cut.Instance.VisibleGroupRowCount);
    }

    [Fact]
    public async Task MaxGroups_truncates_the_tree_and_warns()
    {
        // Caso degenerado: agrupar por uma coluna quase-única renderia um grupo por linha.
        var cut = RenderManySalesGrid(
            ManySales(500, 500),
            p =>
            {
                p.Add(c => c.MaxGroups, 10);
                p.Add(c => c.AutoCollapseGroupsThreshold, 0);
            });
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        Assert.True(cut.Instance.GroupLimitReached);
        Assert.Equal(10, cut.FindAll(".omni-grid-group-row").Count);
        Assert.Contains("10", cut.Find(".omni-grid-group-limit").TextContent);
    }

    [Fact]
    public async Task The_group_limit_warning_disappears_when_the_grouping_is_cleared()
    {
        var cut = RenderManySalesGrid(
            ManySales(500, 500),
            p => p.Add(c => c.MaxGroups, 10));
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        Assert.NotNull(cut.Find(".omni-grid-group-limit"));

        await cut.InvokeAsync(() => cut.Instance.ClearGroupingAsync());

        Assert.False(cut.Instance.GroupLimitReached);
        Assert.Empty(cut.FindAll(".omni-grid-group-limit"));
    }

    [Fact]
    public async Task Virtualized_grouping_publishes_the_row_height_so_group_rows_match_it()
    {
        // O Virtualize usa UM ItemSize para todas as linhas; cabeçalho de grupo é mais baixo
        // que uma linha de dados, e sem igualar as alturas a rolagem mente proporcionalmente
        // à razão cabeçalhos:linhas.
        var cut = RenderManySalesGrid(
            ManySales(200, 4),
            p =>
            {
                p.Add(c => c.Virtualize, true);
                p.Add(c => c.RowHeight, 52);
            });
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));

        var style = cut.Find("div.omni-grid").GetAttribute("style") ?? "";
        Assert.Contains("--omni-grid-row-h: 52px", style);
    }

    [Fact]
    public void Without_virtualize_the_row_height_variable_is_not_published()
    {
        // Modo paginado mantém a altura natural das linhas de grupo, como sempre teve.
        var cut = RenderSalesGrid();

        var style = cut.Find("div.omni-grid").GetAttribute("style") ?? "";
        Assert.DoesNotContain("--omni-grid-row-h", style);
    }

    // ── Memoização do shaping ───────────────────────────────────────────────────
    //
    // O pai re-renderiza por qualquer motivo e cada render dispara o pipeline de
    // filtro+ordenação+agrupamento — 318 ms por clique com 1M de itens ordenados.
    // Estes testes fixam o contrato do carimbo: reprocessar SÓ quando algo que
    // alimenta o pipeline mudou, com RefreshAsync como válvula para mutação in-place.

    private sealed class Pedido
    {
        public string Cliente { get; set; } = "";
        public decimal Total { get; set; }
    }

    private static RenderFragment PedidoColumns() => b =>
    {
        b.OpenComponent<OmniDataGridColumn<Pedido>>(0);
        b.AddAttribute(1, nameof(OmniDataGridColumn<Pedido>.Title), "Cliente");
        b.AddAttribute(2, nameof(OmniDataGridColumn<Pedido>.PropertyName), "Cliente");
        b.AddAttribute(3, nameof(OmniDataGridColumn<Pedido>.Property), (Func<Pedido, object?>)(x => x.Cliente));
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Pedido>>(10);
        b.AddAttribute(11, nameof(OmniDataGridColumn<Pedido>.Title), "Total");
        b.AddAttribute(12, nameof(OmniDataGridColumn<Pedido>.PropertyName), "Total");
        b.AddAttribute(13, nameof(OmniDataGridColumn<Pedido>.Property), (Func<Pedido, object?>)(x => x.Total));
        b.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Pedido>> RenderPedidosGrid(List<Pedido> data) =>
        Render<OmniDataGrid<Pedido>>(p =>
        {
            p.Add(c => c.Data, data);
            p.Add(c => c.AllowSorting, true);
            p.Add(c => c.AllowPaging, false);
            p.Add(c => c.Columns, PedidoColumns());
        });

    private static List<Pedido> Pedidos() => new()
    {
        new() { Cliente = "Bruna", Total = 30m },
        new() { Cliente = "Ana", Total = 10m },
        new() { Cliente = "Caio", Total = 20m },
    };

    [Fact]
    public void A_parent_rerender_with_nothing_changed_does_not_reshape()
    {
        var cut = RenderPedidosGrid(Pedidos());
        var antes = cut.Instance.ShapeApplyCount;

        // Simula o pai re-renderizando à toa (progresso, seleção, badge…).
        cut.Render();
        cut.Render();

        Assert.Equal(antes, cut.Instance.ShapeApplyCount);
    }

    [Fact]
    public void Changing_the_data_reference_reshapes()
    {
        var cut = RenderPedidosGrid(Pedidos());
        var antes = cut.Instance.ShapeApplyCount;

        cut.Render(p => p.Add(c => c.Data, Pedidos()));

        Assert.True(cut.Instance.ShapeApplyCount > antes);
    }

    [Fact]
    public void Appending_to_the_same_list_reshapes()
    {
        // O tail do consumidor faz Add na MESMA lista: a contagem no carimbo é o que
        // impede o item novo de ficar invisível até a próxima interação.
        var data = Pedidos();
        var cut = RenderPedidosGrid(data);
        var antes = cut.Instance.ShapeApplyCount;

        data.Add(new Pedido { Cliente = "Duda", Total = 40m });
        cut.Render();

        Assert.True(cut.Instance.ShapeApplyCount > antes);
        Assert.Contains("Duda", cut.Find("tbody").TextContent);
    }

    [Fact]
    public async Task Clicking_a_sort_header_reshapes_but_extra_renders_do_not()
    {
        var cut = RenderPedidosGrid(Pedidos());

        await cut.InvokeAsync(() => cut.FindAll("th")[0].Click());
        var aposOrdenar = cut.Instance.ShapeApplyCount;
        var celulas = cut.FindAll("tbody td").Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal("Ana", celulas[0]);

        cut.Render();

        Assert.Equal(aposOrdenar, cut.Instance.ShapeApplyCount);
        // E a ordenação continua aplicada mesmo sem reprocessar.
        Assert.Equal("Ana", cut.FindAll("tbody td")[0].TextContent.Trim());
    }

    [Fact]
    public async Task RefreshAsync_reapplies_the_sort_after_an_in_place_mutation()
    {
        // Mesma lista, mesma contagem: o carimbo não tem como ver o campo mudado.
        // RefreshAsync é a válvula documentada para esse caso.
        var data = Pedidos();
        var cut = RenderPedidosGrid(data);
        await cut.InvokeAsync(() => cut.FindAll("th")[0].Click()); // ordena por Cliente asc

        data[0].Cliente = "Zulmira"; // era "Bruna": deveria ir para o fim
        cut.Render();
        Assert.Equal("Ana", cut.FindAll("tbody td")[0].TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        var clientes = cut.FindAll("tbody tr").Select(r => r.QuerySelector("td")!.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Ana", "Caio", "Zulmira" }, clientes);
    }

    [Fact]
    public async Task Grouping_reshapes_and_extra_renders_keep_the_groups()
    {
        var cut = RenderSalesGrid();
        await cut.InvokeAsync(() => cut.Instance.GroupByAsync("Region"));
        var apos = cut.Instance.ShapeApplyCount;
        var grupos = cut.FindAll(".omni-grid-group-row").Count;

        cut.Render();

        Assert.Equal(apos, cut.Instance.ShapeApplyCount);
        Assert.Equal(grupos, cut.FindAll(".omni-grid-group-row").Count);
    }
}
