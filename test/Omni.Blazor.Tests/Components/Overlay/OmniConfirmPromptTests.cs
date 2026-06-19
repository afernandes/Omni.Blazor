using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniConfirmPrompt"/>: typed-confirmation
/// gate, confirm callback, case sensitivity, and the cross-cutting splat.
/// </summary>
public class OmniConfirmPromptTests : TestContextBase
{
    [Fact]
    public void Confirm_button_disabled_until_phrase_matches()
    {
        var cut = Render<OmniConfirmPrompt>(p => p.Add(c => c.ConfirmationText, "delete"));
        Assert.True(cut.Find(".omni-confirm-prompt-confirm").HasAttribute("disabled"));

        cut.Find(".omni-confirm-prompt-input").Input("delete");
        Assert.False(cut.Find(".omni-confirm-prompt-confirm").HasAttribute("disabled"));
    }

    [Fact]
    public void Wrong_phrase_keeps_button_disabled()
    {
        var cut = Render<OmniConfirmPrompt>(p => p.Add(c => c.ConfirmationText, "delete"));
        cut.Find(".omni-confirm-prompt-input").Input("nope");
        Assert.True(cut.Find(".omni-confirm-prompt-confirm").HasAttribute("disabled"));
    }

    [Fact]
    public void Confirm_fires_only_after_match()
    {
        var confirmed = false;
        var cut = Render<OmniConfirmPrompt>(p => p
            .Add(c => c.ConfirmationText, "delete")
            .Add(c => c.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true)));
        cut.Find(".omni-confirm-prompt-input").Input("delete");
        cut.Find(".omni-confirm-prompt-confirm").Click();
        Assert.True(confirmed);
    }

    [Fact]
    public void Case_insensitive_match_when_configured()
    {
        var cut = Render<OmniConfirmPrompt>(p => p
            .Add(c => c.ConfirmationText, "Delete")
            .Add(c => c.CaseSensitive, false));
        cut.Find(".omni-confirm-prompt-input").Input("delete");
        Assert.False(cut.Find(".omni-confirm-prompt-confirm").HasAttribute("disabled"));
    }

    [Fact]
    public void Renders_title_and_button_text()
    {
        var cut = Render<OmniConfirmPrompt>(p => p
            .Add(c => c.ConfirmationText, "x")
            .Add(c => c.Title, "Excluir loja")
            .Add(c => c.ButtonText, "Excluir"));
        Assert.Equal("Excluir loja", cut.Find(".omni-confirm-prompt-title").TextContent);
        Assert.Contains("Excluir", cut.Find(".omni-confirm-prompt-confirm").TextContent);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniConfirmPrompt>(p => p
            .Add(c => c.ConfirmationText, "x")
            .Add(c => c.Class, "y")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "c1"));
        var root = cut.Find(".omni-confirm-prompt");
        Assert.Contains("y", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("c1", root.GetAttribute("data-testid"));
    }
}
