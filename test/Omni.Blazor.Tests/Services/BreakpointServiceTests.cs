namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Observable-state behaviour of <see cref="BreakpointService"/>. Under bUnit's
/// Loose JSInterop mode <c>omniBlazor.subscribeViewport</c> returns default
/// (null string) without throwing, so these tests exercise the C# bookkeeping:
/// subscriber counting, the immediate initial fire, the <c>OnBreakpointChanged</c>
/// transition/dedup logic, dispose guards, and the <see cref="BreakpointExtensions"/>
/// comparisons — none of which depend on a real browser value.
/// </summary>
public class BreakpointServiceTests : TestContextBase
{
    private BreakpointService NewService() => new(JSInterop.JSRuntime);

    [Fact]
    public void Current_defaults_to_Md_before_any_resize()
    {
        var svc = NewService();
        Assert.Equal(Breakpoint.Md, svc.Current);
        Assert.Equal(0, svc.SubscriberCount);
    }

    [Fact]
    public async Task SubscribeAsync_increments_subscriber_count()
    {
        var svc = NewService();
        await svc.SubscribeAsync(_ => { });
        Assert.Equal(1, svc.SubscriberCount);

        await svc.SubscribeAsync(_ => { });
        Assert.Equal(2, svc.SubscriberCount);
    }

    [Fact]
    public async Task SubscribeAsync_fires_handler_immediately_with_current_breakpoint()
    {
        var svc = NewService();
        Breakpoint? seen = null;
        await svc.SubscribeAsync(bp => seen = bp);
        // Immediate fire delivers the current (default Md under Loose mode).
        Assert.Equal(Breakpoint.Md, seen);
    }

    [Fact]
    public async Task SubscribeAsync_null_handler_throws()
    {
        var svc = NewService();
        // The Func overload guards its argument directly via ArgumentNullException.ThrowIfNull.
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => svc.SubscribeAsync((Func<Breakpoint, Task>)null!));
    }

    [Fact]
    public async Task Disposing_handle_removes_the_subscriber()
    {
        var svc = NewService();
        var handle = await svc.SubscribeAsync(_ => { });
        Assert.Equal(1, svc.SubscriberCount);

        await handle.DisposeAsync();
        Assert.Equal(0, svc.SubscriberCount);
    }

    [Fact]
    public async Task Disposing_handle_twice_is_a_no_op()
    {
        var svc = NewService();
        var handle = await svc.SubscribeAsync(_ => { });

        await handle.DisposeAsync();
        await handle.DisposeAsync(); // must not underflow / throw
        Assert.Equal(0, svc.SubscriberCount);
    }

    [Fact]
    public async Task OnBreakpointChanged_updates_Current_and_notifies_subscribers()
    {
        var svc = NewService();
        int fires = 0;
        Breakpoint? last = null;
        // The immediate fire on subscribe counts once; reset after subscribing.
        await svc.SubscribeAsync(bp => { fires++; last = bp; });
        fires = 0;

        await svc.OnBreakpointChanged("lg");

        Assert.Equal(Breakpoint.Lg, svc.Current);
        Assert.Equal(Breakpoint.Lg, last);
        Assert.Equal(1, fires);
    }

    [Fact]
    public async Task OnBreakpointChanged_with_same_breakpoint_does_not_refire()
    {
        var svc = NewService();
        int fires = 0;
        await svc.SubscribeAsync(_ => fires++);
        fires = 0;

        await svc.OnBreakpointChanged("xl");   // Md -> Xl : fires
        await svc.OnBreakpointChanged("xl");   // Xl -> Xl : deduped, no fire

        Assert.Equal(Breakpoint.Xl, svc.Current);
        Assert.Equal(1, fires);
    }

    [Fact]
    public async Task OnBreakpointChanged_unknown_name_maps_to_Md()
    {
        var svc = NewService();
        await svc.OnBreakpointChanged("lg"); // move away from the Md default first
        Assert.Equal(Breakpoint.Lg, svc.Current);

        await svc.OnBreakpointChanged("totally-bogus"); // Parse fallback -> Md
        Assert.Equal(Breakpoint.Md, svc.Current);
    }

    [Fact]
    public async Task OnBreakpointChanged_parses_every_named_breakpoint()
    {
        var svc = NewService();
        // Walk through each name and assert the mapped enum value.
        await svc.OnBreakpointChanged("xs");
        Assert.Equal(Breakpoint.Xs, svc.Current);
        await svc.OnBreakpointChanged("sm");
        Assert.Equal(Breakpoint.Sm, svc.Current);
        await svc.OnBreakpointChanged("lg");
        Assert.Equal(Breakpoint.Lg, svc.Current);
        await svc.OnBreakpointChanged("xl");
        Assert.Equal(Breakpoint.Xl, svc.Current);
        await svc.OnBreakpointChanged("xxl");
        Assert.Equal(Breakpoint.Xxl, svc.Current);
    }

    [Fact]
    public async Task SubscribeAsync_after_dispose_throws_ObjectDisposedException()
    {
        var svc = NewService();
        await svc.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => svc.SubscribeAsync(_ => { }));
    }

    [Fact]
    public async Task DisposeAsync_clears_subscribers_and_is_idempotent()
    {
        var svc = NewService();
        await svc.SubscribeAsync(_ => { });
        await svc.SubscribeAsync(_ => { });

        await svc.DisposeAsync();
        Assert.Equal(0, svc.SubscriberCount);

        await svc.DisposeAsync(); // second dispose is a no-op
        Assert.Equal(0, svc.SubscriberCount);
    }

    [Theory]
    [InlineData(Breakpoint.Xs, true)]
    [InlineData(Breakpoint.Sm, true)]
    [InlineData(Breakpoint.Md, false)]
    [InlineData(Breakpoint.Lg, false)]
    public void IsMobile_is_true_only_up_to_Sm(Breakpoint bp, bool expected)
        => Assert.Equal(expected, bp.IsMobile());

    [Theory]
    [InlineData(Breakpoint.Md, true)]
    [InlineData(Breakpoint.Sm, false)]
    [InlineData(Breakpoint.Lg, false)]
    public void IsTablet_is_true_only_at_Md(Breakpoint bp, bool expected)
        => Assert.Equal(expected, bp.IsTablet());

    [Theory]
    [InlineData(Breakpoint.Lg, true)]
    [InlineData(Breakpoint.Xl, true)]
    [InlineData(Breakpoint.Xxl, true)]
    [InlineData(Breakpoint.Md, false)]
    public void IsDesktop_is_true_from_Lg_up(Breakpoint bp, bool expected)
        => Assert.Equal(expected, bp.IsDesktop());

    [Fact]
    public void AtLeast_and_AtMost_compare_by_ordinal()
    {
        Assert.True(Breakpoint.Lg.AtLeast(Breakpoint.Md));
        Assert.True(Breakpoint.Md.AtLeast(Breakpoint.Md));
        Assert.False(Breakpoint.Sm.AtLeast(Breakpoint.Md));

        Assert.True(Breakpoint.Sm.AtMost(Breakpoint.Md));
        Assert.True(Breakpoint.Md.AtMost(Breakpoint.Md));
        Assert.False(Breakpoint.Xl.AtMost(Breakpoint.Md));
    }
}
