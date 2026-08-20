using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniMultiListBox{TValue}"/>: the multi-value
/// half of the listbox pair. Covers option rendering, toggle semantics over the
/// shared <c>@bind-Value</c> collection contract, EditContext integration and the
/// cross-cutting splat. Single selection lives in <see cref="OmniListBox{TValue}"/>.
/// </summary>
public class OmniMultiListBoxTests : TestContextBase
{
    private sealed class Model
    {
        public IEnumerable<string>? Tags { get; set; }
    }

    [Fact]
    public void Renders_listbox_role_advertising_multi_selection()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" }));

        var root = cut.Find("div.omni-listbox");
        Assert.Equal("listbox", root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("aria-multiselectable"));
        Assert.Contains("omni-listbox-multi", root.ClassName);
    }

    [Fact]
    public void Renders_one_option_per_item()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" }));

        Assert.Equal(3, cut.FindAll("div.omni-listbox-item").Count);
    }

    [Fact]
    public void Marks_selected_items_from_the_bound_collection()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" })
            .Add(c => c.Value, new[] { "a", "c" }));

        var items = cut.FindAll("div.omni-listbox-item");
        Assert.Equal("true", items[0].GetAttribute("aria-selected"));
        Assert.Equal("false", items[1].GetAttribute("aria-selected"));
        Assert.Equal("true", items[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Click_adds_the_item_to_the_selection()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.Value, new[] { "a" })
            .Add(c => c.ValueChanged, (IEnumerable<string>? v) => captured = v));

        cut.FindAll("div.omni-listbox-item")[1].Click();

        Assert.Equal(["a", "b"], captured);
    }

    [Fact]
    public void Click_on_a_selected_item_removes_it()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.Value, new[] { "a", "b" })
            .Add(c => c.ValueChanged, (IEnumerable<string>? v) => captured = v));

        cut.FindAll("div.omni-listbox-item")[0].Click();

        Assert.Equal(["b"], captured);
    }

    [Fact]
    public void Click_on_a_disabled_item_changes_nothing()
    {
        var raised = false;
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.DisabledSelector, v => v == "b")
            .Add(c => c.ValueChanged, (IEnumerable<string>? _) => raised = true));

        cut.FindAll("div.omni-listbox-item")[1].Click();

        Assert.False(raised);
    }

    [Fact]
    public void ValueExpression_builds_the_FieldIdentifier()
    {
        var model = new Model();
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.ValueExpression, () => model.Tags));

        Assert.True(cut.Instance.HasFieldIdentifier);
        Assert.Equal(nameof(Model.Tags), cut.Instance.FieldId.FieldName);
    }

    [Fact]
    public void Empty_selection_does_not_count_as_a_value_for_Required()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Value, Array.Empty<string>()));

        Assert.False(((IOmniFormComponent)cut.Instance).HasValue);
    }

    [Fact]
    public void Non_empty_selection_counts_as_a_value_for_Required()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Value, new[] { "a" }));

        Assert.True(((IOmniFormComponent)cut.Instance).HasValue);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Emptiness_is_read_correctly_from_a_collection_without_non_generic_ICollection(
        int count,
        bool expected)
    {
        // HasValue reads Count when the value is a non-generic ICollection (List, array)
        // and enumerates otherwise. HashSet<T> is the "otherwise": it implements
        // ICollection<T> but not ICollection, so it exercises the fallback path.
        var selection = new HashSet<string>(Enumerable.Range(0, count).Select(i => $"item{i}"));
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "item0" })
            .Add(c => c.Value, selection));

        Assert.Equal(expected, ((IOmniFormComponent)cut.Instance).HasValue);
    }

    [Fact]
    public void Disabled_applies_modifier_and_sets_tabindex_minus_one()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Disabled, true));

        var root = cut.Find("div.omni-listbox");
        Assert.Contains("omni-listbox-disabled", root.ClassName);
        Assert.Equal("-1", root.GetAttribute("tabindex"));
    }

    [Fact]
    public void MaxHeight_renders_in_style()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.MaxHeight, "400px"));

        Assert.Contains("max-height:400px", cut.Find("div.omni-listbox").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-listbox").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Style, "opacity: 0.5"));

        Assert.Contains("opacity: 0.5", cut.Find("div.omni-listbox").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniMultiListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .AddUnmatched("data-testid", "mlb"));

        Assert.Equal("mlb", cut.Find("div.omni-listbox").GetAttribute("data-testid"));
    }
}
