using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniAvatarGroup"/>: child rendering,
/// "+N" overflow chip, size class, and the cross-cutting splat.
/// </summary>
public class OmniAvatarGroupTests : TestContextBase
{
    [Fact]
    public void Renders_root_and_avatar_children()
    {
        var cut = RenderComponent<OmniAvatarGroup>(p => p
            .AddChildContent<OmniAvatar>(a => a.Add(x => x.Initials, "MT"))
            .AddChildContent<OmniAvatar>(a => a.Add(x => x.Initials, "DA")));
        Assert.NotNull(cut.Find(".omni-avatar-group"));
        Assert.Equal(2, cut.FindAll(".omni-avatar-group > .omni-avatar").Count);
    }

    [Fact]
    public void More_renders_overflow_chip()
    {
        var cut = RenderComponent<OmniAvatarGroup>(p => p.Add(c => c.More, 3));
        var chip = cut.Find(".omni-avatar-group-more");
        Assert.Equal("+3", chip.TextContent);
    }

    [Fact]
    public void No_chip_when_More_null_or_zero()
    {
        Assert.Empty(RenderComponent<OmniAvatarGroup>().FindAll(".omni-avatar-group-more"));
        Assert.Empty(RenderComponent<OmniAvatarGroup>(p => p.Add(c => c.More, 0)).FindAll(".omni-avatar-group-more"));
    }

    [Theory]
    [InlineData(AvatarSize.Sm, "omni-avatar-group-sm")]
    [InlineData(AvatarSize.Lg, "omni-avatar-group-lg")]
    [InlineData(AvatarSize.Xl, "omni-avatar-group-xl")]
    public void Size_adds_modifier_class(AvatarSize size, string cls)
    {
        var cut = RenderComponent<OmniAvatarGroup>(p => p.Add(c => c.Size, size));
        Assert.Contains(cls, cut.Find(".omni-avatar-group").ClassName);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = RenderComponent<OmniAvatarGroup>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "g1"));
        var root = cut.Find(".omni-avatar-group");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("g1", root.GetAttribute("data-testid"));
    }
}
