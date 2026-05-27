namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniOverlayHosts"/>: convenience
/// wrapper that composes the four overlay hosts (Dialog, Notification,
/// Tooltip, ContextMenu). Each host renders empty when its service is idle —
/// the wrapper itself just delegates.
/// </summary>
public class OmniOverlayHostsTests : TestContextBase
{
    [Fact]
    public void Renders_without_throwing_when_no_services_active()
    {
        var cut = RenderComponent<OmniOverlayHosts>();

        // All four child hosts render empty when their services are idle —
        // markup is therefore empty, but the call itself must succeed.
        Assert.Empty(cut.FindAll(".omni-toast"));
        Assert.Empty(cut.FindAll(".omni-tooltip"));
        Assert.Empty(cut.FindAll(".omni-context-menu"));
        Assert.Empty(cut.FindAll(".omni-dialog"));
    }
}
