using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDataFilter{TItem}"/> + its item/property
/// pieces: rendering, adding/removing conditions and groups, the logic toggle,
/// type-aware value editors, and — most importantly — that the built predicate
/// filters the data and raises <c>Filter</c>.
/// </summary>
public class OmniDataFilterTests : TestContextBase
{
    public record Person(string Name, int Age, bool Active);

    private static readonly Person[] People =
    {
        new("Ana", 28, true),
        new("Bruno", 41, false),
        new("Carla", 35, true),
        new("Diego", 23, true),
    };

    private IRenderedComponent<OmniDataFilter<Person>> RenderFilter(
        Action<IEnumerable<Person>>? onFilter = null,
        Action<ComponentParameterCollectionBuilder<OmniDataFilter<Person>>>? extra = null)
        => RenderComponent<OmniDataFilter<Person>>(p =>
        {
            p.Add(c => c.Data, (IEnumerable<Person>)People);
            if (onFilter is not null)
                p.Add(c => c.Filter, EventCallback.Factory.Create<IEnumerable<Person>>(this, onFilter));
            p.AddChildContent<OmniDataFilterProperty>(pp => pp.Add(c => c.Property, "Name").Add(c => c.Title, "Nome"));
            p.AddChildContent<OmniDataFilterProperty>(pp => pp.Add(c => c.Property, "Age").Add(c => c.Title, "Idade").Add(c => c.Type, ColumnFilterType.Number));
            p.AddChildContent<OmniDataFilterProperty>(pp => pp.Add(c => c.Property, "Active").Add(c => c.Title, "Ativo").Add(c => c.Type, ColumnFilterType.Boolean));
            extra?.Invoke(p);
        });

    private static void AddCondition(IRenderedComponent<OmniDataFilter<Person>> cut)
        => cut.FindAll(".omni-datafilter-add")[0].Click();

    // Drive a custom OmniSelect inside a condition: open it, then click the option by label.
    // kind = "omni-datafilter-prop" | "omni-datafilter-op".
    private static void Pick(IRenderedComponent<OmniDataFilter<Person>> cut, int condIndex, string kind, string label)
    {
        cut.FindAll(".omni-datafilter-condition")[condIndex].QuerySelector($".{kind} .omni-select-trigger")!.Click();
        cut.FindAll(".omni-datafilter-condition")[condIndex]
            .QuerySelectorAll(".omni-select-option").First(o => o.TextContent.Trim() == label).Click();
    }

    [Fact]
    public void Renders_root_with_add_buttons()
    {
        var cut = RenderFilter();
        Assert.NotNull(cut.Find(".omni-datafilter"));
        Assert.NotNull(cut.Find(".omni-datafilter-root"));
        Assert.Equal(2, cut.FindAll(".omni-datafilter-add").Count); // add condition + add group
    }

    [Fact]
    public void AllowGroups_false_hides_add_group_button()
    {
        var cut = RenderFilter(extra: p => p.Add(c => c.AllowGroups, false));
        Assert.Single(cut.FindAll(".omni-datafilter-add"));
    }

    [Fact]
    public void Add_condition_creates_a_row_with_property_and_operator_selects()
    {
        var cut = RenderFilter();
        AddCondition(cut);
        var cond = cut.Find(".omni-datafilter-condition");
        Assert.Equal(2, cond.QuerySelectorAll(".omni-select").Length); // property + operator (custom dropdowns)
        Assert.NotNull(cond.QuerySelector(".omni-datafilter-input")); // text value editor (Contains)
    }

    [Fact]
    public void Add_group_creates_a_nested_group()
    {
        var cut = RenderFilter();
        Assert.Single(cut.FindAll(".omni-datafilter-group")); // only the root
        cut.FindAll(".omni-datafilter-add")[1].Click(); // add group
        Assert.Equal(2, cut.FindAll(".omni-datafilter-group").Count); // root + nested
    }

    [Fact]
    public void Text_contains_filters_and_raises_Filter()
    {
        IEnumerable<Person>? view = null;
        var cut = RenderFilter(onFilter: v => view = v);
        AddCondition(cut); // default: Name / Contains / (empty) → all match
        cut.Find(".omni-datafilter-condition .omni-datafilter-input").Input("an");
        Assert.NotNull(view);
        Assert.Equal(new[] { "Ana" }, view!.Select(p => p.Name));
    }

    [Fact]
    public void Number_greater_than_filters()
    {
        IEnumerable<Person>? view = null;
        var cut = RenderFilter(onFilter: v => view = v);
        AddCondition(cut);

        Pick(cut, 0, "omni-datafilter-prop", "Idade");
        Pick(cut, 0, "omni-datafilter-op", "Maior que");
        cut.Find(".omni-datafilter-condition .omni-numeric-input").Change("30");

        Assert.Equal(new[] { "Bruno", "Carla" }, view!.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public void Boolean_equals_filters()
    {
        IEnumerable<Person>? view = null;
        var cut = RenderFilter(onFilter: v => view = v);
        AddCondition(cut);

        Pick(cut, 0, "omni-datafilter-prop", "Ativo"); // boolean → default value true
        // operator defaults to Equals; value defaults to true → keep
        Assert.Equal(new[] { "Ana", "Carla", "Diego" }, view!.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public void IsEmpty_operator_hides_value_editor()
    {
        var cut = RenderFilter();
        AddCondition(cut);
        Pick(cut, 0, "omni-datafilter-op", "Vazio");
        Assert.Empty(cut.FindAll(".omni-datafilter-condition .omni-datafilter-input"));
    }

    [Fact]
    public void Remove_condition_removes_the_row()
    {
        var cut = RenderFilter();
        AddCondition(cut);
        Assert.Single(cut.FindAll(".omni-datafilter-condition"));
        cut.Find(".omni-datafilter-condition .omni-datafilter-remove").Click();
        Assert.Empty(cut.FindAll(".omni-datafilter-condition"));
    }

    [Fact]
    public void Two_conditions_combine_with_AND()
    {
        IEnumerable<Person>? view = null;
        var cut = RenderFilter(onFilter: v => view = v);

        AddCondition(cut); // condition 1: Name / Contains
        cut.FindAll(".omni-datafilter-condition")[0].QuerySelector(".omni-datafilter-input")!.Input("a"); // Ana, Carla, Diego

        AddCondition(cut); // condition 2: Idade / Maior que 30
        Pick(cut, 1, "omni-datafilter-prop", "Idade");
        Pick(cut, 1, "omni-datafilter-op", "Maior que");
        cut.FindAll(".omni-datafilter-condition")[1].QuerySelector(".omni-numeric-input")!.Change("30");

        // Name contains "a" AND Age > 30 → Carla (35). (Ana 28 no, Diego 23 no, Bruno has no "a"? "Bruno" no)
        Assert.Equal(new[] { "Carla" }, view!.Select(p => p.Name));
    }

    [Fact]
    public void Root_logic_toggle_switches_active()
    {
        var cut = RenderFilter();
        var btns = cut.Find(".omni-datafilter-root").QuerySelectorAll(".omni-datafilter-logic-btn");
        Assert.Contains("omni-active", btns[0].ClassName); // And active by default
        btns[1].Click(); // Or
        Assert.Contains("omni-active",
            cut.Find(".omni-datafilter-root").QuerySelectorAll(".omni-datafilter-logic-btn")[1].ClassName);
    }

    [Fact]
    public void Clear_removes_all_conditions()
    {
        var cut = RenderFilter();
        AddCondition(cut);
        AddCondition(cut);
        Assert.Equal(2, cut.FindAll(".omni-datafilter-condition").Count);
        // clear-all action button
        cut.Find(".omni-datafilter-actions").QuerySelector("button")!.Click();
        Assert.Empty(cut.FindAll(".omni-datafilter-condition"));
    }

    [Fact]
    public void Seeded_rules_render_and_apply_on_initial_render()
    {
        IEnumerable<Person>? view = null;
        var seeded = new List<OmniFilterRule>
        {
            new() { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 30.0 }
        };
        var cut = RenderFilter(onFilter: v => view = v, extra: p => p.Add(c => c.Rules, seeded));

        // The code-set condition renders without any interaction...
        var cond = cut.Find(".omni-datafilter-condition");
        Assert.Contains("30", cond.QuerySelector(".omni-numeric-input")!.GetAttribute("value") ?? "");

        // ...and the filter is applied on init (Filter raised, View computed).
        Assert.NotNull(view);
        Assert.Equal(new[] { "Bruno", "Carla" }, view!.Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(new[] { "Bruno", "Carla" }, cut.Instance.View.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public void Disabled_adds_class()
    {
        Assert.Contains("omni-datafilter-disabled",
            RenderFilter(extra: p => p.Add(c => c.Disabled, true)).Find(".omni-datafilter").ClassName);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderFilter(extra: p => p
            .Add(c => c.Class, "qb")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "df1"));
        var root = cut.Find(".omni-datafilter");
        Assert.Contains("qb", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("df1", root.GetAttribute("data-testid"));
    }
}
