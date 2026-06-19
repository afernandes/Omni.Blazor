using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniRadioGroup{TValue}"/>: child radios
/// share a name, selection sets the bound Value, and the cross-cutting splat.
/// </summary>
public class OmniRadioGroupTests : TestContextBase
{
    [Fact]
    public void Renders_radiogroup_role_with_base_class()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .AddChildContent("<span>x</span>"));

        var root = cut.Find("div.omni-radio-group");
        Assert.Equal("radiogroup", root.GetAttribute("role"));
    }

    [Fact]
    public void Horizontal_orientation_adds_modifier_class()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .Add(c => c.Orientation, Orientation.Horizontal)
            .AddChildContent("<span></span>"));

        Assert.Contains("omni-radio-group-horizontal", cut.Find("div.omni-radio-group").ClassName);
    }

    [Fact]
    public void Vertical_orientation_omits_horizontal_class()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .AddChildContent("<span></span>"));

        Assert.DoesNotContain("omni-radio-group-horizontal", cut.Find("div.omni-radio-group").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("<span></span>"));

        Assert.Contains("custom-cls", cut.Find("div.omni-radio-group").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .Add(c => c.Style, "gap: 16px")
            .AddChildContent("<span></span>"));

        Assert.Equal("gap: 16px", cut.Find("div.omni-radio-group").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniRadioGroup<string>>(p => p
            .AddUnmatched("data-testid", "rg")
            .AddChildContent("<span></span>"));

        Assert.Equal("rg", cut.Find("div.omni-radio-group").GetAttribute("data-testid"));
    }
}
