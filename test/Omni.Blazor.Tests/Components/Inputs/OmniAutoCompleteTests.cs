using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniAutoComplete{TItem}"/>: input
/// rendering, chevron vs clear button, and the cross-cutting splat. Dropdown
/// search/filter is covered indirectly through user-driven input events.
/// </summary>
public class OmniAutoCompleteTests : TestContextBase
{
    [Fact]
    public void Renders_input_with_autocomplete_classes()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? ""));

        var root = cut.Find("div.omni-autocomplete");
        Assert.Contains("omni-input-group", root.ClassName);
        Assert.NotNull(cut.Find("input.omni-autocomplete-input"));
    }

    [Fact]
    public void Shows_chevron_when_no_value()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? ""));

        // The right slot holds a chevron span (not the clear button).
        Assert.Empty(cut.FindAll("button.omni-input-clear"));
    }

    [Fact]
    public void Shows_clear_button_when_Clearable_and_Value_present()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "abc")
            .Add(c => c.Clearable, true));

        Assert.NotEmpty(cut.FindAll("button.omni-input-clear"));
    }

    [Fact]
    public void Disabled_propagates_to_input()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-autocomplete").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Style, "min-width: 240px"));

        Assert.Equal("min-width: 240px", cut.Find("div.omni-autocomplete").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .AddUnmatched("data-testid", "ac"));

        Assert.Equal("ac", cut.Find("div.omni-autocomplete").GetAttribute("data-testid"));
    }

    // ── ParameterState: derived state recomputes only when Value changes ──

    [Fact]
    public void Initial_Value_seeds_recompute_on_first_render()
    {
        // Derived state (RecomputeCount) populates on first detect cycle.
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "new-cls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_Value_changes()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Value, "beta"));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
