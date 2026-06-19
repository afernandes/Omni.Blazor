using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniFooter"/>: footer slot of
/// <c>OmniLayout</c> with border + fixed modifiers.
/// </summary>
public class OmniFooterTests : TestContextBase
{
    [Fact]
    public void Renders_default_footer_with_bordered_class()
    {
        var cut = Render<OmniFooter>(p => p.AddChildContent("Footer"));

        var footer = cut.Find("footer");
        Assert.Contains("omni-footer", footer.ClassName);
        Assert.Contains("omni-footer-bordered", footer.ClassName);
        Assert.Contains("Footer", footer.TextContent);
    }

    [Fact]
    public void Bordered_false_removes_bordered_class()
    {
        var cut = Render<OmniFooter>(p => p
            .Add(c => c.Bordered, false)
            .AddChildContent("X"));

        Assert.DoesNotContain("omni-footer-bordered", cut.Find("footer").ClassName);
    }

    [Fact]
    public void Fixed_applies_fixed_class_and_data_attr()
    {
        var cut = Render<OmniFooter>(p => p
            .Add(c => c.Fixed, true)
            .AddChildContent("X"));

        var footer = cut.Find("footer");
        Assert.Contains("omni-footer-fixed", footer.ClassName);
        Assert.Equal("1", footer.GetAttribute("data-omni-fixed"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFooter>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("footer").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFooter>(p => p
            .Add(c => c.Style, "background: gray")
            .AddChildContent("X"));

        Assert.Equal("background: gray", cut.Find("footer").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFooter>(p => p
            .AddUnmatched("data-testid", "footer")
            .AddUnmatched("aria-label", "Footer")
            .AddChildContent("X"));

        var footer = cut.Find("footer");
        Assert.Equal("footer", footer.GetAttribute("data-testid"));
        Assert.Equal("Footer", footer.GetAttribute("aria-label"));
    }
}
