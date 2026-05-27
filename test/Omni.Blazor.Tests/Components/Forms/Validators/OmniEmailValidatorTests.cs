using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniEmailValidator"/>: defers to
/// <see cref="System.ComponentModel.DataAnnotations.EmailAddressAttribute"/>.
/// Empty input passes (delegates to Required).
/// </summary>
public class OmniEmailValidatorTests : TestContextBase
{
    private sealed class Model { public string? Email { get; set; } }

    private (IRenderedComponent<OmniEmailValidator> Cut, EditContext Ctx) RenderEmail(
        IOmniFormComponent input,
        string message = "Invalid email.")
    {
        var ctx = new EditContext(new Model());
        var registry = new StubFormRegistry().Register(input);
        var cut = RenderComponent<OmniEmailValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName)
            .Add(c => c.Text, message));
        return (cut, ctx);
    }

    [Theory]
    [InlineData("a@b.com")]
    [InlineData("first.last@subdomain.example.org")]
    [InlineData("user+tag@example.io")]
    public void Valid_email_passes(string email)
    {
        var input = new StubFormComponent(email, "email");
        var (_, ctx) = RenderEmail(input);

        Assert.True(ctx.Validate());
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@no-local.com")]
    [InlineData("no-at-sign.com")]
    [InlineData("two@@signs.com")]
    public void Invalid_email_fails(string email)
    {
        var input = new StubFormComponent(email, "email");
        var (_, ctx) = RenderEmail(input, "Invalid email.");

        Assert.False(ctx.Validate());
        Assert.Contains("Invalid email.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Empty_string_passes_delegated_to_required()
    {
        var input = new StubFormComponent(string.Empty, "email");
        var (_, ctx) = RenderEmail(input);

        Assert.True(ctx.Validate());
    }

    [Fact]
    public void Null_value_passes_delegated_to_required()
    {
        var input = new StubFormComponent(null, "email");
        var (_, ctx) = RenderEmail(input);

        Assert.True(ctx.Validate());
    }
}
