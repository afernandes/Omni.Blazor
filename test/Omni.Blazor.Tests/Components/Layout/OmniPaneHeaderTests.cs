using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniPaneHeader"/>: renders title +
/// subtitle + optional breadcrumb, actions, and subtabs slots.
/// </summary>
public class OmniPaneHeaderTests : TestContextBase
{
    [Fact]
    public void Renders_default_pane_head_root()
    {
        var cut = Render<OmniPaneHeader>();

        var root = cut.Find(".omni-pane-head");
        Assert.Equal("DIV", root.TagName);
    }

    [Fact]
    public void Renders_Title_as_h1_with_view_title_class()
    {
        var cut = Render<OmniPaneHeader>(p => p.Add(c => c.Title, "Customers"));

        var h1 = cut.Find("h1.omni-view-title");
        Assert.Equal("Customers", h1.TextContent);
    }

    [Fact]
    public void Renders_Subtitle_below_title()
    {
        var cut = Render<OmniPaneHeader>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Subtitle, "Sub"));

        var sub = cut.Find("p.omni-view-sub");
        Assert.Equal("Sub", sub.TextContent);
    }

    [Fact]
    public void Actions_slot_renders_in_two_column_row()
    {
        var cut = Render<OmniPaneHeader>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Actions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "data-testid", "act");
                builder.AddContent(2, "Add");
                builder.CloseElement();
            }));

        Assert.NotNull(cut.Find(".omni-pane-head-row"));
        Assert.NotNull(cut.Find("[data-testid='act']"));
    }

    [Fact]
    public void Breadcrumb_renders_above_title()
    {
        var cut = Render<OmniPaneHeader>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Breadcrumb, builder =>
            {
                builder.OpenElement(0, "nav");
                builder.AddAttribute(1, "data-testid", "crumb");
                builder.CloseElement();
            }));

        Assert.NotNull(cut.Find(".omni-pane-head-crumb"));
        Assert.NotNull(cut.Find("[data-testid='crumb']"));
    }

    [Fact]
    public void Subtabs_renders_below_actions()
    {
        var cut = Render<OmniPaneHeader>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Subtabs, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "sub");
                builder.CloseElement();
            }));

        Assert.NotNull(cut.Find(".omni-pane-head-subtabs"));
        Assert.NotNull(cut.Find("[data-testid='sub']"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniPaneHeader>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find(".omni-pane-head").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPaneHeader>(p => p.Add(c => c.Style, "margin: 0"));

        Assert.Equal("margin: 0", cut.Find(".omni-pane-head").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPaneHeader>(p => p
            .AddUnmatched("data-testid", "ph")
            .AddUnmatched("aria-label", "Header"));

        var root = cut.Find(".omni-pane-head");
        Assert.Equal("ph", root.GetAttribute("data-testid"));
        Assert.Equal("Header", root.GetAttribute("aria-label"));
    }
}
