using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCard"/>: title/subtitle/header,
/// variants, clickable, and cross-cutting splat.
/// </summary>
public class OmniCardTests : TestContextBase
{
    [Fact]
    public void Renders_default_root_with_base_class()
    {
        var cut = Render<OmniCard>(p => p.AddChildContent("body"));

        var root = cut.Find("div.omni-card");
        Assert.Contains("omni-card", root.ClassName);
    }

    [Fact]
    public void Renders_title_and_subtitle_in_header()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Title, "Hi")
            .Add(c => c.Subtitle, "World"));

        Assert.Contains("Hi", cut.Find(".omni-card-title").TextContent);
        Assert.Contains("World", cut.Find(".omni-card-sub").TextContent);
    }

    [Fact]
    public void Elevated_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Elevated, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-elevated", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Flat_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Flat, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-flat", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Clickable_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Clickable, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-clickable", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void OnClick_fires_when_card_clicked()
    {
        var fired = 0;
        var cut = Render<OmniCard>(p => p
            .Add(c => c.OnClick, (MouseEventArgs _) => fired++)
            .AddChildContent("x"));

        cut.Find("div.omni-card").Click();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Class, "my-card")
            .AddChildContent("x"));

        Assert.Contains("my-card", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Style, "max-width: 320px")
            .AddChildContent("x"));

        Assert.Equal("max-width: 320px", cut.Find("div.omni-card").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCard>(p => p
            .AddUnmatched("data-testid", "card1")
            .AddChildContent("x"));

        Assert.Equal("card1", cut.Find("div.omni-card").GetAttribute("data-testid"));
    }
}
