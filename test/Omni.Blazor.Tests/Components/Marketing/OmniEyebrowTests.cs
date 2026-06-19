using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Marketing;

/// <summary>
/// Behavioural contract for <see cref="OmniEyebrow"/>: text vs ChildContent,
/// optional dot, and the cross-cutting Class/Style/Attributes splat.
/// </summary>
public class OmniEyebrowTests : TestContextBase
{
    [Fact]
    public void Renders_root_span_with_base_class()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "New"));

        var root = cut.Find("span.omni-eyebrow");
        Assert.Contains("omni-eyebrow", root.ClassName);
        Assert.Contains("New", root.TextContent);
    }

    [Fact]
    public void Shows_dot_by_default()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "X"));

        Assert.NotNull(cut.Find("span.omni-eyebrow-dot"));
    }

    [Fact]
    public void Hides_dot_when_ShowDot_false()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.ShowDot, false)
            .Add(c => c.Text, "X"));

        Assert.Empty(cut.FindAll("span.omni-eyebrow-dot"));
    }

    [Fact]
    public void ChildContent_overrides_Text()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("kids"));

        Assert.Contains("kids", cut.Find("span.omni-eyebrow").TextContent);
        Assert.DoesNotContain("ignored", cut.Find("span.omni-eyebrow").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "user-cls"));

        Assert.Contains("user-cls", cut.Find("span.omni-eyebrow").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "color: red"));

        Assert.Equal("color: red", cut.Find("span.omni-eyebrow").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniEyebrow>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "eyebrow1"));

        Assert.Equal("eyebrow1", cut.Find("span.omni-eyebrow").GetAttribute("data-testid"));
    }
}
