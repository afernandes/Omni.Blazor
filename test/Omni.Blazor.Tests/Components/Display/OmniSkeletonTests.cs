using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniSkeleton"/>: variants, multi-line
/// text, dimensions, splat.
/// </summary>
public class OmniSkeletonTests : TestContextBase
{
    [Fact]
    public void Renders_single_text_by_default()
    {
        var cut = RenderComponent<OmniSkeleton>();

        var root = cut.Find("span.omni-skeleton");
        Assert.Contains("omni-skeleton", root.ClassName);
        Assert.Contains("omni-skeleton-text", root.ClassName);
        Assert.Equal("true", root.GetAttribute("aria-busy"));
    }

    [Theory]
    [InlineData(SkeletonVariant.Text,   "omni-skeleton-text")]
    [InlineData(SkeletonVariant.Rect,   "omni-skeleton-rect")]
    [InlineData(SkeletonVariant.Circle, "omni-skeleton-circle")]
    public void Applies_variant_class(SkeletonVariant variant, string expected)
    {
        var cut = RenderComponent<OmniSkeleton>(p => p
            .Add(c => c.Variant, variant));

        Assert.Contains(expected, cut.Find("span.omni-skeleton").ClassName);
    }

    [Fact]
    public void Multi_line_text_renders_stack_with_lines()
    {
        var cut = RenderComponent<OmniSkeleton>(p => p
            .Add(c => c.Variant, SkeletonVariant.Text)
            .Add(c => c.Lines, 3));

        var stack = cut.Find("div.omni-skeleton-stack");
        Assert.NotNull(stack);
        Assert.Equal(3, cut.FindAll("span.omni-skeleton-text").Count);
    }

    [Fact]
    public void Width_and_Height_applied_to_single()
    {
        var cut = RenderComponent<OmniSkeleton>(p => p
            .Add(c => c.Variant, SkeletonVariant.Rect)
            .Add(c => c.Width, "120px")
            .Add(c => c.Height, "40px"));

        var style = cut.Find("span.omni-skeleton").GetAttribute("style") ?? string.Empty;
        Assert.Contains("width:120px", style);
        Assert.Contains("height:40px", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSkeleton>(p => p
            .Add(c => c.Class, "my-skel"));

        Assert.Contains("my-skel", cut.Find("span.omni-skeleton").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSkeleton>(p => p
            .AddUnmatched("data-testid", "sk1"));

        Assert.Equal("sk1", cut.Find("span.omni-skeleton").GetAttribute("data-testid"));
    }
}
