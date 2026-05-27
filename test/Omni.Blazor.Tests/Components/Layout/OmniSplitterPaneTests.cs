using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniSplitterPane"/>: registers itself
/// with a parent <c>OmniSplitter</c> and exposes its ChildContent through the
/// parent's render loop. Standalone (no parent) the component renders nothing.
/// </summary>
public class OmniSplitterPaneTests : TestContextBase
{
    [Fact]
    public void Standalone_pane_renders_no_markup()
    {
        // Without a parent splitter the pane has nothing to render — verifies
        // that the component is purely a registration carrier.
        var cut = RenderComponent<OmniSplitterPane>(p => p.AddChildContent("body"));
        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Pane_with_parent_renders_its_child_content()
    {
        var cut = RenderComponent<OmniSplitter>(p => p.AddChildContent(builder =>
        {
            builder.OpenComponent<OmniSplitterPane>(0);
            builder.AddAttribute(1, "Size", "100%");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenElement(0, "span");
                b.AddAttribute(1, "data-testid", "pane");
                b.AddContent(2, "content");
                b.CloseElement();
            }));
            builder.CloseComponent();
        }));

        var pane = cut.Find("[data-testid='pane']");
        Assert.Equal("content", pane.TextContent);
    }

    [Fact]
    public void Default_Resizable_true_Collapsible_false()
    {
        var pane = new OmniSplitterPane();
        Assert.True(pane.Resizable);
        Assert.False(pane.Collapsible);
        Assert.False(pane.Collapsed);
    }
}
