using Bunit;
using Microsoft.AspNetCore.Components.Web;
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
    public void Open_panel_wires_the_search_box_as_a_combobox_over_the_rows()
    {
        // Focus has to stay in the search box for typing to keep filtering, so the active
        // row is addressed by aria-activedescendant rather than by moving focus onto it.
        var cut = RenderLocal(1);
        cut.Find("button.omni-entity-picker-trigger").Click();

        var search = cut.Find(".omni-entity-picker-panel input.omni-input");
        Assert.Equal("combobox", search.GetAttribute("role"));
        Assert.Equal("list", search.GetAttribute("aria-autocomplete"));

        // Both references have to resolve to elements that exist, or they are dead ends
        // for a screen reader.
        string controls = search.GetAttribute("aria-controls")!;
        string active = search.GetAttribute("aria-activedescendant")!;
        Assert.NotNull(cut.Find($"#{controls}"));
        Assert.NotNull(cut.Find($"#{active}"));
    }

    [Fact]
    public void Arrow_keys_move_the_cursor_without_taking_focus_off_the_search_box()
    {
        var cut = RenderLocal(1);
        cut.Find("button.omni-entity-picker-trigger").Click();

        var search = cut.Find(".omni-entity-picker-panel input.omni-input");
        string first = search.GetAttribute("aria-activedescendant")!;

        search.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var moved = cut.Find(".omni-entity-picker-panel input.omni-input");
        Assert.NotEqual(first, moved.GetAttribute("aria-activedescendant"));
        Assert.Equal(
            moved.GetAttribute("aria-activedescendant"),
            cut.Find(".omni-entity-picker-panel tr[data-omni-grid-cursor='true']").Id);
    }

    [Fact]
    public void Enter_picks_the_row_under_the_cursor_and_closes()
    {
        var cut = RenderLocal(0);
        cut.Find("button.omni-entity-picker-trigger").Click();

        var search = cut.Find(".omni-entity-picker-panel input.omni-input");
        search.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find(".omni-entity-picker-panel input.omni-input")
           .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Empty(cut.FindAll(".omni-entity-picker-panel"));
        Assert.Equal(Produtos[1].Id, cut.Instance.Value);
    }

    [Fact]
    public void Cursor_returns_to_the_top_when_filtering_changes_the_rows()
    {
        // The row it was pointing at is probably gone, and a stale index would leave
        // aria-activedescendant naming an element that no longer exists.
        var cut = RenderLocal(1);
        cut.Find("button.omni-entity-picker-trigger").Click();

        var search = cut.Find(".omni-entity-picker-panel input.omni-input");
        search.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find(".omni-entity-picker-panel input.omni-input").Input("Pão");

        // The grid debounces its search, so settle before reading the wiring back.
        cut.WaitForAssertion(() =>
        {
            var after = cut.Find(".omni-entity-picker-panel input.omni-input");
            string? active = after.GetAttribute("aria-activedescendant");

            // Either nothing is active, or what is named exists — never a dangling id.
            if (string.IsNullOrEmpty(active))
            {
                Assert.Empty(cut.FindAll(".omni-entity-picker-panel tbody tr[data-omni-grid-cursor='true']"));
                return;
            }

            Assert.Equal(active, cut.Find(".omni-entity-picker-panel tr[data-omni-grid-cursor='true']").Id);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Trigger_wears_the_text_field_contract()
    {
        // The control reads as a text box, so it has to be styled as one rather than
        // approximating it: .omni-input is what carries the box, focus ring, hover,
        // disabled and invalid states, and it cannot drift from OmniTextBox while shared.
        var cut = RenderLocal(1);

        var trigger = cut.Find("button.omni-entity-picker-trigger");
        Assert.Contains("omni-input", trigger.ClassList);
    }

    [Fact]
    public void Trigger_always_has_an_id_a_label_can_point_at()
    {
        // InputId is null unless the consumer sets one; without a fallback there is no
        // id for <OmniLabel For="..."> to reference, and the field goes unlabelled.
        var cut = RenderLocal(1);

        Assert.False(string.IsNullOrEmpty(cut.Find("button.omni-entity-picker-trigger").Id));
    }

    [Fact]
    public void Consumer_supplied_InputId_wins_over_the_generated_one()
    {
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.Value, 1)
            .Add(component => component.InputId, "meu-picker"));

        Assert.Equal("meu-picker", cut.Find("button.omni-entity-picker-trigger").Id);
    }

    [Fact]
    public void Clear_sits_inside_the_field_and_carries_an_accessible_name()
    {
        // It used to be a labelled button stranded outside the box. Inside the field it
        // is icon-only, so the name has to come from aria-label.
        var cut = Render<OmniEntityPicker<Produto, int>>(parameters => parameters
            .Add(component => component.Items, Produtos)
            .Add(component => component.KeySelector, produto => produto.Id)
            .Add(component => component.TextSelector, produto => produto.Nome)
            .Add(component => component.Value, 1)
            .Add(component => component.AllowClear, true));

        var clear = cut.Find(".omni-entity-picker-field .omni-entity-picker-clear");
        Assert.False(string.IsNullOrWhiteSpace(clear.GetAttribute("aria-label")));
    }

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

        // Um aria-expanded ausente não é "fechado": é "não abre nada".
        Assert.Equal("false", cut.Find(".omni-entity-picker-trigger").GetAttribute("aria-expanded"));
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
