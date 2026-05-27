using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniRequiredValidator"/>: empty/null/default
/// values trigger the message via <see cref="EditContext.Validate"/>; non-empty
/// values pass. Drives the validator through the standard EditContext +
/// IOmniFormRegistry cascade — same wiring it sees inside <c>OmniForm</c>.
/// </summary>
public class OmniRequiredValidatorTests : TestContextBase
{
    private sealed class Model
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private (IRenderedComponent<OmniRequiredValidator> Cut, EditContext Ctx) RenderRequired(
        IOmniFormComponent input,
        string message = "Required.",
        object? defaultValue = null)
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);

        var cut = RenderComponent<OmniRequiredValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Text, message)
            .Add(c => c.DefaultValue, defaultValue));
        return (cut, ctx);
    }

    [Fact]
    public void Empty_string_fails_validation()
    {
        var input = new StubFormComponent(string.Empty, "name");
        var (_, ctx) = RenderRequired(input, "Name is required.");

        Assert.False(ctx.Validate());
        Assert.Contains("Name is required.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Null_value_fails_validation()
    {
        var input = new StubFormComponent(null, "name");
        var (_, ctx) = RenderRequired(input, "Name is required.");

        Assert.False(ctx.Validate());
        Assert.Contains("Name is required.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Non_empty_string_passes()
    {
        var input = new StubFormComponent("Anderson", "name");
        var (_, ctx) = RenderRequired(input, "Name is required.");

        Assert.True(ctx.Validate());
        Assert.Empty(ctx.GetValidationMessages());
    }

    [Fact]
    public void Equals_DefaultValue_fails_validation()
    {
        // 0 with DefaultValue=0 is treated as "empty" — clinically common
        // when you want "any non-default int".
        var input = new StubFormComponent(0, "age");
        var (_, ctx) = RenderRequired(input, "Age is required.", defaultValue: 0);

        Assert.False(ctx.Validate());
        Assert.Contains("Age is required.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Different_from_DefaultValue_passes()
    {
        var input = new StubFormComponent(42, "age");
        var (_, ctx) = RenderRequired(input, "Age is required.", defaultValue: 0);

        Assert.True(ctx.Validate());
    }
}
