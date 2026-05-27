using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniDrawer"/>: aside element with
/// anchor / variant / collapsed / open state.
/// </summary>
public class OmniDrawerTests : TestContextBase
{
    [Fact]
    public void Renders_default_drawer_aside_with_base_classes()
    {
        var cut = RenderComponent<OmniDrawer>(p => p.AddChildContent("body"));

        var aside = cut.Find("aside");
        Assert.Contains("omni-sidebar", aside.ClassName);
        Assert.Contains("omni-drawer", aside.ClassName);
        // Default Anchor=Left → data-anchor="left".
        Assert.Equal("left", aside.GetAttribute("data-anchor"));
        // Default Variant=Responsive → data-variant="responsive".
        Assert.Equal("responsive", aside.GetAttribute("data-variant"));
        // Default Open=true.
        Assert.Equal("1", aside.GetAttribute("data-open"));
        Assert.Contains("body", aside.TextContent);
    }

    [Theory]
    [InlineData(DrawerAnchor.Left,  "left")]
    [InlineData(DrawerAnchor.Right, "right")]
    public void Applies_anchor_attribute(DrawerAnchor anchor, string expected)
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Anchor, anchor)
            .AddChildContent("X"));

        Assert.Equal(expected, cut.Find("aside").GetAttribute("data-anchor"));
    }

    [Theory]
    [InlineData(DrawerVariant.Persistent, "persistent")]
    [InlineData(DrawerVariant.Mini,       "mini")]
    [InlineData(DrawerVariant.Temporary,  "temporary")]
    [InlineData(DrawerVariant.Responsive, "responsive")]
    public void Applies_variant_attribute(DrawerVariant variant, string expected)
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Variant, variant)
            .AddChildContent("X"));

        Assert.Equal(expected, cut.Find("aside").GetAttribute("data-variant"));
    }

    [Fact]
    public void Mini_variant_with_Open_false_marks_collapsed_class()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Variant, DrawerVariant.Mini)
            .Add(c => c.Open, false)
            .AddChildContent("X"));

        var aside = cut.Find("aside");
        Assert.Contains("omni-drawer-collapsed", aside.ClassName);
        Assert.Contains("omni-sidebar-collapsed", aside.ClassName);
    }

    [Fact]
    public void Explicit_Collapsed_true_overrides_variant()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Variant, DrawerVariant.Persistent)
            .Add(c => c.Collapsed, true)
            .AddChildContent("X"));

        Assert.Contains("omni-drawer-collapsed", cut.Find("aside").ClassName);
        Assert.True(cut.Instance.IsCollapsed);
    }

    [Fact]
    public void Renders_Header_and_Footer_slots()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Header, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "head");
                builder.CloseElement();
            })
            .Add(c => c.Footer, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "foot");
                builder.CloseElement();
            })
            .AddChildContent("body"));

        Assert.NotNull(cut.Find(".omni-drawer-header [data-testid='head']"));
        Assert.NotNull(cut.Find(".omni-drawer-footer-slot [data-testid='foot']"));
    }

    [Fact]
    public async Task OpenAsync_and_CloseAsync_toggle_state()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Open, false)
            .Add(c => c.Variant, DrawerVariant.Temporary)
            .AddChildContent("X"));

        Assert.False(cut.Instance.Open);

        await cut.InvokeAsync(() => cut.Instance.OpenAsync());
        Assert.True(cut.Instance.Open);

        await cut.InvokeAsync(() => cut.Instance.CloseAsync());
        Assert.False(cut.Instance.Open);
    }

    [Fact]
    public async Task ToggleAsync_flips_Open()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Open, false)
            .Add(c => c.Variant, DrawerVariant.Temporary)
            .AddChildContent("X"));

        await cut.InvokeAsync(() => cut.Instance.ToggleAsync());
        Assert.True(cut.Instance.Open);

        await cut.InvokeAsync(() => cut.Instance.ToggleAsync());
        Assert.False(cut.Instance.Open);
    }

    [Fact]
    public void Width_param_emits_css_variable()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Width, "320px")
            .AddChildContent("X"));

        Assert.Contains("--omni-drawer-w: 320px", cut.Find("aside").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("aside").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniDrawer>(p => p
            .AddUnmatched("data-testid", "drawer")
            .AddUnmatched("aria-label", "Sidebar")
            .AddChildContent("X"));

        var aside = cut.Find("aside");
        Assert.Equal("drawer", aside.GetAttribute("data-testid"));
        Assert.Equal("Sidebar", aside.GetAttribute("aria-label"));
    }
}
