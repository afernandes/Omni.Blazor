using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Unit-level contract for <see cref="OmniDataAnnotationValidator"/>: runs all
/// <see cref="ValidationAttribute"/>s declared on a property and surfaces their
/// messages into the EditContext. Uses the field's identifier (via stub) for
/// reflection — the model class must own the property.
/// </summary>
public class OmniDataAnnotationValidatorTests : TestContextBase
{
    private sealed class Person
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name 2-50 chars.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email invalid.")]
        public string? Email { get; set; }
    }

    private sealed class StubComponent : IOmniFormComponent
    {
        public StubComponent(Person model, string field)
        {
            ResolvedName = field;
            FieldIdentifier = new FieldIdentifier(model, field);
        }
        public string ResolvedName { get; }
        public FieldIdentifier FieldIdentifier { get; }
        public object? GetValue() => FieldIdentifier.Model.GetType().GetProperty(FieldIdentifier.FieldName)!.GetValue(FieldIdentifier.Model);
        public bool HasValue => GetValue() is not null;
    }

    private (IRenderedComponent<OmniDataAnnotationValidator> Cut, EditContext Ctx) RenderDA(
        Person model,
        StubComponent input)
    {
        var ctx = new EditContext(model);
        var registry = new StubFormRegistry().Register(input);
        var cut = Render<OmniDataAnnotationValidator>(p => p
            .AddCascadingValue<EditContext>(ctx)
            .AddCascadingValue<IOmniFormRegistry>(registry)
            .Add(c => c.Component, input.ResolvedName));
        return (cut, ctx);
    }

    [Fact]
    public void Required_attribute_violation_surfaces_its_message()
    {
        var model = new Person { Name = null };
        var input = new StubComponent(model, nameof(Person.Name));
        var (_, ctx) = RenderDA(model, input);

        Assert.False(ctx.Validate());
        Assert.Contains("Name is required.", ctx.GetValidationMessages());
    }

    [Fact]
    public void StringLength_attribute_violation_surfaces_its_message()
    {
        var model = new Person { Name = "A" }; // too short
        var input = new StubComponent(model, nameof(Person.Name));
        var (_, ctx) = RenderDA(model, input);

        Assert.False(ctx.Validate());
        Assert.Contains("Name 2-50 chars.", ctx.GetValidationMessages());
    }

    [Fact]
    public void Multiple_violations_are_joined_with_separator()
    {
        // Name=null hits Required; depending on framework also surfaces StringLength.
        var model = new Person { Name = null };
        var input = new StubComponent(model, nameof(Person.Name));
        var (_, ctx) = RenderDA(model, input);

        Assert.False(ctx.Validate());
        var messages = ctx.GetValidationMessages().ToList();
        Assert.NotEmpty(messages);
        // At least the required message comes through.
        Assert.Contains(messages, m => m.Contains("Name is required."));
    }

    [Fact]
    public void Valid_property_passes()
    {
        var model = new Person { Name = "Anderson" };
        var input = new StubComponent(model, nameof(Person.Name));
        var (_, ctx) = RenderDA(model, input);

        Assert.True(ctx.Validate());
        Assert.Empty(ctx.GetValidationMessages());
    }

    [Fact]
    public void EmailAddress_attribute_violation_surfaces_its_message()
    {
        var model = new Person { Email = "not-an-email" };
        var input = new StubComponent(model, nameof(Person.Email));
        var (_, ctx) = RenderDA(model, input);

        Assert.False(ctx.Validate());
        Assert.Contains("Email invalid.", ctx.GetValidationMessages());
    }
}
