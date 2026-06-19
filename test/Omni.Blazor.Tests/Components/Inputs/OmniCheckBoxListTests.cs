using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniCheckBoxList{TValue}"/>: item
/// rendering, multi-select toggle, orientation modifier, and the cross-cutting splat.
/// </summary>
public class OmniCheckBoxListTests : TestContextBase
{
    [Fact]
    public void Renders_one_label_per_item()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" }));

        Assert.Equal(3, cut.FindAll("label.omni-check-list-item").Count);
    }

    [Fact]
    public void Vertical_is_the_default_orientation()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a" }));

        Assert.Contains("omni-check-list-vertical", cut.Find("div.omni-check-list").ClassName);
    }

    [Fact]
    public void Horizontal_orientation_applies_modifier()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Orientation, Orientation.Horizontal));

        Assert.Contains("omni-check-list-horizontal", cut.Find("div.omni-check-list").ClassName);
    }

    [Fact]
    public void Selected_items_render_with_checked_input()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.Value, new[] { "b" }));

        var inputs = cut.FindAll("input");
        Assert.False(inputs[0].HasAttribute("checked"));
        Assert.True(inputs[1].HasAttribute("checked"));
    }

    [Fact]
    public void Toggling_an_item_raises_ValueChanged_with_updated_list()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.Value, new[] { "a" })
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("input")[1].Change(true);
        Assert.NotNull(captured);
        Assert.Contains("a", captured!);
        Assert.Contains("b", captured!);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-check-list").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCheckBoxList<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .AddUnmatched("data-testid", "cl"));

        Assert.Equal("cl", cut.Find("div.omni-check-list").GetAttribute("data-testid"));
    }
}
