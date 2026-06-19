using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniTagInput"/>: chip rendering, the
/// cross-cutting splat, and the keyboard-free entry paths (comma/paste via
/// <c>oninput</c>, chip removal, suggestion panel). The Enter/Backspace paths
/// run through <c>KeyInterceptorService</c> (real JS) and are covered by
/// integration tests; here we exercise everything reachable without a browser.
/// </summary>
public class OmniTagInputTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class()
    {
        var cut = Render<OmniTagInput>();

        Assert.NotNull(cut.Find("div.omni-taginput"));
        Assert.NotNull(cut.Find("input.omni-taginput-entry"));
    }

    [Fact]
    public void Renders_one_chip_per_value()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "a", "b", "c" }));

        Assert.Equal(3, cut.FindAll(".omni-taginput-chip").Count);
    }

    [Fact]
    public void Renders_remove_button_per_chip_when_editable()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "a", "b" }));

        Assert.Equal(2, cut.FindAll(".omni-taginput-chip-x").Count);
    }

    [Fact]
    public void ReadOnly_hides_remove_buttons()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "a", "b" })
            .Add(c => c.ReadOnly, true));

        Assert.Empty(cut.FindAll(".omni-taginput-chip-x"));
    }

    [Fact]
    public void Placeholder_shown_when_empty()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Placeholder, "Add tag..."));

        Assert.Equal("Add tag...", cut.Find("input.omni-taginput-entry").GetAttribute("placeholder"));
    }

    [Fact]
    public void Placeholder_hidden_when_has_tags()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Placeholder, "Add tag...")
            .Add(c => c.Values, new[] { "a" }));

        var ph = cut.Find("input.omni-taginput-entry").GetAttribute("placeholder");
        Assert.True(string.IsNullOrEmpty(ph));
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = Render<OmniTagInput>(p => p.Add(c => c.Disabled, true));

        Assert.Contains("omni-taginput-disabled", cut.Find("div.omni-taginput").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniTagInput>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-taginput").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniTagInput>(p => p.Add(c => c.Style, "min-width: 320px"));

        Assert.Equal("min-width: 320px", cut.Find("div.omni-taginput").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniTagInput>(p => p.AddUnmatched("data-testid", "ti"));

        Assert.Equal("ti", cut.Find("div.omni-taginput").GetAttribute("data-testid"));
    }

    // ── Entry paths reachable without JS (comma / paste via oninput) ──

    [Fact]
    public void Comma_input_adds_tag_and_fires_ValuesChanged()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.ValuesChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => captured = v)));

        cut.Find("input.omni-taginput-entry").Input("react,");

        Assert.NotNull(captured);
        Assert.Contains("react", captured!);
        Assert.Single(cut.FindAll(".omni-taginput-chip"));
    }

    [Fact]
    public void Pasted_csv_splits_into_multiple_tags()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>()));

        cut.Find("input.omni-taginput-entry").Input("a,b,c,");

        Assert.Equal(3, cut.FindAll(".omni-taginput-chip").Count);
    }

    [Fact]
    public void Duplicate_ignored_by_default()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "react" }));

        cut.Find("input.omni-taginput-entry").Input("react,");

        Assert.Single(cut.FindAll(".omni-taginput-chip"));
    }

    [Fact]
    public void Duplicate_allowed_when_AllowDuplicates()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "react" })
            .Add(c => c.AllowDuplicates, true));

        cut.Find("input.omni-taginput-entry").Input("react,");

        Assert.Equal(2, cut.FindAll(".omni-taginput-chip").Count);
    }

    [Fact]
    public void Max_limits_tag_count()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.Max, 2));

        cut.Find("input.omni-taginput-entry").Input("a,");
        cut.Find("input.omni-taginput-entry").Input("b,");
        cut.Find("input.omni-taginput-entry").Input("c,");

        Assert.Equal(2, cut.FindAll(".omni-taginput-chip").Count);
    }

    [Fact]
    public void Blank_input_does_not_add_empty_tag()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>()));

        cut.Find("input.omni-taginput-entry").Input("   ,");

        Assert.Empty(cut.FindAll(".omni-taginput-chip"));
    }

    [Fact]
    public void ReadOnly_blocks_adding_tags()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.ReadOnly, true));

        cut.Find("input.omni-taginput-entry").Input("nope,");

        Assert.Empty(cut.FindAll(".omni-taginput-chip"));
    }

    [Fact]
    public void Removing_chip_fires_ValuesChanged_without_tag()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "a", "b" })
            .Add(c => c.ValuesChanged, EventCallback.Factory.Create<IEnumerable<string>>(this, v => captured = v)));

        cut.FindAll(".omni-taginput-chip-x")[0].Click();

        Assert.NotNull(captured);
        Assert.DoesNotContain("a", captured!);
        Assert.Contains("b", captured!);
    }

    [Fact]
    public void AllowCustom_false_rejects_values_outside_pool()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.Suggestions, new[] { "read", "write" })
            .Add(c => c.AllowCustom, false));

        cut.Find("input.omni-taginput-entry").Input("delete,"); // not in pool → rejected
        Assert.Empty(cut.FindAll(".omni-taginput-chip"));

        cut.Find("input.omni-taginput-entry").Input("read,");   // in pool → accepted
        Assert.Single(cut.FindAll(".omni-taginput-chip"));
    }

    // ── Suggestions panel ──

    [Fact]
    public void Suggestions_panel_renders_on_focus()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.Suggestions, new[] { "react", "vue", "svelte" }));

        cut.Find("input.omni-taginput-entry").Focus();

        Assert.Equal(3, cut.FindAll(".omni-taginput-option").Count);
    }

    [Fact]
    public void Suggestions_filtered_by_typed_text()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, Array.Empty<string>())
            .Add(c => c.Suggestions, new[] { "react", "vue", "svelte" }));

        cut.Find("input.omni-taginput-entry").Input("re");

        var opts = cut.FindAll(".omni-taginput-option");
        Assert.Single(opts);
        Assert.Contains("react", opts[0].TextContent);
    }

    [Fact]
    public void Already_added_value_excluded_from_suggestions()
    {
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.Values, new[] { "react" })
            .Add(c => c.Suggestions, new[] { "react", "vue", "svelte" }));

        cut.Find("input.omni-taginput-entry").Focus();

        Assert.Equal(2, cut.FindAll(".omni-taginput-option").Count); // react excluded
    }

    // ── ParameterState: FieldIdentifier rebuild fires only when ValuesExpression changes ──

    private sealed class Model
    {
        public IEnumerable<string>? Tags { get; set; }
        public IEnumerable<string>? Scopes { get; set; }
    }

    [Fact]
    public void Initial_ValuesExpression_triggers_recompute()
    {
        var model = new Model();
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.ValuesExpression, () => model.Tags));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var model = new Model();
        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.ValuesExpression, () => model.Tags));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_ValuesExpression_changes()
    {
        var model = new Model();
        System.Linq.Expressions.Expression<Func<IEnumerable<string>?>> first = () => model.Tags;
        System.Linq.Expressions.Expression<Func<IEnumerable<string>?>> second = () => model.Scopes;

        var cut = Render<OmniTagInput>(p => p
            .Add(c => c.ValuesExpression, first));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.ValuesExpression, second));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
