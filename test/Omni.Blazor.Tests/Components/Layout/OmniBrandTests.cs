using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniBrand"/>: renders a div or anchor
/// depending on Href, with initial mark + name + optional tenant.
/// </summary>
public class OmniBrandTests : TestContextBase
{
    [Fact]
    public void Renders_div_when_Href_is_null()
    {
        var cut = RenderComponent<OmniBrand>();

        var root = cut.Find("div.omni-brand");
        Assert.NotNull(root);
        Assert.Contains("omni-brand", root.ClassName);
        // Default Initial="F", Name="Forneria".
        Assert.Contains("F", root.TextContent);
        Assert.Contains("Forneria", root.TextContent);
    }

    [Fact]
    public void Renders_anchor_when_Href_is_set()
    {
        var cut = RenderComponent<OmniBrand>(p => p.Add(c => c.Href, "/home"));

        var anchor = cut.Find("a.omni-brand");
        Assert.NotNull(anchor);
        Assert.Equal("/home", anchor.GetAttribute("href"));
    }

    [Fact]
    public void Renders_tenant_when_provided()
    {
        var cut = RenderComponent<OmniBrand>(p => p.Add(c => c.Tenant, "Acme"));

        Assert.Contains("Acme", cut.Markup);
        Assert.NotNull(cut.Find(".omni-brand-slash"));
        Assert.NotNull(cut.Find(".omni-brand-tenant"));
    }

    [Fact]
    public void Omits_tenant_when_null()
    {
        var cut = RenderComponent<OmniBrand>();

        Assert.Empty(cut.FindAll(".omni-brand-tenant"));
        Assert.Empty(cut.FindAll(".omni-brand-slash"));
    }

    [Fact]
    public void Large_applies_lg_mark_modifier()
    {
        var cut = RenderComponent<OmniBrand>(p => p.Add(c => c.Large, true));

        var mark = cut.Find(".omni-brand-mark");
        Assert.Contains("omni-brand-mark-lg", mark.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniBrand>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find(".omni-brand").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniBrand>(p => p.Add(c => c.Style, "color: red"));

        Assert.Equal("color: red", cut.Find(".omni-brand").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniBrand>(p => p
            .AddUnmatched("data-testid", "brand")
            .AddUnmatched("aria-label", "Logo"));

        var root = cut.Find(".omni-brand");
        Assert.Equal("brand", root.GetAttribute("data-testid"));
        Assert.Equal("Logo", root.GetAttribute("aria-label"));
    }
}
