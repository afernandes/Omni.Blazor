using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="ConfirmDialog"/>: shows message, cancel
/// and confirm buttons; the confirm path closes the active dialog with
/// <c>true</c>, the cancel path with <c>false</c>.
/// </summary>
public class ConfirmDialogTests : TestContextBase
{
    [Fact]
    public void Renders_message_and_both_buttons()
    {
        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Message, "Are you sure?")
            .Add(c => c.Options, new ConfirmOptions
            {
                OkButtonText = "Yes",
                CancelButtonText = "No"
            }));

        Assert.Contains("Are you sure?", cut.Markup);
        Assert.Contains("Yes", cut.Markup);
        Assert.Contains("No", cut.Markup);
    }

    [Fact]
    public async Task Clicking_Ok_closes_dialog_with_true()
    {
        var dialog = Services.GetRequiredService<DialogService>();
        var task = dialog.OpenAsync<ConfirmDialog>("Confirmar",
            new Dictionary<string, object?>
            {
                ["Message"] = "x",
                ["Options"] = new ConfirmOptions()
            });

        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new ConfirmOptions()));

        cut.Find("[data-omni-default]").Click();

        var result = await task;
        Assert.True((bool)result!);
    }

    [Fact]
    public async Task Clicking_Cancel_closes_dialog_with_false()
    {
        var dialog = Services.GetRequiredService<DialogService>();
        var task = dialog.OpenAsync<ConfirmDialog>("Confirmar",
            new Dictionary<string, object?>
            {
                ["Message"] = "x",
                ["Options"] = new ConfirmOptions()
            });

        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new ConfirmOptions()));

        cut.Find("[data-omni-cancel]").Click();

        var result = await task;
        Assert.False((bool)result!);
    }

    [Fact]
    public void Uses_layout_classes_that_exist_in_the_stylesheet()
    {
        // Ver AlertDialogTests: o markup usava utilitários inexistentes, então o
        // rodapé de ações não alinhava à direita nem descia para baixo da mensagem.
        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new ConfirmOptions { Icon = "alert-triangle" }));

        Assert.Contains("omni-prompt-actions", cut.Markup);
        Assert.DoesNotContain("omni-stack-16", cut.Markup);
        Assert.DoesNotContain("omni-row", cut.Markup);
    }

    [Fact]
    public void Confirm_button_comes_after_cancel_in_the_actions_row()
    {
        // A ordem no DOM é a ordem de leitura e a ordem de tabulação: cancelar
        // primeiro, confirmar por último — como no rodapé de qualquer diálogo do
        // sistema. O CSS alinha o conjunto à direita.
        var cut = Render<ConfirmDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new ConfirmOptions()));

        var markup = cut.Markup;
        Assert.True(markup.IndexOf("data-omni-cancel", StringComparison.Ordinal)
                  < markup.IndexOf("data-omni-default", StringComparison.Ordinal));
    }
}
