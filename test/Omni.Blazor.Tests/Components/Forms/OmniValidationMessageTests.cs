using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms;

/// <summary>
/// Behavioural contract for <see cref="OmniValidationMessage{TValue}"/>: per-field
/// error display that subscribes to <see cref="EditContext.OnValidationStateChanged"/>
/// and re-renders only when its field has messages. Also exercises the
/// cross-cutting Class/Style/Attributes splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniValidationMessageTests : TestContextBase
{
    private sealed class Person
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be valid.")]
        public string? Email { get; set; }

        public string? Name { get; set; }
    }

    private static EditContext BuildContextWithErrors(Person model, params (string Field, string Message)[] errors)
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
        var model = new Person { Email = "a@b.com" };
        var ctx = new EditContext(model);

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        Assert.Empty(cut.FindAll(".omni-validation-message"));
    }

    [Fact]
    public void Renders_messages_for_target_field()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model,
            (nameof(Person.Email), "Email is required."),
            (nameof(Person.Email), "Email must be valid."));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        var root = cut.Find(".omni-validation-message");
        Assert.Equal("alert", root.GetAttribute("role"));
        var items = cut.FindAll(".omni-validation-message-item");
        Assert.Equal(2, items.Count);
        Assert.Equal("Email is required.", items[0].TextContent);
        Assert.Equal("Email must be valid.", items[1].TextContent);
    }

    [Fact]
    public void ShowAll_false_renders_only_first_message()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model,
            (nameof(Person.Email), "Email is required."),
            (nameof(Person.Email), "Email must be valid."));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email)
            .Add(c => c.ShowAll, false));

        var items = cut.FindAll(".omni-validation-message-item");
        Assert.Single(items);
        Assert.Equal("Email is required.", items[0].TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model, (nameof(Person.Email), "x"));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email)
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find(".omni-validation-message").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model, (nameof(Person.Email), "x"));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email)
            .Add(c => c.Style, "margin-top: 4px"));

        Assert.Equal("margin-top: 4px", cut.Find(".omni-validation-message").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model, (nameof(Person.Email), "x"));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email)
            .AddUnmatched("data-testid", "email-error"));

        Assert.Equal("email-error", cut.Find(".omni-validation-message").GetAttribute("data-testid"));
    }

    [Fact]
    public void Refreshes_when_validation_state_changes()
    {
        var model = new Person();
        var ctx = new EditContext(model);
        var store = new ValidationMessageStore(ctx);

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        // No errors yet.
        Assert.Empty(cut.FindAll(".omni-validation-message-item"));

        // Add an error and notify — component should re-render.
        store.Add(new FieldIdentifier(model, nameof(Person.Email)), "boom");
        ctx.NotifyValidationStateChanged();

        Assert.Single(cut.FindAll(".omni-validation-message-item"));
        Assert.Equal("boom", cut.Find(".omni-validation-message-item").TextContent);
    }

    [Fact]
    public void Ignores_messages_for_other_fields()
    {
        var model = new Person();
        var ctx = new EditContext(model);
        var store = new ValidationMessageStore(ctx);
        // Push an error for a field this component is not watching.
        store.Add(new FieldIdentifier(model, "OtherField"), "not-mine");
        ctx.NotifyValidationStateChanged();

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        Assert.Empty(cut.FindAll(".omni-validation-message"));
    }

    // ── ParameterState: recompute fires only on tracked params ──

    [Fact]
    public void Initial_recompute_fires_on_first_render()
    {
        var model = new Person();
        var ctx = new EditContext(model);
        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        // For + EditContext + ShowAll → three initial fires.
        Assert.Equal(3, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var model = new Person();
        var ctx = BuildContextWithErrors(model, (nameof(Person.Email), "boom"));

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, () => model.Email));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color:red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_For_changes()
    {
        var model = new Person { Email = "a@b" };
        var ctx = new EditContext(model);

        System.Linq.Expressions.Expression<Func<string?>> emailExpr = () => model.Email;
        System.Linq.Expressions.Expression<Func<string?>> nameExpr  = () => model.Name;

        var cut = RenderComponent<OmniValidationMessage<string>>(p => p
            .AddCascadingValue(ctx)
            .Add(c => c.For, emailExpr));

        var baseline = cut.Instance.RecomputeCount;
        cut.SetParametersAndRender(p => p.Add(c => c.For, nameExpr));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
