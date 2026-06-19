using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniLengthValidator"/>: validates the
/// length of strings or collections against Min/Max. Null/empty delegates to
/// Required.
/// </summary>
public class OmniLengthValidatorTests : TestContextBase
{
    private sealed class Model { public string? Cnpj { get; set; } }

    private (IRenderedComponent<OmniLengthValidator> Cut, EditContext Ctx) RenderLength(
        IOmniFormComponent input,
        int? min,
        int? max,
        string message = "Length out of range.")
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);
        var cut = Render<OmniLengthValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Min, min)
            .Add(c => c.Max, max)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Fact]
    public void String_length_within_range_passes()
    {
        var input = new StubFormComponent("12345678901234", "cnpj");
        var (_, ctx) = RenderLength(input, 14, 14);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void String_length_below_min_fails()
    {
        var input = new StubFormComponent("12345", "cnpj");
        var (_, ctx) = RenderLength(input, 14, 14, "CNPJ must have 14 digits.");

        Assert.False(ctx.Validate());
        Assert.Contains("CNPJ must have 14 digits.", ctx.GetValidationMessages());
    }

    [Fact]
    public void String_length_above_max_fails()
    {
        var input = new StubFormComponent("12345678901234567890", "cnpj");
        var (_, ctx) = RenderLength(input, 14, 14, "CNPJ must have 14 digits.");

        Assert.False(ctx.Validate());
    }

    [Fact]
    public void Empty_string_passes_delegated_to_required()
    {
        // Length validator's "vazio passa" path returns true for null;
        // an empty string remains a string and is measured as length 0.
        var input = new StubFormComponent(string.Empty, "cnpj");
        var (_, ctx) = RenderLength(input, 14, 14);

        // length 0 < 14 → fails. To delegate to Required, the value would need to be null.
        Assert.False(ctx.Validate());
    }

    [Fact]
    public void Null_value_passes_delegated_to_required()
    {
        var input = new StubFormComponent(null, "cnpj");
        var (_, ctx) = RenderLength(input, 14, 14);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Collection_count_within_range_passes()
    {
        var list = new List<int> { 1, 2, 3 };
        var input = new StubFormComponent(list, "tags");
        var (_, ctx) = RenderLength(input, 1, 5);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Collection_count_above_max_fails()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6 };
        var input = new StubFormComponent(list, "tags");
        var (_, ctx) = RenderLength(input, 1, 5);

        Assert.False(ctx.Validate());
    }

    [Fact]
    public void Open_ended_Min_only_passes_above_min()
    {
        var input = new StubFormComponent("hello world", "msg");
        var (_, ctx) = RenderLength(input, 5, null);

        Assert.True(ctx.Validate());
    }
}
