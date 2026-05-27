using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Navigation;

public class OmniStepperTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_omni_stepper_class()
    {
        var cut = RenderComponent<OmniStepper>();
        Assert.NotNull(cut.Find(".omni-stepper"));
        Assert.NotNull(cut.Find(".omni-stepper-head"));
        Assert.NotNull(cut.Find(".omni-stepper-body"));
        Assert.NotNull(cut.Find(".omni-stepper-foot"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniStepper>(p => p.Add(c => c.Class, "my-stepper"));
        Assert.Contains("my-stepper", cut.Find(".omni-stepper").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniStepper>(p => p.Add(c => c.Style, "margin: 8px"));
        Assert.Equal("margin: 8px", cut.Find(".omni-stepper").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniStepper>(p => p
            .AddUnmatched("data-testid", "stepper"));
        Assert.Equal("stepper", cut.Find(".omni-stepper").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_step_node_per_registered_step()
    {
        var cut = RenderComponent<OmniStepper>(p => p
            .AddChildContent<OmniStep>(s => s.Add(c => c.Title, "Account"))
            .AddChildContent<OmniStep>(s => s.Add(c => c.Title, "Details"))
            .AddChildContent<OmniStep>(s => s.Add(c => c.Title, "Confirm")));

        var nodes = cut.FindAll(".omni-stepper-node");
        Assert.True(nodes.Count >= 3);
    }

    [Fact]
    public void Default_button_texts_render_in_footer()
    {
        var cut = RenderComponent<OmniStepper>(p => p
            .AddChildContent<OmniStep>(s => s.Add(c => c.Title, "A"))
            .AddChildContent<OmniStep>(s => s.Add(c => c.Title, "B")));

        var footText = cut.Find(".omni-stepper-foot").TextContent;
        Assert.Contains("Voltar", footText);
        Assert.Contains("Próximo", footText);
    }
}
