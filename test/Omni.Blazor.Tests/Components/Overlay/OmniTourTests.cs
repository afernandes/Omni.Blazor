using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Contrato do declarativo <see cref="OmniTour"/> + <see cref="OmniTourStep"/>:
/// registro/coleta de passos, AutoStart, e mapeamento Target/Title/Position.
/// </summary>
public class OmniTourTests : TestContextBase
{
    private TourService Svc => Services.GetRequiredService<TourService>();

    private static void Step(RenderTreeBuilder b, int seq, string target, string? title = null, TourPosition pos = TourPosition.Auto)
    {
        b.OpenComponent<OmniTourStep>(seq);
        b.AddAttribute(seq + 1, "Target", target);
        if (title is not null) b.AddAttribute(seq + 2, "Title", title);
        b.AddAttribute(seq + 3, "Position", pos);
        b.CloseComponent();
    }

    [Fact]
    public void Steps_register_with_parent()
    {
        var cut = RenderComponent<OmniTour>(p => p.AddChildContent(b =>
        {
            Step(b, 0, "#a");
            Step(b, 10, "#b");
            Step(b, 20, "#c");
        }));

        Assert.Equal(3, cut.Instance.StepCount);
    }

    [Fact]
    public void AutoStart_starts_the_tour()
    {
        var svc = Svc;
        var cut = RenderComponent<OmniTour>(p =>
        {
            p.Add(c => c.AutoStart, true);
            p.AddChildContent(b => Step(b, 0, "#a"));
        });

        Assert.True(svc.IsActive);
        Assert.Equal(1, svc.StepCount);
    }

    [Fact]
    public void Step_maps_target_title_position()
    {
        var svc = Svc;
        RenderComponent<OmniTour>(p =>
        {
            p.Add(c => c.AutoStart, true);
            p.AddChildContent(b => Step(b, 0, "#x", "Olá", TourPosition.Left));
        });

        var step = svc.CurrentStep!;
        Assert.Equal("#x", step.Target);
        Assert.Equal("Olá", step.Title);
        Assert.Equal(TourPosition.Left, step.Position);
    }
}
