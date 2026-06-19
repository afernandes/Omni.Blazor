using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="AlertDialog"/>: shows the message body
/// and the OK button. Clicking OK closes the active dialog through
/// <c>DialogService</c>.
/// </summary>
public class AlertDialogTests : TestContextBase
{
    [Fact]
    public void Renders_message_and_ok_button()
    {
        var cut = Render<AlertDialog>(p => p
            .Add(c => c.Message, "Hello world")
            .Add(c => c.Options, new AlertOptions { OkButtonText = "OK" }));

        Assert.Contains("Hello world", cut.Markup);
        Assert.Contains("OK", cut.Markup);
    }

    [Fact]
    public void Default_options_use_Entendi_label()
    {
        var cut = Render<AlertDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new AlertOptions()));

        Assert.Contains("Entendi", cut.Markup);
    }

    [Fact]
    public async Task Clicking_Ok_closes_dialog_with_true_result()
    {
        var dialog = Services.GetRequiredService<DialogService>();
        var task = dialog.OpenAsync<AlertDialog>("Aviso",
            new Dictionary<string, object?>
            {
                ["Message"] = "x",
                ["Options"] = new AlertOptions()
            });

        var cut = Render<AlertDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new AlertOptions()));

        // Click the primary OK button (data-omni-default flag).
        cut.Find("[data-omni-default]").Click();

        var result = await task;
        Assert.True((bool)result!);
    }
}
