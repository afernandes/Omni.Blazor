using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniPromptInputTests : TestContextBase
{
    private IRenderedComponent<OmniPromptInput> Render(Action<ComponentParameterCollectionBuilder<OmniPromptInput>>? extra = null)
        => RenderComponent<OmniPromptInput>(p => { extra?.Invoke(p); });

    [Fact]
    public void Renders_textarea_bound_to_value()
    {
        var cut = Render(p => p.Add(c => c.Value, "draft").Add(c => c.Placeholder, "Ask…"));
        var ta = cut.Find("textarea.omni-prompt-input-field");
        Assert.Equal("draft", ta.GetAttribute("value"));
        Assert.Equal("Ask…", ta.GetAttribute("placeholder"));
        Assert.Equal("Ask…", ta.GetAttribute("aria-label"));
    }

    [Fact]
    public void Typing_updates_Value()
    {
        string? changed = null;
        var cut = Render(p => p.Add(c => c.ValueChanged, v => changed = v));
        cut.Find("textarea").Input("hello");
        Assert.Equal("hello", changed);
    }

    [Fact]
    public void Ctrl_Enter_sends()
    {
        string? sent = null;
        var cut = Render(p => p.Add(c => c.Value, "hi").Add(c => c.OnSend, t => sent = t));
        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Equal("hi", sent);
    }

    [Fact]
    public void Meta_Enter_sends()
    {
        string? sent = null;
        var cut = Render(p => p.Add(c => c.Value, "hi").Add(c => c.OnSend, t => sent = t));
        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", MetaKey = true });
        Assert.Equal("hi", sent);
    }

    [Theory]
    [InlineData(false, false)] // plain Enter → newline, no send
    [InlineData(true, true)]   // Ctrl+Shift+Enter → no send
    public void Plain_or_shifted_Enter_does_not_send(bool ctrl, bool shift)
    {
        string? sent = null;
        var cut = Render(p => p.Add(c => c.Value, "hi").Add(c => c.OnSend, t => sent = t));
        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = ctrl, ShiftKey = shift });
        Assert.Null(sent);
    }

    [Fact]
    public void Send_button_sends_and_is_disabled_when_empty()
    {
        string? sent = null;
        var cut = Render(p => p.Add(c => c.OnSend, t => sent = t));
        var send = cut.Find("button[aria-label='Send']");
        Assert.True(send.HasAttribute("disabled"));   // empty → cannot send

        cut.SetParametersAndRender(p => p.Add(c => c.Value, "go"));
        send = cut.Find("button[aria-label='Send']");
        Assert.False(send.HasAttribute("disabled"));
        send.Click();
        Assert.Equal("go", sent);
    }

    [Fact]
    public void Counter_shows_count_and_max()
    {
        var cut = Render(p => p.Add(c => c.Value, "abcd"));
        Assert.Equal("4", cut.Find(".omni-prompt-input-count").TextContent);

        cut.SetParametersAndRender(p => p.Add(c => c.Value, "abcd").Add(c => c.MaxLength, 100));
        Assert.Equal("4/100", cut.Find(".omni-prompt-input-count").TextContent);
        Assert.Equal("100", cut.Find("textarea").GetAttribute("maxlength"));
    }

    [Fact]
    public void Renders_Actions_slot()
    {
        var cut = Render(p => p.Add(c => c.Actions, b => b.AddMarkupContent(0, "<button class=\"mic\">🎤</button>")));
        Assert.NotNull(cut.Find(".omni-prompt-input-actions button.mic"));
    }

    [Fact]
    public void Appends_Class_Style_and_Attributes()
    {
        var cut = Render(p => p
            .Add(c => c.Class, "cc").Add(c => c.Style, "width: 400px")
            .AddUnmatched("data-testid", "composer"));
        var root = cut.Find("div.omni-prompt-input");
        Assert.Contains("cc", root.ClassName);
        Assert.Equal("width: 400px", root.GetAttribute("style"));
        Assert.Equal("composer", root.GetAttribute("data-testid"));
    }
}
