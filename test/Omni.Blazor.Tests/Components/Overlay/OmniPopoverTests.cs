namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniPopover"/>: a wrapper span that
/// always renders the Trigger and conditionally renders the popover panel
/// when <c>Open</c>. Position/ShowArrow/AlignEnd map to modifier classes.
/// </summary>
public class OmniPopoverTests : TestContextBase
{
    private static RenderFragment Probe(string label = "trigger") => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "probe-trigger");
        builder.AddContent(2, label);
        builder.CloseElement();
    };

    [Fact]
    public void Renders_root_wrapper_with_default_class()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe()));

        var root = cut.Find(".omni-popover-wrap");
        Assert.Contains("omni-popover-wrap", root.ClassName);
        // Default Open=false → no omni-popover-open modifier.
        Assert.DoesNotContain("omni-popover-open", root.ClassName);
    }

    [Fact]
    public void Renders_Trigger_slot()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe("hello")));

        Assert.NotNull(cut.Find(".omni-popover-trigger .probe-trigger"));
        Assert.Contains("hello", cut.Find(".omni-popover-trigger").TextContent);
    }

    [Fact]
    public void Open_false_default_hides_popover_panel()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe()));

        Assert.Empty(cut.FindAll(".omni-popover"));
    }

    [Fact]
    public void Click_on_trigger_opens_popover_and_renders_ChildContent()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .AddChildContent("<span class=\"probe-content\">content</span>"));

        cut.Find(".omni-popover-trigger").Click();

        Assert.NotNull(cut.Find(".omni-popover"));
        Assert.NotNull(cut.Find(".omni-popover .probe-content"));
        Assert.Contains("omni-popover-open", cut.Find(".omni-popover-wrap").ClassName);
    }

    [Theory]
    [InlineData(PopoverPosition.Top,    "omni-popover-top")]
    [InlineData(PopoverPosition.Bottom, "omni-popover-bottom")]
    [InlineData(PopoverPosition.Left,   "omni-popover-left")]
    [InlineData(PopoverPosition.Right,  "omni-popover-right")]
    public void Applies_position_modifier_when_open(PopoverPosition pos, string expected)
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .Add(c => c.Position, pos)
            .AddChildContent("x"));

        cut.Find(".omni-popover-trigger").Click();

        Assert.Contains(expected, cut.Find(".omni-popover").ClassName);
    }

    [Fact]
    public void ShowArrow_false_adds_no_arrow_modifier()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .Add(c => c.ShowArrow, false)
            .AddChildContent("x"));

        cut.Find(".omni-popover-trigger").Click();

        Assert.Contains("omni-popover-no-arrow", cut.Find(".omni-popover").ClassName);
    }

    [Fact]
    public void AlignEnd_true_adds_align_end_modifier()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .Add(c => c.AlignEnd, true)
            .AddChildContent("x"));

        cut.Find(".omni-popover-trigger").Click();

        Assert.Contains("omni-popover-align-end", cut.Find(".omni-popover").ClassName);
    }

    [Fact]
    public void Open_panel_is_an_aria_modal_dialog()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .AddChildContent("x"));

        cut.Find(".omni-popover-trigger").Click();

        var panel = cut.Find(".omni-popover");
        Assert.Equal("dialog", panel.GetAttribute("role"));
        Assert.Equal("true", panel.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .Add(c => c.Class, "custom-pop"));

        Assert.Contains("custom-pop", cut.Find(".omni-popover-wrap").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .Add(c => c.Style, "display: inline-block"));

        Assert.Equal("display: inline-block", cut.Find(".omni-popover-wrap").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPopover>(p => p
            .Add(c => c.Trigger, Probe())
            .AddUnmatched("data-testid", "pop")
            .AddUnmatched("aria-label", "More"));

        var root = cut.Find(".omni-popover-wrap");
        Assert.Equal("pop", root.GetAttribute("data-testid"));
        Assert.Equal("More", root.GetAttribute("aria-label"));
    }
}
