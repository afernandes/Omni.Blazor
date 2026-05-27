using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Navigation;

public class OmniSegmentedControlTests : TestContextBase
{
    private static readonly OmniSegmentedControl<string>.Item[] Items =
    {
        new("a", "First"),
        new("b", "Second"),
        new("c", "Third"),
    };

    [Fact]
    public void Renders_root_with_omni_segmented_class()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Items, Items));
        Assert.NotNull(cut.Find(".omni-segmented"));
        Assert.Equal(3, cut.FindAll("button").Count);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Class, "my-seg")
            .Add(c => c.Items, Items));
        Assert.Contains("my-seg", cut.Find(".omni-segmented").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Style, "width: 200px")
            .Add(c => c.Items, Items));
        Assert.Equal("width: 200px", cut.Find(".omni-segmented").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .AddUnmatched("data-testid", "seg")
            .Add(c => c.Items, Items));
        Assert.Equal("seg", cut.Find(".omni-segmented").GetAttribute("data-testid"));
    }

    [Fact]
    public void Highlights_active_item_matching_Value()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Items, Items)
            .Add(c => c.Value, "b"));

        var buttons = cut.FindAll("button");
        Assert.DoesNotContain("omni-active", buttons[0].ClassName);
        Assert.Contains("omni-active", buttons[1].ClassName);
        Assert.DoesNotContain("omni-active", buttons[2].ClassName);
    }

    [Fact]
    public void Click_emits_ValueChanged_with_new_value()
    {
        string? captured = null;
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Items, Items)
            .Add(c => c.Value, "a")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("button")[2].Click();

        Assert.Equal("c", captured);
    }

    [Fact]
    public void Click_on_active_item_does_not_fire_ValueChanged()
    {
        var fired = 0;
        var cut = RenderComponent<OmniSegmentedControl<string>>(p => p
            .Add(c => c.Items, Items)
            .Add(c => c.Value, "a")
            .Add(c => c.ValueChanged, _ => fired++));

        cut.FindAll("button")[0].Click();

        Assert.Equal(0, fired);
    }

    [Fact]
    public void Empty_Items_renders_no_buttons()
    {
        var cut = RenderComponent<OmniSegmentedControl<string>>();
        Assert.NotNull(cut.Find(".omni-segmented"));
        Assert.Empty(cut.FindAll("button"));
    }
}
