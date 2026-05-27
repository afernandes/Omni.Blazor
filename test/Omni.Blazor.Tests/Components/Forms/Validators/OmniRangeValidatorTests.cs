using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniRangeValidator"/>: enforces Min/Max
/// over any <see cref="IComparable"/>. Null delegates to Required.
/// </summary>
public class OmniRangeValidatorTests : TestContextBase
{
    private sealed class Model { public int Age { get; set; } }

    private (IRenderedComponent<OmniRangeValidator> Cut, EditContext Ctx) RenderRange(
        IOmniFormComponent input,
        object? min,
        object? max,
        string message = "Out of range.")
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);
        var cut = RenderComponent<OmniRangeValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Min, min)
            .Add(c => c.Max, max)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Fact]
    public void Value_within_inclusive_range_passes()
    {
        var input = new StubFormComponent(50, "age");
        var (_, ctx) = RenderRange(input, 18, 120);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Value_at_min_boundary_passes()
    {
        var input = new StubFormComponent(18, "age");
        var (_, ctx) = RenderRange(input, 18, 120);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Value_at_max_boundary_passes()
    {
        var input = new StubFormComponent(120, "age");
        var (_, ctx) = RenderRange(input, 18, 120);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Value_below_min_fails()
    {
        var input = new StubFormComponent(10, "age");
        var (_, ctx) = RenderRange(input, 18, 120, "Age between 18 and 120.");

        Assert.False(ctx.Validate());
        Assert.Contains("Age between 18 and 120.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Value_above_max_fails()
    {
        var input = new StubFormComponent(200, "age");
        var (_, ctx) = RenderRange(input, 18, 120, "Age between 18 and 120.");

        Assert.False(ctx.Validate());
        Assert.Contains("Age between 18 and 120.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Null_value_passes_delegated_to_required()
    {
        var input = new StubFormComponent(null, "age");
        var (_, ctx) = RenderRange(input, 18, 120);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Only_Min_constraint_applies_when_Max_is_null()
    {
        var input = new StubFormComponent(10, "age");
        var (_, ctx) = RenderRange(input, 5, null);
        Assert.True(ctx.Validate());

        var inputLow = new StubFormComponent(3, "age");
        var (_, ctxLow) = RenderRange(inputLow, 5, null);
        Assert.False(ctxLow.Validate());
    }

    [Fact]
    public void Only_Max_constraint_applies_when_Min_is_null()
    {
        var input = new StubFormComponent(100, "age");
        var (_, ctx) = RenderRange(input, null, 50);
        Assert.False(ctx.Validate());
    }
}
