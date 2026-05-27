using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniRegexValidator"/>: matches a string
/// against a .NET regex pattern. Empty input delegates to <see cref="OmniRequiredValidator"/>.
/// </summary>
public class OmniRegexValidatorTests : TestContextBase
{
    private sealed class Model { public string? Cep { get; set; } }

    private (IRenderedComponent<OmniRegexValidator> Cut, EditContext Ctx) RenderRegex(
        IOmniFormComponent input,
        string pattern,
        string message = "Invalid format.",
        RegexOptions options = RegexOptions.None)
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);
        var cut = RenderComponent<OmniRegexValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Pattern, pattern)
            .Add(c => c.Options, options)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Fact]
    public void Matching_pattern_passes()
    {
        var input = new StubFormComponent("01310-100", "cep");
        var (_, ctx) = RenderRegex(input, @"^\d{5}-\d{3}$", "CEP must be 00000-000.");

        Assert.True(ctx.Validate());
        Assert.Empty(ctx.GetValidationMessages());
    }

    [Fact]
    public void Non_matching_pattern_fails()
    {
        var input = new StubFormComponent("abc", "cep");
        var (_, ctx) = RenderRegex(input, @"^\d{5}-\d{3}$", "CEP must be 00000-000.");

        Assert.False(ctx.Validate());
        Assert.Contains("CEP must be 00000-000.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Empty_value_passes_delegated_to_required()
    {
        var input = new StubFormComponent(string.Empty, "cep");
        var (_, ctx) = RenderRegex(input, @"^\d{5}-\d{3}$");

        Assert.True(ctx.Validate());
        Assert.Empty(ctx.GetValidationMessages());
    }

    [Fact]
    public void Null_value_passes_delegated_to_required()
    {
        var input = new StubFormComponent(null, "cep");
        var (_, ctx) = RenderRegex(input, @"^\d{5}-\d{3}$");

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void RegexOptions_IgnoreCase_is_applied()
    {
        var input = new StubFormComponent("ANDERSON", "name");
        var (_, ctx) = RenderRegex(input, "^anderson$", options: RegexOptions.IgnoreCase);

        Assert.True(ctx.Validate());
    }
}
