using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniStatusBadge"/>: kinds, pulse, label,
/// meta, cross-cutting splat.
/// </summary>
public class OmniStatusBadgeTests : TestContextBase
{
    [Fact]
    public void Renders_default_neutral_with_label()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "Online"));

        var root = cut.Find("span.omni-status");
        Assert.Contains("omni-status", root.ClassName);
        Assert.Contains("omni-status-neutral", root.ClassName);
        Assert.Contains("Online", root.TextContent);
    }

    [Theory]
    [InlineData(StatusBadgeKind.Neutral, "omni-status-neutral")]
    [InlineData(StatusBadgeKind.Accent,  "omni-status-accent")]
    [InlineData(StatusBadgeKind.Good,    "omni-status-good")]
    [InlineData(StatusBadgeKind.Warn,    "omni-status-warn")]
    [InlineData(StatusBadgeKind.Danger,  "omni-status-danger")]
    public void Applies_kind_class(StatusBadgeKind kind, string expected)
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "X")
            .Add(c => c.Kind, kind));

        Assert.Contains(expected, cut.Find("span.omni-status").ClassName);
    }

    [Fact]
    public void Pulse_adds_modifier_to_dot()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "Live")
            .Add(c => c.Pulse, true));

        Assert.Contains("omni-pulse", cut.Find(".omni-status-dot").ClassName);
    }

    [Fact]
    public void Meta_renders_when_set()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "Ready")
            .Add(c => c.Meta, "08:42"));

        Assert.Contains("08:42", cut.Find(".omni-status-meta").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "X")
            .Add(c => c.Class, "my-sb"));

        var cls = cut.Find("span.omni-status").ClassName;
        // The class should appear (de-duped in case the legacy Cls() also forwarded it).
        Assert.Contains("my-sb", cls);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "X")
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("span.omni-status").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniStatusBadge>(p => p
            .Add(c => c.Label, "X")
            .AddUnmatched("data-testid", "sb1"));

        Assert.Equal("sb1", cut.Find("span.omni-status").GetAttribute("data-testid"));
    }
}
