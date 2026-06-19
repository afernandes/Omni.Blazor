using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniBanner"/>: severity→tone/icon, message,
/// dismiss, sticky, actions, and the cross-cutting splat.
/// </summary>
public class OmniBannerTests : TestContextBase
{
    [Fact]
    public void Default_renders_info_tone_with_icon_and_message()
    {
        var cut = Render<OmniBanner>(p => p.AddChildContent("Aviso"));
        var root = cut.Find(".omni-banner");
        Assert.Contains("omni-banner-info", root.ClassName);
        Assert.NotNull(cut.Find(".omni-banner-ico svg"));
        Assert.Contains("Aviso", cut.Find(".omni-banner-body").TextContent);
    }

    [Theory]
    [InlineData(NotificationSeverity.Success, "omni-banner-success")]
    [InlineData(NotificationSeverity.Warning, "omni-banner-warn")]
    [InlineData(NotificationSeverity.Error, "omni-banner-danger")]
    [InlineData(NotificationSeverity.Info, "omni-banner-info")]
    public void Severity_maps_to_tone_class(NotificationSeverity sev, string cls)
    {
        var cut = Render<OmniBanner>(p => p.Add(c => c.Severity, sev));
        Assert.Contains(cls, cut.Find(".omni-banner").ClassName);
    }

    [Fact]
    public void Sticky_adds_modifier()
    {
        var cut = Render<OmniBanner>(p => p.Add(c => c.Sticky, true));
        Assert.Contains("omni-banner-sticky", cut.Find(".omni-banner").ClassName);
    }

    [Fact]
    public void Dismiss_hides_the_banner_and_fires_callback()
    {
        var closed = false;
        var cut = Render<OmniBanner>(p => p
            .Add(c => c.Dismissible, true)
            .Add(c => c.OnClosed, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => closed = true))
            .AddChildContent("x"));
        cut.Find(".omni-banner-close").Click();
        Assert.Empty(cut.FindAll(".omni-banner"));
        Assert.True(closed);
    }

    [Fact]
    public void Renders_actions_fragment()
    {
        var cut = Render<OmniBanner>(p => p
            .Add(c => c.Actions, b => b.AddMarkupContent(0, "<button class=\"act\">Ok</button>")));
        Assert.NotNull(cut.Find(".omni-banner-actions .act"));
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniBanner>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "top:4px")
            .AddUnmatched("data-testid", "b1"));
        var root = cut.Find(".omni-banner");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("top:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("b1", root.GetAttribute("data-testid"));
    }
}
