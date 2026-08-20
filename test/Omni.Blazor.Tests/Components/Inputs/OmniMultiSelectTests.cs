using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniMultiSelect{TValue}"/>: root +
/// trigger rendering, chips visibility, and the cross-cutting splat. The
/// dropdown popup itself lives in <c>OmniPopover</c> and is exercised by
/// integration tests; here we only assert basic structure.
/// </summary>
public class OmniMultiSelectTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" }));

        Assert.NotNull(cut.Find("div.omni-multiselect"));
    }

    [Fact]
    public void Renders_placeholder_when_empty()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Placeholder, "Pick..."));

        Assert.Contains("Pick...", cut.Find("span.omni-multiselect-placeholder").TextContent);
    }

    [Fact]
    public void Renders_one_chip_per_selected_value()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" })
            .Add(c => c.Value, new[] { "a", "c" }));

        Assert.Equal(2, cut.FindAll("span.omni-multiselect-chip").Count);
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Disabled, true));

        Assert.Contains("omni-multiselect-disabled", cut.Find("div.omni-multiselect").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-multiselect").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Style, "min-width: 320px"));

        Assert.Equal("min-width: 320px", cut.Find("div.omni-multiselect").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .AddUnmatched("data-testid", "ms"));

        Assert.Equal("ms", cut.Find("div.omni-multiselect").GetAttribute("data-testid"));
    }

    // ── Shared multi-value contract: FormComponent<IEnumerable<TValue>> ──

    private sealed class Model
    {
        public IEnumerable<string>? Cats { get; set; }
    }

    [Fact]
    public void ValueExpression_builds_the_FieldIdentifier()
    {
        var model = new Model();
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.ValueExpression, () => model.Cats));

        Assert.True(cut.Instance.HasFieldIdentifier);
        Assert.Equal(nameof(Model.Cats), cut.Instance.FieldId.FieldName);
    }

    [Fact]
    public void Removing_a_chip_raises_ValueChanged_without_the_removed_value()
    {
        IEnumerable<string>? captured = null;
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a", "b", "c" })
            .Add(c => c.Value, new[] { "a", "c" })
            .Add(c => c.ValueChanged, (IEnumerable<string>? v) => captured = v));

        cut.FindAll("button.omni-multiselect-chip-x")[0].Click();

        Assert.NotNull(captured);
        Assert.Equal(["c"], captured);
    }

    [Fact]
    public void Required_rejects_an_empty_selection()
    {
        var model = new Model { Cats = Array.Empty<string>() };
        var cut = Render<OmniMultiSelect<string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.Value, model.Cats)
            .Add(c => c.ValueExpression, () => model.Cats)
            .Add(c => c.Required, true)
            .Add(c => c.OnlyValidateIfDirty, false));

        Assert.False(((IOmniFormComponent)cut.Instance).HasValue);
    }

    [Fact]
    public async Task Closing_popover_cancels_pending_provider_request()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observedToken = default;
        async ValueTask<OmniItemsPage<string>> Provider(
            OmniItemsRequest request,
            CancellationToken cancellationToken)
        {
            observedToken = cancellationToken;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new OmniItemsPage<string>([], 0);
        }

        var cut = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, Provider));

        var opening = cut.Find(".omni-multiselect-trigger").ClickAsync(new());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        var closing = cut.Find(".omni-multiselect-trigger").ClickAsync(new());

        await Task.WhenAll(opening, closing).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.True(observedToken.IsCancellationRequested);
    }
}
