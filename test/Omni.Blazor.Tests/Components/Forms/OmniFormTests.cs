using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms;

/// <summary>
/// Behavioural contract for <see cref="OmniForm{TModel}"/>: wraps Blazor's
/// EditForm with auto-attached DataAnnotationsValidator, IsTouched/IsValid/Errors
/// ref-based APIs, custom Validation callbacks, snapshot/restore, and the
/// IOmniFormRegistry cascade.
/// </summary>
public class OmniFormTests : TestContextBase
{
    private sealed class Person
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email is invalid.")]
        public string? Email { get; set; }
    }

    [Fact]
    public void Renders_form_element_with_child_content()
    {
        var model = new Person();

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("<div data-testid='inside'>body</div>"));

        Assert.NotNull(cut.Find("form"));
        Assert.NotNull(cut.Find("[data-testid='inside']"));
    }

    [Fact]
    public void Auto_attaches_DataAnnotationsValidator_by_default()
    {
        var model = new Person();
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("<button type='submit'>Submit</button>"));

        // Trigger submit with invalid model — IsValid should be false because
        // DataAnnotationsValidator surfaced [Required] / [EmailAddress] errors.
        cut.Find("form").Submit();

        Assert.False(cut.Instance.IsValid);
        Assert.Contains("Name is required.", cut.Instance.Errors);
    }

    [Fact]
    public void Can_disable_DataAnnotationsValidator()
    {
        var model = new Person();
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.AddDataAnnotationsValidator, false)
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        // No DataAnnotations validator wired → no errors surfaced.
        Assert.True(cut.Instance.IsValid);
        Assert.Empty(cut.Instance.Errors);
    }

    [Fact]
    public void OnValidSubmit_fires_when_model_is_valid()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var fired = 0;

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => fired++)
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        Assert.Equal(1, fired);
        Assert.True(cut.Instance.IsValid);
    }

    [Fact]
    public void OnInvalidSubmit_fires_when_model_has_errors()
    {
        var model = new Person(); // Missing required Name.
        var validFired = 0;
        var invalidFired = 0;

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => validFired++)
            .Add(c => c.OnInvalidSubmit, (EditContext _) => invalidFired++)
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        Assert.Equal(0, validFired);
        Assert.Equal(1, invalidFired);
    }

    [Fact]
    public void Validate_returns_false_when_model_invalid()
    {
        var model = new Person();
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("body"));

        Assert.False(cut.Instance.Validate());
        Assert.NotEmpty(cut.Instance.Errors);
    }

    [Fact]
    public void Validate_returns_true_when_model_valid()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("body"));

        Assert.True(cut.Instance.Validate());
    }

    [Fact]
    public async Task ValidateAsync_runs_custom_async_validator()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var asyncRan = false;

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ValidationAsync, async (EditContext _, ValidationMessageStore store) =>
            {
                asyncRan = true;
                store.Add(new FieldIdentifier(model, nameof(Person.Name)), "Async error.");
                await Task.CompletedTask;
            })
            .AddChildContent("body"));

        var result = await cut.Instance.ValidateAsync();

        Assert.True(asyncRan);
        Assert.False(result);
        Assert.Contains("Async error.", cut.Instance.Errors);
    }

    [Fact]
    public void Sync_Validation_callback_runs_on_submit()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Validation, (EditContext _, ValidationMessageStore store) =>
            {
                store.Add(new FieldIdentifier(model, nameof(Person.Name)), "Custom sync error.");
            })
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        Assert.Contains("Custom sync error.", cut.Instance.Errors);
        Assert.False(cut.Instance.IsValid);
    }

    [Fact]
    public async Task SubmitAsync_invokes_OnValidSubmit_when_valid()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var fired = 0;

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => fired++)
            .AddChildContent("body"));

        await cut.Instance.SubmitAsync();

        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task SubmitAsync_invokes_OnInvalidSubmit_when_invalid()
    {
        var model = new Person();
        var validFired = 0;
        var invalidFired = 0;

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => validFired++)
            .Add(c => c.OnInvalidSubmit, (EditContext _) => invalidFired++)
            .AddChildContent("body"));

        await cut.Instance.SubmitAsync();

        Assert.Equal(0, validFired);
        Assert.Equal(1, invalidFired);
    }

    [Fact]
    public void ResetValidation_clears_custom_messages()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var ranValidation = false;
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Validation, (EditContext _, ValidationMessageStore store) =>
            {
                ranValidation = true;
                store.Add(new FieldIdentifier(model, nameof(Person.Name)), "Custom.");
            })
            .AddChildContent("<button type='submit'>Submit</button>"));

        // Trigger sync Validation via submit (which calls EditContext.Validate
        // which fires OnValidationRequested → our custom validator runs).
        cut.Find("form").Submit();
        Assert.True(ranValidation);
        Assert.Contains("Custom.", cut.Instance.Errors);

        cut.Instance.ResetValidation();
        // ResetValidation only clears OmniForm's own store. The
        // DataAnnotationsValidator's store may still have entries, but the
        // *custom* message added via the sync Validation callback is gone.
        Assert.DoesNotContain("Custom.", cut.Instance.Errors);
    }

    [Fact]
    public void Snapshot_and_Restore_round_trip_model_values()
    {
        var model = new Person { Name = "Original", Email = "orig@x.com" };
        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("body"));

        cut.Instance.Snapshot();
        model.Name = "Mutated";
        model.Email = "mut@x.com";
        cut.Instance.Restore();

        Assert.Equal("Original", model.Name);
        Assert.Equal("orig@x.com", model.Email);
    }

    [Fact]
    public async Task ResetAsync_restores_snapshot_and_resets_touched()
    {
        var model = new Person { Name = "Original", Email = "orig@x.com" };
        var touchedChanges = new List<bool>();

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.IsTouched, true) // start touched
            .Add(c => c.IsTouchedChanged, (bool v) => touchedChanges.Add(v))
            .AddChildContent("body"));

        cut.Instance.Snapshot();
        model.Name = "Mutated";
        await cut.Instance.ResetAsync();

        Assert.Equal("Original", model.Name);
        Assert.Contains(false, touchedChanges); // touched flipped back to false
    }

    [Fact]
    public async Task ResetTouchedAsync_emits_changed_callback()
    {
        var model = new Person();
        var changes = new List<bool>();

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.IsTouched, true)
            .Add(c => c.IsTouchedChanged, (bool v) => changes.Add(v))
            .AddChildContent("body"));

        await cut.Instance.ResetTouchedAsync();

        Assert.Contains(false, changes);
    }

    [Fact]
    public async Task IsTouched_starts_false_and_does_not_fire_on_reset_when_already_false()
    {
        var model = new Person();
        var changes = new List<bool>();

        var cut = RenderComponent<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.IsTouchedChanged, (bool v) => changes.Add(v))
            .AddChildContent("body"));

        await cut.Instance.ResetTouchedAsync();
        Assert.Empty(changes);
    }
}
