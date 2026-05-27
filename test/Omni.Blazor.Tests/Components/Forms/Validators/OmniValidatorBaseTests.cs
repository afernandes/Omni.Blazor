using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniValidatorBase"/>: enforces the
/// presence of EditContext + IOmniFormRegistry cascades, publishes/clears
/// messages on field-changed and validation-requested events, and removes its
/// own messages on dispose.
/// </summary>
public class OmniValidatorBaseTests : TestContextBase
{
    /// <summary>Concrete validator used to exercise the base wiring — passes when value is "ok".</summary>
    private sealed class StubValidator : OmniValidatorBase
    {
        public int CallCount { get; private set; }

        protected override bool Validate(IOmniFormComponent component)
        {
            CallCount++;
            return component.GetValue() as string == "ok";
        }
    }

    [Fact]
    public void Throws_without_EditContext_cascade()
    {
        var registry = new StubFormRegistry();
        Assert.Throws<InvalidOperationException>(() =>
            RenderComponent<StubValidator>(p => p
                .AddCascadingValue<IOmniFormRegistry>(registry)
                .Add(c => c.Component, "field")));
    }

    [Fact]
    public void Throws_without_FormRegistry_cascade()
    {
        var ctx = new EditContext(new object());
        Assert.Throws<InvalidOperationException>(() =>
            RenderComponent<StubValidator>(p => p
                .AddCascadingValue<EditContext>(ctx)
                .Add(c => c.Component, "field")));
    }

    [Fact]
    public void Failed_validation_pushes_Text_into_EditContext()
    {
        var ctx = new EditContext(new object());
        var input = new StubFormComponent("bad", "field");
        var registry = new StubFormRegistry().Register(input);

        RenderComponent<StubValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, "field")
            .Add(c => c.Text, "Must be ok."));

        Assert.False(ctx.Validate());
        Assert.Contains("Must be ok.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Passing_validation_does_not_push_message()
    {
        var ctx = new EditContext(new object());
        var input = new StubFormComponent("ok", "field");
        var registry = new StubFormRegistry().Register(input);

        RenderComponent<StubValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, "field"));

        Assert.True(ctx.Validate());
        Assert.Empty(ctx.GetValidationMessages());
    }

    [Fact]
    public void Validate_runs_when_validation_is_requested()
    {
        var ctx = new EditContext(new object());
        var input = new StubFormComponent("bad", "field");
        var registry = new StubFormRegistry().Register(input);

        var cut = RenderComponent<StubValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, "field"));

        ctx.Validate();
        Assert.True(cut.Instance.CallCount >= 1);
    }

    [Fact]
    public void Field_changed_for_target_triggers_validation()
    {
        var ctx = new EditContext(new object());
        var input = new StubFormComponent("bad", "field");
        var registry = new StubFormRegistry().Register(input);

        var cut = RenderComponent<StubValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, "field"));

        var before = cut.Instance.CallCount;
        ctx.NotifyFieldChanged(input.FieldIdentifier);
        Assert.True(cut.Instance.CallCount > before);
    }

    [Fact]
    public void Unknown_component_name_silently_skips_validation()
    {
        var ctx = new EditContext(new object());
        var registry = new StubFormRegistry(); // empty — nothing registered

        var cut = RenderComponent<StubValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, "ghost"));

        ctx.Validate();
        Assert.Equal(0, cut.Instance.CallCount);
    }
}
