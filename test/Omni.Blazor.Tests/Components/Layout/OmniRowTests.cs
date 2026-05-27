using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniRow"/>: 12-column responsive grid
/// container with custom gap (CSS variable).
/// </summary>
public class OmniRowTests : TestContextBase
{
    [Fact]
    public void Renders_default_row_with_base_class_and_gap()
    {
        var cut = RenderComponent<OmniRow>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-row-grid", div.ClassName);
        Assert.Contains("--omni-row-gap: 16px", div.GetAttribute("style") ?? "");
        Assert.Contains("body", div.TextContent);
    }

    [Fact]
    public void Custom_Gap_emits_css_variable()
    {
        var cut = RenderComponent<OmniRow>(p => p
            .Add(c => c.Gap, 24)
            .AddChildContent("X"));

        Assert.Contains("--omni-row-gap: 24px", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniRow>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Appends_consumer_Style_after_gap_variable()
    {
        var cut = RenderComponent<OmniRow>(p => p
            .Add(c => c.Style, "margin-top: 12px")
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-row-gap: 16px", style);
        Assert.Contains("margin-top: 12px", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniRow>(p => p
            .AddUnmatched("data-testid", "row")
            .AddUnmatched("aria-label", "Row")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("row", div.GetAttribute("data-testid"));
        Assert.Equal("Row", div.GetAttribute("aria-label"));
    }
}
