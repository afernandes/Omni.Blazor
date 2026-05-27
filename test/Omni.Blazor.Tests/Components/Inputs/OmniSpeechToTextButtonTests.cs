using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSpeechToTextButton"/>: wraps
/// OmniButton with mic/stop icon + state-aware Class. The actual recognition
/// runs through JSInterop (loose mode here) so we only assert the visible
/// rendering, not state transitions.
/// </summary>
public class OmniSpeechToTextButtonTests : TestContextBase
{
    [Fact]
    public void Renders_button_with_omni_speech_btn_class()
    {
        var cut = RenderComponent<OmniSpeechToTextButton>();
        var btn = cut.Find("button");
        Assert.Contains("omni-speech-btn", btn.ClassName);
    }

    [Fact]
    public void Initial_state_is_Idle()
    {
        var cut = RenderComponent<OmniSpeechToTextButton>();
        Assert.Equal(SpeechRecognitionState.Idle, cut.Instance.State);
        Assert.False(cut.Instance.IsRecording);
    }

    [Theory]
    [InlineData(ButtonVariant.Primary, "omni-btn-primary")]
    [InlineData(ButtonVariant.Ghost,   "omni-btn-ghost")]
    [InlineData(ButtonVariant.Danger,  "omni-btn-danger")]
    public void Forwards_Variant_to_underlying_button(ButtonVariant variant, string expected)
    {
        var cut = RenderComponent<OmniSpeechToTextButton>(p => p.Add(c => c.Variant, variant));
        Assert.Contains(expected, cut.Find("button").ClassName);
    }

    [Fact]
    public void Disabled_propagates_to_button()
    {
        var cut = RenderComponent<OmniSpeechToTextButton>(p => p.Add(c => c.Disabled, true));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Text_renders_into_button()
    {
        var cut = RenderComponent<OmniSpeechToTextButton>(p => p.Add(c => c.Text, "Falar"));
        Assert.Contains("Falar", cut.Find("button").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_button()
    {
        var cut = RenderComponent<OmniSpeechToTextButton>(p => p
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("button").ClassName);
    }
}
