using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniAutoComplete{TItem}"/>: input
/// rendering, chevron vs clear button, and the cross-cutting splat. Dropdown
/// search/filter is covered indirectly through user-driven input events.
/// </summary>
public class OmniAutoCompleteTests : TestContextBase
{
    [Fact]
    public void Renders_input_with_autocomplete_classes()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? ""));

        var root = cut.Find("div.omni-autocomplete");
        Assert.Contains("omni-input-group", root.ClassName);
        Assert.NotNull(cut.Find("input.omni-autocomplete-input"));
    }

    [Fact]
    public void Shows_chevron_when_no_value()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? ""));

        // The right slot holds a chevron span (not the clear button).
        Assert.Empty(cut.FindAll("button.omni-input-clear"));
    }

    [Fact]
    public void Shows_clear_button_when_Clearable_and_Value_present()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "abc")
            .Add(c => c.Clearable, true));

        Assert.NotEmpty(cut.FindAll("button.omni-input-clear"));
    }

    [Fact]
    public void Disabled_propagates_to_input()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-autocomplete").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Style, "min-width: 240px"));

        Assert.Equal("min-width: 240px", cut.Find("div.omni-autocomplete").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .AddUnmatched("data-testid", "ac"));

        Assert.Equal("ac", cut.Find("div.omni-autocomplete").GetAttribute("data-testid"));
    }

    // ── ParameterState: derived state recomputes only when Value changes ──

    [Fact]
    public void Initial_Value_seeds_recompute_on_first_render()
    {
        // Derived state (RecomputeCount) populates on first detect cycle.
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "new-cls")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_Value_changes()
    {
        var cut = Render<OmniAutoComplete<string>>(p => p
            .Add(c => c.TextSelector, s => s ?? "")
            .Add(c => c.Value, "alpha"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Value, "beta"));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public async Task ItemsProvider_cancels_superseded_search_and_latest_result_wins()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;

        async ValueTask<OmniItemsPage<string>> Provider(
            OmniItemsRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Search == "primeiro")
            {
                firstToken = cancellationToken;
                firstStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new OmniItemsPage<string>([request.Search ?? "vazio"], 1);
        }

        var cut = Render<OmniAutoComplete<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, Provider)
            .Add(component => component.DebounceMs, 0)
            .Add(component => component.TextSelector, value => value));
        var input = cut.Find("input");

        var first = input.InputAsync("primeiro");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        var second = input.InputAsync("segundo");

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() =>
        {
            Assert.True(firstToken.IsCancellationRequested);
            Assert.Equal("segundo", cut.Find(".omni-autocomplete-option").TextContent.Trim());
        });
    }

    [Fact]
    public async Task ItemsProvider_enforces_page_and_retained_item_limits()
    {
        var requests = new List<OmniItemsRequest>();
        ValueTask<OmniItemsPage<string>> Provider(
            OmniItemsRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            requests.Add(request);
            var items = Enumerable.Range(request.Skip, 10).Select(index => $"item-{index}").ToArray();
            return ValueTask.FromResult(new OmniItemsPage<string>(items, 100));
        }

        var cut = Render<OmniAutoComplete<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, Provider)
            .Add(component => component.ProviderPageSize, 2)
            .Add(component => component.MaxProviderItems, 3)
            .Add(component => component.DebounceMs, 0)
            .Add(component => component.TextSelector, value => value));

        await cut.Find("input").FocusAsync(new());
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".omni-autocomplete-option").Count));
        await cut.Find(".omni-autocomplete-load-more").ClickAsync(new());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindAll(".omni-autocomplete-option").Count);
            Assert.Empty(cut.FindAll(".omni-autocomplete-load-more"));
            Assert.Collection(requests,
                first => Assert.Equal((0, 2), (first.Skip, first.Take)),
                second => Assert.Equal((2, 1), (second.Skip, second.Take)));
        });
    }

    [Fact]
    public async Task Disposal_cancels_pending_provider_request()
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

        var cut = Render<OmniAutoComplete<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, Provider)
            .Add(component => component.DebounceMs, 0)
            .Add(component => component.TextSelector, value => value));
        var pending = cut.Find("input").InputAsync("consulta");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        await cut.Instance.DisposeAsync();
        await pending.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        Assert.True(observedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Provider_failure_is_reported_and_retry_can_recover()
    {
        var attempts = 0;
        Exception? reported = null;
        ValueTask<OmniItemsPage<string>> Provider(
            OmniItemsRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++attempts == 1) throw new InvalidOperationException("falha esperada");
            return ValueTask.FromResult(new OmniItemsPage<string>(["recuperado"], 1));
        }

        var cut = Render<OmniAutoComplete<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, Provider)
            .Add(component => component.DebounceMs, 0)
            .Add(component => component.ItemsProviderFailed, exception => reported = exception)
            .Add(component => component.TextSelector, value => value));

        await cut.Find("input").FocusAsync(new());
        cut.WaitForAssertion(() =>
        {
            Assert.IsType<InvalidOperationException>(reported);
            Assert.NotNull(cut.Find("[role='alert']"));
        });
        await cut.Find("[role='alert'] button").ClickAsync(new());

        cut.WaitForAssertion(() =>
            Assert.Equal("recuperado", cut.Find(".omni-autocomplete-option").TextContent.Trim()));
    }
}
