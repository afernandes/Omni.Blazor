using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniContextMenuHost"/>: empty until the
/// <c>ContextMenuService</c> opens with items, then renders an
/// <c>.omni-context-menu</c> wrapper hosting an <c>OmniMenu</c>.
/// </summary>
public class OmniContextMenuHostTests : TestContextBase
{
    [Fact]
    public void Renders_nothing_when_closed()
    {
        var cut = Render<OmniContextMenuHost>();

        Assert.Empty(cut.FindAll(".omni-context-menu"));
    }

    [Fact]
    public async Task Renders_menu_when_service_opens()
    {
        var menu = Services.GetRequiredService<ContextMenuService>();
        var cut = Render<OmniContextMenuHost>();

        menu.Open(10, 20, new[]
        {
            new ContextMenuItem { Text = "Rename" },
            ContextMenuItem.Separator(),
            new ContextMenuItem { Text = "Delete", IsDanger = true }
        });
        await cut.InvokeAsync(() => { });

        Assert.NotNull(cut.Find(".omni-context-menu"));
        // Inner OmniMenu emits role="menu"
        Assert.NotNull(cut.Find(".omni-context-menu .omni-menu"));
    }
}
