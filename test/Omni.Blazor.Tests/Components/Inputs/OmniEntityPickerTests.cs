using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>Behavioural contract for local/server entity selection, key binding and resolver lifetime.</summary>
public sealed class OmniEntityPickerTests : TestContextBase
{
    private sealed record Produto(int Id, string Nome);

    private static readonly Produto[] Produtos =
    [
        new(1, "Café"),
        new(2, "Pão")
    ];

    private IRenderedComponent<OmniEntityPicker<Produto, int>> RenderLocal(int value = 0)
        => Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.Value, value));

    [Fact]
    public void Renders_common_surface_and_resolves_a_local_value_without_opening()
    {
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.Value, 1)
            .Add(component => component.Class, "picker-custom")
            .Add(component => component.Style, "max-width:300px")
            .AddUnmatched("data-testid", "produto-picker"));

        var root = cut.Find(".omni-entity-picker");
        Assert.Contains("picker-custom", root.ClassList);
        Assert.Equal("max-width:300px", root.GetAttribute("style"));
        Assert.Equal("produto-picker", root.GetAttribute("data-testid"));
        Assert.Contains("Café", cut.Find(".omni-entity-picker-trigger").TextContent);
    }

    [Fact]
    public void Selecting_and_clearing_round_trips_the_stable_key_and_entity()
    {
        int? selectedKey = null;
        Produto? selectedItem = null;
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.ValueChanged, key => selectedKey = key)
            .Add(component => component.SelectedItemChanged, item => selectedItem = item));

        cut.Find(".omni-entity-picker-trigger").Click();
        Assert.Equal("dialog", cut.Find(".omni-entity-picker-panel").GetAttribute("role"));
        cut.FindAll("tbody tr").Single(row => row.TextContent.Contains("Pão")).Click();

        Assert.Equal(2, selectedKey);
        Assert.Equal(Produtos[1], selectedItem);
        Assert.Empty(cut.FindAll(".omni-entity-picker-panel"));
        Assert.Contains("Pão", cut.Find(".omni-entity-picker-trigger").TextContent);

        cut.Find(".omni-entity-picker-clear").Click();
        Assert.Equal(0, selectedKey);
        Assert.Null(selectedItem);
        Assert.Contains("Selecione um registro", cut.Find(".omni-entity-picker-trigger").TextContent);
    }

    [Fact]
    public void Shared_grid_schema_configures_the_picker_columns()
    {
        DataGridSchema<Produto> schema = DataGridSchema<Produto>.Create(grid => grid
            .Column(produto => produto.Id, column => column.Title("Código"))
            .Column(produto => produto.Nome, column => column.Title("Produto"))
            .Search());
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.GridSchema, schema)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome));

        cut.Find(".omni-entity-picker-trigger").Click();

        Assert.Contains("Código", cut.Find("thead").TextContent);
        Assert.Contains("Produto", cut.Find("thead").TextContent);
        Assert.NotNull(cut.Find(".omni-grid-search"));
    }

    [Fact]
    public async Task External_key_resolution_is_latest_wins_and_cancels_the_superseded_request()
    {
        TaskCompletionSource<CancellationToken> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EntityPickerResolver<Produto, int> resolver = async (key, cancellationToken) =>
        {
            if (key == 1)
            {
                firstStarted.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            return new Produto(key, key == 2 ? "Mais recente" : "Antigo");
        };
        GridDataProvider<Produto> provider = static (_, _) =>
            ValueTask.FromResult(new GridLoadResult<Produto>([], 0));
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.DataProvider, provider)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.ResolveItem, resolver)
            .Add(component => component.Value, 1));
        CancellationToken firstToken = await firstStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        cut.Render(parameters => parameters
            .Add(component => component.DataProvider, provider)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.ResolveItem, resolver)
            .Add(component => component.Value, 2));

        cut.WaitForAssertion(() => Assert.Contains("Mais recente", cut.Markup));
        Assert.True(firstToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Dispose_cancels_an_active_entity_resolution()
    {
        TaskCompletionSource<CancellationToken> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EntityPickerResolver<Produto, int> resolver = async (_, cancellationToken) =>
        {
            started.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        };
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.DataProvider,
                (GridDataProvider<Produto>)((_, _) => ValueTask.FromResult(new GridLoadResult<Produto>([], 0))))
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.ResolveItem, resolver)
            .Add(component => component.Value, 1));
        CancellationToken token = await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        cut.Instance.Dispose();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Exactly_one_source_is_required()
    {
        Assert.ThrowsAny<Exception>(() => Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)));
        Assert.ThrowsAny<Exception>(() => Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.DataProvider,
                (GridDataProvider<Produto>)((_, _) => ValueTask.FromResult(new GridLoadResult<Produto>([], 0))))
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)));
    }
}
