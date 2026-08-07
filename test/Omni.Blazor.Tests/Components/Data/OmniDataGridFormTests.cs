using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for the DataGrid/DataForm CRUD coordinator: immutable
/// typed schema, copy-safe local mutations, provider persistence, custom actions,
/// collection embedding, cancellation and the common Omni surface.
/// </summary>
public sealed class OmniDataGridFormTests : TestContextBase
{
    private sealed class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        public string? Nome { get; set; }

        [EmailAddress, DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [ScaffoldColumn(false)]
        public string? Segredo { get; set; }

        public bool Ativo { get; set; }
    }

    private sealed class Cadastro
    {
        public List<Cliente> Contatos { get; set; } = [];
    }

    private static readonly DataFormSchema<Cliente> FormSchema =
        DataFormSchema<Cliente>.Create(form => form
            .AutoGenerateFields(false)
            .Field(cliente => cliente.Nome, field => field.Label("Nome"))
            .Field(cliente => cliente.Ativo, field => field.Label("Ativo")));

    private static DataGridFormSchema<Cliente, int> CreateSchema(
        Func<DataGridFormActionContext<Cliente, int>, CancellationToken, ValueTask>? action = null)
        => DataGridFormSchema<Cliente, int>.Create(crud =>
        {
            crud.Key(cliente => cliente.Id)
                .Form(FormSchema)
                .Grid(grid => grid
                    .AutoColumnsFromForm(false)
                    .Column(cliente => cliente.Nome, column => column.Filterable())
                    .Column(cliente => cliente.Ativo)
                    .AllowSearch()
                    .AllowPaging(pageSize: 10))
                .Create(create => create
                    .Factory(() => new Cliente { Id = 2 })
                    .Title("Novo cliente"))
                .Edit(edit => edit
                    .Clone(cliente => new Cliente
                    {
                        Id = cliente.Id,
                        Nome = cliente.Nome,
                        Ativo = cliente.Ativo
                    })
                    .Title(cliente => $"Editar — {cliente.Nome}"))
                .Delete(delete => delete
                    .Confirm(cliente => $"Excluir {cliente.Nome}?"));

            if (action is not null)
            {
                crud.Action("Alternar", "refresh-cw", action,
                    options => options.Id("alternar"));
            }
        });

    private static DataGridFormSchema<Cliente, int> CreateBulkSchema(
        Func<DataGridFormBulkActionContext<Cliente, int>, CancellationToken, ValueTask> execute,
        bool confirm = true)
        => DataGridFormSchema<Cliente, int>.Create(crud =>
        {
            crud.Key(cliente => cliente.Id)
                .Form(FormSchema)
                .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
                .BulkAction("Ativar", "check", execute, action =>
                {
                    action.Id("ativar").Variant(ButtonVariant.Primary);
                    if (confirm)
                        action.Confirm(items => $"Ativar {items.Count} cliente(s)?");
                });
        });

    private IRenderedComponent<OmniDataGridForm<Cliente, int>> RenderLocal(
        List<Cliente> items,
        DataGridFormSchema<Cliente, int>? schema = null)
        => Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema ?? CreateSchema())
            .Add(component => component.Items, items));

    [Fact]
    public void Schema_is_typed_immutable_and_infers_only_requested_columns()
    {
        DataGridFormSchemaBuilder<Cliente, int> builder = DataGridFormSchema<Cliente, int>.Builder();
        builder.Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid
                .AutoColumnsFromForm(false)
                .Column(cliente => cliente.Nome, column => column.Title("Cliente")));
        DataGridFormSchema<Cliente, int> schema = builder.Build();

        Assert.Single(schema.Grid.Columns);
        Assert.Equal(nameof(Cliente.Nome), schema.Grid.Columns[0].PropertyName);
        Assert.Equal("Cliente", schema.Grid.Columns[0].Title);
        Assert.Throws<InvalidOperationException>(() => builder.Grid(grid => grid.AllowExport()));
    }

    [Fact]
    public void Grid_accepts_the_shared_DataGrid_schema()
    {
        DataGridSchema<Cliente> gridSchema = DataGridSchema<Cliente>.Create(grid => grid
            .Column(cliente => cliente.Nome, column => column.Title("Cliente"))
            .Column(cliente => cliente.Ativo)
            .Search()
            .ColumnResize());

        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(gridSchema));

        Assert.Equal(2, schema.Grid.Columns.Count);
        Assert.Equal("Cliente", schema.Grid.Columns[0].Title);
        Assert.True(schema.Grid.AllowSearch);
        Assert.True(schema.Grid.AllowColumnResize);
    }

    [Fact]
    public void Auto_columns_use_only_the_explicit_typed_form_fields()
    {
        DataFormSchema<Cliente> formSchema = DataFormSchema<Cliente>.Create(form => form
            .AutoGenerateFields(false)
            .Field(cliente => cliente.Email));
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(
            builder => builder
                .Key(cliente => cliente.Id)
                .Form(formSchema));

        Assert.Contains(schema.Grid.Columns, column => column.PropertyName == nameof(Cliente.Email));
        Assert.DoesNotContain(schema.Grid.Columns, column => column.PropertyName == nameof(Cliente.Segredo));
    }

    [Fact]
    public void Renders_generated_columns_actions_and_common_surface()
    {
        List<Cliente> items = [new() { Id = 1, Nome = "Ana", Ativo = true }];
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Items, items)
            .Add(component => component.Class, "custom-crud")
            .Add(component => component.Style, "min-height:200px")
            .Add(component => component.Attributes,
                new Dictionary<string, object> { ["data-testid"] = "clientes-crud" }));

        var root = cut.Find(".omni-data-grid-form");
        Assert.Contains("custom-crud", root.ClassList);
        Assert.Equal("min-height:200px", root.GetAttribute("style"));
        Assert.Equal("clientes-crud", root.GetAttribute("data-testid"));
        Assert.Contains("Nome", cut.Find("thead").TextContent);
        Assert.Contains("Ativo", cut.Find("thead").TextContent);
        Assert.Contains("Adicionar", cut.Find(".omni-grid-toolbar").TextContent);
        Assert.Contains("Editar", cut.Find("tbody").TextContent);
        Assert.Contains("Remover", cut.Find("tbody").TextContent);
    }

    [Fact]
    public async Task Operation_policies_control_visibility_disabled_state_and_public_entry_points()
    {
        Cliente ativo = new() { Id = 1, Nome = "Ana", Ativo = true };
        Cliente inativo = new() { Id = 2, Nome = "Bruno", Ativo = false };
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
            .Create(create => create
                .Factory(() => new Cliente())
                .VisibleWhen(() => false))
            .Edit(edit => edit
                .Clone(cliente => new Cliente { Id = cliente.Id, Nome = cliente.Nome, Ativo = cliente.Ativo })
                .VisibleWhen(cliente => cliente.Ativo)
                .DisabledWhen(cliente => cliente.Nome == "Ana"))
            .Delete(delete => delete
                .Confirm(cliente => $"Excluir {cliente.Nome}?")
                .VisibleWhen(cliente => !cliente.Ativo)
                .DisabledWhen(cliente => cliente.Nome == "Bruno")));
        var cut = RenderLocal([ativo, inativo], schema);

        Assert.Empty(cut.FindAll(".omni-data-grid-form-create"));
        var edit = Assert.Single(cut.FindAll("tbody button"), button => button.TextContent.Contains("Editar"));
        Assert.True(edit.HasAttribute("disabled"));
        var delete = Assert.Single(cut.FindAll("tbody button"), button => button.TextContent.Contains("Remover"));
        Assert.True(delete.HasAttribute("disabled"));

        await cut.InvokeAsync(() => cut.Instance.BeginCreateAsync());
        await cut.InvokeAsync(() => cut.Instance.BeginEditAsync(inativo));
        Assert.DoesNotContain("omni-data-grid-form-editor", cut.Markup);
    }

    [Fact]
    public void Create_validates_then_commits_to_the_local_list()
    {
        List<Cliente> items = [new() { Id = 1, Nome = "Ana", Ativo = true }];
        var cut = RenderLocal(items);

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Informe o nome.", cut.Markup));

        cut.Find("input[name='Nome']").Input("Bruno");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, items.Count);
            Assert.Equal("Bruno", items[1].Nome);
            Assert.DoesNotContain("omni-data-grid-form-editor", cut.Markup);
            Assert.Contains("Bruno", cut.Find("tbody").TextContent);
        });
    }

    [Fact]
    public void Cancel_never_mutates_the_live_row_and_save_replaces_it()
    {
        Cliente original = new() { Id = 1, Nome = "Ana", Ativo = true };
        List<Cliente> items = [original];
        var cut = RenderLocal(items);

        cut.FindAll("button").Single(button => button.TextContent.Contains("Editar")).Click();
        cut.Find("input[name='Nome']").Input("Alterado sem salvar");
        cut.FindAll(".omni-data-grid-form-editor button")
            .Single(button => button.TextContent.Contains("Cancelar"))
            .Click();

        Assert.Contains("alterações não salvas", cut.Find(".omni-data-grid-form-discard-confirm").TextContent);
        cut.FindAll(".omni-data-grid-form-discard-confirm button")
            .Single(button => button.TextContent.Contains("Continuar editando"))
            .Click();
        Assert.Equal("Alterado sem salvar", cut.Find("input[name='Nome']").GetAttribute("value"));
        cut.FindAll(".omni-data-grid-form-editor button")
            .Single(button => button.TextContent.Contains("Cancelar"))
            .Click();
        cut.FindAll(".omni-data-grid-form-discard-confirm button")
            .Single(button => button.TextContent.Contains("Descartar alterações"))
            .Click();

        Assert.Same(original, items[0]);
        Assert.Equal("Ana", items[0].Nome);

        cut.FindAll("button").Single(button => button.TextContent.Contains("Editar")).Click();
        cut.Find("input[name='Nome']").Input("Ana Maria");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotSame(original, items[0]);
            Assert.Equal("Ana Maria", items[0].Nome);
            Assert.Contains("Ana Maria", cut.Find("tbody").TextContent);
        });
    }

    [Fact]
    public void Delete_requires_confirmation_and_honors_the_minimum_bound()
    {
        List<Cliente> items =
        [
            new() { Id = 1, Nome = "Ana" },
            new() { Id = 2, Nome = "Bruno" }
        ];
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Items, items)
            .Add(component => component.MinimumItems, 1));

        cut.FindAll("button").First(button => button.TextContent.Contains("Remover")).Click();
        Assert.Contains("Excluir Ana?", cut.Find(".omni-data-grid-form-confirm").TextContent);
        cut.Find(".omni-data-grid-form-confirm .omni-btn-danger").Click();

        cut.WaitForAssertion(() => Assert.Single(items));
        var remainingDelete = cut.FindAll("button").Single(button => button.TextContent.Contains("Remover"));
        Assert.True(remainingDelete.HasAttribute("disabled"));
    }

    [Fact]
    public void Custom_action_is_typed_refreshable_and_reports_completion()
    {
        DataGridFormOperationEventArgs<Cliente, int>? completed = null;
        List<Cliente> items = [new() { Id = 1, Nome = "Ana", Ativo = false }];
        DataGridFormSchema<Cliente, int> schema = CreateSchema(async (context, cancellationToken) =>
        {
            context.Item.Ativo = true;
            await context.RefreshAsync(cancellationToken);
        });
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, items)
            .Add(component => component.OperationCompleted, args => completed = args));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Alternar")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(items[0].Ativo);
            Assert.Equal(DataGridFormOperation.Custom, completed?.Operation);
            Assert.Equal("alternar", completed?.ActionId);
        });
    }

    [Fact]
    public void Bulk_action_uses_a_bounded_snapshot_confirms_and_reports_affected_count()
    {
        List<Cliente> items =
        [
            new() { Id = 1, Nome = "Ana" },
            new() { Id = 2, Nome = "Bruno" }
        ];
        DataGridFormOperationEventArgs<Cliente, int>? completed = null;
        IReadOnlyList<int>? keys = null;
        DataGridFormSchema<Cliente, int> schema = CreateBulkSchema(async (context, _) =>
        {
            keys = context.Keys;
            foreach (Cliente item in context.Items) item.Ativo = true;
            await context.ClearSelectionAsync();
        });
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, items)
            .Add(component => component.OperationCompleted, args => completed = args));

        foreach (var checkbox in cut.FindAll("tbody input[type='checkbox']")) checkbox.Change(true);
        Assert.Contains("2 selecionado(s)", cut.Find(".omni-data-grid-form-bulk-actions").TextContent);
        cut.FindAll(".omni-data-grid-form-bulk-actions button")
            .Single(button => button.TextContent.Contains("Ativar"))
            .Click();
        Assert.Contains("Ativar 2 cliente(s)?", cut.Find(".omni-data-grid-form-bulk-confirm").TextContent);
        cut.FindAll(".omni-data-grid-form-bulk-confirm button")
            .Single(button => button.TextContent.Contains("Confirmar"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([1, 2], keys);
            Assert.All(items, item => Assert.True(item.Ativo));
            Assert.Equal(DataGridFormOperation.Bulk, completed?.Operation);
            Assert.Equal("ativar", completed?.ActionId);
            Assert.Equal(2, completed?.AffectedCount);
            Assert.Empty(cut.FindAll(".omni-data-grid-form-bulk-actions"));
        });
    }

    [Fact]
    public async Task Disposing_a_running_bulk_action_cancels_and_observes_its_token()
    {
        TaskCompletionSource<CancellationToken> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataGridFormSchema<Cliente, int> schema = CreateBulkSchema(async (_, cancellationToken) =>
        {
            started.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }, confirm: false);
        var cut = RenderLocal([new Cliente { Id = 1, Nome = "Ana" }], schema);
        cut.Find("tbody input[type='checkbox']").Change(true);

        Task click = cut.InvokeAsync(() => cut.Find(".omni-data-grid-form-bulk-actions button").Click());
        CancellationToken token = await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        cut.Instance.Dispose();

        await click.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Provider_mode_loads_and_persists_create_edit_delete_before_refreshing()
    {
        RecordingProvider provider = new([new Cliente { Id = 1, Nome = "Ana", Ativo = true }]);
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Provider, provider));
        cut.WaitForAssertion(() => Assert.Contains("Ana", cut.Find("tbody").TextContent));

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find("input[name='Nome']").Input("Bruno");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.CreateCalls);
            Assert.Contains("Bruno", cut.Find("tbody").TextContent);
        });

        cut.FindAll("tbody tr")[0].QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Editar"))
            .Click();
        cut.Find("input[name='Nome']").Input("Ana Maria");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.UpdateCalls);
            Assert.Contains("Ana Maria", cut.Find("tbody").TextContent);
        });

        cut.FindAll("tbody tr")[0].QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Remover"))
            .Click();
        cut.Find(".omni-data-grid-form-confirm .omni-btn-danger").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.DeleteCalls);
            Assert.DoesNotContain("Ana Maria", cut.Find("tbody").TextContent);
        });
    }

    [Fact]
    public void Provider_failure_is_observed_reported_and_keeps_the_valid_draft_open()
    {
        InvalidOperationException expected = new("Falha controlada.");
        FailingCreateProvider provider = new(expected);
        DataGridFormOperationFailedEventArgs<Cliente, int>? failure = null;
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Provider, provider)
            .Add(component => component.OperationFailed, args => failure = args));

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find("input[name='Nome']").Input("Ana");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Same(expected, failure?.Exception);
            Assert.Equal(DataGridFormMutationStatus.Failure, failure?.Status);
            Assert.Equal(DataGridFormOperation.Create, failure?.Operation);
            Assert.Contains("Falha controlada.", cut.Find(".omni-data-grid-form-error").TextContent);
            Assert.NotNull(cut.Find(".omni-data-grid-form-editor"));
        });
    }

    [Fact]
    public void Typed_provider_validation_failure_exposes_messages_and_keeps_the_draft_open()
    {
        DataGridFormOperationFailedEventArgs<Cliente, int>? failure = null;
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Provider, new ValidationCreateProvider())
            .Add(component => component.OperationFailed, args => failure = args));

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find("input[name='Nome']").Input("Ana");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(DataGridFormMutationStatus.ValidationFailed, failure?.Status);
            Assert.Contains("Já existe um cliente com este nome.", failure!.Errors);
            Assert.Contains("Já existe um cliente com este nome.", cut.Find(".omni-data-grid-form-error").TextContent);
            Assert.NotNull(cut.Find(".omni-data-grid-form-editor"));
        });
    }

    [Fact]
    public void Optimistic_concurrency_conflict_exposes_the_current_item_without_losing_the_draft()
    {
        Cliente current = new() { Id = 1, Nome = "Ana no servidor", Ativo = true };
        DataGridFormOperationFailedEventArgs<Cliente, int>? failure = null;
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Provider, new ConflictProvider(current))
            .Add(component => component.OperationFailed, args => failure = args));
        cut.WaitForAssertion(() => Assert.Contains("Ana no servidor", cut.Find("tbody").TextContent));

        cut.FindAll("tbody button").Single(button => button.TextContent.Contains("Editar")).Click();
        cut.Find("input[name='Nome']").Input("Minha alteração");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(DataGridFormMutationStatus.Conflict, failure?.Status);
            Assert.Same(current, failure?.CurrentItem);
            Assert.Contains("Registro desatualizado.", cut.Find(".omni-data-grid-form-error").TextContent);
            Assert.Equal("Minha alteração", cut.Find("input[name='Nome']").GetAttribute("value"));
        });
    }

    [Fact]
    public void Provider_refresh_failure_after_commit_closes_the_draft_to_prevent_duplicate_create()
    {
        InvalidOperationException expected = new("Falha ao recarregar.");
        CommittedCreateFailingRefreshProvider provider = new(expected);
        DataGridFormOperationFailedEventArgs<Cliente, int>? failure = null;
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Provider, provider)
            .Add(component => component.OperationFailed, args => failure = args));
        cut.WaitForAssertion(() => Assert.True(provider.LoadCalls > 0));

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find("input[name='Nome']").Input("Ana");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.CreateCalls);
            Assert.Same(expected, failure?.Exception);
            Assert.Equal(DataGridFormMutationStatus.RefreshFailed, failure?.Status);
            Assert.Empty(cut.FindAll(".omni-data-grid-form-editor"));
            Assert.Contains("Falha ao recarregar.", cut.Find(".omni-data-grid-form-error").TextContent);
        });
    }

    [Fact]
    public async Task Dispose_cancels_an_active_custom_action_and_releases_its_token()
    {
        TaskCompletionSource<CancellationToken> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataGridFormSchema<Cliente, int> schema = CreateSchema(async (_, cancellationToken) =>
        {
            started.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        var cut = RenderLocal([new Cliente { Id = 1, Nome = "Ana" }], schema);

        Task click = cut.InvokeAsync(() =>
            cut.FindAll("button").Single(button => button.TextContent.Contains("Alternar")).Click());
        CancellationToken token = await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        cut.Instance.Dispose();

        await click.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Collection_grid_reuses_the_crud_schema_and_parent_validation_pipeline()
    {
        DataGridFormSchema<Cliente, int> itemCrud = CreateSchema();
        DataFormSchema<Cadastro> parentSchema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field
                .Collection<Cliente>(collection => collection
                    .Bounds(1, 4)
                    .Reorderable()
                    .Grid(itemCrud))));
        Cadastro model = new()
        {
            Contatos = [new Cliente { Id = 1, Nome = null }]
        };
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, parentSchema));

        Assert.Single(cut.FindComponents<OmniDataGridForm<Cliente, int>>());
        Assert.False(cut.Instance.Validate());
        Assert.Contains("Item 1: Informe o nome.", cut.Instance.Errors);

        cut.Find(".omni-data-grid-form-create").Click();
        cut.Find("input[name='Nome']").Input("Bruno");
        cut.Find(".omni-data-grid-form-editor .omni-btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, model.Contatos.Count);
            Assert.True(cut.Instance.CurrentEditContext?.IsModified());
        });
    }

    [Fact]
    public async Task Collection_grid_awaits_async_item_validators_during_parent_validation()
    {
        DataFormSchema<Cliente> itemForm = DataFormSchema<Cliente>.Create(form => form
            .AutoGenerateFields(false)
            .Field(cliente => cliente.Nome, field => field.ValidateAsync(async (value, _, cancellationToken) =>
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return value == "reservado" ? "Nome indisponível." : null;
            })));
        DataGridFormSchema<Cliente, int> itemCrud = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(itemForm)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome)));
        DataFormSchema<Cadastro> parentSchema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field.Collection<Cliente>(collection => collection
                .Grid(itemCrud))));
        Cadastro model = new() { Contatos = [new Cliente { Id = 1, Nome = "reservado" }] };
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, parentSchema));

        bool valid = await cut.InvokeAsync(() => cut.Instance.ValidateAsync(
            Xunit.TestContext.Current.CancellationToken));

        Assert.False(valid);
        Assert.Contains("Item 1: Nome indisponível.", cut.Instance.Errors);
    }

    [Fact]
    public async Task Disposing_collection_grid_cancels_an_active_parent_async_validation()
    {
        TaskCompletionSource<CancellationToken> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataFormSchema<Cliente> itemForm = DataFormSchema<Cliente>.Create(form => form
            .AutoGenerateFields(false)
            .Field(cliente => cliente.Nome, field => field.ValidateAsync(async (_, _, cancellationToken) =>
            {
                started.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return null;
            })));
        DataGridFormSchema<Cliente, int> itemCrud = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(itemForm)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome)));
        DataFormSchema<Cadastro> parentSchema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field.Collection<Cliente>(collection => collection
                .Grid(itemCrud))));
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model,
                new Cadastro { Contatos = [new Cliente { Id = 1, Nome = "Ana" }] })
            .Add(component => component.Schema, parentSchema));

        Task<bool> validation = cut.InvokeAsync(() => cut.Instance.ValidateAsync());
        CancellationToken token = await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        cut.FindComponent<OmniDataFormCollectionEditor<Cadastro, List<Cliente>, Cliente>>()
            .Instance.Dispose();

        await validation.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Exactly_one_data_source_is_required()
    {
        DataGridFormSchema<Cliente, int> schema = CreateSchema();
        Assert.ThrowsAny<Exception>(() => Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)));
        Assert.ThrowsAny<Exception>(() => Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, new List<Cliente>())
            .Add(component => component.Provider, new TestProvider())));
    }

    [Fact]
    public void Read_only_local_sources_disable_mutating_operations_before_an_editor_opens()
    {
        IList<Cliente> items = new List<Cliente> { new() { Id = 1, Nome = "Ana" } }.AsReadOnly();
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Items, items));

        Assert.True(cut.FindAll("button").Single(button => button.TextContent.Contains("Adicionar")).HasAttribute("disabled"));
        Assert.True(cut.FindAll("button").Single(button => button.TextContent.Contains("Editar")).HasAttribute("disabled"));
        Assert.True(cut.FindAll("button").Single(button => button.TextContent.Contains("Remover")).HasAttribute("disabled"));
    }

    [Fact]
    public void Collection_grid_rejects_configuration_that_would_be_silently_ignored()
    {
        DataFormSchema<Cliente> competingSchema = DataFormSchema<Cliente>.Create(
            form => form.AutoGenerateFields(false).Field(cliente => cliente.Nome));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            DataFormSchema<Cadastro>.Create(form => form
                .AutoGenerateFields(false)
                .Field(model => model.Contatos, field => field.Collection<Cliente>(collection => collection
                    .ItemSchema(competingSchema)
                    .Grid(CreateSchema())))));

        Assert.Contains("Remove ItemSchema", exception.Message);
    }

    [Fact]
    public void Actions_column_uses_the_native_resizer_and_can_be_frozen()
    {
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid
                .AutoColumnsFromForm(false)
                .Column(cliente => cliente.Nome)
                .AllowColumnResize())
            .Edit(edit => edit.Clone(cliente => new Cliente { Id = cliente.Id, Nome = cliente.Nome })));
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, new List<Cliente> { new() { Id = 1, Nome = "Ana" } })
            .Add(component => component.ActionsWidth, "180px")
            .Add(component => component.ActionsFrozen, FrozenPosition.Right));

        var actionsHeader = cut.FindAll("thead th").First(header => header.TextContent.Contains("Ações"));
        Assert.Single(actionsHeader.QuerySelectorAll(".omni-grid-resizer"));
        Assert.Contains("omni-grid-frozen-right", actionsHeader.ClassList);
        Assert.Contains("right: 0px", actionsHeader.GetAttribute("style"));
        var actionsCell = cut.FindAll("tbody td").Single(cell => cell.TextContent.Contains("Editar"));
        Assert.Contains("omni-grid-frozen-right", actionsCell.ClassList);
    }

    [Fact]
    public async Task Inline_and_overflow_actions_coexist_and_owned_callbacks_are_released_on_dispose()
    {
        bool menuActionExecuted = false;
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
            .Edit(edit => edit.Clone(cliente => new Cliente { Id = cliente.Id, Nome = cliente.Nome }))
            .Delete(delete => delete.Confirm(cliente => $"Excluir {cliente.Nome}?").InMenu())
            .Action("Inline", "bolt", static (_, _) => ValueTask.CompletedTask)
            .Action("No menu", "archive", (_, _) =>
            {
                menuActionExecuted = true;
                return ValueTask.CompletedTask;
            }, action => action.InMenu()));
        var cut = RenderLocal([new Cliente { Id = 1, Nome = "Ana" }], schema);
        ContextMenuService menu = Services.GetRequiredService<ContextMenuService>();

        Assert.Contains("Editar", cut.Find("tbody").TextContent);
        Assert.Contains("Inline", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("No menu", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("Remover", cut.Find("tbody").TextContent);

        cut.Find(".omni-data-grid-form-more-actions").Click();

        Assert.True(menu.IsOpen);
        Assert.Collection(
            menu.Items,
            action => Assert.Equal("No menu", action.Text),
            action =>
            {
                Assert.Equal("Remover", action.Text);
                Assert.True(action.IsDanger);
            });
        await cut.InvokeAsync(() => menu.Items[0].OnClick!());
        Assert.True(menuActionExecuted);

        cut.Instance.Dispose();
        Assert.False(menu.IsOpen);
        Assert.Empty(menu.Items);
    }

    [Fact]
    public void Fluent_builders_expose_action_placement_for_edit_delete_and_custom_actions()
    {
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Edit(edit => edit.Clone(cliente => new Cliente { Id = cliente.Id }).InMenu())
            .Delete(delete => delete.InMenu())
            .Action("Arquivar", "archive", static (_, _) => ValueTask.CompletedTask, action => action.InMenu()));

        Assert.Equal(DataGridFormActionPlacement.Menu, schema.EditOptions!.Placement);
        Assert.Equal(DataGridFormActionPlacement.Menu, schema.DeleteOptions!.Placement);
        Assert.Equal(DataGridFormActionPlacement.Menu, Assert.Single(schema.Actions).Placement);
    }

    [Fact]
    public void Automatic_row_overflow_keeps_the_highest_priority_actions_inline()
    {
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
            .ActionsColumn(column => column
                .Overflow(DataGridFormActionOverflow.Automatic, maximumInlineActions: 2)
                .Menu(ariaLabel: "Outras ações"))
            .Edit(edit => edit
                .Clone(cliente => new Cliente { Id = cliente.Id, Nome = cliente.Nome })
                .Priority(100))
            .Delete(delete => delete.Priority(10).MenuMetadata("Danger zone", description: "Permanent removal"))
            .Action("Destacar", "star", static (_, _) => ValueTask.CompletedTask,
                action => action.Priority(80))
            .Action("Arquivar", "archive", static (_, _) => ValueTask.CompletedTask,
                action => action.Priority(20).MenuMetadata("More", "A", "Moves the row to the archive")));
        var cut = RenderLocal([new Cliente { Id = 1, Nome = "Ana" }], schema);

        string rowText = cut.Find("tbody").TextContent;
        Assert.Contains("Editar", rowText);
        Assert.Contains("Destacar", rowText);
        Assert.DoesNotContain("Arquivar", rowText);
        Assert.DoesNotContain("Remover", rowText);

        cut.Find(".omni-data-grid-form-more-actions").Click();
        ContextMenuService menu = Services.GetRequiredService<ContextMenuService>();
        Assert.Collection(
            menu.Items,
            item =>
            {
                Assert.Equal("Arquivar", item.Text);
                Assert.Equal("More", item.Group);
                Assert.Equal("A", item.Shortcut);
            },
            item =>
            {
                Assert.Equal("Remover", item.Text);
                Assert.Equal("Danger zone", item.Group);
                Assert.True(item.IsDanger);
            });
    }

    [Fact]
    public void Automatic_bulk_overflow_uses_the_same_priority_and_menu_metadata_contract()
    {
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
            .BulkActions(actions => actions
                .Overflow(DataGridFormActionOverflow.Automatic, maximumInlineActions: 1)
                .Menu(ariaLabel: "Outras ações em massa"))
            .BulkAction("Ativar", "check", static (_, _) => ValueTask.CompletedTask,
                action => action.Priority(100))
            .BulkAction("Arquivar", "archive", static (_, _) => ValueTask.CompletedTask,
                action => action.Priority(10).MenuMetadata("Organização", "A", "Arquiva a seleção")));
        var selected = new HashSet<Cliente> { new() { Id = 1, Nome = "Ana" } };
        var cut = Render<OmniDataGridForm<Cliente, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, selected.ToList())
            .Add(component => component.SelectedItems, selected));

        var toolbar = cut.Find(".omni-data-grid-form-bulk-actions");
        Assert.Contains("Ativar", toolbar.TextContent);
        Assert.DoesNotContain("Arquivar", toolbar.TextContent);
        cut.Find(".omni-data-grid-form-bulk-more-actions").Click();

        ContextMenuItem action = Assert.Single(Services.GetRequiredService<ContextMenuService>().Items);
        Assert.Equal("Arquivar", action.Text);
        Assert.Equal("Organização", action.Group);
        Assert.Equal("Arquiva a seleção", action.Description);
    }

    [Fact]
    public async Task Authorization_policies_are_deduplicated_and_support_hide_disable_and_refresh()
    {
        var decisions = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["customers.edit"] = false,
            ["customers.delete"] = false
        };
        var calls = new Dictionary<string, int>(StringComparer.Ordinal);
        Services.AddSingleton<IDataGridFormPolicyEvaluator>(new DelegateDataGridFormPolicyEvaluator((policy, _) =>
        {
            calls[policy] = calls.GetValueOrDefault(policy) + 1;
            return ValueTask.FromResult(decisions[policy]);
        }));
        DataGridFormSchema<Cliente, int> schema = DataGridFormSchema<Cliente, int>.Create(crud => crud
            .Key(cliente => cliente.Id)
            .Form(FormSchema)
            .Grid(grid => grid.AutoColumnsFromForm(false).Column(cliente => cliente.Nome))
            .Edit(edit => edit
                .Clone(cliente => new Cliente { Id = cliente.Id, Nome = cliente.Nome })
                .RequiresPolicy("customers.edit"))
            .Action("Revisar", "eye", static (_, _) => ValueTask.CompletedTask,
                action => action.RequiresPolicy("customers.edit"))
            .Delete(delete => delete.RequiresPolicy(
                "customers.delete",
                DataGridFormUnauthorizedBehavior.Disable)));
        var cut = RenderLocal([new Cliente { Id = 1, Nome = "Ana" }], schema);

        Assert.DoesNotContain("Editar", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("Revisar", cut.Find("tbody").TextContent);
        var remove = cut.FindAll("button").Single(button => button.TextContent.Contains("Remover"));
        Assert.True(remove.HasAttribute("disabled"));
        Assert.Equal(1, calls["customers.edit"]);
        Assert.Equal(1, calls["customers.delete"]);

        decisions["customers.edit"] = true;
        await cut.InvokeAsync(() => cut.Instance.RefreshAuthorizationAsync());

        Assert.Contains("Editar", cut.Find("tbody").TextContent);
        Assert.Contains("Revisar", cut.Find("tbody").TextContent);
        Assert.Equal(2, calls["customers.edit"]);
        Assert.Equal(2, calls["customers.delete"]);
    }

    private sealed class TestProvider : IDataGridFormProvider<Cliente, int>
    {
        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new GridLoadResult<Cliente>([], 0));

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
    }

    private sealed class RecordingProvider(List<Cliente> items) : IDataGridFormProvider<Cliente, int>
    {
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cliente[] window = items.Skip(state.Skip).Take(state.Top).ToArray();
            return ValueTask.FromResult(new GridLoadResult<Cliente>(window, items.Count));
        }

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCalls++;
            items.Add(item);
            return ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));
        }

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateCalls++;
            int index = items.FindIndex(existing => existing.Id == key);
            items[index] = item;
            return ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));
        }

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteCalls++;
            items.RemoveAll(item => item.Id == key);
            return ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
        }
    }

    private sealed class FailingCreateProvider(Exception exception) : IDataGridFormProvider<Cliente, int>
    {
        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new GridLoadResult<Cliente>([], 0));

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromException<DataGridFormMutationResult<Cliente>>(exception);

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
    }

    private sealed class ValidationCreateProvider : IDataGridFormProvider<Cliente, int>
    {
        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new GridLoadResult<Cliente>([], 0));

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.ValidationFailed(
                ["Já existe um cliente com este nome."]));

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
    }

    private sealed class ConflictProvider(Cliente current) : IDataGridFormProvider<Cliente, int>
    {
        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new GridLoadResult<Cliente>([current], 1));

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Conflict(
                current,
                "Registro desatualizado."));

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
    }

    private sealed class CommittedCreateFailingRefreshProvider(Exception exception)
        : IDataGridFormProvider<Cliente, int>
    {
        private bool _refreshFailed;

        public int LoadCalls { get; private set; }
        public int CreateCalls { get; private set; }

        public ValueTask<GridLoadResult<Cliente>> LoadAsync(
            GridState<Cliente> state,
            CancellationToken cancellationToken)
        {
            LoadCalls++;
            if (CreateCalls != 0 && !_refreshFailed)
            {
                _refreshFailed = true;
                return ValueTask.FromException<GridLoadResult<Cliente>>(exception);
            }
            return ValueTask.FromResult(new GridLoadResult<Cliente>([], 0));
        }

        public ValueTask<DataGridFormMutationResult<Cliente>> CreateAsync(
            Cliente item,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            return ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));
        }

        public ValueTask<DataGridFormMutationResult<Cliente>> UpdateAsync(
            int key,
            Cliente item,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Success(item));

        public ValueTask<DataGridFormMutationResult<Cliente>> DeleteAsync(
            int key,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(DataGridFormMutationResult<Cliente>.Deleted());
    }
}
