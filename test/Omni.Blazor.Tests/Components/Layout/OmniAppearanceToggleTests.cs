using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniAppearanceToggle"/>: theme switcher
/// with Dropdown / Segmented / Cycle visual variants.
/// </summary>
public class OmniAppearanceToggleTests : TestContextBase
{
    public OmniAppearanceToggleTests()
    {
        Services.AddSingleton<ThemeService>(sp => new ThemeService(sp.GetRequiredService<IJSRuntime>()));
    }

    [Fact]
    public void Default_renders_dropdown_trigger_button()
    {
        var cut = Render<OmniAppearanceToggle>();
        // Trigger is the underlying OmniButton with omni-app-tog class.
        var btn = cut.Find("button");
        Assert.Contains("omni-app-tog", btn.ClassName);
        Assert.Contains("omni-app-tog-trigger", btn.ClassName);
    }

    [Fact]
    public void Segmented_variant_renders_three_buttons()
    {
        var cut = Render<OmniAppearanceToggle>(p => p
            .Add(c => c.Variant, AppearanceToggleVariant.Segmented));

        var group = cut.Find(".omni-app-tog-seg");
        Assert.NotNull(group);
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal(3, cut.FindAll(".omni-app-tog-seg-btn").Count);
    }

    [Fact]
    public void Cycle_variant_renders_single_button()
    {
        var cut = Render<OmniAppearanceToggle>(p => p
            .Add(c => c.Variant, AppearanceToggleVariant.Cycle));

        var btn = cut.Find("button");
        Assert.Contains("omni-app-tog-cycle", btn.ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-app-tog-sm")]
    [InlineData(ComponentSize.Md, "omni-app-tog-md")]
    [InlineData(ComponentSize.Lg, "omni-app-tog-lg")]
    [InlineData(ComponentSize.Xl, "omni-app-tog-xl")]
    public void Segmented_applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniAppearanceToggle>(p => p
            .Add(c => c.Variant, AppearanceToggleVariant.Segmented)
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find(".omni-app-tog-seg").ClassName);
    }

    [Fact]
    public void Custom_AriaLabel_applies_to_segmented_group()
    {
        var cut = Render<OmniAppearanceToggle>(p => p
            .Add(c => c.Variant, AppearanceToggleVariant.Segmented)
            .Add(c => c.AriaLabel, "Theme mode"));

        Assert.Equal("Theme mode", cut.Find(".omni-app-tog-seg").GetAttribute("aria-label"));
    }

    [Fact]
    public void Segmented_with_ShowLabels_renders_inline_labels()
    {
        var cut = Render<OmniAppearanceToggle>(p => p
            .Add(c => c.Variant, AppearanceToggleVariant.Segmented)
            .Add(c => c.ShowLabels, true));

        var labels = cut.FindAll(".omni-app-tog-lbl");
        Assert.Equal(3, labels.Count);
    }
}
