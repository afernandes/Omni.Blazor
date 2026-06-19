using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniSpinner"/>: sizes, aria-label,
/// cross-cutting splat.
/// </summary>
public class OmniSpinnerTests : TestContextBase
{
    [Fact]
    public void Renders_default_md_spinner()
    {
        var cut = Render<OmniSpinner>();

        var root = cut.Find("span.omni-spinner");
        Assert.Contains("omni-spinner", root.ClassName);
        Assert.Contains("omni-spinner-md", root.ClassName);
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("Carregando", root.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-spinner-sm")]
    [InlineData(ComponentSize.Md, "omni-spinner-md")]
    [InlineData(ComponentSize.Lg, "omni-spinner-lg")]
    [InlineData(ComponentSize.Xl, "omni-spinner-xl")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniSpinner>(p => p
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find("span.omni-spinner").ClassName);
    }

    [Fact]
    public void Custom_Label_sets_aria_label()
    {
        var cut = Render<OmniSpinner>(p => p
            .Add(c => c.Label, "Saving"));

        Assert.Equal("Saving", cut.Find("span.omni-spinner").GetAttribute("aria-label"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSpinner>(p => p
            .Add(c => c.Class, "my-spin"));

        Assert.Contains("my-spin", cut.Find("span.omni-spinner").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniSpinner>(p => p
            .Add(c => c.Style, "color: green"));

        Assert.Equal("color: green", cut.Find("span.omni-spinner").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSpinner>(p => p
            .AddUnmatched("data-testid", "spin1"));

        Assert.Equal("spin1", cut.Find("span.omni-spinner").GetAttribute("data-testid"));
    }
}
