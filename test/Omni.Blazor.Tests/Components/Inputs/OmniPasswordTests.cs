using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniPassword"/>: input type toggle,
/// size modifiers, two-way Value binding, and the cross-cutting splat.
/// </summary>
public class OmniPasswordTests : TestContextBase
{
    [Fact]
    public void Renders_password_input_inside_group()
    {
        var cut = RenderComponent<OmniPassword>();
        var input = cut.Find("input");
        Assert.Equal("password",  input.GetAttribute("type"));
        Assert.Contains("omni-input", input.ClassName);
    }

    [Fact]
    public void Toggle_button_switches_input_type_to_text()
    {
        var cut = RenderComponent<OmniPassword>();

        Assert.Equal("password", cut.Find("input").GetAttribute("type"));
        cut.Find("button").Click();
        Assert.Equal("text", cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void ShowToggle_false_hides_eye_button()
    {
        var cut = RenderComponent<OmniPassword>(p => p.Add(c => c.ShowToggle, false));
        Assert.Empty(cut.FindAll("button"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-input-sm")]
    [InlineData(ComponentSize.Lg, "omni-input-lg")]
    public void Applies_size_modifier_to_input(ComponentSize size, string expected)
    {
        var cut = RenderComponent<OmniPassword>(p => p.Add(c => c.Size, size));
        Assert.Contains(expected, cut.Find("input").ClassName);
    }

    [Fact]
    public void Input_event_propagates_to_ValueChanged()
    {
        string? captured = null;
        var cut = RenderComponent<OmniPassword>(p => p
            .Add(c => c.Value, "")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("input").Input("s3cret");
        Assert.Equal("s3cret", captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_root_group()
    {
        var cut = RenderComponent<OmniPassword>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-input-group").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniPassword>(p => p.Add(c => c.Style, "width: 240px"));
        Assert.Equal("width: 240px", cut.Find("div.omni-input-group").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniPassword>(p => p
            .AddUnmatched("data-testid", "pw1"));

        Assert.Equal("pw1", cut.Find("div.omni-input-group").GetAttribute("data-testid"));
    }
}
