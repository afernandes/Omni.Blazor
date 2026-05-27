using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniCompareValidator"/>: compares a
/// field's value against either a literal or another sibling field via the
/// FormRegistry. Exercises the full operator surface.
/// </summary>
public class OmniCompareValidatorTests : TestContextBase
{
    private sealed class Model { public string? A { get; set; } public string? B { get; set; } }

    private (IRenderedComponent<OmniCompareValidator> Cut, EditContext Ctx) RenderCompare(
        IOmniFormComponent input,
        OmniCompareValidator.CompareOperator op,
        object? value = null,
        string? otherComponent = null,
        IOmniFormRegistry? registry = null,
        string message = "Invalid.")
    {
        var ctx = new EditContext(new Model());
        registry ??= new StubFormRegistry().Register(input);
        var cut = RenderComponent<OmniCompareValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Operator, op)
            .Add(c => c.Value, value)
            .Add(c => c.OtherComponent, otherComponent)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Fact]
    public void Equal_with_matching_literal_passes()
    {
        var input = new StubFormComponent("secret", "confirm");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.Equal, value: "secret");

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Equal_with_mismatched_literal_fails()
    {
        var input = new StubFormComponent("wrong", "confirm");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.Equal, value: "secret", message: "Mismatch.");

        Assert.False(ctx.Validate());
        Assert.Contains("Mismatch.", ctx.GetValidationMessages());
    }

    [Fact]
    public void NotEqual_passes_when_different()
    {
        var input = new StubFormComponent("a", "x");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.NotEqual, value: "b");

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Equal_with_OtherComponent_compares_via_registry()
    {
        var primary = new StubFormComponent("secret", "password");
        var confirm = new StubFormComponent("secret", "confirm");
        var registry = new StubFormRegistry()
            .Register(primary)
            .Register(confirm);

        var (_, ctx) = RenderCompare(confirm, OmniCompareValidator.CompareOperator.Equal,
            otherComponent: "password", registry: registry);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Equal_with_OtherComponent_mismatch_fails()
    {
        var primary = new StubFormComponent("secret", "password");
        var confirm = new StubFormComponent("different", "confirm");
        var registry = new StubFormRegistry()
            .Register(primary)
            .Register(confirm);

        var (_, ctx) = RenderCompare(confirm, OmniCompareValidator.CompareOperator.Equal,
            otherComponent: "password", registry: registry, message: "Passwords don't match.");

        Assert.False(ctx.Validate());
        Assert.Contains("Passwords don't match.", ctx.GetValidationMessages());
    }

    [Fact]
    public void GreaterThan_compares_numerics()
    {
        var input = new StubFormComponent(10, "x");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.GreaterThan, value: 5);
        Assert.True(ctx.Validate());

        var lowInput = new StubFormComponent(1, "x");
        var (_, lowCtx) = RenderCompare(lowInput, OmniCompareValidator.CompareOperator.GreaterThan, value: 5);
        Assert.False(lowCtx.Validate());
    }

    [Fact]
    public void LessThanEqual_includes_boundary()
    {
        var input = new StubFormComponent(5, "x");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.LessThanEqual, value: 5);
        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Both_null_with_Equal_passes()
    {
        var input = new StubFormComponent(null, "x");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.Equal, value: null);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Only_one_side_null_with_NotEqual_passes()
    {
        var input = new StubFormComponent(null, "x");
        var (_, ctx) = RenderCompare(input, OmniCompareValidator.CompareOperator.NotEqual, value: "something");

        Assert.True(ctx.Validate());
    }
}
