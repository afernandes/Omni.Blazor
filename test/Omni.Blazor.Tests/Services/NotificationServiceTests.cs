namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Contrato do <see cref="NotificationService"/> (estado puro, sem IJSRuntime).
/// Cobre Notify/overloads/atalhos, Remove (com OnClose), Clear, o auto-remove
/// por Duration e o disparo de OnChange. Sob Loose mode nao ha JS: assertamos
/// somente o estado observavel (lista Messages, Position) e a contagem de eventos.
/// Duration = 0 evita o Task.Delay agendado, mantendo os testes deterministicos.
/// </summary>
public class NotificationServiceTests : TestContextBase
{
    private static NotificationMessage Msg(double duration = 0, string? summary = "s") =>
        new() { Summary = summary, Duration = duration };

    [Fact]
    public void Notify_adds_message_and_fires_OnChange()
    {
        var svc = new NotificationService();
        var changes = 0;
        svc.OnChange += () => changes++;

        svc.Notify(Msg());

        Assert.Single(svc.Messages);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Notify_overload_builds_message_from_args()
    {
        var svc = new NotificationService();

        svc.Notify(NotificationSeverity.Warning, "Heads up", "detail here", duration: 0);

        var m = Assert.Single(svc.Messages);
        Assert.Equal(NotificationSeverity.Warning, m.Severity);
        Assert.Equal("Heads up", m.Summary);
        Assert.Equal("detail here", m.Detail);
        Assert.Equal(0, m.Duration);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public void Severity_shortcuts_set_expected_severity(NotificationSeverity severity)
    {
        var svc = new NotificationService();

        // duration 0 evita o auto-remove agendado; testamos so a severidade resultante.
        switch (severity)
        {
            case NotificationSeverity.Info: svc.Info("x", duration: 0); break;
            case NotificationSeverity.Success: svc.Success("x", duration: 0); break;
            case NotificationSeverity.Warning: svc.Warning("x", duration: 0); break;
            case NotificationSeverity.Error: svc.Error("x", duration: 0); break;
        }

        var m = Assert.Single(svc.Messages);
        Assert.Equal(severity, m.Severity);
        Assert.Equal("x", m.Summary);
    }

    [Fact]
    public void Remove_present_message_removes_and_invokes_OnClose_and_OnChange()
    {
        var svc = new NotificationService();
        var closed = 0;
        var m = Msg();
        m.OnClose = _ => closed++;
        svc.Notify(m);

        var changes = 0;
        svc.OnChange += () => changes++;

        svc.Remove(m);

        Assert.Empty(svc.Messages);
        Assert.Equal(1, closed);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Remove_unknown_message_is_noop()
    {
        var svc = new NotificationService();
        svc.Notify(Msg());

        var closed = 0;
        var changes = 0;
        var stranger = Msg();
        stranger.OnClose = _ => closed++;
        svc.OnChange += () => changes++;

        svc.Remove(stranger); // nunca foi adicionada -> guarda: no-op

        Assert.Single(svc.Messages);
        Assert.Equal(0, closed);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Clear_empties_list_and_fires_OnChange()
    {
        var svc = new NotificationService();
        svc.Notify(Msg());
        svc.Notify(Msg());

        var changes = 0;
        svc.OnChange += () => changes++;

        svc.Clear();

        Assert.Empty(svc.Messages);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Position_defaults_to_TopRight_and_is_settable()
    {
        var svc = new NotificationService();

        Assert.Equal(NotificationPosition.TopRight, svc.Position);

        svc.Position = NotificationPosition.BottomLeft;

        Assert.Equal(NotificationPosition.BottomLeft, svc.Position);
    }

    [Fact]
    public void Notify_preserves_insertion_order()
    {
        var svc = new NotificationService();
        var a = Msg(summary: "a");
        var b = Msg(summary: "b");

        svc.Notify(a);
        svc.Notify(b);

        Assert.Equal(2, svc.Messages.Count);
        Assert.Same(a, svc.Messages[0]);
        Assert.Same(b, svc.Messages[1]);
    }

    [Fact]
    public async Task Notify_with_positive_duration_auto_removes_after_delay()
    {
        using var svc = new NotificationService();
        var m = Msg(duration: 30);

        // The auto-remove fires OnChange when the list empties — await that (bounded),
        // no time-based polling loop (deterministic, not flaky under load).
        var emptied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.OnChange += () => { if (svc.Messages.Count == 0) emptied.TrySetResult(); };

        svc.Notify(m);
        Assert.Single(svc.Messages);

        await emptied.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);

        Assert.Empty(svc.Messages);
    }

    [Fact]
    public void OnChange_can_reenter_Clear_without_deadlock()
    {
        using var svc = new NotificationService();
        var reentered = 0;
        svc.OnChange += () =>
        {
            if (Interlocked.Exchange(ref reentered, 1) == 0)
            {
                svc.Clear();
            }
        };

        svc.Notify(Msg());

        Assert.Empty(svc.Messages);
    }

    [Fact]
    public async Task Clear_cancels_pending_expiration_without_late_change()
    {
        using var svc = new NotificationService();
        var changes = 0;
        svc.OnChange += () => Interlocked.Increment(ref changes);
        svc.Notify(Msg(duration: 50));

        svc.Clear();
        await Task.Delay(150, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(2, Volatile.Read(ref changes));
        Assert.Empty(svc.Messages);
    }

    [Fact]
    public void Dispose_clears_state_and_rejects_new_notifications()
    {
        var svc = new NotificationService();
        svc.Notify(Msg(duration: 5_000));

        svc.Dispose();

        Assert.Empty(svc.Messages);
        Assert.Throws<ObjectDisposedException>(() => svc.Notify(Msg()));
    }
}
