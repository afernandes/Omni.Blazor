using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Contrato de render do <see cref="OmniTourHost"/>: render condicional, contador,
/// rótulo do botão final, ocultar "Anterior" no 1º passo, cliques mutando o serviço,
/// a11y e Esc. A geometria do spotlight (JS) é exercida só por verificação visual.
/// </summary>
public class OmniTourHostTests : TestContextBase
{
    private TourService Svc => Services.GetRequiredService<TourService>();

    private static TourStep[] Steps(int n) =>
        Enumerable.Range(0, n).Select(i => new TourStep { Target = $"#s{i}", Title = $"T{i}", Description = $"D{i}" }).ToArray();

    [Fact]
    public void Not_rendered_when_inactive()
    {
        var cut = RenderComponent<OmniTourHost>();
        Assert.Empty(cut.FindAll(".omni-tour-coachmark"));
    }

    [Fact]
    public void Renders_coachmark_and_counter_when_active()
    {
        _ = Svc.StartAsync(Steps(2));
        var cut = RenderComponent<OmniTourHost>();

        Assert.NotNull(cut.Find(".omni-tour-coachmark"));
        Assert.Equal("1 / 2", cut.Find(".omni-tour-counter").TextContent.Trim());
        Assert.NotNull(cut.Find(".omni-tour-cutout")); // backdrop padrão
    }

    [Fact]
    public void Backdrop_can_be_disabled()
    {
        _ = Svc.StartAsync(Steps(1), new TourOptions { ShowBackdrop = false });
        var cut = RenderComponent<OmniTourHost>();

        Assert.NotNull(cut.Find(".omni-tour-coachmark"));
        Assert.Empty(cut.FindAll(".omni-tour-cutout"));
    }

    [Fact]
    public void Next_advances_and_shows_complete_on_last()
    {
        _ = Svc.StartAsync(Steps(2));
        var cut = RenderComponent<OmniTourHost>();

        cut.Find(".omni-tour-btn-primary").Click(); // Próximo
        Assert.Equal("2 / 2", cut.Find(".omni-tour-counter").TextContent.Trim());
        Assert.Contains("Concluir", cut.Find(".omni-tour-btn-primary").TextContent);
    }

    [Fact]
    public void Prev_button_hidden_on_first_step()
    {
        _ = Svc.StartAsync(Steps(2));
        var cut = RenderComponent<OmniTourHost>();

        Assert.DoesNotContain("Anterior", cut.Markup);
        cut.Find(".omni-tour-btn-primary").Click(); // → passo 2
        Assert.Contains("Anterior", cut.Markup);
    }

    [Fact]
    public void Close_button_skips_tour()
    {
        var svc = Svc;
        _ = svc.StartAsync(Steps(2));
        var cut = RenderComponent<OmniTourHost>();

        cut.Find(".omni-tour-close").Click();

        Assert.False(svc.IsActive);
        Assert.Empty(cut.FindAll(".omni-tour-coachmark"));
    }

    [Fact]
    public void A11y_attributes_present()
    {
        _ = Svc.StartAsync(Steps(1));
        var cut = RenderComponent<OmniTourHost>();

        var coach = cut.Find(".omni-tour-coachmark");
        Assert.Equal("dialog", coach.GetAttribute("role"));
        Assert.Equal("true", coach.GetAttribute("aria-modal"));
        Assert.Equal("true", coach.GetAttribute("data-omni-focus-trap"));
        Assert.NotNull(cut.Find(".omni-tour-counter").GetAttribute("aria-live"));
    }

    [Fact]
    public async Task OnEscape_skips_tour()
    {
        var svc = Svc;
        _ = svc.StartAsync(Steps(2));
        var cut = RenderComponent<OmniTourHost>();

        await cut.Instance.OnEscape();

        Assert.False(svc.IsActive);
    }
}
