using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniButton"/>: variants, sizes, loading
/// state, disabled handling, icon slots, and the cross-cutting Class/Style/
/// Attributes splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniButtonTests : TestContextBase
{
    [Fact]
    public void Renders_default_button_with_md_class()
    {
        var cut = Render<OmniButton>(p => p.AddChildContent("Save"));

        var btn = cut.Find("button");
        Assert.Contains("omni-btn", btn.ClassName);
        Assert.Contains("omni-btn-md", btn.ClassName);
        Assert.Equal("button", btn.GetAttribute("type"));
        Assert.Contains("Save", btn.TextContent);
    }

    [Theory]
    [InlineData(ButtonVariant.Primary, "omni-btn-primary")]
    [InlineData(ButtonVariant.Ghost,   "omni-btn-ghost")]
    [InlineData(ButtonVariant.Danger,  "omni-btn-danger")]
    [InlineData(ButtonVariant.Link,    "omni-btn-link")]
    public void Applies_variant_modifier(ButtonVariant variant, string expectedClass)
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Variant, variant)
            .AddChildContent("X"));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-btn-sm")]
    [InlineData(ComponentSize.Md, "omni-btn-md")]
    [InlineData(ComponentSize.Lg, "omni-btn-lg")]
    [InlineData(ComponentSize.Xl, "omni-btn-xl")]
    public void Applies_size_modifier(ComponentSize size, string expectedClass)
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Size, size)
            .AddChildContent("X"));

        Assert.Contains(expectedClass, cut.Find("button").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("button").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Style, "width: 200px")
            .AddChildContent("X"));

        Assert.Equal("width: 200px", cut.Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniButton>(p => p
            .AddUnmatched("data-testid", "save-btn")
            .AddUnmatched("aria-label", "Save changes")
            .AddChildContent("X"));

        var btn = cut.Find("button");
        Assert.Equal("save-btn", btn.GetAttribute("data-testid"));
        Assert.Equal("Save changes", btn.GetAttribute("aria-label"));
    }

    [Fact]
    public void Disabled_blocks_OnClick_and_sets_attribute()
    {
        var clicked = 0;
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, _ => clicked++)
            .AddChildContent("X"));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        btn.Click();
        // Browser would block click on disabled, but bUnit fires the handler
        // regardless. The HandleClickAsync guard short-circuits — assert that.
        Assert.Equal(0, clicked);
    }

    [Fact]
    public void Loading_renders_spinner_and_blocks_click()
    {
        var clicked = 0;
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Loading, true)
            .Add(c => c.OnClick, _ => clicked++)
            .AddChildContent("X"));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        Assert.Equal("true", btn.GetAttribute("aria-busy"));
        Assert.Contains("omni-btn-loading", btn.ClassName);
        Assert.NotNull(cut.Find(".omni-spinner"));
        btn.Click();
        Assert.Equal(0, clicked);
    }

    [Fact]
    public void OnClick_fires_when_enabled()
    {
        var clicked = 0;
        var cut = Render<OmniButton>(p => p
            .Add(c => c.OnClick, (MouseEventArgs _) => clicked++)
            .AddChildContent("X"));

        cut.Find("button").Click();
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void IconOnly_applies_modifier_class()
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Icon, "check")
            .Add(c => c.IconOnly, true));

        Assert.Contains("omni-btn-icon", cut.Find("button").ClassName);
    }

    [Fact]
    public void Block_applies_modifier_class()
    {
        var cut = Render<OmniButton>(p => p
            .Add(c => c.Block, true)
            .AddChildContent("X"));

        Assert.Contains("omni-btn-block", cut.Find("button").ClassName);
    }
}
