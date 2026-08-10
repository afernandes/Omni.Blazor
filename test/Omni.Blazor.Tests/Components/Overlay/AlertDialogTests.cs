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

    [Fact]
    public void Uses_layout_classes_that_exist_in_the_stylesheet()
    {
        // Regressão: o markup montava o layout com .omni-stack-16, .omni-row,
        // .omni-row-gap-12 e .omni-row-end — nenhuma delas existe no omni.css.
        // Sem flex-direction o .omni-stack caía em row, e o botão OK aparecia ao
        // lado da mensagem em vez de num rodapé abaixo dela.
        var cut = Render<AlertDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new AlertOptions { Icon = "check-circle" }));

        Assert.Contains("omni-prompt", cut.Markup);
        Assert.Contains("omni-prompt-message", cut.Markup);
        Assert.Contains("omni-prompt-actions", cut.Markup);

        Assert.DoesNotContain("omni-stack-16", cut.Markup);
        Assert.DoesNotContain("omni-row", cut.Markup);
    }

    [Fact]
    public void Icon_semantic_colour_class_wraps_the_icon_instead_of_sharing_its_element()
    {
        // .omni-toast-info é seletor DESCENDENTE de .omni-toast-ico: com as duas
        // classes no MESMO elemento a regra de cor nunca casava e o ícone ficava
        // cinza. O wrapper restaura a cor semântica.
        var cut = Render<AlertDialog>(p => p
            .Add(c => c.Message, "x")
            .Add(c => c.Options, new AlertOptions { Icon = "check-circle" }));

        var ico = cut.Find(".omni-toast-ico");
        Assert.NotNull(ico.ParentElement);
        Assert.Contains("omni-toast-info", ico.ParentElement!.GetAttribute("class"));
        Assert.DoesNotContain("omni-toast-info", ico.GetAttribute("class"));
    }
}
