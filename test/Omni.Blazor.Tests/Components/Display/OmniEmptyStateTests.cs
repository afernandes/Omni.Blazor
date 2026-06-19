using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniEmptyState"/>: default icon, text,
/// actions, compact modifier, and the cross-cutting splat.
/// </summary>
public class OmniEmptyStateTests : TestContextBase
{
    [Fact]
    public void Default_renders_root_and_icon()
    {
        var cut = Render<OmniEmptyState>();
        Assert.NotNull(cut.Find(".omni-empty"));
        Assert.NotNull(cut.Find(".omni-empty-icon svg"));
    }

    [Fact]
    public void Renders_title_and_description()
    {
        var cut = Render<OmniEmptyState>(p => p
            .Add(c => c.Title, "Nenhum pedido")
            .Add(c => c.Description, "Aparecem aqui."));
        Assert.Equal("Nenhum pedido", cut.Find(".omni-empty-title").TextContent);
        Assert.Contains("Aparecem aqui.", cut.Find(".omni-empty-desc").TextContent);
    }

    [Fact]
    public void Renders_actions_fragment()
    {
        var cut = Render<OmniEmptyState>(p => p
            .Add(c => c.Actions, b => b.AddMarkupContent(0, "<button class=\"new\">Novo</button>")));
        Assert.NotNull(cut.Find(".omni-empty-actions .new"));
    }

    [Fact]
    public void Compact_adds_modifier_class()
    {
        var cut = Render<OmniEmptyState>(p => p.Add(c => c.Compact, true));
        Assert.Contains("omni-empty-compact", cut.Find(".omni-empty").ClassName);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniEmptyState>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "e1"));
        var root = cut.Find(".omni-empty");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("e1", root.GetAttribute("data-testid"));
    }
}
