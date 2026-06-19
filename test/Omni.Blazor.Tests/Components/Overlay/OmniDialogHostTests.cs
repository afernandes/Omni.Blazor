using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniDialogHost"/>: stays empty when no
/// dialog is open, renders an overlay/dialog wrap when <c>DialogService</c>
/// pushes a dialog reference.
/// </summary>
public class OmniDialogHostTests : TestContextBase
{
    [Fact]
    public void Renders_nothing_when_no_open_dialogs()
    {
        var cut = Render<OmniDialogHost>();

        Assert.Empty(cut.FindAll(".omni-dialog-host"));
        Assert.Empty(cut.FindAll(".omni-dialog"));
    }

    [Fact]
    public async Task Renders_dialog_when_service_opens_one()
    {
        var dialog = Services.GetRequiredService<DialogService>();
        var cut = Render<OmniDialogHost>();

        _ = dialog.OpenAsync<OmniMenuSeparator>("Test", parameters: null);
        await cut.InvokeAsync(() => { /* let OnChange propagate */ });

        Assert.NotNull(cut.Find(".omni-dialog-host"));
        Assert.NotNull(cut.Find(".omni-dialog"));
    }
}
