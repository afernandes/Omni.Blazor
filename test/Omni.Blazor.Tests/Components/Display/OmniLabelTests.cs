using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniLabel"/>: span vs label rendering,
/// sizes, required, disabled, helper, and cross-cutting splat.
/// </summary>
public class OmniLabelTests : TestContextBase
{
    [Fact]
    public void Renders_as_span_when_no_For()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "Email"));

        var root = cut.Find("span.omni-label");
        Assert.Contains("omni-label", root.ClassName);
        Assert.Contains("Email", root.TextContent);
    }

    [Fact]
    public void Renders_as_label_with_for_when_For_set()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "Email")
            .Add(c => c.For, "email-input"));

        var lbl = cut.Find("label.omni-label");
        Assert.Equal("email-input", lbl.GetAttribute("for"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-label-sm")]
    [InlineData(ComponentSize.Md, "omni-label-md")]
    [InlineData(ComponentSize.Lg, "omni-label-lg")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find(".omni-label").ClassName);
    }

    [Fact]
    public void Required_renders_asterisk()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "Name")
            .Add(c => c.Required, true));

        Assert.NotNull(cut.Find(".omni-label-required"));
    }

    [Fact]
    public void Disabled_adds_modifier_class()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Disabled, true));

        Assert.Contains("omni-label-disabled", cut.Find(".omni-label").ClassName);
    }

    [Fact]
    public void Helper_text_renders_when_set()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "Name")
            .Add(c => c.Helper, "Required field"));

        Assert.Contains("Required field", cut.Find(".omni-label-helper").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "user-cls"));

        Assert.Contains("user-cls", cut.Find(".omni-label").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "color: blue"));

        Assert.Equal("color: blue", cut.Find(".omni-label").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniLabel>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "lbl1"));

        Assert.Equal("lbl1", cut.Find(".omni-label").GetAttribute("data-testid"));
    }
}
