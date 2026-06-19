using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDataGridColumn{TItem}"/>: a config
/// component that renders nothing of its own but registers itself with the
/// parent grid. We test it by declaring it inside an
/// <see cref="OmniDataGrid{TItem}"/> and asserting the column header appears.
/// </summary>
public class OmniDataGridColumnTests : TestContextBase
{
    private record Person(string Name, int Age);

    private static readonly Person[] Sample =
    {
        new("Alice", 30),
        new("Bob", 25),
    };

    [Fact]
    public void Registers_with_parent_grid_and_renders_header()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, b =>
            {
                b.OpenComponent<OmniDataGridColumn<Person>>(0);
                b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Nome");
                b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
                b.CloseComponent();
            }));

        var headers = cut.FindAll("table.omni-grid-table thead th");
        Assert.Contains(headers, h => h.TextContent.Contains("Nome"));
    }

    [Fact]
    public void Property_supplies_cell_text()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, b =>
            {
                b.OpenComponent<OmniDataGridColumn<Person>>(0);
                b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Nome");
                b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
                b.CloseComponent();
            }));

        var body = cut.Find("table.omni-grid-table tbody").TextContent;
        Assert.Contains("Alice", body);
        Assert.Contains("Bob", body);
    }

    [Fact]
    public void Template_overrides_default_cell_text()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, b =>
            {
                b.OpenComponent<OmniDataGridColumn<Person>>(0);
                b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Nome");
                b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
                b.AddAttribute(3, nameof(OmniDataGridColumn<Person>.Template),
                    (RenderFragment<Person>)(person => fb => fb.AddMarkupContent(0, $"<em class='templated'>{person.Name}!</em>")));
                b.CloseComponent();
            }));

        var ems = cut.FindAll("em.templated");
        Assert.Equal(2, ems.Count);
        Assert.Contains("Alice!", ems[0].TextContent);
        Assert.Contains("Bob!", ems[1].TextContent);
    }

    [Fact]
    public void Visible_false_hides_column_header()
    {
        var cut = Render<OmniDataGrid<Person>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.Columns, b =>
            {
                b.OpenComponent<OmniDataGridColumn<Person>>(0);
                b.AddAttribute(1, nameof(OmniDataGridColumn<Person>.Title), "Nome");
                b.AddAttribute(2, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Name));
                b.CloseComponent();

                b.OpenComponent<OmniDataGridColumn<Person>>(3);
                b.AddAttribute(4, nameof(OmniDataGridColumn<Person>.Title), "Hidden");
                b.AddAttribute(5, nameof(OmniDataGridColumn<Person>.Visible), false);
                b.AddAttribute(6, nameof(OmniDataGridColumn<Person>.Property), (Func<Person, object?>)(p => p.Age));
                b.CloseComponent();
            }));

        var headers = cut.FindAll("table.omni-grid-table thead th").Select(h => h.TextContent).ToList();
        Assert.Contains(headers, t => t.Contains("Nome"));
        Assert.DoesNotContain(headers, t => t.Contains("Hidden"));
    }
}
