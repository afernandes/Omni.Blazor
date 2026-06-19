using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniNotificationHost"/>: empty when no
/// toasts queued, renders a toast container with the per-position host modifier
/// and per-severity toast modifier when <c>NotificationService</c> pushes a
/// message.
/// </summary>
public class OmniNotificationHostTests : TestContextBase
{
    [Fact]
    public void Renders_nothing_when_no_messages()
    {
        var cut = Render<OmniNotificationHost>();

        Assert.Empty(cut.FindAll(".omni-toast-host"));
        Assert.Empty(cut.FindAll(".omni-toast"));
    }

    [Fact]
    public async Task Renders_toast_when_service_pushes_message()
    {
        var notif = Services.GetRequiredService<NotificationService>();
        var cut = Render<OmniNotificationHost>();

        notif.Info("Hello", "World", duration: 0);
        await cut.InvokeAsync(() => { /* let OnChange propagate */ });

        var host = cut.Find(".omni-toast-host");
        Assert.NotNull(host);
        var toast = cut.Find(".omni-toast");
        Assert.Contains("omni-toast-info", toast.ClassName);
        Assert.Contains("Hello", toast.TextContent);
    }

    [Fact]
    public async Task Severity_maps_to_modifier_class()
    {
        var notif = Services.GetRequiredService<NotificationService>();
        var cut = Render<OmniNotificationHost>();

        notif.Error("Oops", duration: 0);
        await cut.InvokeAsync(() => { });

        Assert.Contains("omni-toast-error", cut.Find(".omni-toast").ClassName);
    }
}
