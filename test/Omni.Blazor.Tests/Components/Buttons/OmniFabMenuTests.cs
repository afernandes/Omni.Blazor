namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniFabMenu"/>: position/direction
/// modifier classes, open/close state (incl. two-way IsOpen binding), the
/// backdrop's <c>omni-fab-backdrop-scoped</c> variant for <c>Static</c>
/// position, and Class/Style/Attributes splat onto the root.
/// </summary>
public class OmniFabMenuTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_menubar_class_and_aria_group()
    {
        var cut = Render<OmniFabMenu>();

        var root = cut.Find("div.omni-fab-menu");
        Assert.Contains("omni-fab-menu", root.ClassName);
        Assert.Equal("group", root.GetAttribute("role"));
    }

    [Theory]
    [InlineData(FabPosition.BottomRight,  "omni-fab-bottom-right")]
    [InlineData(FabPosition.BottomLeft,   "omni-fab-bottom-left")]
    [InlineData(FabPosition.TopRight,     "omni-fab-top-right")]
    [InlineData(FabPosition.TopLeft,      "omni-fab-top-left")]
    [InlineData(FabPosition.BottomCenter, "omni-fab-bottom-center")]
    [InlineData(FabPosition.Static,       "omni-fab-static")]
    public void Applies_position_modifier(FabPosition pos, string expectedClass)
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.Position, pos));

        Assert.Contains(expectedClass, cut.Find("div.omni-fab-menu").ClassName);
    }

    [Theory]
    [InlineData(FabMenuDirection.Up,    "omni-fab-dir-up")]
    [InlineData(FabMenuDirection.Down,  "omni-fab-dir-down")]
    [InlineData(FabMenuDirection.Left,  "omni-fab-dir-left")]
    [InlineData(FabMenuDirection.Right, "omni-fab-dir-right")]
    public void Applies_direction_modifier(FabMenuDirection dir, string expectedClass)
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.Direction, dir));

        Assert.Contains(expectedClass, cut.Find("div.omni-fab-menu").ClassName);
    }

    [Theory]
    [InlineData(FabMenuAnimation.Stagger, "omni-fab-anim-stagger")]
    [InlineData(FabMenuAnimation.Linear,  "omni-fab-anim-linear")]
    [InlineData(FabMenuAnimation.None,    "omni-fab-anim-none")]
    public void Applies_animation_modifier(FabMenuAnimation anim, string expectedClass)
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.Animation, anim));

        Assert.Contains(expectedClass, cut.Find("div.omni-fab-menu").ClassName);
    }

    [Fact]
    public void IsOpen_initial_true_adds_open_class_and_renders_items()
    {
        var cut = Render<OmniFabMenu>(p => p
            .Add(c => c.IsOpen, true)
            .AddChildContent("<span class=\"probe\">probe</span>"));

        Assert.Contains("omni-fab-open", cut.Find("div.omni-fab-menu").ClassName);
        Assert.NotNull(cut.Find(".omni-fab-items"));
    }

    [Fact]
    public void IsOpen_initial_false_hides_items_container()
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.IsOpen, false));

        Assert.Empty(cut.FindAll(".omni-fab-items"));
        Assert.DoesNotContain("omni-fab-open", cut.Find("div.omni-fab-menu").ClassName);
    }

    [Fact]
    public void Backdrop_only_renders_when_ShowBackdrop_and_open()
    {
        // No backdrop closed.
        var closed = Render<OmniFabMenu>(p => p
            .Add(c => c.ShowBackdrop, true)
            .Add(c => c.IsOpen, false));
        Assert.Empty(closed.FindAll(".omni-fab-backdrop"));

        // Backdrop visible when open.
        var open = Render<OmniFabMenu>(p => p
            .Add(c => c.ShowBackdrop, true)
            .Add(c => c.IsOpen, true));
        Assert.NotNull(open.Find(".omni-fab-backdrop"));
    }

    [Fact]
    public void Backdrop_uses_scoped_modifier_when_Static_position()
    {
        var cut = Render<OmniFabMenu>(p => p
            .Add(c => c.ShowBackdrop, true)
            .Add(c => c.IsOpen, true)
            .Add(c => c.Position, FabPosition.Static));

        Assert.Contains("omni-fab-backdrop-scoped", cut.Find(".omni-fab-backdrop").ClassName);
    }

    [Fact]
    public void Backdrop_not_scoped_for_fixed_positions()
    {
        var cut = Render<OmniFabMenu>(p => p
            .Add(c => c.ShowBackdrop, true)
            .Add(c => c.IsOpen, true)
            .Add(c => c.Position, FabPosition.BottomRight));

        Assert.DoesNotContain("omni-fab-backdrop-scoped", cut.Find(".omni-fab-backdrop").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.Class, "custom-fm"));

        Assert.Contains("custom-fm", cut.Find("div.omni-fab-menu").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.Style, "z-index: 50"));

        Assert.Equal("z-index: 50", cut.Find("div.omni-fab-menu").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFabMenu>(p => p
            .AddUnmatched("data-testid", "fm")
            .AddUnmatched("id", "main-fm"));

        var root = cut.Find("div.omni-fab-menu");
        Assert.Equal("fm", root.GetAttribute("data-testid"));
        Assert.Equal("main-fm", root.GetAttribute("id"));
    }

    [Fact]
    public void AriaLabel_is_forwarded_to_root()
    {
        var cut = Render<OmniFabMenu>(p => p.Add(c => c.AriaLabel, "Speed dial"));

        Assert.Equal("Speed dial", cut.Find("div.omni-fab-menu").GetAttribute("aria-label"));
    }
}
