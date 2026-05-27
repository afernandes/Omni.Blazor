using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniKbd"/>: text vs ChildContent,
/// cross-cutting splat.
/// </summary>
public class OmniKbdTests : TestContextBase
{
    [Fact]
    public void Renders_kbd_with_text()
    {
        var cut = RenderComponent<OmniKbd>(p => p
            .Add(c => c.Text, "Ctrl"));

        var kbd = cut.Find("kbd.omni-kbd");
        Assert.Contains("omni-kbd", kbd.ClassName);
        Assert.Equal("Ctrl", kbd.TextContent);
    }

    [Fact]
    public void Renders_ChildContent_when_provided()
    {
        var cut = RenderComponent<OmniKbd>(p => p
            .AddChildContent("Shift"));

        Assert.Contains("Shift", cut.Find("kbd.omni-kbd").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniKbd>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "my-key"));

        Assert.Contains("my-key", cut.Find("kbd.omni-kbd").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniKbd>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "color: blue"));

        Assert.Equal("color: blue", cut.Find("kbd.omni-kbd").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniKbd>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "k1"));

        Assert.Equal("k1", cut.Find("kbd.omni-kbd").GetAttribute("data-testid"));
    }
}
