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
        var cut = Render<OmniFormField>(p => p.AddChildContent("body"));
        Assert.NotNull(cut.Find("div.omni-field"));
    }

    [Fact]
    public void Renders_label_when_provided()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .AddChildContent("x"));

        Assert.Contains("Email", cut.Find("label.omni-field-label").TextContent);
    }

    [Fact]
    public void Required_label_adds_required_modifier()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .Add(c => c.Required, true)
            .AddChildContent("x"));

        Assert.Contains("omni-field-required", cut.Find("label.omni-field-label").ClassName);
    }

    [Fact]
    public void Renders_explicit_error_and_marks_field_invalid()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Error, "Boom!")
            .AddChildContent("x"));

        Assert.Contains("omni-field-invalid", cut.Find("div.omni-field").ClassName);
        Assert.Contains("Boom!", cut.Find("span.omni-field-error").TextContent);
    }

    [Fact]
    public void Hint_shows_when_no_error()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .AddChildContent("x"));

        Assert.Contains("Helpful tip", cut.Find("span.omni-field-hint").TextContent);
    }

    [Fact]
    public void Hint_has_stable_id_for_aria_describedby()
    {
        var cut = Render<OmniFormField>(p => p
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
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .AddChildContent("x"));

        var first = cut.Find("span.omni-field-hint").GetAttribute("id");

        cut.Render(p => p
            .Add(c => c.Hint, "Helpful tip")
            .Add(c => c.Class, "newcls"));

        var second = cut.Find("span.omni-field-hint").GetAttribute("id");
        Assert.Equal(first, second);
    }

    [Fact]
    public void Error_hides_hint()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Hint, "Helpful tip")
            .Add(c => c.Error, "Boom!")
            .AddChildContent("x"));

        Assert.Empty(cut.FindAll("span.omni-field-hint"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("x"));

        Assert.Contains("custom-cls", cut.Find("div.omni-field").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Style, "margin: 4px")
            .AddChildContent("x"));

        Assert.Equal("margin: 4px", cut.Find("div.omni-field").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFormField>(p => p
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
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Label, "Email")
            .AddChildContent("x"));

        // Two ParameterStates (ValidationFor + EditContext) — initial detect fires both.
        Assert.Equal(2, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Label, "L")
            .AddChildContent("x"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
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
        System.Linq.Expressions.Expression<Func<object?>> first = () => model.A;
        System.Linq.Expressions.Expression<Func<object?>> second = () => model.B;

        var cut = Render<OmniFormField>(p => p
            .Add(c => c.ValidationFor, first)
            .AddChildContent("x"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.ValidationFor, second));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }

    // ── Reserved message line: keeps a validated field's height constant, so it
    //    neither shifts the page nor drops a side-by-side neighbour out of line
    //    when its message appears. ──

    [Fact]
    public void Validated_field_reserves_the_message_line_while_it_has_none()
    {
        var model = new Model();
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.ValidationFor, () => model.A)
            .AddChildContent("<input />"));

        var reserve = cut.Find(".omni-field-message-reserve");
        // Layout only — assistive tech must not announce an empty line.
        Assert.Equal("true", reserve.GetAttribute("aria-hidden"));
        Assert.Equal("", reserve.TextContent);
    }

    [Fact]
    public void Field_without_validation_reserves_nothing()
    {
        // A field that can never show a message never changes height, so adding
        // space there would be pure padding.
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.Label, "Plain")
            .AddChildContent("<input />"));

        Assert.Empty(cut.FindAll(".omni-field-message-reserve"));
    }

    [Fact]
    public void Reserved_line_gives_way_to_the_real_message()
    {
        var model = new Model();
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.ValidationFor, () => model.A)
            .Add(c => c.Error, "Obrigatório")
            .AddChildContent("<input />"));

        Assert.Empty(cut.FindAll(".omni-field-message-reserve"));
        Assert.Equal("Obrigatório", cut.Find(".omni-field-error").TextContent);
    }

    [Fact]
    public void Reserved_line_gives_way_to_the_hint()
    {
        var model = new Model();
        var cut = Render<OmniFormField>(p => p
            .Add(c => c.ValidationFor, () => model.A)
            .Add(c => c.Hint, "Mínimo 8 caracteres")
            .AddChildContent("<input />"));

        Assert.Empty(cut.FindAll(".omni-field-message-reserve"));
        Assert.Equal("Mínimo 8 caracteres", cut.Find(".omni-field-hint").TextContent);
    }

    [Fact]
    public void Message_line_metrics_are_identical_in_all_three_states()
    {
        // The whole point: hint, error and the reserved blank must measure the
        // same, or the field still changes height as the message switches kind.
        // Only the shipped bundle can prove that — bUnit does not lay anything out.
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Omni.Blazor", "wwwroot", "css", "omni.css"));

        const string selector =
            ".omni-field > .omni-field-hint,\n.omni-field > .omni-field-error,\n.omni-field > .omni-field-message-reserve";
        int start = css.Replace("\r\n", "\n", StringComparison.Ordinal)
            .IndexOf(selector, StringComparison.Ordinal);
        Assert.True(start >= 0, "The shipped CSS does not size the three message states together.");

        // Scoped to direct children so HintRight, which sits in .omni-field-row
        // beside the label, keeps its own metrics.
        Assert.DoesNotContain(".omni-field-row > .omni-field-hint {", css, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
