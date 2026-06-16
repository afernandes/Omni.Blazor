using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniAuthLayout"/>: centered vs split,
/// decor toggle, child + brand rendering, and the cross-cutting splat.
/// </summary>
public class OmniAuthLayoutTests : TestContextBase
{
    [Fact]
    public void Default_is_centered_with_decor_and_renders_children()
    {
        var cut = RenderComponent<OmniAuthLayout>(p => p
            .AddChildContent("<div class=\"card\">form</div>"));
        var root = cut.Find(".omni-auth");
        Assert.Contains("omni-auth-centered", root.ClassName);
        Assert.Contains("omni-auth-decor", root.ClassName);
        Assert.NotNull(cut.Find(".card"));
    }

    [Fact]
    public void Brand_set_switches_to_split_and_renders_brand_panel()
    {
        var cut = RenderComponent<OmniAuthLayout>(p => p
            .Add(c => c.Brand, b => b.AddMarkupContent(0, "<div class=\"brand-x\">ACME</div>"))
            .AddChildContent("<div class=\"form-x\">form</div>"));
        var root = cut.Find(".omni-auth");
        Assert.Contains("omni-auth-split", root.ClassName);
        Assert.DoesNotContain("omni-auth-centered", root.ClassName);
        Assert.NotNull(cut.Find(".omni-auth-brand .brand-x"));
        Assert.NotNull(cut.Find(".omni-auth-panel .form-x"));
    }

    [Fact]
    public void Decor_false_removes_decor_class()
    {
        var cut = RenderComponent<OmniAuthLayout>(p => p
            .Add(c => c.Decor, false)
            .AddChildContent("<div>form</div>"));
        Assert.DoesNotContain("omni-auth-decor", cut.Find(".omni-auth").ClassName);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniAuthLayout>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "height:600px")
            .AddUnmatched("data-testid", "a1")
            .AddChildContent("<div>form</div>"));
        var root = cut.Find(".omni-auth");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("height:600px", root.GetAttribute("style") ?? "");
        Assert.Equal("a1", root.GetAttribute("data-testid"));
    }
}
