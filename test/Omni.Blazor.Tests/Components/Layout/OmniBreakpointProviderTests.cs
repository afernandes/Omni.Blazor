using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniBreakpointProvider"/>: cascades the
/// current breakpoint to descendants. Default cascaded value starts at Md.
/// </summary>
public class OmniBreakpointProviderTests : TestContextBase
{
    private sealed class BreakpointConsumer : ComponentBase
    {
        [CascadingParameter(Name = "Breakpoint")] public Breakpoint Bp { get; set; }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "consumer");
            builder.AddAttribute(2, "data-bp", Bp.ToString());
            builder.CloseElement();
        }
    }

    [Fact]
    public void Renders_children_and_cascades_default_breakpoint()
    {
        var cut = Render<OmniBreakpointProvider>(p => p
            .AddChildContent<BreakpointConsumer>());

        var consumer = cut.Find("[data-testid='consumer']");
        // Default _current starts at Md.
        Assert.Equal("Md", consumer.GetAttribute("data-bp"));
    }

    [Fact]
    public void Renders_arbitrary_child_content()
    {
        var cut = Render<OmniBreakpointProvider>(p => p
            .AddChildContent("<span data-testid='raw'>hello</span>"));

        Assert.NotNull(cut.Find("[data-testid='raw']"));
    }
}
