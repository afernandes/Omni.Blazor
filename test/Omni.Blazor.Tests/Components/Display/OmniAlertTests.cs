using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniAlert"/>: severities, dismiss,
/// title rendering, and the cross-cutting splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniAlertTests : TestContextBase
{
    [Fact]
    public void Renders_default_info_alert()
    {
        var cut = Render<OmniAlert>(p => p.AddChildContent("Message"));

        var root = cut.Find("div.omni-alert");
        Assert.Contains("omni-alert", root.ClassName);
        Assert.Contains("omni-alert-info", root.ClassName);
        Assert.Equal("alert", root.GetAttribute("role"));
        Assert.Contains("Message", root.TextContent);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info,    "omni-alert-info")]
    [InlineData(NotificationSeverity.Success, "omni-alert-success")]
    [InlineData(NotificationSeverity.Warning, "omni-alert-warn")]
    [InlineData(NotificationSeverity.Error,   "omni-alert-danger")]
    public void Applies_severity_modifier(NotificationSeverity sev, string expected)
    {
        var cut = Render<OmniAlert>(p => p
            .Add(c => c.Severity, sev)
            .AddChildContent("x"));

        Assert.Contains(expected, cut.Find("div.omni-alert").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniAlert>(p => p
            .Add(c => c.Class, "my-alert")
            .AddChildContent("x"));

        Assert.Contains("my-alert", cut.Find("div.omni-alert").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniAlert>(p => p
            .Add(c => c.Style, "margin-top: 4px")
            .AddChildContent("x"));

        Assert.Equal("margin-top: 4px", cut.Find("div.omni-alert").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniAlert>(p => p
            .AddUnmatched("data-testid", "warn-alert")
            .AddChildContent("x"));

        Assert.Equal("warn-alert", cut.Find("div.omni-alert").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_title_when_provided()
    {
        var cut = Render<OmniAlert>(p => p
            .Add(c => c.Title, "Heads up")
            .AddChildContent("body"));

        Assert.Contains("Heads up", cut.Find(".omni-alert-title").TextContent);
    }

    [Fact]
    public void Dismissible_fires_OnClosed_and_hides_alert()
    {
        var closed = 0;
        var cut = Render<OmniAlert>(p => p
            .Add(c => c.Dismissible, true)
            .Add(c => c.OnClosed, () => closed++)
            .AddChildContent("x"));

        cut.Find("button.omni-alert-close").Click();
        Assert.Equal(1, closed);
        // After close, the alert is removed from the tree.
        Assert.Empty(cut.FindAll("div.omni-alert"));
    }
}
