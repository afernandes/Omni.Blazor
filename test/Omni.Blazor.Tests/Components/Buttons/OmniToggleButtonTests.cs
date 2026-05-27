using Microsoft.AspNetCore.Components.Web;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniToggleButton"/>: pressed/unpressed
/// state (incl. <c>aria-pressed</c>), variant + size modifier classes, two-way
/// <c>Active</c> binding via click, disabled handling, and the Class/Style/
/// Attributes splat.
/// </summary>
public class OmniToggleButtonTests : TestContextBase
{
    [Fact]
    public void Renders_default_button_with_md_class_and_aria_pressed_false()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p.AddChildContent("Bold"));

        var btn = cut.Find("button");
        Assert.Contains("omni-toggle-btn", btn.ClassName);
        Assert.Contains("omni-toggle-btn-md", btn.ClassName);
        Assert.Equal("false", btn.GetAttribute("aria-pressed"));
        Assert.Contains("Bold", btn.TextContent);
    }

    [Fact]
    public void Active_state_sets_aria_pressed_and_active_class()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Active, true)
            .AddChildContent("Bold"));

        var btn = cut.Find("button");
        Assert.Equal("true", btn.GetAttribute("aria-pressed"));
        Assert.Contains("omni-toggle-btn-active", btn.ClassName);
    }

    [Theory]
    [InlineData(ToggleVariant.Default, null)]
    [InlineData(ToggleVariant.Primary, "omni-toggle-btn-primary")]
    [InlineData(ToggleVariant.Accent,  "omni-toggle-btn-accent")]
    [InlineData(ToggleVariant.Ghost,   "omni-toggle-btn-ghost")]
    public void Applies_variant_modifier(ToggleVariant variant, string? expectedClass)
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Variant, variant)
            .AddChildContent("X"));

        var className = cut.Find("button").ClassName;
        if (expectedClass is null)
        {
            Assert.DoesNotContain("omni-toggle-btn-primary", className);
            Assert.DoesNotContain("omni-toggle-btn-accent", className);
            Assert.DoesNotContain("omni-toggle-btn-ghost", className);
        }
        else
        {
            Assert.Contains(expectedClass, className);
        }
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-toggle-btn-sm")]
    [InlineData(ComponentSize.Md, "omni-toggle-btn-md")]
    [InlineData(ComponentSize.Lg, "omni-toggle-btn-lg")]
    [InlineData(ComponentSize.Xl, "omni-toggle-btn-xl")]
    public void Applies_size_modifier(ComponentSize size, string expectedClass)
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Size, size)
            .AddChildContent("X"));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Fact]
    public void IconOnly_applies_modifier_class()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.IconOnly, true)
            .Add(c => c.Icon, "bold"));

        Assert.Contains("omni-toggle-btn-icon", cut.Find("button").ClassName);
    }

    [Fact]
    public void Click_toggles_Active_and_fires_ActiveChanged()
    {
        var captured = false;
        var fires = 0;
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Active, false)
            .Add(c => c.ActiveChanged, EventCallback.Factory.Create<bool>(this, v => { captured = v; fires++; }))
            .AddChildContent("X"));

        cut.Find("button").Click();
        Assert.True(captured);
        Assert.Equal(1, fires);
    }

    [Fact]
    public void Click_also_fires_OnClick_callback()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++)
            .AddChildContent("X"));

        cut.Find("button").Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Disabled_blocks_toggle()
    {
        var captured = false;
        var fires = 0;
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.ActiveChanged, EventCallback.Factory.Create<bool>(this, v => { captured = v; fires++; }))
            .AddChildContent("X"));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        btn.Click();
        Assert.Equal(0, fires);
        Assert.False(captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Class, "custom-tb")
            .AddChildContent("X"));

        Assert.Contains("custom-tb", cut.Find("button").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .Add(c => c.Style, "color: red")
            .AddChildContent("X"));

        Assert.Equal("color: red", cut.Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniToggleButton>(p => p
            .AddUnmatched("data-testid", "tb")
            .AddUnmatched("aria-label", "Bold formatting")
            .AddChildContent("X"));

        var btn = cut.Find("button");
        Assert.Equal("tb", btn.GetAttribute("data-testid"));
        Assert.Equal("Bold formatting", btn.GetAttribute("aria-label"));
    }
}
