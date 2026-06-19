using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniTooltipHost"/>: renders nothing when
/// the service is closed, emits an <c>.omni-tooltip</c> div at the configured
/// coordinates when opened.
/// </summary>
public class OmniTooltipHostTests : TestContextBase
{
    [Fact]
    public void Renders_nothing_when_closed()
    {
        var cut = Render<OmniTooltipHost>();

        Assert.Empty(cut.FindAll(".omni-tooltip"));
    }

    [Fact]
    public async Task Renders_tooltip_when_service_opens()
    {
        var tip = Services.GetRequiredService<TooltipService>();
        var cut = Render<OmniTooltipHost>();

        tip.Open(50, 60, "Hello");
        await cut.InvokeAsync(() => { });

        var div = cut.Find(".omni-tooltip");
        Assert.Contains("Hello", div.TextContent);
        Assert.Contains("left:50px", div.GetAttribute("style") ?? "");
    }
}
