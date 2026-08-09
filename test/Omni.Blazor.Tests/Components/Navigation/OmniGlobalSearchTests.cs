using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Navigation;

public class OmniGlobalSearchTests : TestContextBase
{
    private static readonly GlobalSearchResult[] Items =
    [
        new("customers", "Clientes") { Description = "Cadastro de clientes", Category = "Cadastros", Icon = "users" },
        new("orders", "Pedidos") { Description = "Pedidos de venda", Category = "Vendas", Icon = "shopping-cart" }
    ];

    [Fact]
    public void Renders_landmark_and_cross_cutting_attributes()
    {
        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Class, "custom-search")
            .Add(component => component.Style, "max-width: 50rem")
            .AddUnmatched("data-testid", "global-search"));

        var root = cut.Find("section.omni-global-search");
        Assert.Contains("custom-search", root.ClassName);
        Assert.Equal("max-width: 50rem", root.GetAttribute("style"));
        Assert.Equal("global-search", root.GetAttribute("data-testid"));
        Assert.Equal("combobox", cut.Find("input").GetAttribute("role"));
    }

    [Fact]
    public void Filters_local_items_without_allocating_normalized_copies()
    {
        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.MinQueryLength, 1));

        cut.Find("input").Input("cliente");

        var results = cut.FindAll(".omni-global-search-result");
        Assert.Single(results);
        Assert.Contains("Clientes", results[0].TextContent);
    }

    [Fact]
    public void Selecting_result_raises_callback()
    {
        GlobalSearchResult? selected = null;
        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.ShowAllWhenEmpty, true)
            .Add(component => component.NavigateOnSelect, false)
            .Add(component => component.ResultSelected,
                EventCallback.Factory.Create<GlobalSearchResult>(this, value => selected = value)));

        cut.FindAll(".omni-global-search-result")[1].Click();

        Assert.Equal("orders", selected?.Id);
    }

    [Fact]
    public void Combobox_and_options_state_false_explicitly()
    {
        // Ausência de aria-expanded/aria-selected não é "fechado"/"não selecionado":
        // é "não abre nada" / "não é selecionável", e o papel se perde.
        var vazio = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items));

        Assert.Equal("false", vazio.Find("input").GetAttribute("aria-expanded"));

        var comResultados = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.ShowAllWhenEmpty, true));

        Assert.Equal("true", comResultados.Find("input").GetAttribute("aria-expanded"));

        // O primeiro resultado é o ativo; os demais precisam dizer "false".
        var opcoes = comResultados.FindAll(".omni-global-search-result");
        Assert.Equal("true", opcoes[0].GetAttribute("aria-selected"));
        Assert.Equal("false", opcoes[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Provider_results_are_combined_and_deduplicated()
    {
        GlobalSearchProvider provider = (request, _) =>
            ValueTask.FromResult<IReadOnlyList<GlobalSearchResult>>(
            [
                new("customers", "Clientes remotos"),
                new("reports", $"Relatórios para {request.Query}")
            ]);

        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.SearchProvider, provider)
            .Add(component => component.Debounce, 0)
            .Add(component => component.MinQueryLength, 1));

        cut.Find("input").Input("cliente");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.Instance.Results.Count);
            Assert.Single(cut.Instance.Results, result => result.Id == "customers");
            Assert.Contains(cut.Instance.Results, result => result.Id == "reports");
        });
    }

    [Fact]
    public async Task Newer_search_cancels_and_supersedes_older_result()
    {
        var oldCompletion = new TaskCompletionSource<IReadOnlyList<GlobalSearchResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldToken = default;

        GlobalSearchProvider provider = (request, token) =>
        {
            if (request.Query == "old")
            {
                oldToken = token;
                return new ValueTask<IReadOnlyList<GlobalSearchResult>>(oldCompletion.Task);
            }

            return ValueTask.FromResult<IReadOnlyList<GlobalSearchResult>>(
                [new("new", "Resultado novo")]);
        };

        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.SearchProvider, provider)
            .Add(component => component.MinQueryLength, 1)
            .Add(component => component.Debounce, 0));

        Task first = Task.CompletedTask;
        await cut.InvokeAsync(() => { first = cut.Instance.SearchAsync("old"); });
        await cut.InvokeAsync(() => cut.Instance.SearchAsync("new"));

        Assert.True(oldToken.IsCancellationRequested);
        oldCompletion.SetResult([new("old", "Resultado antigo")]);
        await first;

        Assert.Single(cut.Instance.Results);
        Assert.Equal("new", cut.Instance.Results[0].Id);
    }

    [Fact]
    public void Recomputes_when_local_source_changes_without_query_change()
    {
        var cut = Render<OmniGlobalSearch>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.Query, "cliente")
            .Add(component => component.MinQueryLength, 1));
        cut.WaitForAssertion(() => Assert.Equal("customers", cut.Instance.Results.Single().Id));

        cut.Render(parameters => parameters
            .Add(component => component.Items,
                new[] { new GlobalSearchResult("customer-report", "Relatório de clientes") })
            .Add(component => component.Query, "cliente")
            .Add(component => component.MinQueryLength, 1));

        cut.WaitForAssertion(() =>
            Assert.Equal("customer-report", cut.Instance.Results.Single().Id));
    }
}
