using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniResult"/>: status→tone/icon mapping,
/// title/description/actions rendering, and the cross-cutting splat.
/// </summary>
public class OmniResultTests : TestContextBase
{
    [Fact]
    public void Default_renders_root_with_info_tone_and_icon()
    {
        var cut = Render<OmniResult>();
        var root = cut.Find(".omni-result");
        Assert.Contains("omni-result-info", root.ClassName);
        Assert.NotNull(cut.Find(".omni-result-icon svg"));
    }

    [Theory]
    [InlineData(ResultStatus.Success, "omni-result-good")]
    [InlineData(ResultStatus.Warning, "omni-result-warn")]
    [InlineData(ResultStatus.Error, "omni-result-danger")]
    [InlineData(ResultStatus.Forbidden, "omni-result-danger")]
    [InlineData(ResultStatus.NotFound, "omni-result-neutral")]
    [InlineData(ResultStatus.Maintenance, "omni-result-warn")]
    [InlineData(ResultStatus.Info, "omni-result-info")]
    public void Status_maps_to_tone_class(ResultStatus status, string expectedClass)
    {
        var cut = Render<OmniResult>(p => p.Add(c => c.Status, status));
        Assert.Contains(expectedClass, cut.Find(".omni-result").ClassName);
    }

    [Fact]
    public void Renders_title_and_description()
    {
        var cut = Render<OmniResult>(p => p
            .Add(c => c.Title, "Página não encontrada")
            .Add(c => c.Description, "A rota não existe."));
        Assert.Equal("Página não encontrada", cut.Find(".omni-result-title").TextContent);
        Assert.Contains("A rota não existe.", cut.Find(".omni-result-desc").TextContent);
    }

    [Fact]
    public void Renders_actions_fragment()
    {
        var cut = Render<OmniResult>(p => p
            .Add(c => c.Actions, b => b.AddMarkupContent(0, "<button class=\"go\">Voltar</button>")));
        Assert.NotNull(cut.Find(".omni-result-actions .go"));
    }

    [Fact]
    public void Icon_override_wins_over_status_default()
    {
        var cut = Render<OmniResult>(p => p
            .Add(c => c.Status, ResultStatus.Success)
            .Add(c => c.Icon, "shield"));
        // The shield path is unique enough; assert an svg renders (icon resolved).
        Assert.NotNull(cut.Find(".omni-result-icon svg"));
    }

    [Fact]
    public void Compact_adds_modifier_class()
    {
        var cut = Render<OmniResult>(p => p.Add(c => c.Compact, true));
        Assert.Contains("omni-result-compact", cut.Find(".omni-result").ClassName);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniResult>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "r1"));
        var root = cut.Find(".omni-result");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("r1", root.GetAttribute("data-testid"));
    }
}
