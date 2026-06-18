using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniFormField"/>: label + hint + error
/// composition and the cross-cutting splat.
/// </summary>
public class OmniFormFieldTests : TestContextBase
{
    [Fact]
    public void Renders_field_wrapper_with_base_class()
    {
        var cut = RenderComponent<OmniFormField>(p => p.AddChildContent("body"));
        Assert.NotNull(cut.Find("div.omni-field"));
    }

    [Fact]
    public void Renders_label_when_provided()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .AddChildContent("x"));

        Assert.Contains("Email", cut.Find("label.omni-field-label").TextContent);
    }

    [Fact]
    public void Required_label_adds_required_modifier()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .Add(c => c.Required, true)
            .AddChildContent("x"));

        Assert.Contains("omni-field-required", cut.Find("label.omni-field-label").ClassName);
    }

    [Fact]
    public void Renders_explicit_error_and_marks_field_invalid()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Error, "Boom!")
            .AddChildContent("x"));

        Assert.Contains("omni-field-invalid", cut.Find("div.omni-field").ClassName);
        Assert.Contains("Boom!", cut.Find("span.omni-field-error").TextContent);
    }

    [Fact]
    public void Hint_shows_when_no_error()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .AddChildContent("x"));

        Assert.Contains("Helpful tip", cut.Find("span.omni-field-hint").TextContent);
    }

    [Fact]
    public void Hint_has_stable_id_for_aria_describedby()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .AddChildContent("x"));

        var hint = cut.Find("span.omni-field-hint");
        var id = hint.GetAttribute("id");

        Assert.False(string.IsNullOrEmpty(id));
        // Id is derived from the component Id so a consumer input can target it.
        Assert.Equal(cut.Instance.HintId, id);
        Assert.EndsWith("-hint", id);
    }

    [Fact]
    public void Hint_id_is_stable_across_rerenders()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .AddChildContent("x"));

        var first = cut.Find("span.omni-field-hint").GetAttribute("id");

        cut.SetParametersAndRender(p => p
            .Add(c => c.Hint, "Helpful tip")
            .Add(c => c.Class, "newcls"));

        var second = cut.Find("span.omni-field-hint").GetAttribute("id");
        Assert.Equal(first, second);
    }

    [Fact]
    public void Error_hides_hint()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .Add(c => c.Error, "Boom!")
            .AddChildContent("x"));

        Assert.Empty(cut.FindAll("span.omni-field-hint"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("x"));

        Assert.Contains("custom-cls", cut.Find("div.omni-field").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Style, "margin: 4px")
            .AddChildContent("x"));

        Assert.Equal("margin: 4px", cut.Find("div.omni-field").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .AddUnmatched("data-testid", "ff")
            .AddChildContent("x"));

        Assert.Equal("ff", cut.Find("div.omni-field").GetAttribute("data-testid"));
    }

    // ── ParameterState: recompute fires only on tracked params ──

    private sealed class Model
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    [Fact]
    public void Initial_recompute_fires_on_first_render()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .AddChildContent("x"));

        // Two ParameterStates (ValidationFor + EditContext) — initial detect fires both.
        Assert.Equal(2, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.Label, "L")
            .AddChildContent("x"));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p
            .Add(c => c.Label, "Other")
            .Add(c => c.Hint, "tip")
            .Add(c => c.HintRight, "right")
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color:red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
        // DOM: label text reflects the new value.
        Assert.Contains("Other", cut.Find("label.omni-field-label").TextContent);
    }

    [Fact]
    public void Recompute_fires_when_ValidationFor_changes()
    {
        var model = new Model();
        System.Linq.Expressions.Expression<Func<object?>> first  = () => model.A;
        System.Linq.Expressions.Expression<Func<object?>> second = () => model.B;

        var cut = RenderComponent<OmniFormField>(p => p
            .Add(c => c.ValidationFor, first)
            .AddChildContent("x"));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p.Add(c => c.ValidationFor, second));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
