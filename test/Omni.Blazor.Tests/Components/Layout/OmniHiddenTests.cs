using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniHidden"/>: renders children when the
/// current breakpoint passes the visibility rule. Test asserts the rule logic
/// against the public <c>Visible</c> getter (current breakpoint starts at Md).
/// </summary>
public class OmniHiddenTests : TestContextBase
{
    [Fact]
    public void Renders_children_when_visible()
    {
        // Default: Mode=Down, Breakpoint=Md. _current starts at Md → hide=true →
        // Visible=false (because Md<=Md). Use Invert=true to flip the rule.
        var cut = Render<OmniHidden>(p => p
            .Add(c => c.Breakpoint, Breakpoint.Md)
            .Add(c => c.Mode, HiddenMode.Up)        // hide if current >= Md
            .Add(c => c.Invert, true)               // therefore render when current >= Md
            .AddChildContent("<span data-testid=\"slot\">visible</span>"));

        Assert.NotNull(cut.Find("[data-testid='slot']"));
    }

    [Fact]
    public void Hides_children_when_not_visible()
    {
        // Default _current=Md. Mode=Down: hide when current<=Md → hides.
        var cut = Render<OmniHidden>(p => p
            .Add(c => c.Breakpoint, Breakpoint.Md)
            .Add(c => c.Mode, HiddenMode.Down)
            .AddChildContent("<span data-testid=\"slot\">hidden</span>"));

        Assert.Empty(cut.FindAll("[data-testid='slot']"));
    }

    [Fact]
    public void Only_mode_hides_only_at_exact_breakpoint()
    {
        // current=Md, breakpoint=Md, Only → hide=true.
        var cut = Render<OmniHidden>(p => p
            .Add(c => c.Breakpoint, Breakpoint.Md)
            .Add(c => c.Mode, HiddenMode.Only)
            .AddChildContent("<span data-testid=\"slot\">x</span>"));

        Assert.False(cut.Instance.Visible);
        Assert.Empty(cut.FindAll("[data-testid='slot']"));
    }

    [Fact]
    public void Only_mode_renders_when_breakpoint_differs()
    {
        var cut = Render<OmniHidden>(p => p
            .Add(c => c.Breakpoint, Breakpoint.Xl)   // current is Md by default; Only=Xl → not hidden
            .Add(c => c.Mode, HiddenMode.Only)
            .AddChildContent("<span data-testid=\"slot\">x</span>"));

        Assert.True(cut.Instance.Visible);
        Assert.NotNull(cut.Find("[data-testid='slot']"));
    }

    [Fact]
    public void Invert_flips_the_visibility_rule()
    {
        // Default would hide (Md<=Md, Mode=Down). Invert=true should render.
        var cut = Render<OmniHidden>(p => p
            .Add(c => c.Breakpoint, Breakpoint.Md)
            .Add(c => c.Mode, HiddenMode.Down)
            .Add(c => c.Invert, true)
            .AddChildContent("<span data-testid=\"slot\">x</span>"));

        Assert.NotNull(cut.Find("[data-testid='slot']"));
    }
}
