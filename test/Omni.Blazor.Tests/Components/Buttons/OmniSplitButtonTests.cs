using Microsoft.AspNetCore.Components.Web;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniSplitButton"/>: composite layout
/// (primary action + chevron popover), size modifier class, disabled/loading
/// behaviour, and the Class/Style/Attributes splat on the wrapping
/// <c>&lt;div&gt;</c>.
/// </summary>
public class OmniSplitButtonTests : TestContextBase
{
    [Fact]
    public void Renders_wrapper_div_with_split_button_class()
    {
        var cut = RenderComponent<OmniSplitButton>(p => p.Add(c => c.Text, "Save"));

        var root = cut.Find("div.omni-split-btn");
        Assert.Contains("omni-split-btn", root.ClassName);
        Assert.Contains("omni-split-btn-md", root.ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-split-btn-sm")]
    [InlineData(ComponentSize.Md, "omni-split-btn-md")]
    [InlineData(ComponentSize.Lg, "omni-split-btn-lg")]
    [InlineData(ComponentSize.Xl, "omni-split-btn-xl")]
    public void Applies_size_modifier(ComponentSize size, string expectedClass)
    {
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Size, size)
            .Add(c => c.Text, "Save"));

        Assert.Contains(expectedClass, cut.Find("div.omni-split-btn").ClassName);
    }

    [Fact]
    public void Renders_primary_and_chevron_buttons()
    {
        var cut = RenderComponent<OmniSplitButton>(p => p.Add(c => c.Text, "Save"));

        Assert.NotNull(cut.Find(".omni-split-btn-primary"));
        Assert.NotNull(cut.Find(".omni-split-btn-chevron"));
    }

    [Fact]
    public void OnClick_fires_when_primary_clicked()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find(".omni-split-btn-primary").Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Disabled_blocks_primary_click()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find(".omni-split-btn-primary").Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Loading_blocks_primary_click_and_disables_chevron()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Loading, true)
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find(".omni-split-btn-primary").Click();
        Assert.Equal(0, clicks);
        Assert.True(cut.Find(".omni-split-btn-chevron").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Class, "custom-split"));

        Assert.Contains("custom-split", cut.Find("div.omni-split-btn").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Style, "margin-left: 8px"));

        Assert.Equal("margin-left: 8px", cut.Find("div.omni-split-btn").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .AddUnmatched("data-testid", "split")
            .AddUnmatched("id", "main-split"));

        var root = cut.Find("div.omni-split-btn");
        Assert.Equal("split", root.GetAttribute("data-testid"));
        Assert.Equal("main-split", root.GetAttribute("id"));
    }
}
