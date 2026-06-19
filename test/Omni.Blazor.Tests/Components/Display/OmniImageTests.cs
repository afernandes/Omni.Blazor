using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniImage"/>: src/alt, object-fit, lazy
/// loading, interactive (click + keyboard) mode, and the cross-cutting splat.
/// </summary>
public class OmniImageTests : TestContextBase
{
    private IRenderedComponent<OmniImage> Render(Action<ComponentParameterCollectionBuilder<OmniImage>>? extra = null)
        => Render<OmniImage>(p => { p.Add(c => c.Src, "x.png"); extra?.Invoke(p); });

    [Fact]
    public void Renders_img_with_src_and_alt()
    {
        var cut = Render(p => p.Add(c => c.Alt, "Foto do produto"));
        var img = cut.Find("img.omni-image");
        Assert.Equal("x.png", img.GetAttribute("src"));
        Assert.Equal("Foto do produto", img.GetAttribute("alt"));
    }

    [Fact]
    public void Lazy_loading_by_default()
    {
        Assert.Equal("lazy", Render().Find("img").GetAttribute("loading"));
        Assert.Null(Render(p => p.Add(c => c.Lazy, false)).Find("img").GetAttribute("loading"));
    }

    [Theory]
    [InlineData(ObjectFit.Cover, "omni-image-cover")]
    [InlineData(ObjectFit.Contain, "omni-image-contain")]
    [InlineData(ObjectFit.ScaleDown, "omni-image-scaledown")]
    public void Fit_maps_to_class(ObjectFit fit, string expected)
    {
        var cut = Render(p => p.Add(c => c.Fit, fit));
        Assert.Contains(expected, cut.Find("img").ClassName);
    }

    [Fact]
    public void Fill_fit_adds_no_object_fit_class()
    {
        var cut = Render(p => p.Add(c => c.Fit, ObjectFit.Fill));
        Assert.DoesNotContain("omni-image-cover", cut.Find("img").ClassName);
        Assert.DoesNotContain("omni-image-fill", cut.Find("img").ClassName);
    }

    [Fact]
    public void Non_interactive_by_default()
    {
        var img = Render().Find("img");
        Assert.Null(img.GetAttribute("role"));
        Assert.Null(img.GetAttribute("tabindex"));
        Assert.DoesNotContain("omni-image-clickable", img.ClassName);
    }

    [Fact]
    public void Click_makes_it_a_button_and_fires()
    {
        var clicked = false;
        var cut = Render(p => p.Add(c => c.Click,
            EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true)));
        var img = cut.Find("img");
        Assert.Equal("button", img.GetAttribute("role"));
        Assert.Equal("0", img.GetAttribute("tabindex"));
        Assert.Contains("omni-image-clickable", img.ClassName);

        img.Click();
        Assert.True(clicked);
    }

    [Fact]
    public void Enter_key_activates_click()
    {
        var clicked = false;
        var cut = Render(p => p.Add(c => c.Click,
            EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true)));
        cut.Find("img").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.True(clicked);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render(p => p
            .Add(c => c.Class, "hero")
            .Add(c => c.Style, "width:200px")
            .AddUnmatched("data-testid", "img1"));
        var img = cut.Find("img.omni-image");
        Assert.Contains("hero", img.ClassName);
        Assert.Contains("width:200px", img.GetAttribute("style") ?? "");
        Assert.Equal("img1", img.GetAttribute("data-testid"));
    }
}
