using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniTheme"/>: emits the library
/// stylesheet link + the inline anti-FOUC bootstrap script.
/// </summary>
public class OmniThemeTests : TestContextBase
{
    [Fact]
    public void Renders_link_and_script_elements()
    {
        var cut = RenderComponent<OmniTheme>();

        var link = cut.Find("link");
        Assert.Equal("stylesheet", link.GetAttribute("rel"));
        Assert.StartsWith("_content/Omni.Blazor/css/omni.css", link.GetAttribute("href"));

        var script = cut.Find("script");
        Assert.NotNull(script);
        Assert.Contains("data-accent", script.TextContent);
    }

    [Fact]
    public void Custom_Accent_appears_in_inline_script()
    {
        var cut = RenderComponent<OmniTheme>(p => p.Add(c => c.Accent, "emerald"));

        Assert.Contains("'emerald'", cut.Find("script").TextContent);
    }

    [Fact]
    public void Unknown_Accent_falls_back_to_amber()
    {
        var cut = RenderComponent<OmniTheme>(p => p.Add(c => c.Accent, "neon-pink"));

        Assert.Contains("'amber'", cut.Find("script").TextContent);
    }

    [Fact]
    public void Dark_true_seeds_inline_script_with_true()
    {
        var cut = RenderComponent<OmniTheme>(p => p.Add(c => c.Dark, true));

        Assert.Contains("var dark = true", cut.Find("script").TextContent);
    }

    [Fact]
    public void Dark_false_seeds_inline_script_with_false()
    {
        var cut = RenderComponent<OmniTheme>(p => p.Add(c => c.Dark, false));

        Assert.Contains("var dark = false", cut.Find("script").TextContent);
    }
}
