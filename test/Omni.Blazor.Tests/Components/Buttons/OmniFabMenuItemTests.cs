using Microsoft.AspNetCore.Components.Web;

namespace Omni.Blazor.Tests.Components.Buttons;

/// <summary>
/// Behavioural contract for <see cref="OmniFabMenuItem"/>: required Icon, the
/// label-position modifiers, label visibility, click forwarding, and the
/// Class/Style/Attributes splat onto the root.
/// </summary>
public class OmniFabMenuItemTests : TestContextBase
{
    [Fact]
    public void Renders_default_root_with_menuitem_role_and_auto_label_class()
    {
        var cut = Render<OmniFabMenuItem>(p => p.Add(c => c.Icon, "plus"));

        var root = cut.Find("div.omni-fab-item");
        Assert.Equal("menuitem", root.GetAttribute("role"));
        Assert.Contains("omni-fab-item-label-auto", root.ClassName);
    }

    [Theory]
    [InlineData(FabMenuItemLabelPosition.Auto,  "omni-fab-item-label-auto")]
    [InlineData(FabMenuItemLabelPosition.Left,  "omni-fab-item-label-left")]
    [InlineData(FabMenuItemLabelPosition.Right, "omni-fab-item-label-right")]
    [InlineData(FabMenuItemLabelPosition.None,  "omni-fab-item-no-label")]
    public void Applies_label_position_modifier(FabMenuItemLabelPosition lp, string expectedClass)
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.LabelPosition, lp)
            .Add(c => c.Icon, "plus"));

        Assert.Contains(expectedClass, cut.Find("div.omni-fab-item").ClassName);
    }

    [Fact]
    public void Label_renders_when_set_and_position_is_not_None()
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .Add(c => c.Label, "Folder"));

        var label = cut.Find(".omni-fab-item-label");
        Assert.Equal("Folder", label.TextContent);
    }

    [Fact]
    public void Label_hidden_when_LabelPosition_is_None()
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .Add(c => c.Label, "Folder")
            .Add(c => c.LabelPosition, FabMenuItemLabelPosition.None));

        Assert.Empty(cut.FindAll(".omni-fab-item-label"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .Add(c => c.Class, "custom-fi"));

        Assert.Contains("custom-fi", cut.Find("div.omni-fab-item").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .Add(c => c.Style, "opacity: 0.5"));

        Assert.Equal("opacity: 0.5", cut.Find("div.omni-fab-item").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .AddUnmatched("data-testid", "fi")
            .AddUnmatched("data-action", "create"));

        var root = cut.Find("div.omni-fab-item");
        Assert.Equal("fi", root.GetAttribute("data-testid"));
        Assert.Equal("create", root.GetAttribute("data-action"));
    }

    [Fact]
    public void OnClick_fires_when_inner_button_clicked()
    {
        var clicks = 0;
        var cut = Render<OmniFabMenuItem>(p => p
            .Add(c => c.Icon, "plus")
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find("button").Click();
        Assert.Equal(1, clicks);
    }
}
