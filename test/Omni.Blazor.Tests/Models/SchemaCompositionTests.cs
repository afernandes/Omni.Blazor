using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Models;

public sealed class SchemaCompositionTests
{
    private sealed record Product(int Id, string Name, decimal Price);

    private sealed record Metric(string Category, double Value);

    [Fact]
    public void DataGrid_extend_preserves_base_and_replaces_column_in_place()
    {
        DataGridSchema<Product> baseSchema = DataGridSchema<Product>.Create(grid => grid
            .Column(item => item.Name, column => column.Title("Name"))
            .Column(item => item.Price, column => column.Title("Price"))
            .Search(placeholder: "Base search"));

        DataGridSchema<Product> derived = baseSchema.Extend(grid => grid
            .OverrideColumn(item => item.Name, column => column.Title("Product"))
            .Paging(25));

        Assert.Equal(["Name", "Price"], baseSchema.Columns.Select(static column => column.Title));
        Assert.Equal(["Product", "Price"], derived.Columns.Select(static column => column.Title));
        Assert.Equal("Base search", derived.SearchPlaceholder);
        Assert.Equal(25, derived.PageSize);
    }

    [Fact]
    public void DataFilter_extend_can_override_or_replace_the_projection()
    {
        DataFilterSchema<Product> baseSchema = DataFilterSchema<Product>.Create(filter => filter
            .Field(item => item.Name, field => field.Title("Name"))
            .Field(item => item.Price, field => field.Title("Price"))
            .Limits(8, 80));

        DataFilterSchema<Product> overridden = baseSchema.Extend(filter => filter
            .OverrideField(item => item.Name, field => field.Title("Product")));
        DataFilterSchema<Product> replaced = baseSchema.Extend(filter => filter
            .ClearFields()
            .Field(item => item.Id));

        Assert.Equal("Name", baseSchema.Fields[0].Title);
        Assert.Equal("Product", overridden.Fields[0].Title);
        Assert.Single(replaced.Fields);
        Assert.Equal(8, replaced.MaximumDepth);
        Assert.Equal(80, replaced.MaximumRules);
    }

    [Fact]
    public void Chart_extend_snapshots_a_derived_series_without_mutating_the_base()
    {
        Metric[] source = [new("Jan", 10), new("Feb", 20)];
        ChartSchema baseSchema = ChartSchema.Create(chart => chart
            .Series("Revenue", source, item => item.Category, item => item.Value)
            .Size("240px"));

        ChartSchema derived = baseSchema.Extend(chart => chart
            .OverrideSeries("Revenue", source, item => item.Category, item => item.Value,
                series => series.Type(ChartSeriesType.StackedColumn))
            .Size("360px"));

        Assert.Equal(ChartSeriesType.Line, baseSchema.Series[0].Type);
        Assert.Equal("240px", baseSchema.Height);
        Assert.Equal(ChartSeriesType.StackedColumn, derived.Series[0].Type);
        Assert.Equal("360px", derived.Height);
    }

    [Fact]
    public void EntityEditor_extend_reuses_form_and_changes_only_requested_options()
    {
        DataFormSchema<Product> form = DataFormSchema<Product>.Create(dataForm => dataForm
            .Field(item => item.Name));
        EntityEditorSchema<Product, int> baseSchema = EntityEditorSchema<Product, int>.Create(editor => editor
            .Key(item => item.Id)
            .Form(form)
            .Create(() => new Product(0, string.Empty, 0))
            .Edit(item => item with { }));

        EntityEditorSchema<Product, int> derived = baseSchema.Extend(editor => editor
            .Delete(item => $"Delete {item.Name}?"));

        Assert.Null(baseSchema.DeleteOptions);
        Assert.NotNull(derived.DeleteOptions);
        Assert.Same(form, derived.FormSchema);
        Assert.Equal(7, derived.KeySelector(new Product(7, "A", 1)));
    }
}
