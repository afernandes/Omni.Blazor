using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniIcon"/>: known/unknown names,
/// sizes, splat.
/// </summary>
public class OmniIconTests : TestContextBase
{
    // "check" is a well-known icon in OmniIconLibrary; the rest of these tests
    // depend on its presence in the registry.

    [Fact]
    public void Renders_known_icon_with_base_class()
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "check"));

        var root = cut.Find("span.omni-icon");
        Assert.Contains("omni-icon", root.ClassName);
        Assert.NotNull(cut.Find("svg"));
    }

    [Fact]
    public void Renders_nothing_for_unknown_icon()
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "this-icon-does-not-exist-xyz"));

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-icon-sm")]
    [InlineData(ComponentSize.Md, "omni-icon-md")]
    [InlineData(ComponentSize.Lg, "omni-icon-lg")]
    [InlineData(ComponentSize.Xl, "omni-icon-xl")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "check")
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find("span.omni-icon").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "check")
            .Add(c => c.Class, "my-ico"));

        Assert.Contains("my-ico", cut.Find("span.omni-icon").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "check")
            .Add(c => c.Style, "color: red"));

        Assert.Equal("color: red", cut.Find("span.omni-icon").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniIcon>(p => p
            .Add(c => c.Name, "check")
            .AddUnmatched("data-testid", "ico1"));

        Assert.Equal("ico1", cut.Find("span.omni-icon").GetAttribute("data-testid"));
    }
}
