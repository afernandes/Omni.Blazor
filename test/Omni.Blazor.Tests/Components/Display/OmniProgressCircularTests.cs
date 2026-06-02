using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniProgressCircular"/>: the SVG ring,
/// normalized dash offset (with clamping), the centred value/label, aria
/// progressbar attributes, indeterminate mode, size/variant/colour, custom
/// centre content, and the cross-cutting splat.
/// </summary>
public class OmniProgressCircularTests : TestContextBase
{
    private IRenderedComponent<OmniProgressCircular> Render(
        Action<ComponentParameterCollectionBuilder<OmniProgressCircular>>? extra = null)
        => RenderComponent<OmniProgressCircular>(p => extra?.Invoke(p));

    [Fact]
    public void Renders_progressbar_with_two_circles()
    {
        var cut = Render(p => p.Add(c => c.Value, 50));
        var root = cut.Find(".omni-progress-circular");
        Assert.Equal("progressbar", root.GetAttribute("role"));
        Assert.NotNull(cut.Find(".omni-progress-circular-track"));
        Assert.NotNull(cut.Find(".omni-progress-circular-value"));
    }

    [Fact]
    public void Aria_attributes_reflect_min_max_value()
    {
        var root = Render(p => p.Add(c => c.Value, 65).Add(c => c.Max, 200)).Find(".omni-progress-circular");
        Assert.Equal("0", root.GetAttribute("aria-valuemin"));
        Assert.Equal("200", root.GetAttribute("aria-valuemax"));
        Assert.Equal("65", root.GetAttribute("aria-valuenow"));
    }

    [Theory]
    [InlineData(25, 0, 100, "75")]   // 25% filled → 75 empty
    [InlineData(0, 0, 100, "100")]   // empty
    [InlineData(100, 0, 100, "0")]   // full
    [InlineData(50, 0, 200, "75")]   // scaled by Max
    public void Dash_offset_is_normalized_empty_percentage(double value, double min, double max, string expected)
    {
        var circle = Render(p => p.Add(c => c.Value, value).Add(c => c.Min, min).Add(c => c.Max, max))
            .Find(".omni-progress-circular-value");
        Assert.Equal(expected, circle.GetAttribute("stroke-dashoffset"));
    }

    [Theory]
    [InlineData(150, "0")]    // above Max clamps to full
    [InlineData(-20, "100")]  // below Min clamps to empty
    public void Dash_offset_clamps_out_of_range(double value, string expected)
    {
        var circle = Render(p => p.Add(c => c.Value, value)).Find(".omni-progress-circular-value");
        Assert.Equal(expected, circle.GetAttribute("stroke-dashoffset"));
    }

    [Fact]
    public void Shows_value_with_unit_by_default()
    {
        var cut = Render(p => p.Add(c => c.Value, 42));
        Assert.Equal("42%", cut.Find(".omni-progress-circular-label").TextContent.Trim());
    }

    [Fact]
    public void Unit_is_customisable_and_value_is_raw_not_percentage()
    {
        var cut = Render(p => p.Add(c => c.Value, 750).Add(c => c.Max, 1000).Add(c => c.Unit, " vendas"));
        Assert.Equal("750 vendas", cut.Find(".omni-progress-circular-label").TextContent.Trim());
        // dash still reflects the normalized fraction (750/1000 → 25 empty)
        Assert.Equal("25", cut.Find(".omni-progress-circular-value").GetAttribute("stroke-dashoffset"));
    }

    [Fact]
    public void ShowValue_false_hides_label()
    {
        Assert.Empty(Render(p => p.Add(c => c.Value, 30).Add(c => c.ShowValue, false))
            .FindAll(".omni-progress-circular-label"));
    }

    [Fact]
    public void Indeterminate_adds_class_and_omits_value_now_and_offset()
    {
        var cut = Render(p => p.Add(c => c.Indeterminate, true));
        var root = cut.Find(".omni-progress-circular");
        Assert.Contains("omni-progress-circular-indeterminate", root.ClassName);
        Assert.Null(root.GetAttribute("aria-valuenow"));
        Assert.Null(cut.Find(".omni-progress-circular-value").GetAttribute("stroke-dashoffset"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-progress-circular-sm")]
    [InlineData(ComponentSize.Md, "omni-progress-circular-md")]
    [InlineData(ComponentSize.Lg, "omni-progress-circular-lg")]
    [InlineData(ComponentSize.Xl, "omni-progress-circular-xl")]
    public void Size_maps_to_class(ComponentSize size, string expected)
    {
        Assert.Contains(expected, Render(p => p.Add(c => c.Size, size)).Find(".omni-progress-circular").ClassName);
    }

    [Theory]
    [InlineData(BadgeVariant.Good, "omni-progress-circular-good")]
    [InlineData(BadgeVariant.Warn, "omni-progress-circular-warn")]
    [InlineData(BadgeVariant.Danger, "omni-progress-circular-danger")]
    [InlineData(BadgeVariant.Info, "omni-progress-circular-info")]
    public void Variant_maps_to_class(BadgeVariant variant, string expected)
    {
        Assert.Contains(expected, Render(p => p.Add(c => c.Variant, variant)).Find(".omni-progress-circular").ClassName);
    }

    [Fact]
    public void Accent_variant_adds_no_variant_class()
    {
        var cn = Render(p => p.Add(c => c.Variant, BadgeVariant.Accent)).Find(".omni-progress-circular").ClassName;
        Assert.DoesNotContain("omni-progress-circular-good", cn);
        Assert.DoesNotContain("omni-progress-circular-accent", cn);
    }

    [Fact]
    public void Color_overrides_stroke_on_value_circle()
    {
        var circle = Render(p => p.Add(c => c.Value, 50).Add(c => c.Color, "red")).Find(".omni-progress-circular-value");
        Assert.Contains("stroke:red", circle.GetAttribute("style") ?? "");
    }

    [Fact]
    public void ChildContent_replaces_value_label()
    {
        var cut = Render(p => p.Add(c => c.Value, 50).AddChildContent("<b>custom</b>"));
        var label = cut.Find(".omni-progress-circular-label");
        Assert.Contains("custom", label.TextContent);
        Assert.DoesNotContain("50%", label.TextContent);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var root = Render(p => p
            .Add(c => c.Value, 10)
            .Add(c => c.Class, "ring")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "pc1")).Find(".omni-progress-circular");
        Assert.Contains("ring", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("pc1", root.GetAttribute("data-testid"));
    }
}
