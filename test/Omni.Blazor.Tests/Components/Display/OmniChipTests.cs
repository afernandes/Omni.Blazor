using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniChip"/>: variants, active, click,
/// and cross-cutting splat.
/// </summary>
public class OmniChipTests : TestContextBase
{
    [Fact]
    public void Renders_default_chip_with_text()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "Filter"));

        var btn = cut.Find("button.omni-chip");
        Assert.Contains("omni-chip", btn.ClassName);
        Assert.Contains("Filter", btn.TextContent);
        Assert.Equal("button", btn.GetAttribute("type"));
    }

    [Fact]
    public void Active_adds_modifier_class()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Active, true));

        Assert.Contains("omni-chip-active", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Accent_adds_modifier_class()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Accent, true));

        Assert.Contains("omni-chip-accent", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Static_adds_modifier_class()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Static, true));

        Assert.Contains("omni-chip-static", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Accent_static_combines_both_modifier_classes()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "Status")
            .Add(c => c.Accent, true)
            .Add(c => c.Static, true));

        var className = cut.Find("button.omni-chip").ClassName;
        Assert.Contains("omni-chip-accent", className);
        Assert.Contains("omni-chip-static", className);
    }

    [Fact]
    public void Accent_static_style_is_shipped_in_the_css_bundle()
    {
        var cssPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Omni.Blazor",
            "wwwroot",
            "css",
            "omni.css");
        var css = File.ReadAllText(cssPath);
        const string selector = ".omni-chip-accent.omni-chip-static";

        var ruleStart = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(ruleStart >= 0, $"The shipped CSS bundle does not contain '{selector}'.");

        var ruleEnd = css.IndexOf('}', ruleStart);
        Assert.True(ruleEnd > ruleStart, $"The shipped CSS rule for '{selector}' is incomplete.");

        var rule = css[ruleStart..ruleEnd];
        Assert.Contains("background: var(--omni-accent-soft)", rule, StringComparison.Ordinal);
        Assert.Contains("color: var(--omni-accent)", rule, StringComparison.Ordinal);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "my-chip"));

        Assert.Contains("my-chip", cut.Find("button.omni-chip").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("button.omni-chip").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "chip1"));

        Assert.Equal("chip1", cut.Find("button.omni-chip").GetAttribute("data-testid"));
    }

    [Fact]
    public void OnClick_fires_with_event_args()
    {
        var fired = 0;
        var cut = Render<OmniChip>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.OnClick, (MouseEventArgs _) => fired++));

        cut.Find("button.omni-chip").Click();
        Assert.Equal(1, fired);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Omni.Blazor repository root.");
    }
}
