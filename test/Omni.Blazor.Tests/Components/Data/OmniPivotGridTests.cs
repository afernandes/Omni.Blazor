using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniPivotGrid{TItem}"/>: cross-tab
/// aggregation, nested rows (rowspan), nested columns, multiple measures, row /
/// column / grand totals, drill-down, and the cross-cutting splat.
/// </summary>
public class OmniPivotGridTests : TestContextBase
{
    public record Sale(string Category, string Region, int Year, decimal Amount, int Qty);

    private static readonly Sale[] Sales =
    {
        new("Food",  "North", 2024, 100m, 2),
        new("Food",  "South", 2024,  50m, 1),
        new("Food",  "North", 2025, 200m, 4),
        new("Drink", "North", 2024,  30m, 3),
    };

    private static RenderFragment PivotRows(params string[] props) => b =>
    {
        var seq = 0;
        foreach (var p in props)
        {
            b.OpenComponent<OmniPivotRow<Sale>>(seq++);
            b.AddAttribute(seq++, nameof(OmniPivotRow<Sale>.Property), p);
            b.CloseComponent();
        }
    };

    private static RenderFragment PivotCols(params string[] props) => b =>
    {
        var seq = 100;
        foreach (var p in props)
        {
            b.OpenComponent<OmniPivotColumn<Sale>>(seq++);
            b.AddAttribute(seq++, nameof(OmniPivotColumn<Sale>.Property), p);
            b.CloseComponent();
        }
    };

    private static RenderFragment PivotVals(params (string prop, AggregateFunction agg)[] vals) => b =>
    {
        var seq = 200;
        foreach (var (prop, agg) in vals)
        {
            b.OpenComponent<OmniPivotValue<Sale>>(seq++);
            b.AddAttribute(seq++, nameof(OmniPivotValue<Sale>.Property), prop);
            b.AddAttribute(seq++, nameof(OmniPivotValue<Sale>.Aggregate), agg);
            b.AddAttribute(seq++, nameof(OmniPivotValue<Sale>.FormatString), "{0:0}");
            b.CloseComponent();
        }
    };

    private IRenderedComponent<OmniPivotGrid<Sale>> RenderPivot(
        RenderFragment rows, RenderFragment cols, RenderFragment vals,
        Action<ComponentParameterCollectionBuilder<OmniPivotGrid<Sale>>>? extra = null,
        Sale[]? data = null)
        => Render<OmniPivotGrid<Sale>>(p =>
        {
            p.Add(g => g.Data, data ?? Sales);
            p.Add(g => g.Rows, rows);
            p.Add(g => g.Columns, cols);
            p.Add(g => g.Values, vals);
            extra?.Invoke(p);
        });

    private static string CellText(IElement el) => el.TextContent.Trim();

    // ─── Cross-cutting ────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_and_table()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)));
        Assert.NotNull(cut.Find("div.omni-pivot"));
        Assert.NotNull(cut.Find("table.omni-pivot-table"));
    }

    [Fact]
    public void Appends_Class_and_splats_attributes()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)),
            p => p.Add(g => g.Class, "my-pivot").AddUnmatched("data-testid", "pv1"));
        var root = cut.Find("div.omni-pivot");
        Assert.Contains("my-pivot", root.ClassName);
        Assert.Equal("pv1", root.GetAttribute("data-testid"));
    }

    // ─── Aggregation ──────────────────────────────────────────────────────

    [Fact]
    public void Computes_cell_aggregates()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)));
        var bodyRows = cut.FindAll("tbody.omni-pivot-body tr");
        Assert.Equal(2, bodyRows.Count); // Drink, Food (sorted)

        // Row order: Drink, Food. Cols: 2024, 2025.
        var foodRow = bodyRows[1];
        Assert.Contains("Food", CellText(foodRow.QuerySelector("td.omni-pivot-row-head")!));
        var cells = foodRow.QuerySelectorAll("td.omni-pivot-value");
        Assert.Equal("150", CellText(cells[0])); // Food × 2024 = 100 + 50
        Assert.Equal("200", CellText(cells[1])); // Food × 2025 = 200

        var drinkCells = bodyRows[0].QuerySelectorAll("td.omni-pivot-value");
        Assert.Equal("30", CellText(drinkCells[0]));  // Drink × 2024 = 30
        Assert.Equal("", CellText(drinkCells[1]));     // Drink × 2025 = none
    }

    [Fact]
    public void Count_aggregate_counts_rows()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Count)));
        var foodCells = cut.FindAll("tbody.omni-pivot-body tr")[1].QuerySelectorAll("td.omni-pivot-value");
        Assert.Equal("2", CellText(foodCells[0])); // Food × 2024 → 2 rows
    }

    // ─── Totals ───────────────────────────────────────────────────────────

    [Fact]
    public void Row_totals_aggregate_across_columns()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)),
            p => p.Add(g => g.ShowRowTotals, true));
        var foodTotal = cut.FindAll("tbody.omni-pivot-body tr")[1].QuerySelectorAll("td.omni-pivot-total-cell");
        Assert.Equal("350", CellText(foodTotal[0])); // 100 + 50 + 200
    }

    [Fact]
    public void Column_totals_and_grand_total()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)),
            p => p.Add(g => g.ShowColumnTotals, true).Add(g => g.ShowRowTotals, true));
        var footCells = cut.FindAll("tfoot td.omni-pivot-foot-cell");
        Assert.Equal("180", CellText(footCells[0])); // 2024 = 100+50+30
        Assert.Equal("200", CellText(footCells[1])); // 2025 = 200
        var grand = cut.Find("tfoot td.omni-pivot-foot-grand");
        Assert.Equal("380", CellText(grand)); // 180 + 200
    }

    // ─── Nesting ──────────────────────────────────────────────────────────

    [Fact]
    public void Nested_rows_merge_parent_with_rowspan()
    {
        var cut = RenderPivot(PivotRows("Category", "Region"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)));
        // Food appears under North + South → its category header cell spans 2 rows.
        var foodHeader = cut.FindAll("td.omni-pivot-row-head")
            .First(c => CellText(c) == "Food");
        Assert.Equal("2", foodHeader.GetAttribute("rowspan"));
    }

    [Fact]
    public void Multiple_values_render_a_cell_per_value_per_column()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"),
            PivotVals(("Amount", AggregateFunction.Sum), ("Qty", AggregateFunction.Sum)));
        // 2 column leaves × 2 values = 4 value cells per row.
        var foodCells = cut.FindAll("tbody.omni-pivot-body tr")[1].QuerySelectorAll("td.omni-pivot-value");
        Assert.Equal(4, foodCells.Length);
    }

    // ─── Drill-down ───────────────────────────────────────────────────────

    [Fact]
    public void Collapsing_a_row_group_reduces_visible_rows()
    {
        var cut = RenderPivot(PivotRows("Category", "Region"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)));
        // Leaves: Drink/North, Food/North, Food/South → 3 rows.
        Assert.Equal(3, cut.FindAll("tbody.omni-pivot-body tr").Count);

        // Collapse the "Food" group (its toggle button).
        var foodCell = cut.FindAll("td.omni-pivot-row-head").First(c => CellText(c) == "Food");
        foodCell.QuerySelector("button.omni-pivot-toggle")!.Click();

        // Food collapses into one subtotal row → Drink/North + Food = 2 rows.
        Assert.Equal(2, cut.FindAll("tbody.omni-pivot-body tr").Count);
    }

    // ─── Empty ────────────────────────────────────────────────────────────

    [Fact]
    public void Shows_empty_state_without_data()
    {
        var cut = RenderPivot(PivotRows("Category"), PivotCols("Year"), PivotVals(("Amount", AggregateFunction.Sum)),
            data: System.Array.Empty<Sale>());
        Assert.NotNull(cut.Find(".omni-pivot-empty"));
        Assert.Empty(cut.FindAll("table.omni-pivot-table"));
    }
}
