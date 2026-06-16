using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniSocialButton"/>: provider→class/label,
/// compact/block modifiers, click, disabled, and the cross-cutting splat.
/// </summary>
public class OmniSocialButtonTests : TestContextBase
{
    [Fact]
    public void Default_renders_google_with_label_and_mark()
    {
        var cut = RenderComponent<OmniSocialButton>();
        var btn = cut.Find("button.omni-social-btn");
        Assert.Contains("omni-social-google", btn.ClassName);
        Assert.Contains("Continuar com Google", btn.TextContent);
        Assert.NotNull(cut.Find(".omni-social-btn svg"));
    }

    [Theory]
    [InlineData(SocialProvider.Microsoft, "omni-social-microsoft", "Microsoft")]
    [InlineData(SocialProvider.Apple, "omni-social-apple", "Apple")]
    [InlineData(SocialProvider.GitHub, "omni-social-github", "GitHub")]
    [InlineData(SocialProvider.Facebook, "omni-social-facebook", "Facebook")]
    public void Provider_maps_to_class_and_label(SocialProvider provider, string cls, string label)
    {
        var cut = RenderComponent<OmniSocialButton>(p => p.Add(c => c.Provider, provider));
        var btn = cut.Find("button.omni-social-btn");
        Assert.Contains(cls, btn.ClassName);
        Assert.Contains(label, btn.TextContent);
    }

    [Fact]
    public void Passkey_uses_distinct_label()
    {
        var cut = RenderComponent<OmniSocialButton>(p => p.Add(c => c.Provider, SocialProvider.Passkey));
        Assert.Contains("Entrar com Passkey", cut.Find("button").TextContent);
    }

    [Fact]
    public void Compact_drops_label_and_adds_modifier()
    {
        var cut = RenderComponent<OmniSocialButton>(p => p.Add(c => c.Compact, true));
        var btn = cut.Find("button.omni-social-btn");
        Assert.Contains("omni-social-btn-compact", btn.ClassName);
        Assert.Empty(btn.QuerySelectorAll("span"));
        // aria-label still carries the full text for screen readers
        Assert.Contains("Google", btn.GetAttribute("aria-label") ?? "");
    }

    [Fact]
    public void Block_adds_modifier()
    {
        var cut = RenderComponent<OmniSocialButton>(p => p.Add(c => c.Block, true));
        Assert.Contains("omni-social-btn-block", cut.Find("button").ClassName);
    }

    [Fact]
    public void Click_invokes_callback()
    {
        var clicked = false;
        var cut = RenderComponent<OmniSocialButton>(p => p
            .Add(c => c.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));
        cut.Find("button").Click();
        Assert.True(clicked);
    }

    [Fact]
    public void Disabled_button_is_disabled()
    {
        var cut = RenderComponent<OmniSocialButton>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniSocialButton>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "s1"));
        var btn = cut.Find("button.omni-social-btn");
        Assert.Contains("x", btn.ClassName);
        Assert.Contains("margin:4px", btn.GetAttribute("style") ?? "");
        Assert.Equal("s1", btn.GetAttribute("data-testid"));
    }
}
