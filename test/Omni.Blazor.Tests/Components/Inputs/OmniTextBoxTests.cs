using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniTextBox"/>: bare input vs affix
/// group, size modifiers, two-way Value binding, and the cross-cutting
/// Class/Style/Attributes splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniTextBoxTests : TestContextBase
{
    private sealed class TextModel
    {
        public string? Value { get; set; }
    }

    [Fact]
    public void Renders_bare_input_when_no_affixes()
    {
        var cut = Render<OmniTextBox>();

        var input = cut.Find("input");
        Assert.Contains("omni-input", input.ClassName);
        Assert.Equal("text", input.GetAttribute("type"));
    }

    [Fact]
    public void Renders_input_with_current_Value()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.Value, "hello"));

        Assert.Equal("hello", cut.Find("input").GetAttribute("value"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-input-sm")]
    [InlineData(ComponentSize.Lg, "omni-input-lg")]
    public void Applies_size_modifier_to_input(ComponentSize size, string expected)
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find("input").ClassName);
    }

    [Fact]
    public void LeadingIcon_wraps_in_input_group()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.LeadingIcon, "search"));

        var group = cut.Find("div.omni-input-group");
        Assert.NotNull(group);
        // Right padding only kicks in when there's a trailing affix without a leading one.
        Assert.DoesNotContain("omni-input-group-right", group.ClassName);
    }

    [Fact]
    public void TrailingIcon_only_applies_right_padding_class()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.TrailingIcon, "x"));

        var group = cut.Find("div.omni-input-group");
        Assert.Contains("omni-input-group-right", group.ClassName);
    }

    [Fact]
    public void Clearable_with_value_shows_clear_button_and_clears_on_click()
    {
        string? captured = "abc";
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Clearable, true)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("button.omni-input-clear").Click();
        Assert.Null(captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_bare_input()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("input").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_group_wrapper()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.LeadingIcon, "search")
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-input-group").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniTextBox>(p => p
            .Add(c => c.LeadingIcon, "search")
            .Add(c => c.Style, "width: 200px"));

        Assert.Equal("width: 200px", cut.Find("div.omni-input-group").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_input_when_no_group()
    {
        var cut = Render<OmniTextBox>(p => p
            .AddUnmatched("data-testid", "tb1"));

        Assert.Equal("tb1", cut.Find("input").GetAttribute("data-testid"));
    }

    [Fact]
    public async Task Validation_is_latest_wins_when_legacy_task_completes_out_of_order()
    {
        var model = new TextModel { Value = "first" };
        var context = new EditContext(model);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<string?, Task<string?>> validator = async value =>
        {
            if (value == "first")
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
            }
            return $"{value} error.";
        };

        var cut = Render<OmniTextBox>(p => p
            .AddCascadingValue(context)
            .Add(c => c.Value, model.Value)
            .Add(c => c.ValueExpression, () => model.Value)
            .Add(c => c.Validation, validator)
            .Add(c => c.OnlyValidateIfDirty, false));

        var first = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await firstStarted.Task;

        model.Value = "second";
        cut.Render(p => p.Add(c => c.Value, model.Value));
        var second = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await second;

        releaseFirst.TrySetResult();
        await first;

        Assert.Contains("second error.", context.GetValidationMessages());
        Assert.DoesNotContain("first error.", context.GetValidationMessages());
    }

    [Fact]
    public async Task New_input_validation_cancels_previous_token()
    {
        var model = new TextModel { Value = "first" };
        var context = new EditContext(model);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<string?, CancellationToken, Task<string?>> validator = async (value, cancellationToken) =>
        {
            if (value != "first") return null;
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
            return null;
        };

        var cut = Render<OmniTextBox>(p => p
            .AddCascadingValue(context)
            .Add(c => c.Value, model.Value)
            .Add(c => c.ValueExpression, () => model.Value)
            .Add(c => c.Validation, validator)
            .Add(c => c.OnlyValidateIfDirty, false));

        var first = cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await firstStarted.Task;

        model.Value = "second";
        cut.Render(p => p.Add(c => c.Value, model.Value));
        await cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);
        await first;

        Assert.True(firstCancelled.Task.IsCompleted);
    }
}
