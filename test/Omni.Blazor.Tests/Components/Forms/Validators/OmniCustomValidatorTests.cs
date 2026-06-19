using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniCustomValidator{TValue}"/>: invokes a
/// typed predicate against the field value.
/// </summary>
public class OmniCustomValidatorTests : TestContextBase
{
    private sealed class Model { public string? Username { get; set; } }

    private (IRenderedComponent<OmniCustomValidator<string>> Cut, EditContext Ctx) RenderCustom(
        IOmniFormComponent input,
        Func<string?, bool> validator,
        string message = "Invalid.")
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);
        var cut = Render<OmniCustomValidator<string>>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Validator, validator)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Fact]
    public void Predicate_returning_true_passes()
    {
        var input = new StubFormComponent("anderson", "username");
        var (_, ctx) = RenderCustom(input, v => !string.IsNullOrEmpty(v) && v.Length > 3);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Predicate_returning_false_fails()
    {
        var input = new StubFormComponent("an", "username");
        var (_, ctx) = RenderCustom(input, v => !string.IsNullOrEmpty(v) && v.Length > 3, "Too short.");

        Assert.False(ctx.Validate());
        Assert.Contains("Too short.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Predicate_receives_typed_value()
    {
        string? captured = "<not-called>";
        var input = new StubFormComponent("hello", "field");
        var (_, ctx) = RenderCustom(input, v => { captured = v; return true; });

        ctx.Validate();
        Assert.Equal("hello", captured);
    }

    [Fact]
    public void Predicate_gets_default_when_value_wrong_type()
    {
        // Validator typed as string<string>, but stub returns an int.
        // The dispatcher should hand it the default(string) = null.
        string? captured = "<not-called>";
        var input = new StubFormComponent(42, "field");
        var (_, ctx) = RenderCustom(input, v => { captured = v; return true; });

        ctx.Validate();
        Assert.Null(captured);
    }
}
