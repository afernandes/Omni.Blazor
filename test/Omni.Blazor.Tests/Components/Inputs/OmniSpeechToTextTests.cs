using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSpeechToText"/>: render-prop
/// passthrough, default state, and lifecycle. The component is headless —
/// no root element — so this file pins down the few observable contracts:
/// initial state, default IsRecording flag, and ChildContent forwarding.
/// </summary>
public class OmniSpeechToTextTests : TestContextBase
{
    [Fact]
    public void Renders_no_markup_without_ChildContent()
    {
        var cut = Render<OmniSpeechToText>();
        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Initial_state_is_Idle()
    {
        var cut = Render<OmniSpeechToText>();
        Assert.Equal(SpeechRecognitionState.Idle, cut.Instance.State);
        Assert.False(cut.Instance.IsRecording);
        Assert.False(cut.Instance.IsBusy);
    }

    [Fact]
    public void Passes_context_to_ChildContent()
    {
        // The render-prop pattern is RenderFragment<TContext>; bUnit wires it
        // up via the typed Add overload from ComponentParameterBuilderExtensions.
        var cut = Render<OmniSpeechToText>(p => p
            .Add(c => c.ChildContent, (OmniSpeechToText.SpeechContext ctx) => (RenderFragment)(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "data-state", ctx.State.ToString());
                b.AddContent(2, "mic");
                b.CloseElement();
            })));

        var btn = cut.Find("button");
        Assert.Equal("Idle", btn.GetAttribute("data-state"));
    }
}
