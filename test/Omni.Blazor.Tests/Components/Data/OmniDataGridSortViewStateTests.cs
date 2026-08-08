using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Sorting must not force the view-state contract on grids that never asked for it.
///
/// Capturing a view state requires a stable PropertyName on every column, which a
/// template or actions column legitimately does not have. Sorting used to capture
/// unconditionally, so clicking a header on such a grid threw
/// "DataGrid view state requires a stable PropertyName on every column" — even with no
/// PersistKey and no ViewStateChanged handler. The requirement still holds for grids
/// that did opt in; that half is asserted here too, so the fix cannot loosen it.
/// </summary>
public class OmniDataGridSortViewStateTests : TestContextBase
{
    private record Person(string Name, int Age);

    private static readonly Person[] Sample =
    [
        new("Alice", 30),
        new("Bob", 25),
        new("Carol", 41)
    ];

    /// <summary>
    /// A sortable data column plus an actions column with neither PropertyName nor Title —
    /// Title is the fallback, so a column needs both missing to have no resolved name.
    /// </summary>
    private static RenderFragment ColumnsWithActionColumn() => b =>
    {
        b.OpenComponent<OmniDataGridColumn<Person>>(0);
        b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
        b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
        b.CloseComponent();

        b.OpenComponent<OmniDataGridColumn<Person>>(3);
        b.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Template),
            (RenderFragment<Person>)(_ => cell => cell.AddMarkupContent(0, "<button>Editar</button>")));
        b.CloseComponent();
    };

    [Fact]
    public void Sorting_a_grid_without_view_state_does_not_require_property_names()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, ColumnsWithActionColumn()));

        // Clicking the sortable header is what the user does; it must not throw.
        cut.FindAll("table.omni-grid-table thead th")[0].Click();

        var firstCell = cut.FindAll("table.omni-grid-table tbody tr td")[0];
        Assert.Contains("Alice", firstCell.TextContent);
    }

    [Fact]
    public void A_grid_with_view_state_still_reports_a_column_without_a_property_name()
    {
        DataGridViewState? captured = null;

        // A grid that opted in captures on its first render, so it fails there — before any
        // sort. The requirement is unchanged; only grids that never opted in stop paying it.
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => Render<OmniDataGrid<Person>>(p => p
                .Add(c => c.Data, Sample)
                .Add(c => c.Columns, ColumnsWithActionColumn())
                .Add(c => c.ViewStateChanged, EventCallback.Factory.Create<DataGridViewState>(
                    this, state => captured = state))));

        Assert.Contains("stable PropertyName", error.Message, StringComparison.Ordinal);
        Assert.Null(captured);
    }

    [Fact]
    public void Sorting_a_grid_with_view_state_and_named_columns_reports_the_new_sort()
    {
        DataGridViewState? captured = null;
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, b =>
            {
                b.OpenComponent<OmniDataGridColumn<Person>>(0);
                b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Name");
                b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
                b.CloseComponent();
            })
            .Add(c => c.ViewStateChanged, EventCallback.Factory.Create<DataGridViewState>(
                this, state => captured = state)));

        cut.FindAll("table.omni-grid-table thead th")[0].Click();

        Assert.NotNull(captured);
        Assert.Equal("Name", Assert.Single(captured.Sort).Property);
    }
}
