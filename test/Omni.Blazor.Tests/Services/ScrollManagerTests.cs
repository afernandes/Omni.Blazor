namespace Omni.Blazor.Tests.Services;

/// <summary>
/// <see cref="ScrollManager"/> is a thin façade over <c>window.omniBlazor.*</c>
/// JS interop. Under bUnit's Loose JSInterop mode those calls are no-ops that
/// return default, so these tests assert the *invocation contract* — the exact
/// JS function name and the marshalled arguments — via <c>JSInterop.VerifyInvoke</c>,
/// plus the reader guards (default value, never throws) and the enum → JS-string
/// mapping. Values that would come back from a real browser are never asserted.
/// </summary>
public class ScrollManagerTests : TestContextBase
{
    private ScrollManager Svc => new(JSInterop.JSRuntime);

    [Fact]
    public async Task LockScroll_defaults_to_auto_selector()
    {
        await Svc.LockScrollAsync();
        var inv = JSInterop.VerifyInvoke("omniBlazor.lockScroll");
        Assert.Equal("auto", inv.Arguments[0]);
    }

    [Fact]
    public async Task UnlockScroll_forwards_explicit_selector()
    {
        await Svc.UnlockScrollAsync("html");
        var inv = JSInterop.VerifyInvoke("omniBlazor.unlockScroll");
        Assert.Equal("html", inv.Arguments[0]);
    }

    [Fact]
    public async Task GetLockCount_returns_zero_under_loose_mode_and_does_not_throw()
    {
        // JS return is not observable in Loose mode; the guard yields default(int).
        Assert.Equal(0, await Svc.GetLockCountAsync());
    }

    [Fact]
    public async Task ScrollTo_uses_auto_when_selector_is_null_and_passes_position_payload()
    {
        await Svc.ScrollToAsync(null, top: 120, left: 40, behavior: ScrollBehavior.Smooth);
        var inv = JSInterop.VerifyInvoke("omniBlazor.scrollTo");
        Assert.Equal("auto", inv.Arguments[0]);
        // Second arg is an anonymous object { top, left, behavior }; assert it was marshalled.
        Assert.NotNull(inv.Arguments[1]);
    }

    [Fact]
    public async Task ScrollTo_forwards_explicit_selector()
    {
        await Svc.ScrollToAsync(".omni-body", top: 10);
        var inv = JSInterop.VerifyInvoke("omniBlazor.scrollTo");
        Assert.Equal(".omni-body", inv.Arguments[0]);
    }

    [Fact]
    public async Task ScrollIntoView_forwards_selector_and_payload()
    {
        await Svc.ScrollIntoViewAsync("#anchor", ScrollBehavior.Instant, ScrollBlock.Center);
        var inv = JSInterop.VerifyInvoke("omniBlazor.scrollIntoView");
        Assert.Equal("#anchor", inv.Arguments[0]);
        Assert.NotNull(inv.Arguments[1]);
    }

    [Fact]
    public async Task ScrollToTop_defaults_selector_to_auto()
    {
        await Svc.ScrollToTopAsync();
        var inv = JSInterop.VerifyInvoke("omniBlazor.scrollToTop");
        Assert.Equal("auto", inv.Arguments[0]);
        // behavior default is Smooth → mapped to "smooth".
        Assert.Equal("smooth", inv.Arguments[1]);
    }

    [Fact]
    public async Task ScrollToBottom_maps_auto_behavior_to_auto_string()
    {
        await Svc.ScrollToBottomAsync(behavior: ScrollBehavior.Auto);
        var inv = JSInterop.VerifyInvoke("omniBlazor.scrollToBottom");
        Assert.Equal("auto", inv.Arguments[0]);
        Assert.Equal("auto", inv.Arguments[1]);
    }

    [Fact]
    public async Task GetScrollOffsetY_returns_zero_under_loose_mode_and_does_not_throw()
    {
        Assert.Equal(0d, await Svc.GetScrollOffsetYAsync());
    }

    [Fact]
    public async Task ObserveScrollPosition_registers_the_observer_and_returns_disposable()
    {
        Task Handler(ScrollPositionInfo _) => Task.CompletedTask;
        var handle = await Svc.ObserveScrollPositionAsync("auto", Handler);

        var inv = JSInterop.VerifyInvoke("omniBlazor.observeScrollPosition");
        Assert.Equal("auto", inv.Arguments[0]);
        Assert.NotNull(handle);

        // Disposal must not throw even though the JS token came back null in Loose mode.
        await handle.DisposeAsync();
    }
}
