using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniMultiSelect{TValue}"/>: root +
/// trigger rendering, chips visibility, and the cross-cutting splat. The
/// dropdown popup itself lives in <c>OmniPopover</c> and is exercised by
/// integration tests; here we only assert basic structure.
/// </summary>
public class OmniMultiSelectTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" }));

        Assert.NotNull(cut.Find("div.omni-multiselect"));
    }

    [Fact]
    public void Renders_placeholder_when_empty()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Placeholder, "Pick..."));

        Assert.Contains("Pick...", cut.Find("span.omni-multiselect-placeholder").TextContent);
    }

    [Fact]
    public void Renders_one_chip_per_selected_value()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" })
            .Add(c => c.Values, new[] { "a", "c" }));

        Assert.Equal(2, cut.FindAll("span.omni-multiselect-chip").Count);
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Disabled, true));

        Assert.Contains("omni-multiselect-disabled", cut.Find("div.omni-multiselect").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-multiselect").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Style, "min-width: 320px"));

        Assert.Equal("min-width: 320px", cut.Find("div.omni-multiselect").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .AddUnmatched("data-testid", "ms"));

        Assert.Equal("ms", cut.Find("div.omni-multiselect").GetAttribute("data-testid"));
    }

    // ── ParameterState: FieldIdentifier rebuild fires only when ValuesExpression changes ──

    private sealed class Model
    {
        public IEnumerable<string>? Cats { get; set; }
        public IEnumerable<string>? Tags { get; set; }
    }

    [Fact]
    public void Initial_ValuesExpression_triggers_recompute()
    {
        var model = new Model();
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.ValuesExpression, () => model.Cats));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var model = new Model();
        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.ValuesExpression, () => model.Cats));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_ValuesExpression_changes()
    {
        var model = new Model();
        System.Linq.Expressions.Expression<Func<IEnumerable<string>?>> first  = () => model.Cats;
        System.Linq.Expressions.Expression<Func<IEnumerable<string>?>> second = () => model.Tags;

        var cut = RenderComponent<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.ValuesExpression, first));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p.Add(c => c.ValuesExpression, second));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
