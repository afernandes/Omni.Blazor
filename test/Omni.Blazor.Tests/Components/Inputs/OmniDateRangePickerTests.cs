using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniDateRangePicker"/>: trigger button
/// rendering, placeholder behavior, cross-cutting splat and the shared
/// top-layer floating-popover lifecycle.
/// </summary>
public class OmniDateRangePickerTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class_and_trigger()
    {
        var cut = Render<OmniDateRangePicker>();
        Assert.NotNull(cut.Find("div.omni-daterange"));
        Assert.NotNull(cut.Find("button.omni-daterange-input"));
    }

    [Fact]
    public void Collapsed_trigger_states_aria_expanded_as_false()
    {
        // Um aria-expanded ausente não é "fechado": é "não abre nada".
        var cut = Render<OmniDateRangePicker>();
        Assert.Equal("false", cut.Find("button.omni-daterange-input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Shows_placeholder_when_no_range_set()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .Add(c => c.Placeholder, "Período"));

        Assert.Contains("Período", cut.Find("span.omni-daterange-placeholder").TextContent);
    }

    [Fact]
    public void Shows_values_when_From_and_To_set()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .Add(c => c.From, new DateOnly(2025, 1, 1))
            .Add(c => c.To, new DateOnly(2025, 1, 31)));

        Assert.Equal(2, cut.FindAll("span.omni-daterange-value").Count);
    }

    [Fact]
    public void Disabled_disables_trigger_and_applies_modifier()
    {
        var cut = Render<OmniDateRangePicker>(p => p.Add(c => c.Disabled, true));

        Assert.True(cut.Find("button.omni-daterange-input").HasAttribute("disabled"));
        Assert.Contains("omni-daterange-disabled", cut.Find("div.omni-daterange").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniDateRangePicker>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-daterange").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniDateRangePicker>(p => p.Add(c => c.Style, "min-width: 320px"));
        Assert.Equal("min-width: 320px", cut.Find("div.omni-daterange").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .AddUnmatched("data-testid", "drp"));

        Assert.Equal("drp", cut.Find("div.omni-daterange").GetAttribute("data-testid"));
    }

    [Fact]
    public async Task Open_promotes_the_range_panel_through_the_floating_popover_lifecycle()
    {
        var cut = Render<OmniDateRangePicker>();

        await cut.Find("button.omni-daterange-input").ClickAsync(new());

        var popover = cut.Find(".omni-daterange-popover");
        Assert.Equal("manual", popover.GetAttribute("popover"));
        Assert.Equal("dialog", popover.GetAttribute("role"));
        Assert.Equal(popover.Id, cut.Find(".omni-daterange-input").GetAttribute("aria-controls"));
        JSInterop.VerifyInvoke("omniBlazor.floatingPopoverOpen");

        await cut.InvokeAsync(cut.Instance.OnClickOutsideAsync);

        Assert.Empty(cut.FindAll(".omni-daterange-popover"));
        JSInterop.VerifyInvoke("omniBlazor.floatingPopoverClose");
    }

    // ── ParameterState: pending sync fires only on From/To/FromExpression changes ──

    [Fact]
    public void Initial_From_To_populate_pending_on_first_render()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .Add(c => c.From, new DateOnly(2025, 1, 1))
            .Add(c => c.To, new DateOnly(2025, 1, 31)));

        // Initial detect fires both From + To + FromExpression handlers (3).
        Assert.True(cut.Instance.RecomputeCount >= 2);
        // DOM: two value spans rendered.
        Assert.Equal(2, cut.FindAll("span.omni-daterange-value").Count);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .Add(c => c.From, new DateOnly(2025, 1, 1))
            .Add(c => c.To, new DateOnly(2025, 1, 31)));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_From_changes()
    {
        var cut = Render<OmniDateRangePicker>(p => p
            .Add(c => c.From, new DateOnly(2025, 1, 1))
            .Add(c => c.To, new DateOnly(2025, 1, 31)));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.From, new DateOnly(2025, 2, 1)));

        Assert.True(cut.Instance.RecomputeCount > baseline);
    }
}
