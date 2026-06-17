using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Contrato do <see cref="TourService"/> (puro C#, sem render). Cobre ativação,
/// navegação com clamp, conclusão/pulo resolvendo a Task, GoTo e persistência.
/// </summary>
public class TourServiceTests : TestContextBase
{
    private TourService Svc => Services.GetRequiredService<TourService>();

    private static TourStep[] Steps(int n) =>
        Enumerable.Range(0, n).Select(i => new TourStep { Target = $"#s{i}", Title = $"T{i}" }).ToArray();

    [Fact]
    public void StartAsync_empty_does_not_activate()
    {
        var svc = Svc;
        _ = svc.StartAsync(Array.Empty<TourStep>());
        Assert.False(svc.IsActive);
    }

    [Fact]
    public void StartAsync_activates_and_fires_OnChange()
    {
        var svc = Svc;
        var changes = 0;
        svc.OnChange += () => changes++;

        _ = svc.StartAsync(Steps(3));

        Assert.True(svc.IsActive);
        Assert.Equal(3, svc.StepCount);
        Assert.Equal(0, svc.CurrentStepIndex);
        Assert.Equal("T0", svc.CurrentStep!.Title);
        Assert.True(changes >= 1);
    }

    [Fact]
    public void Prev_is_noop_at_first_step()
    {
        var svc = Svc;
        _ = svc.StartAsync(Steps(2));
        svc.Prev();
        Assert.Equal(0, svc.CurrentStepIndex);
    }

    [Fact]
    public async Task Complete_resolves_true_and_clears()
    {
        var svc = Svc;
        var task = svc.StartAsync(Steps(2));
        await svc.CompleteAsync();
        Assert.True(await task);
        Assert.False(svc.IsActive);
    }

    [Fact]
    public async Task Skip_resolves_false_and_clears()
    {
        var svc = Svc;
        var task = svc.StartAsync(Steps(2));
        await svc.SkipAsync();
        Assert.False(await task);
        Assert.False(svc.IsActive);
    }

    [Fact]
    public async Task Next_on_last_step_completes()
    {
        var svc = Svc;
        var task = svc.StartAsync(Steps(2));
        svc.Next(); // → 1
        svc.Next(); // último → conclui
        Assert.True(await task);
        Assert.False(svc.IsActive);
    }

    [Fact]
    public void GoTo_validates_range()
    {
        var svc = Svc;
        _ = svc.StartAsync(Steps(3));
        svc.GoTo(2);
        Assert.Equal(2, svc.CurrentStepIndex);
        svc.GoTo(99);
        Assert.Equal(2, svc.CurrentStepIndex);
    }

    [Fact]
    public async Task Persist_writes_storage_on_complete()
    {
        var svc = Svc;
        var task = svc.StartAsync(Steps(1), new TourOptions { TourId = "demo", Persist = true });
        await svc.CompleteAsync();
        await task;
        var inv = JSInterop.VerifyInvoke("omniBlazor.storageSet");
        Assert.Equal("omni.tour.dismissed.demo", inv.Arguments[0]);
        Assert.Equal("1", inv.Arguments[1]);
    }
}
