using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniCheckBox"/>: boolean toggle round-trip,
/// label/childcontent rendering, and the cross-cutting splat.
/// </summary>
public class OmniCheckBoxTests : TestContextBase
{
    [Fact]
    public void Renders_label_with_omni_check_class()
    {
        var cut = RenderComponent<OmniCheckBox>();
        var label = cut.Find("label");
        Assert.Contains("omni-check", label.ClassName);
    }

    [Fact]
    public void Initial_Value_true_renders_checked()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p.Add(c => c.Value, true));
        Assert.True(cut.Find("input").HasAttribute("checked"));
    }

    [Fact]
    public void Click_toggles_Value_via_ValueChanged()
    {
        bool? captured = null;
        var cut = RenderComponent<OmniCheckBox>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("input").Change(true);
        Assert.True(captured);
    }

    [Fact]
    public void Renders_label_text()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p.Add(c => c.Label, "Aceito"));
        Assert.Contains("Aceito", cut.Find("label").TextContent);
    }

    [Fact]
    public void Disabled_sets_input_disabled_attribute()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("label").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p.Add(c => c.Style, "margin:4px"));
        Assert.Equal("margin:4px", cut.Find("label").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniCheckBox>(p => p
            .AddUnmatched("data-testid", "cb1"));

        Assert.Equal("cb1", cut.Find("label").GetAttribute("data-testid"));
    }
}
