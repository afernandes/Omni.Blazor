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

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("<div data-testid='inside'>body</div>"));

        Assert.NotNull(cut.Find("form"));
        Assert.NotNull(cut.Find("[data-testid='inside']"));
    }

    [Fact]
    public void Accepts_external_EditContext_and_preserves_its_identity()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var editContext = new EditContext(model);
        EditContext? submittedContext = null;
        var cut = Render<OmniForm<Person>>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.OnValidSubmit, context => submittedContext = context)
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        Assert.Same(editContext, cut.Instance.CurrentEditContext);
        Assert.Same(editContext, submittedContext);
    }

    [Fact]
    public void Model_and_EditContext_are_mutually_exclusive()
    {
        var model = new Person();
        var editContext = new EditContext(model);

        Exception both = Assert.ThrowsAny<Exception>(() =>
            Render<OmniForm<Person>>(parameters => parameters
                .Add(component => component.Model, model)
                .Add(component => component.EditContext, editContext)));
        Assert.Contains("not both", both.ToString());

        Exception neither = Assert.ThrowsAny<Exception>(() => Render<OmniForm<Person>>());
        Assert.Contains("requires either", neither.ToString());
    }

    [Fact]
    public void External_EditContext_model_must_match_TModel()
    {
        Exception error = Assert.ThrowsAny<Exception>(() =>
            Render<OmniForm<Person>>(parameters => parameters
                .Add(component => component.EditContext, new EditContext(new object()))));

        Assert.Contains(nameof(Person), error.ToString());
    }

    [Fact]
    public void Switching_EditContext_unsubscribes_from_the_previous_instance()
    {
        var firstModel = new Person();
        var secondModel = new Person();
        var first = new EditContext(firstModel);
        var second = new EditContext(secondModel);
        var touchedChanges = new List<bool>();
        var cut = Render<OmniForm<Person>>(parameters => parameters
            .Add(component => component.EditContext, first)
            .Add(component => component.IsTouchedChanged, value => touchedChanges.Add(value))
            .AddChildContent("body"));

        cut.Render(parameters => parameters
            .Add(component => component.EditContext, second)
            .Add(component => component.IsTouchedChanged, value => touchedChanges.Add(value))
            .AddChildContent("body"));

        first.NotifyFieldChanged(new FieldIdentifier(firstModel, nameof(Person.Name)));
        Assert.Empty(touchedChanges);

        second.NotifyFieldChanged(new FieldIdentifier(secondModel, nameof(Person.Name)));
        Assert.Equal([true], touchedChanges);
    }

    [Fact]
    public async Task Switching_EditContext_cancels_pending_validation()
    {
        var first = new EditContext(new Person { Name = "First" });
        var second = new EditContext(new Person { Name = "Second" });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<EditContext, ValidationMessageStore, CancellationToken, Task> validator =
            async (context, _, cancellationToken) =>
            {
                if (!ReferenceEquals(context, first)) return;
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }
            };
        var cut = Render<OmniForm<Person>>(parameters => parameters
            .Add(component => component.EditContext, first)
            .Add(component => component.ValidationAsyncWithCancellation, validator)
            .AddChildContent("body"));

        Task pending = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await started.Task;
        cut.Render(parameters => parameters
            .Add(component => component.EditContext, second)
            .Add(component => component.ValidationAsyncWithCancellation, validator)
            .AddChildContent("body"));

        await pending;
        Assert.True(cancelled.Task.IsCompleted);
    }

    [Fact]
    public void Auto_attaches_DataAnnotationsValidator_by_default()
    {
        var model = new Person();
        var cut = Render<OmniForm<Person>>(p => p
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
        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
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
        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("body"));

        Assert.False(cut.Instance.Validate());
        Assert.NotEmpty(cut.Instance.Errors);
    }

    [Fact]
    public void Validate_returns_true_when_model_valid()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .AddChildContent("body"));

        Assert.True(cut.Instance.Validate());
    }

    [Fact]
    public async Task ValidateAsync_runs_custom_async_validator()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var asyncRan = false;

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ValidationAsync, async (EditContext _, ValidationMessageStore store) =>
            {
                asyncRan = true;
                store.Add(new FieldIdentifier(model, nameof(Person.Name)), "Async error.");
                await Task.CompletedTask;
            })
            .AddChildContent("body"));

        var result = await cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.True(asyncRan);
        Assert.False(result);
        Assert.Contains("Async error.", cut.Instance.Errors);
    }

    [Fact]
    public async Task ValidateAsync_preserves_sync_and_async_messages()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var field = new FieldIdentifier(model, nameof(Person.Name));

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Validation, (EditContext _, ValidationMessageStore store) =>
                store.Add(field, "Sync error."))
            .Add(c => c.ValidationAsync, (EditContext _, ValidationMessageStore store) =>
            {
                store.Add(field, "Async error.");
                return Task.CompletedTask;
            })
            .AddChildContent("body"));

        var result = await cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Contains("Sync error.", cut.Instance.Errors);
        Assert.Contains("Async error.", cut.Instance.Errors);
    }

    [Fact]
    public async Task ValidateAsync_uses_latest_result_when_legacy_validator_completes_out_of_order()
    {
        var model = new Person { Name = "first", Email = "a@b.com" };
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ValidationAsync, async (EditContext context, ValidationMessageStore store) =>
            {
                var valueAtStart = ((Person)context.Model).Name;
                if (valueAtStart == "first")
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task;
                }

                store.Add(
                    new FieldIdentifier(context.Model, nameof(Person.Name)),
                    $"{valueAtStart} error.");
            })
            .AddChildContent("body"));

        var first = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await firstStarted.Task;

        model.Name = "second";
        var second = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await second;

        releaseFirst.TrySetResult();
        await first;

        Assert.Contains("second error.", cut.Instance.Errors);
        Assert.DoesNotContain("first error.", cut.Instance.Errors);
    }

    [Fact]
    public async Task New_validation_cancels_cancellable_validator()
    {
        var model = new Person { Name = "first", Email = "a@b.com" };
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ValidationAsyncWithCancellation,
                async (EditContext context, ValidationMessageStore _, CancellationToken cancellationToken) =>
                {
                    if (((Person)context.Model).Name != "first") return;
                    firstStarted.TrySetResult();
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        firstCancelled.TrySetResult();
                        throw;
                    }
                })
            .AddChildContent("body"));

        var first = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await firstStarted.Task;

        model.Name = "second";
        await cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await first;

        Assert.True(firstCancelled.Task.IsCompleted);
    }

    [Fact]
    public void Browser_submit_runs_async_validation_before_invalid_callback()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var asyncRan = false;
        var invalidFired = false;

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.ValidationAsync, (EditContext context, ValidationMessageStore store) =>
            {
                asyncRan = true;
                store.Add(new FieldIdentifier(context.Model, nameof(Person.Name)), "Async error.");
                return Task.CompletedTask;
            })
            .Add(c => c.OnInvalidSubmit, (EditContext _) => invalidFired = true)
            .AddChildContent("<button type='submit'>Submit</button>"));

        cut.Find("form").Submit();

        Assert.True(asyncRan);
        Assert.True(invalidFired);
        Assert.Contains("Async error.", cut.Instance.Errors);
    }

    [Fact]
    public void Sync_Validation_callback_runs_on_submit()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };

        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => fired++)
            .AddChildContent("body"));

        await cut.Instance.SubmitAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task SubmitAsync_invokes_OnInvalidSubmit_when_invalid()
    {
        var model = new Person();
        var validFired = 0;
        var invalidFired = 0;

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnValidSubmit, (EditContext _) => validFired++)
            .Add(c => c.OnInvalidSubmit, (EditContext _) => invalidFired++)
            .AddChildContent("body"));

        await cut.Instance.SubmitAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(0, validFired);
        Assert.Equal(1, invalidFired);
    }

    [Fact]
    public void ResetValidation_clears_custom_messages()
    {
        var model = new Person { Name = "Anderson", Email = "a@b.com" };
        var ranValidation = false;
        var cut = Render<OmniForm<Person>>(p => p
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
        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
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

        var cut = Render<OmniForm<Person>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.IsTouchedChanged, (bool v) => changes.Add(v))
            .AddChildContent("body"));

        await cut.Instance.ResetTouchedAsync();
        Assert.Empty(changes);
    }
}
