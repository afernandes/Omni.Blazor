using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms;

/// <summary>
/// Behavioural contract for <see cref="OmniValidationSummary"/>: aggregates all
/// EditContext messages into a styled alert, re-renders on validation state
/// changes, and supports the cross-cutting splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniValidationSummaryTests : TestContextBase
{
    private sealed class Model
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    private static EditContext BuildContextWithErrors(Model model, params (string Field, string Message)[] errors)
    {
        var ctx = new EditContext(model);
        var store = new ValidationMessageStore(ctx);
        foreach (var (field, message) in errors)
        {
            store.Add(new FieldIdentifier(model, field), message);
        }
        ctx.NotifyValidationStateChanged();
        return ctx;
    }

    [Fact]
    public void Does_not_render_when_no_messages()
    {
        var model = new Model();
        var ctx = new EditContext(model);

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));

        Assert.Empty(cut.FindAll(".omni-alert"));
    }

    [Fact]
    public void Renders_alert_with_default_title_and_messages()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model,
            ("A", "A is required."),
            ("B", "B must be valid."));

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));

        var root = cut.Find(".omni-alert");
        Assert.Contains("omni-alert", root.ClassName);
        Assert.Contains("omni-alert-danger", root.ClassName);
        Assert.Equal("alert", root.GetAttribute("role"));
        Assert.Contains("Corrija os erros abaixo:", cut.Find(".omni-alert-title").TextContent);

        var items = cut.FindAll(".omni-validation-summary-list li");
        Assert.Equal(2, items.Count);
        Assert.Equal("A is required.", items[0].TextContent);
        Assert.Equal("B must be valid.", items[1].TextContent);
    }

    [Fact]
    public void Custom_Title_is_rendered()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "x"));

        var cut = RenderComponent<OmniValidationSummary>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.Title, "Please fix:"));

        Assert.Equal("Please fix:", cut.Find(".omni-alert-title").TextContent);
    }

    [Fact]
    public void Empty_Title_omits_title_element()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "x"));

        var cut = RenderComponent<OmniValidationSummary>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.Title, string.Empty));

        Assert.Empty(cut.FindAll(".omni-alert-title"));
    }

    [Fact]
    public void Deduplicates_repeated_messages()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model,
            ("A", "duplicate"),
            ("B", "duplicate"));

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));

        var items = cut.FindAll(".omni-validation-summary-list li");
        Assert.Single(items);
        Assert.Equal("duplicate", items[0].TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "x"));

        var cut = RenderComponent<OmniValidationSummary>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.Class, "custom-summary"));

        Assert.Contains("custom-summary", cut.Find(".omni-alert").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "x"));

        var cut = RenderComponent<OmniValidationSummary>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.Style, "margin: 8px"));

        Assert.Equal("margin: 8px", cut.Find(".omni-alert").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "x"));

        var cut = RenderComponent<OmniValidationSummary>(p => p
            .AddCascadingValue(ctx)
            .AddUnmatched("data-testid", "summary"));

        Assert.Equal("summary", cut.Find(".omni-alert").GetAttribute("data-testid"));
    }

    [Fact]
    public void Refreshes_when_validation_state_changes()
    {
        var model = new Model();
        var ctx = new EditContext(model);
        var store = new ValidationMessageStore(ctx);

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));
        Assert.Empty(cut.FindAll(".omni-alert"));

        store.Add(new FieldIdentifier(model, "A"), "boom");
        ctx.NotifyValidationStateChanged();

        Assert.NotNull(cut.Find(".omni-alert"));
        Assert.Equal("boom", cut.Find(".omni-validation-summary-list li").TextContent);
    }

    // ── ParameterState: subscription rebind fires only on EditContext changes ──

    [Fact]
    public void Initial_recompute_fires_on_first_render()
    {
        var model = new Model();
        var ctx = new EditContext(model);

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var model = new Model();
        var ctx = BuildContextWithErrors(model, ("A", "boom"));

        var cut = RenderComponent<OmniValidationSummary>(p => p.AddCascadingValue(ctx));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p
            .Add(c => c.Title, "Other title")
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color:red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
        // DOM: new title rendered.
        Assert.Equal("Other title", cut.Find(".omni-alert-title").TextContent);
    }

    [Fact]
    public void Recompute_fires_when_EditContext_cascading_value_changes()
    {
        // bUnit doesn't allow swapping the cascading value via
        // SetParametersAndRender, so wrap the component in a host with a
        // mutable EditContext field bound to a CascadingValue.
        var model1 = new Model();
        var ctx1 = BuildContextWithErrors(model1, ("A", "first"));

        var cut = RenderComponent<EditContextHost>(p => p
            .Add(h => h.Ctx, ctx1));

        var inner = cut.FindComponent<OmniValidationSummary>().Instance;
        var baseline = inner.RecomputeCount;

        var model2 = new Model();
        var ctx2 = BuildContextWithErrors(model2, ("A", "second"));
        cut.SetParametersAndRender(p => p.Add(h => h.Ctx, ctx2));

        Assert.Equal(baseline + 1, inner.RecomputeCount);
    }

    private sealed class EditContextHost : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.Parameter]
        public EditContext? Ctx { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<Microsoft.AspNetCore.Components.CascadingValue<EditContext?>>(0);
            builder.AddAttribute(1, "Value", Ctx);
            builder.AddAttribute(2, "ChildContent",
                (Microsoft.AspNetCore.Components.RenderFragment)(b2 =>
                {
                    b2.OpenComponent<OmniValidationSummary>(0);
                    b2.CloseComponent();
                }));
            builder.CloseComponent();
        }
    }
}
