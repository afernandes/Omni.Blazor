using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniListBox{TValue}"/>: option
/// rendering, single vs multi mode, MaxHeight forwarding, and the
/// cross-cutting splat.
/// </summary>
public class OmniListBoxTests : TestContextBase
{
    [Fact]
    public void Renders_listbox_role_with_base_class()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" }));

        var root = cut.Find("div.omni-listbox");
        Assert.Equal("listbox", root.GetAttribute("role"));
        Assert.Contains("omni-listbox-single", root.ClassName);
    }

    [Fact]
    public void Multi_mode_applies_multi_modifier()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Multiple, true));

        Assert.Contains("omni-listbox-multi", cut.Find("div.omni-listbox").ClassName);
        Assert.Equal("true", cut.Find("div.omni-listbox").GetAttribute("aria-multiselectable"));
    }

    [Fact]
    public void Renders_one_option_per_item()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" }));

        Assert.Equal(3, cut.FindAll("div.omni-listbox-item").Count);
    }

    [Fact]
    public void Click_in_single_mode_raises_ValueChanged()
    {
        string? captured = null;
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("div.omni-listbox-item")[1].Click();
        Assert.Equal("b", captured);
    }

    [Fact]
    public void Disabled_applies_modifier_and_sets_tabindex_minus_one()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Disabled, true));

        Assert.Contains("omni-listbox-disabled", cut.Find("div.omni-listbox").ClassName);
        Assert.Equal("-1", cut.Find("div.omni-listbox").GetAttribute("tabindex"));
    }

    [Fact]
    public void MaxHeight_renders_in_style()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.MaxHeight, "400px"));

        Assert.Contains("max-height:400px", cut.Find("div.omni-listbox").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-listbox").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniListBox<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .AddUnmatched("data-testid", "lb"));

        Assert.Equal("lb", cut.Find("div.omni-listbox").GetAttribute("data-testid"));
    }
}
