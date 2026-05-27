using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniAvatar"/>: sizes, square, image vs
/// initials, and cross-cutting splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniAvatarTests : TestContextBase
{
    [Fact]
    public void Renders_default_md_with_initials()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.Initials, "AB"));

        var root = cut.Find("span.omni-avatar");
        Assert.Contains("omni-avatar", root.ClassName);
        Assert.Contains("omni-avatar-md", root.ClassName);
        Assert.Contains("AB", root.TextContent);
    }

    [Theory]
    [InlineData(AvatarSize.Sm,  "omni-avatar-sm")]
    [InlineData(AvatarSize.Md,  "omni-avatar-md")]
    [InlineData(AvatarSize.Lg,  "omni-avatar-lg")]
    [InlineData(AvatarSize.Xl,  "omni-avatar-xl")]
    [InlineData(AvatarSize.XXl, "omni-avatar-2xl")]
    public void Applies_size_modifier(AvatarSize size, string expected)
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.Size, size)
            .Add(c => c.Initials, "A"));

        Assert.Contains(expected, cut.Find("span.omni-avatar").ClassName);
    }

    [Fact]
    public void Square_adds_modifier_class()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.Square, true)
            .Add(c => c.Initials, "A"));

        Assert.Contains("omni-avatar-square", cut.Find("span.omni-avatar").ClassName);
    }

    [Fact]
    public void Renders_image_when_ImageUrl_set()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.ImageUrl, "/foo.png")
            .Add(c => c.Initials, "AB"));

        var img = cut.Find("img");
        Assert.Equal("/foo.png", img.GetAttribute("src"));
        Assert.Equal("AB", img.GetAttribute("alt"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.Class, "u1")
            .Add(c => c.Initials, "X"));

        Assert.Contains("u1", cut.Find("span.omni-avatar").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .Add(c => c.Style, "background: red")
            .Add(c => c.Initials, "X"));

        Assert.Equal("background: red", cut.Find("span.omni-avatar").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniAvatar>(p => p
            .AddUnmatched("data-testid", "a1")
            .Add(c => c.Initials, "X"));

        Assert.Equal("a1", cut.Find("span.omni-avatar").GetAttribute("data-testid"));
    }
}
