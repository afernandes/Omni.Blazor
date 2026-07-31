using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

public class OmniSignaturePadTests : TestContextBase
{
    [Fact]
    public void Renders_canvas_toolbar_and_cross_cutting_attributes()
    {
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.Class, "custom-signature")
            .Add(component => component.Style, "max-width: 42rem")
            .AddUnmatched("data-testid", "signature"));

        var root = cut.Find(".omni-signature-pad");
        Assert.Contains("custom-signature", root.ClassName);
        Assert.Equal("max-width: 42rem", root.GetAttribute("style"));
        Assert.Equal("signature", root.GetAttribute("data-testid"));
        Assert.NotNull(cut.Find("canvas.omni-signature-pad-canvas"));
        Assert.Equal(2, cut.FindAll(".omni-signature-pad-action").Count);
    }

    [Fact]
    public void Can_hide_toolbar_and_configure_canvas_height()
    {
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.ShowToolbar, false)
            .Add(component => component.Height, 320));

        Assert.Empty(cut.FindAll(".omni-signature-pad-toolbar"));
        Assert.Contains("320px", cut.Find("canvas").GetAttribute("style"));
    }

    [Fact]
    public void Disabled_state_is_accessible_and_blocks_canvas_tab_stop()
    {
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.Disabled, true));

        Assert.Equal("true", cut.Find(".omni-signature-pad").GetAttribute("aria-disabled"));
        Assert.Equal("-1", cut.Find("canvas").GetAttribute("tabindex"));
        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public async Task Browser_callback_updates_binding_and_completion_event()
    {
        string? boundValue = null;
        string? completedValue = null;
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.ValueChanged,
                EventCallback.Factory.Create<string?>(this, value => boundValue = value))
            .Add(component => component.StrokeCompleted,
                EventCallback.Factory.Create<string?>(this, value => completedValue = value)));

        await cut.InvokeAsync(() =>
            cut.Instance.OnSignatureChangedAsync("data:image/png;base64,abc", isEmpty: false));

        Assert.True(cut.Instance.HasSignature);
        Assert.Equal("data:image/png;base64,abc", boundValue);
        Assert.Equal(boundValue, completedValue);
        Assert.Contains("Assinatura capturada", cut.Markup);
    }

    [Fact]
    public async Task Clear_without_browser_handle_resets_value_safely()
    {
        string? boundValue = "old";
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.Value, "old")
            .Add(component => component.ValueChanged,
                EventCallback.Factory.Create<string?>(this, value => boundValue = value)));

        await cut.InvokeAsync(cut.Instance.ClearAsync);

        Assert.Null(boundValue);
        Assert.False(cut.Instance.HasSignature);
    }

    [Fact]
    public void External_initial_value_is_reflected_in_status()
    {
        var cut = Render<OmniSignaturePad>(parameters => parameters
            .Add(component => component.Value, "data:image/png;base64,existing"));

        Assert.True(cut.Instance.HasSignature);
        Assert.Contains("Assinatura capturada", cut.Markup);
    }
}
