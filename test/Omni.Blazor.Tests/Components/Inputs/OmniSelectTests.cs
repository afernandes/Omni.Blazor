using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSelect{TValue}"/> — now a custom
/// dropdown (matching OmniAutoComplete): trigger + panel, placeholder, opening,
/// option rendering, selection (identity / ValueSelector / enum), size, disabled,
/// and the cross-cutting splat.
/// </summary>
public class OmniSelectTests : TestContextBase
{
    [Fact]
    public void Renders_trigger_with_base_class()
    {
        var cut = Render<OmniSelect<string>>();
        Assert.Contains("omni-select", cut.Find(".omni-select").ClassName);
        Assert.NotNull(cut.Find(".omni-select-trigger"));
    }

    [Fact]
    public void Shows_placeholder_when_no_value()
    {
        var cut = Render<OmniSelect<string>>(p => p.Add(c => c.Placeholder, "Selecione..."));
        Assert.Contains("Selecione...", cut.Find(".omni-select-placeholder").TextContent);
    }

    [Fact]
    public void Closed_by_default_opens_on_trigger_click()
    {
        var cut = Render<OmniSelect<string>>(p => p.Add(c => c.Items, new[] { "a", "b" }));
        Assert.Empty(cut.FindAll(".omni-select-panel"));
        cut.Find(".omni-select-trigger").Click();
        Assert.NotNull(cut.Find(".omni-select-panel"));
        Assert.Equal(2, cut.FindAll(".omni-select-option").Count);
    }

    [Fact]
    public void Options_render_text_via_TextSelector()
    {
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.TextSelector, v => v?.ToUpperInvariant() ?? ""));
        cut.Find(".omni-select-trigger").Click();
        var opts = cut.FindAll(".omni-select-option");
        Assert.Equal("A", opts[0].TextContent.Trim());
        Assert.Equal("B", opts[1].TextContent.Trim());
    }

    [Fact]
    public void Selecting_option_sets_value_identity()
    {
        string? captured = null;
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "x", "y" })
            .Add(c => c.Value, "x")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find(".omni-select-trigger").Click();
        cut.FindAll(".omni-select-option")[1].Click(); // "y"
        Assert.Equal("y", captured);
    }

    [Fact]
    public void Selecting_option_with_ValueSelector()
    {
        string? captured = null;
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "x", "y" })
            .Add(c => c.ValueSelector, v => v)
            .Add(c => c.TextSelector, v => v ?? "")
            .Add(c => c.Value, "x")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find(".omni-select-trigger").Click();
        cut.FindAll(".omni-select-option")[1].Click();
        Assert.Equal("y", captured);
    }

    private enum Fruit { Apple, Banana, Cherry }

    [Fact]
    public void Selecting_enum_value()
    {
        Fruit captured = Fruit.Apple;
        var cut = Render<OmniSelect<Fruit>>(p => p
            .Add(c => c.Items, new[] { Fruit.Apple, Fruit.Banana, Fruit.Cherry })
            .Add(c => c.Value, Fruit.Apple)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find(".omni-select-trigger").Click();
        var opts = cut.FindAll(".omni-select-option");
        Assert.Equal("Cherry", opts[2].TextContent.Trim());
        opts[2].Click();
        Assert.Equal(Fruit.Cherry, captured);
    }

    [Fact]
    public void Selected_value_is_shown_in_the_trigger()
    {
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "Alpha", "Beta" })
            .Add(c => c.Value, "Beta"));
        Assert.Contains("Beta", cut.Find(".omni-select-value").TextContent);
    }

    [Fact]
    public void Selected_option_is_marked()
    {
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.Value, "b"));
        cut.Find(".omni-select-trigger").Click();
        var opts = cut.FindAll(".omni-select-option");
        Assert.Contains("omni-selected", opts[1].ClassName);
        Assert.DoesNotContain("omni-selected", opts[0].ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-input-sm")]
    [InlineData(ComponentSize.Lg, "omni-input-lg")]
    public void Size_maps_to_class(ComponentSize size, string expected)
    {
        Assert.Contains(expected, Render<OmniSelect<string>>(p => p.Add(c => c.Size, size)).Find(".omni-select").ClassName);
    }

    [Fact]
    public void Disabled_sets_trigger_attribute()
    {
        var cut = Render<OmniSelect<string>>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find(".omni-select-trigger").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniSelect<string>>(p => p
            .Add(c => c.Class, "custom-cls")
            .Add(c => c.Style, "min-width: 200px")
            .AddUnmatched("data-testid", "sel1"));
        var root = cut.Find(".omni-select");
        Assert.Contains("custom-cls", root.ClassName);
        Assert.Contains("min-width: 200px", root.GetAttribute("style") ?? "");
        Assert.Equal("sel1", root.GetAttribute("data-testid"));
    }
}
