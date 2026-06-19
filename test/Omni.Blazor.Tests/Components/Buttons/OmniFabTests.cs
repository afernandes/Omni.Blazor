using Microsoft.AspNetCore.Components.Web;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniFab"/>: position presets, icon-vs-
/// extended layout, accessibility text fall-through, and the Class/Style/
/// Attributes splat. OmniFab wraps an inner <see cref="OmniButton"/> — the
/// root element rendered is therefore a <c>&lt;button&gt;</c>.
/// </summary>
public class OmniFabTests : TestContextBase
{
    [Fact]
    public void Renders_default_button_with_fab_classes()
    {
        var cut = Render<OmniFab>();

        var btn = cut.Find("button");
        Assert.Contains("omni-fab", btn.ClassName);
        Assert.Contains("omni-fab-bottom-right", btn.ClassName);
        Assert.Contains("omni-fab-icon", btn.ClassName);
    }

    [Theory]
    [InlineData(FabPosition.BottomRight,  "omni-fab-bottom-right")]
    [InlineData(FabPosition.BottomLeft,   "omni-fab-bottom-left")]
    [InlineData(FabPosition.TopRight,     "omni-fab-top-right")]
    [InlineData(FabPosition.TopLeft,      "omni-fab-top-left")]
    [InlineData(FabPosition.BottomCenter, "omni-fab-bottom-center")]
    [InlineData(FabPosition.Static,       "omni-fab-static")]
    public void Applies_position_modifier(FabPosition pos, string expectedClass)
    {
        var cut = Render<OmniFab>(p => p.Add(c => c.Position, pos));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Fact]
    public void Setting_Text_switches_to_extended_layout()
    {
        var cut = Render<OmniFab>(p => p.Add(c => c.Text, "Compose"));

        var btn = cut.Find("button");
        Assert.Contains("omni-fab-extended", btn.ClassName);
        Assert.DoesNotContain("omni-fab-icon", btn.ClassName);
        Assert.Contains("Compose", btn.TextContent);
    }

    [Fact]
    public void Empty_Text_keeps_icon_only_layout()
    {
        var cut = Render<OmniFab>(p => p.Add(c => c.Text, ""));

        var btn = cut.Find("button");
        Assert.Contains("omni-fab-icon", btn.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFab>(p => p.Add(c => c.Class, "custom-fab"));

        Assert.Contains("custom-fab", cut.Find("button").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFab>(p => p.Add(c => c.Style, "z-index: 999"));

        Assert.Contains("z-index: 999", cut.Find("button").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFab>(p => p
            .AddUnmatched("data-testid", "fab")
            .AddUnmatched("id", "main-fab"));

        var btn = cut.Find("button");
        Assert.Equal("fab", btn.GetAttribute("data-testid"));
        Assert.Equal("main-fab", btn.GetAttribute("id"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_Title_then_Text()
    {
        // AriaLabel explicit wins.
        var cutExplicit = Render<OmniFab>(p => p
            .Add(c => c.AriaLabel, "explicit")
            .Add(c => c.Title, "title")
            .Add(c => c.Text, "text"));
        Assert.Equal("explicit", cutExplicit.Find("button").GetAttribute("aria-label"));

        // No AriaLabel -> Title.
        var cutTitle = Render<OmniFab>(p => p
            .Add(c => c.Title, "title")
            .Add(c => c.Text, "text"));
        Assert.Equal("title", cutTitle.Find("button").GetAttribute("aria-label"));

        // No AriaLabel + no Title -> Text.
        var cutText = Render<OmniFab>(p => p.Add(c => c.Text, "text"));
        Assert.Equal("text", cutText.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void OnClick_fires_when_enabled()
    {
        var clicks = 0;
        var cut = Render<OmniFab>(p => p
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find("button").Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Disabled_blocks_click_and_sets_attribute()
    {
        var clicks = 0;
        var cut = Render<OmniFab>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        btn.Click();
        Assert.Equal(0, clicks);
    }
}
