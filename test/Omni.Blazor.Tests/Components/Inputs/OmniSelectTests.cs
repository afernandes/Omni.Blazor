using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSelect{TValue}"/>: option rendering,
/// placeholder, two-way binding, and the cross-cutting splat.
/// </summary>
public class OmniSelectTests : TestContextBase
{
    [Fact]
    public void Renders_select_with_base_class()
    {
        var cut = RenderComponent<OmniSelect<string>>();
        Assert.Contains("omni-select", cut.Find("select").ClassName);
    }

    [Fact]
    public void Renders_placeholder_option_when_provided()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .Add(c => c.Placeholder, "Selecione..."));

        var first = cut.Find("option");
        Assert.Equal("", first.GetAttribute("value"));
        Assert.Contains("Selecione...", first.TextContent);
    }

    [Fact]
    public void Renders_options_from_Items_with_selectors()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.ValueSelector, v => v)
            .Add(c => c.TextSelector, v => v?.ToUpperInvariant() ?? ""));

        var options = cut.FindAll("option");
        Assert.Equal(2, options.Count);
        Assert.Equal("a", options[0].GetAttribute("value"));
        Assert.Equal("A", options[0].TextContent.Trim());
        Assert.Equal("b", options[1].GetAttribute("value"));
        Assert.Equal("B", options[1].TextContent.Trim());
    }

    [Fact]
    public void Renders_options_from_Items_without_ValueSelector_uses_item_as_value()
    {
        // Regression: without a ValueSelector each option must carry its own
        // value (identity), not default(TValue) for all of them.
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" }));

        var options = cut.FindAll("option");
        Assert.Equal(3, options.Count);
        Assert.Equal("a", options[0].GetAttribute("value"));
        Assert.Equal("b", options[1].GetAttribute("value"));
        Assert.Equal("c", options[2].GetAttribute("value"));
    }

    [Fact]
    public void Change_event_propagates_without_ValueSelector()
    {
        string? captured = null;
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "x", "y" })
            .Add(c => c.Value, "x")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("select").Change("y");
        Assert.Equal("y", captured);
    }

    [Fact]
    public void Change_event_propagates_to_ValueChanged()
    {
        string? captured = null;
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .Add(c => c.Items, new[] { "x", "y" })
            .Add(c => c.ValueSelector, v => v)
            .Add(c => c.TextSelector, v => v ?? "")
            .Add(c => c.Value, "x")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("select").Change("y");
        Assert.Equal("y", captured);
    }

    private enum Fruit { Apple, Banana, Cherry }

    [Fact]
    public void Change_event_propagates_enum_value()
    {
        // Regression: enum names can't be parsed by Convert.ChangeType, so
        // selecting any enum option used to silently fall back to default.
        Fruit captured = Fruit.Apple;
        var cut = RenderComponent<OmniSelect<Fruit>>(p => p
            .Add(c => c.Items, new[] { Fruit.Apple, Fruit.Banana, Fruit.Cherry })
            .Add(c => c.Value, Fruit.Apple)
            .Add(c => c.ValueChanged, v => captured = v));

        var options = cut.FindAll("option");
        Assert.Equal("Cherry", options[2].GetAttribute("value"));

        cut.Find("select").Change("Cherry");
        Assert.Equal(Fruit.Cherry, captured);
    }

    [Fact]
    public void Disabled_sets_attribute()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("select").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p.Add(c => c.Style, "min-width: 200px"));
        Assert.Equal("min-width: 200px", cut.Find("select").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSelect<string>>(p => p
            .AddUnmatched("data-testid", "sel1"));

        Assert.Equal("sel1", cut.Find("select").GetAttribute("data-testid"));
    }
}
