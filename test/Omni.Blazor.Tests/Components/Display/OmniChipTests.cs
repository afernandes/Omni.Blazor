using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniChip"/>: variants, active, click,
/// and cross-cutting splat.
/// </summary>
public class OmniChipTests : TestContextBase
{
    [Fact]
    public void Renders_default_chip_with_text()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "Filter"));

        var btn = cut.Find("button.omni-chip");
        Assert.Contains("omni-chip", btn.ClassName);
        Assert.Contains("Filter", btn.TextContent);
        Assert.Equal("button", btn.GetAttribute("type"));
    }

    [Fact]
    public void Active_adds_modifier_class()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Active, true));

        Assert.Contains("omni-chip-active", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Accent_adds_modifier_class()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Accent, true));

        Assert.Contains("omni-chip-accent", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Static_adds_modifier_class()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Static, true));

        Assert.Contains("omni-chip-static", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "my-chip"));

        Assert.Contains("my-chip", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("button.omni-chip").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "chip1"));

        Assert.Equal("chip1", cut.Find("button.omni-chip").GetAttribute("data-testid"));
    }

    [Fact]
    public void OnClick_fires_with_event_args()
    {
        var fired = 0;
        var cut = RenderComponent<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.OnClick, (MouseEventArgs _) => fired++));

        cut.Find("button.omni-chip").Click();
        Assert.Equal(1, fired);
    }
}
