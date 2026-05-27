using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniTextArea"/>: base/textarea classes,
/// rows/maxlength, two-way Value, and the cross-cutting splat.
/// </summary>
public class OmniTextAreaTests : TestContextBase
{
    [Fact]
    public void Renders_textarea_with_base_classes()
    {
        var cut = RenderComponent<OmniTextArea>();

        var ta = cut.Find("textarea");
        Assert.Contains("omni-input",    ta.ClassName);
        Assert.Contains("omni-textarea", ta.ClassName);
    }

    [Fact]
    public void Defaults_rows_to_four()
    {
        var cut = RenderComponent<OmniTextArea>();
        Assert.Equal("4", cut.Find("textarea").GetAttribute("rows"));
    }

    [Fact]
    public void Honors_explicit_Rows()
    {
        var cut = RenderComponent<OmniTextArea>(p => p.Add(c => c.Rows, 8));
        Assert.Equal("8", cut.Find("textarea").GetAttribute("rows"));
    }

    [Fact]
    public void MaxLength_forwarded_to_textarea()
    {
        var cut = RenderComponent<OmniTextArea>(p => p.Add(c => c.MaxLength, 50));
        Assert.Equal("50", cut.Find("textarea").GetAttribute("maxlength"));
    }

    [Fact]
    public void Value_two_way_binding_via_input_event()
    {
        string? captured = null;
        var cut = RenderComponent<OmniTextArea>(p => p
            .Add(c => c.Value, "")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("textarea").Input("typed");
        Assert.Equal("typed", captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniTextArea>(p => p
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("textarea").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniTextArea>(p => p
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("textarea").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniTextArea>(p => p
            .AddUnmatched("data-testid", "ta1"));

        Assert.Equal("ta1", cut.Find("textarea").GetAttribute("data-testid"));
    }
}
