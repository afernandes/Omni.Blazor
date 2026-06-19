using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>Behavioural contract for <see cref="OmniBentoItem"/>: a typed Bento tile with col/row span.</summary>
public class OmniBentoItemTests : TestContextBase
{
    [Fact]
    public void Renders_item_without_span_by_default()
    {
        var cut = Render<OmniBentoItem>(p => p.AddChildContent("x"));

        var div = cut.Find("div");
        Assert.Contains("omni-bento-item", div.ClassName);
        var style = div.GetAttribute("style") ?? "";
        Assert.DoesNotContain("grid-column", style);
        Assert.DoesNotContain("grid-row", style);
    }

    [Fact]
    public void Emits_span_classes()
    {
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.ColSpan, 2)
            .Add(c => c.RowSpan, 3)
            .AddChildContent("x"));

        var cls = cut.Find("div").ClassName;
        Assert.Contains("omni-bento-span-2", cls);
        Assert.Contains("omni-bento-row-span-3", cls);
    }

    [Fact]
    public void Emits_responsive_span_classes()
    {
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.ColSpan, 4)
            .Add(c => c.ColSpanSm, 1)
            .Add(c => c.ColSpanMd, 2)
            .Add(c => c.RowSpanLg, 2)
            .AddChildContent("x"));

        var cls = cut.Find("div").ClassName;
        Assert.Contains("omni-bento-span-4", cls);
        Assert.Contains("omni-bento-span-sm-1", cls);
        Assert.Contains("omni-bento-span-md-2", cls);
        Assert.Contains("omni-bento-row-span-lg-2", cls);
    }

    [Fact]
    public void NoPadding_adds_flush_modifier()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.NoPadding, true).AddChildContent("x"));

        Assert.Contains("omni-bento-item-flush", cut.Find("div").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.Class, "cc").AddChildContent("x"));

        Assert.Contains("cc", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.ColSpan, 2)
            .Add(c => c.Style, "opacity: .5")
            .AddChildContent("x"));

        Assert.Contains("opacity: .5", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniBentoItem>(p => p
            .AddUnmatched("data-testid", "it")
            .AddChildContent("x"));

        Assert.Equal("it", cut.Find("div").GetAttribute("data-testid"));
    }

    [Fact]
    public void Href_renders_anchor_interactive()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.Href, "/relatorio").AddChildContent("x"));

        var a = cut.Find("a");
        Assert.Equal("/relatorio", a.GetAttribute("href"));
        Assert.Contains("omni-bento-item-interactive", a.ClassName);
    }

    [Fact]
    public void OnClick_renders_button_and_fires()
    {
        var clicked = false;
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true))
            .AddChildContent("x"));

        var btn = cut.Find("button");
        Assert.Contains("omni-bento-item-interactive", btn.ClassName);
        btn.Click();
        Assert.True(clicked);
    }

    [Fact]
    public void Disabled_button_has_attribute_and_class()
    {
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => { }))
            .AddChildContent("x"));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        Assert.Contains("omni-bento-item-disabled", btn.ClassName);
    }

    [Fact]
    public void Tone_emits_tone_class()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.Tone, CardTone.Accent).AddChildContent("x"));

        Assert.Contains("omni-bento-item-tone-accent", cut.Find("div").ClassName);
    }

    [Fact]
    public void AspectRatio_emits_class_and_variable()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.AspectRatio, "16/9").AddChildContent("x"));

        var div = cut.Find("div");
        Assert.Contains("omni-bento-item-ar", div.ClassName);
        Assert.Contains("--omni-bento-ar: 16/9", div.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Content_alignment_emits_flex_classes()
    {
        var cut = Render<OmniBentoItem>(p => p
            .Add(c => c.AlignContent, StackAlign.Center)
            .Add(c => c.JustifyContent, StackAlign.End)
            .AddChildContent("x"));

        var cls = cut.Find("div").ClassName;
        Assert.Contains("omni-bento-item-flex", cls);
        Assert.Contains("omni-bento-item-align-center", cls);
        Assert.Contains("omni-bento-item-justify-end", cls);
    }

    [Fact]
    public void Scrim_emits_class()
    {
        var cut = Render<OmniBentoItem>(p => p.Add(c => c.Scrim, true).AddChildContent("x"));

        Assert.Contains("omni-bento-item-scrim", cut.Find("div").ClassName);
    }
}
