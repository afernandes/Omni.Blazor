using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniOptionCard"/>: content, selected state,
/// click/selection callbacks, disabled, and the cross-cutting splat.
/// </summary>
public class OmniOptionCardTests : TestContextBase
{
    [Fact]
    public void Default_renders_button_with_radio()
    {
        var cut = RenderComponent<OmniOptionCard>(p => p.Add(c => c.Title, "Pro"));
        var btn = cut.Find("button.omni-option-card");
        Assert.NotNull(cut.Find(".omni-option-radio"));
        Assert.Contains("Pro", btn.TextContent);
        Assert.Equal("false", btn.GetAttribute("aria-checked"));
    }

    [Fact]
    public void Selected_adds_class_and_aria()
    {
        var cut = RenderComponent<OmniOptionCard>(p => p.Add(c => c.Selected, true));
        var btn = cut.Find("button.omni-option-card");
        Assert.Contains("omni-option-card-selected", btn.ClassName);
        Assert.Equal("true", btn.GetAttribute("aria-checked"));
    }

    [Fact]
    public void Renders_description_badge_and_price()
    {
        var cut = RenderComponent<OmniOptionCard>(p => p
            .Add(c => c.Description, "Para crescer")
            .Add(c => c.Badge, "POPULAR")
            .Add(c => c.Price, "R$ 99")
            .Add(c => c.PricePeriod, "/mês"));
        Assert.Contains("Para crescer", cut.Find(".omni-option-desc").TextContent);
        Assert.Contains("POPULAR", cut.Find(".omni-option-badge").TextContent);
        Assert.Contains("R$ 99", cut.Find(".omni-option-price").TextContent);
    }

    [Fact]
    public void Click_invokes_OnClick_and_SelectedChanged()
    {
        var clicked = false;
        bool? selected = null;
        var cut = RenderComponent<OmniOptionCard>(p => p
            .Add(c => c.OnClick, EventCallback.Factory.Create(this, () => clicked = true))
            .Add(c => c.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => selected = v)));
        cut.Find("button").Click();
        Assert.True(clicked);
        Assert.True(selected);
    }

    [Fact]
    public void Disabled_card_is_disabled()
    {
        var cut = RenderComponent<OmniOptionCard>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniOptionCard>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "o1"));
        var btn = cut.Find("button.omni-option-card");
        Assert.Contains("x", btn.ClassName);
        Assert.Contains("margin:4px", btn.GetAttribute("style") ?? "");
        Assert.Equal("o1", btn.GetAttribute("data-testid"));
    }
}
