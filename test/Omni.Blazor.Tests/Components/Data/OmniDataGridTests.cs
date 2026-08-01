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
    public void Utility_column_headers_have_scope_col_and_aria_label()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
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
        var cut = Render<OmniDataGrid<Person>>(p => p
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
        // O caminho que o usuário percorre de fato — arrastar a alça até a faixa —
        // e não o GroupByAsync que o resto dos testes usa.
        var cut = RenderSalesGrid();

        await cut.FindAll(".omni-grid-col-drag")[0].TriggerEventAsync("ondragstart", new DragEventArgs());
        await cut.Find(".omni-grid-group-panel").TriggerEventAsync("ondrop", new DragEventArgs());

        Assert.NotEmpty(cut.FindAll(".omni-grid-group-row"));
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

        cut.Find(".omni-grid-td-expand button").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Detalhe de Raiz", cut.Find(".omni-grid-detail-row").TextContent));
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
}
