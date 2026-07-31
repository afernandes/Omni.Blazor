using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Forms;

/// <summary>
/// Behavioural contract for <see cref="OmniDataForm{TModel}"/>: metadata
/// inference, generated Omni editors, model binding, EditContext validation,
/// overrides, events, model replacement and the cross-cutting Omni surface.
/// </summary>
public class OmniDataFormTests : TestContextBase
{
    private enum Perfil
    {
        [Display(Name = "Administrador")]
        Administrador,

        [Display(Name = "Operador de caixa")]
        Operador
    }

    private sealed class Cadastro
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [Display(Name = "Nome completo", Prompt = "Seu nome", Order = 1)]
        [StringLength(80, MinimumLength = 3)]
        public string? Nome { get; set; }

        [Range(18, 120)]
        [Display(Name = "Idade", Order = 2)]
        public int Idade { get; set; } = 18;

        [Display(Name = "Perfil", Order = 3)]
        public Perfil Perfil { get; set; }

        [Display(Name = "Ativo", Order = 4)]
        public bool Ativo { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Observações", Description = "Informações complementares.", Order = 5)]
        public string? Observacoes { get; set; }

        [Editable(false)]
        [Display(Name = "Código", Order = 6)]
        public string Codigo { get; set; } = "CAD-1";

        [ScaffoldColumn(false)]
        public string Segredo { get; set; } = "oculto";

        [Display(AutoGenerateField = false)]
        public string Interno { get; set; } = "interno";

        public object Unsupported { get; set; } = new();
    }

    private sealed class DuplicateDataTypeMetadata
    {
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Phone]
        public string? Telefone { get; set; }

        [Url]
        [DataType(DataType.Password)]
        public string? ExplicitOverride { get; set; }
    }

    private sealed class AdvancedCadastro
    {
        public string? Nome { get; set; }
        public bool Empresa { get; set; }
        public bool Bloqueado { get; set; }
        public Endereco Endereco { get; set; } = new();
        public string? Categoria { get; set; }
    }

    private sealed class Endereco
    {
        public string? Cidade { get; set; }
        public string? Cep { get; set; }
    }

    private sealed record EstadoOpcao(int Id, string Nome);

    private sealed class CadastroLookup
    {
        public int PaisId { get; set; }
        public int? EstadoId { get; set; }
    }

    private sealed class Pedido
    {
        public List<Contato> Contatos { get; set; } = [];
    }

    private sealed class PedidoOpcional
    {
        public List<Contato>? Contatos { get; set; }
    }

    private sealed class Contato
    {
        [Required(ErrorMessage = "Informe o contato.")]
        public string? Nome { get; set; }
    }

    private sealed class CadastroConvencao
    {
        public string? Nome { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Observacao { get; set; }
    }

    private sealed class CadastroConvencaoProfile : IDataFormSchemaProfile<CadastroConvencao>
    {
        public void Configure(DataFormSchemaBuilder<CadastroConvencao> builder)
            => builder.Field(model => model.Observacao, field => field.Label("Perfil"));
    }

    [Fact]
    public void Infers_supported_editors_and_annotation_metadata()
    {
        var cut = RenderForm(new Cadastro());

        string[] labels = cut.FindAll(".omni-field-label")
            .Select(element => element.TextContent.Trim().TrimEnd('*'))
            .ToArray();

        Assert.Equal(
            ["Nome completo", "Idade", "Perfil", "Ativo", "Observações", "Código"],
            labels);
        Assert.Single(cut.FindComponents<OmniNumeric<int>>());
        Assert.Single(cut.FindComponents<OmniSelect<Perfil>>());
        Assert.Single(cut.FindComponents<OmniSwitch>());
        Assert.Single(cut.FindComponents<OmniTextArea>());
        Assert.Empty(cut.FindAll("[name='Segredo']"));
        Assert.Empty(cut.FindAll("[name='Interno']"));
        Assert.Empty(cut.FindAll("[name='Unsupported']"));

        OmniTextBox name = cut.FindComponents<OmniTextBox>()
            .Single(component => component.Instance.Name == nameof(Cadastro.Nome))
            .Instance;
        Assert.Equal("Seu nome", name.Placeholder);
        Assert.Equal(3, name.MinLength);
        Assert.Equal(80, name.MaxLength);

        OmniNumeric<int> age = cut.FindComponent<OmniNumeric<int>>().Instance;
        Assert.Equal(18, age.Min);
        Assert.Equal(120, age.Max);

        OmniTextBox code = cut.FindComponents<OmniTextBox>()
            .Single(component => component.Instance.Name == nameof(Cadastro.Codigo))
            .Instance;
        Assert.True(code.ReadOnly);
    }

    [Fact]
    public void DataType_inference_handles_specialized_and_explicit_attributes_without_ambiguity()
    {
        var cut = Render<OmniDataForm<DuplicateDataTypeMetadata>>(parameters => parameters
            .Add(component => component.Model, new DuplicateDataTypeMetadata()));

        OmniTextBox email = cut.FindComponents<OmniTextBox>()
            .Single(component => component.Instance.Name == nameof(DuplicateDataTypeMetadata.Email))
            .Instance;
        OmniTextBox phone = cut.FindComponents<OmniTextBox>()
            .Single(component => component.Instance.Name == nameof(DuplicateDataTypeMetadata.Telefone))
            .Instance;

        Assert.Equal("email", email.Type);
        Assert.Equal("tel", phone.Type);
        Assert.Equal(
            nameof(DuplicateDataTypeMetadata.ExplicitOverride),
            cut.FindComponent<OmniPassword>().Instance.Name);
    }

    [Fact]
    public void Generated_inputs_write_model_and_raise_FieldChanged()
    {
        var model = new Cadastro();
        DataFormFieldChangedEventArgs<Cadastro>? changed = null;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.FieldChanged, value => changed = value));

        cut.Find("input[name='Nome']").Input("Ana Maria");

        Assert.Equal("Ana Maria", model.Nome);
        Assert.NotNull(changed);
        Assert.Same(model, changed.Model);
        Assert.Equal(nameof(Cadastro.Nome), changed.Property);
        Assert.Equal("Ana Maria", changed.Value);
    }

    [Fact]
    public void Enum_select_uses_Display_labels_and_writes_model()
    {
        var model = new Cadastro();
        var cut = RenderForm(model);

        cut.Find(".omni-select-trigger").Click();
        var options = cut.FindAll(".omni-select-option");

        Assert.Equal("Administrador", options[0].TextContent.Trim());
        Assert.Equal("Operador de caixa", options[1].TextContent.Trim());

        options[1].Click();
        Assert.Equal(Perfil.Operador, model.Perfil);
    }

    [Fact]
    public void Underlying_EditContext_runs_DataAnnotations_and_submit_callbacks()
    {
        var model = new Cadastro { Nome = null };
        var valid = 0;
        var invalid = 0;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.OnValidSubmit, (EditContext _) => valid++)
            .Add(component => component.OnInvalidSubmit, (EditContext _) => invalid++));

        cut.Find("form").Submit();

        Assert.Equal(0, valid);
        Assert.Equal(1, invalid);
        Assert.False(cut.Instance.IsValid);
        Assert.Contains("Informe o nome.", cut.Instance.Errors);
        Assert.Contains("Informe o nome.", cut.Find(".omni-validation-summary-list").TextContent);

        cut.Find("input[name='Nome']").Input("Ana Maria");
        cut.Find("form").Submit();

        Assert.Equal(1, valid);
        Assert.Equal(1, invalid);
        Assert.True(cut.Instance.IsValid);
    }

    [Fact]
    public void ChildContent_supports_name_based_Omni_validators()
    {
        RenderFragment validator = builder =>
        {
            builder.OpenComponent<OmniRequiredValidator>(0);
            builder.AddAttribute(1, nameof(OmniRequiredValidator.Component), nameof(Cadastro.Nome));
            builder.AddAttribute(2, nameof(OmniRequiredValidator.Text), "Nome exigido pelo validator.");
            builder.CloseComponent();
        };

        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro { Nome = null })
            .Add(component => component.AddDataAnnotationsValidator, false)
            .Add(component => component.ChildContent, validator));

        cut.Find("form").Submit();

        Assert.False(cut.Instance.IsValid);
        Assert.Contains("Nome exigido pelo validator.", cut.Instance.Errors);
        Assert.Contains("Nome exigido pelo validator.", cut.Find(".omni-validation-summary-list").TextContent);
    }

    [Fact]
    public void Explicit_fields_override_inference_and_can_define_exact_surface()
    {
        DataFormSchema<Cadastro> schema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Observacoes, field => field
                .Label("Resumo")
                .Span(2)
                .Text(editor => editor.Clearable()))
            .Field(model => model.Nome, field => field
                .Label("Nome preferido")
                .Visible(false)));

        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.Columns, 2)
            .Add(component => component.Schema, schema));

        Assert.Single(cut.FindAll(".omni-data-form-cell"));
        Assert.Contains("Resumo", cut.Find(".omni-field-label").TextContent);
        Assert.Contains("--omni-data-form-span: 2", cut.Find(".omni-data-form-cell").GetAttribute("style"));
        Assert.True(cut.FindComponent<OmniTextBox>().Instance.Clearable);
        Assert.Empty(cut.FindAll("[name='Nome']"));
    }

    [Fact]
    public void Custom_template_receives_model_property_type_and_EditContext()
    {
        DataFormFieldContext<Cadastro, string>? received = null;
        RenderFragment<DataFormFieldContext<Cadastro, string>> template = context => builder =>
        {
            received = context;
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "custom-editor");
            builder.AddContent(2, context.Value?.ToString());
            builder.CloseElement();
        };

        var model = new Cadastro { Codigo = "CAD-42" };
        DataFormSchema<Cadastro> schema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(value => value.Codigo, field => field.Template(template)));

        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema));

        Assert.Equal("CAD-42", cut.Find("[data-testid='custom-editor']").TextContent);
        Assert.NotNull(received);
        Assert.Same(model, received.Model);
        Assert.Equal(nameof(Cadastro.Codigo), received.Property);
        Assert.Same(model, received.EditContext.Model);
    }

    [Fact]
    public async Task Typed_template_ValueChanged_writes_model_and_raises_FieldChanged()
    {
        DataFormFieldContext<Cadastro, string?>? received = null;
        RenderFragment<DataFormFieldContext<Cadastro, string?>> template = context => builder =>
        {
            received = context;
            builder.OpenElement(0, "span");
            builder.CloseElement();
        };
        DataFormSchema<Cadastro> schema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.Template(template)));
        DataFormFieldChangedEventArgs<Cadastro>? changed = null;
        var model = new Cadastro();
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema)
            .Add(component => component.FieldChanged, value => changed = value));

        Assert.NotNull(received);
        await cut.InvokeAsync(() => received.ValueChanged.InvokeAsync("Nome pelo template"));

        Assert.Equal("Nome pelo template", model.Nome);
        Assert.Equal(nameof(Cadastro.Nome), changed?.Property);
    }

    [Fact]
    public void Replacing_model_rebinds_expressions_without_mutating_old_instance()
    {
        var first = new Cadastro { Nome = "Primeiro" };
        var second = new Cadastro { Nome = "Segundo" };
        var cut = RenderForm(first);

        cut.Render(parameters => parameters.Add(component => component.Model, second));
        cut.Find("input[name='Nome']").Input("Atualizado");

        Assert.Equal("Primeiro", first.Nome);
        Assert.Equal("Atualizado", second.Nome);
    }

    [Fact]
    public void Disabled_and_ReadOnly_propagate_to_generated_editors()
    {
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.Disabled, true)
            .Add(component => component.ReadOnly, true));

        Assert.All(cut.FindComponents<OmniTextBox>(), component =>
        {
            Assert.True(component.Instance.Disabled);
            Assert.True(component.Instance.ReadOnly);
        });
        Assert.True(cut.FindComponent<OmniSwitch>().Instance.Disabled);
        Assert.Contains("omni-data-form-disabled", cut.Find(".omni-data-form").ClassName);
        Assert.True(cut.Find("button[type='submit']").HasAttribute("disabled"));
    }

    [Fact]
    public void Applies_Class_Style_Attributes_and_custom_actions_to_root()
    {
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.Class, "custom-form")
            .Add(component => component.Style, "max-width: 720px")
            .Add(component => component.Actions, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-testid", "actions");
                builder.CloseElement();
            })
            .AddUnmatched("data-testid", "data-form"));

        var root = cut.Find("[data-testid='data-form']");
        Assert.Contains("custom-form", root.ClassName);
        Assert.Equal("max-width: 720px", root.GetAttribute("style"));
        Assert.NotNull(cut.Find("[data-testid='actions']"));
        Assert.Empty(cut.FindAll("button[type='submit']"));
    }

    [Fact]
    public async Task Forwards_cancellable_async_validation()
    {
        var model = new Cadastro { Nome = "Ana Maria" };
        var called = false;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.ValidationAsyncWithCancellation,
                (EditContext context, ValidationMessageStore store, CancellationToken cancellationToken) =>
                {
                    called = true;
                    Assert.False(cancellationToken.IsCancellationRequested);
                    store.Add(new FieldIdentifier(context.Model, nameof(Cadastro.Nome)), "Erro remoto.");
                    return Task.CompletedTask;
                }));

        bool result = await cut.Instance.ValidateAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.True(called);
        Assert.False(result);
        Assert.Contains("Erro remoto.", cut.Instance.Errors);
    }

    [Fact]
    public void Duplicate_or_non_property_schema_fields_fail_fast()
    {
        Exception duplicateError = Assert.ThrowsAny<Exception>(() =>
            DataFormSchema<Cadastro>.Create(form => form
                .Field(model => model.Nome)
                .Field(model => model.Nome)));
        Assert.Contains("declared more than once", duplicateError.ToString());

        Exception expressionError = Assert.ThrowsAny<Exception>(() =>
            DataFormSchema<Cadastro>.Create(form => form
                .Field(model => model.Nome!.Trim())));
        Assert.Contains("select one readable", expressionError.ToString());
    }

    [Fact]
    public void Schema_builder_applies_typed_editor_options_and_is_immutable_after_build()
    {
        DataFormSchemaBuilder<Cadastro> builder = DataFormSchema<Cadastro>.Builder();
        builder.Field(model => model.Idade, field => field.Numeric(editor => editor
            .Min(21)
            .Max(90)
            .Step(2)
            .Prefix("~")));
        DataFormSchema<Cadastro> schema = builder.Build();

        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.Schema, schema));

        OmniNumeric<int> numeric = cut.FindComponent<OmniNumeric<int>>().Instance;
        Assert.Equal(21, numeric.Min);
        Assert.Equal(90, numeric.Max);
        Assert.Equal(2, numeric.Step);
        Assert.Equal("~", numeric.Prefix);
        Assert.Equal(1, schema.Count);
        Assert.Throws<InvalidOperationException>(() => builder.AutoGenerateFields(false));
    }

    [Fact]
    public void Schema_builder_rejects_incompatible_editor_during_configuration()
    {
        Exception error = Assert.Throws<InvalidOperationException>(() =>
            DataFormSchema<Cadastro>.Create(form => form
                .Field(model => model.Idade, field => field.Email())));

        Assert.Contains(nameof(Cadastro.Idade), error.Message);
    }

    [Fact]
    public void Schema_Required_adds_real_validation_without_DataAnnotations()
    {
        DataFormSchema<Cadastro> schema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Observacoes, field => field
                .Required("Observações obrigatórias.")));
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.Schema, schema));

        bool valid = cut.Instance.Validate();

        Assert.False(valid);
        Assert.Contains("Observações obrigatórias.", cut.Instance.Errors);
    }

    [Fact]
    public void Accepts_external_EditContext_and_preserves_its_identity()
    {
        var model = new Cadastro { Nome = "Ana Maria" };
        var editContext = new EditContext(model);
        EditContext? submittedContext = null;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.OnValidSubmit, context => submittedContext = context));

        cut.Find("input[name='Nome']").Input("Beatriz Lima");
        cut.Find("form").Submit();

        Assert.Equal("Beatriz Lima", model.Nome);
        Assert.Same(editContext, submittedContext);
        Assert.Same(editContext, cut.Instance.CurrentEditContext);
    }

    [Fact]
    public void Model_and_EditContext_are_mutually_exclusive()
    {
        var model = new Cadastro();
        var editContext = new EditContext(model);

        Exception both = Assert.ThrowsAny<Exception>(() =>
            Render<OmniDataForm<Cadastro>>(parameters => parameters
                .Add(component => component.Model, model)
                .Add(component => component.EditContext, editContext)));
        Assert.Contains("not both", both.ToString());

        Exception neither = Assert.ThrowsAny<Exception>(() =>
            Render<OmniDataForm<Cadastro>>());
        Assert.Contains("requires either", neither.ToString());
    }

    [Fact]
    public void Schema_supports_nested_groups_responsive_layout_and_nested_binding()
    {
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Layout(layout => layout.Columns(1).Columns(Breakpoint.Md, 3).RowGap("12px"))
            .Group("Contato", group => group
                .Id("contato")
                .Description("Dados usados para contato e entrega.")
                .Layout(layout => layout.Columns(1).Columns(Breakpoint.Md, 2))
                .Field(model => model.Nome)
                .Group("Endereço", address => address
                    .Id("endereco")
                    .Layout(layout => layout.Columns(1).Columns(Breakpoint.Lg, 2))
                    .Field(model => model.Endereco.Cidade, field => field.Span(Breakpoint.Lg, 2))
                    .Field(model => model.Endereco.Cep))));
        var model = new AdvancedCadastro();
        DataFormFieldChangedEventArgs<AdvancedCadastro>? changed = null;

        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema)
            .Add(component => component.FieldChanged, args => changed = args));

        Assert.Equal(2, cut.FindAll("fieldset.omni-data-form-group").Count);
        Assert.Contains("--omni-data-form-columns-md: 3", cut.Find(".omni-data-form > form > .omni-data-form-grid").GetAttribute("style"));
        Assert.Contains(
            "--omni-data-form-columns-md: 2",
            cut.FindAll("fieldset.omni-data-form-group > .omni-data-form-grid")[0].GetAttribute("style"));

        var city = cut.Find("input[name='Cidade']");
        city.Input("Curitiba");

        Assert.Equal("Curitiba", model.Endereco.Cidade);
        Assert.Equal("Endereco.Cidade", changed?.Property);
        Assert.NotNull(cut.Find($"label[for='{city.Id}']"));
        DataFormFieldState? state = cut.Instance.GetFieldState(value => value.Endereco.Cidade);
        Assert.NotNull(state);
        Assert.True(state.IsTouched);
        Assert.True(state.IsModified);
    }

    [Fact]
    public void Conditional_rules_are_reevaluated_without_rebuilding_the_schema()
    {
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field
                .EnabledWhen(model => !model.Bloqueado)
                .RequiredWhen(model => model.Empresa, "Nome empresarial obrigatório."))
            .Field(model => model.Categoria, field => field.VisibleWhen(model => model.Empresa)));
        var model = new AdvancedCadastro { Bloqueado = true };
        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema));

        Assert.True(cut.Find("input[name='Nome']").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("input[name='Categoria']"));

        model.Bloqueado = false;
        model.Empresa = true;
        cut.Render();

        Assert.False(cut.Find("input[name='Nome']").HasAttribute("disabled"));
        Assert.NotEmpty(cut.FindAll("input[name='Categoria']"));
        Assert.False(cut.Instance.Validate());
        Assert.Contains("Nome empresarial obrigatório.", cut.Instance.Errors);
    }

    [Fact]
    public async Task Typed_field_validation_is_cancellable_latest_wins_and_updates_state()
    {
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.ValidateAsync(async (value, _, cancellationToken) =>
            {
                await Task.Delay(value == "lento" ? 200 : 1, cancellationToken);
                return value == "válido" ? null : "Nome remoto inválido.";
            })));
        var model = new AdvancedCadastro { Nome = "lento" };
        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema));

        Task<DataFormValidationResult> oldValidation = cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);
        model.Nome = "válido";
        Task<DataFormValidationResult> latestValidation = cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(DataFormValidationStatus.Valid, (await latestValidation).Status);
        Assert.Equal(DataFormValidationStatus.Superseded, (await oldValidation).Status);
        Assert.True(cut.Instance.GetFieldState(value => value.Nome)?.IsValid);
        Assert.DoesNotContain("Nome remoto inválido.", cut.Instance.Errors);
    }

    [Fact]
    public async Task Disposal_cancels_inflight_field_validation_and_releases_the_operation()
    {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.ValidateAsync(async (_, _, cancellationToken) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return null;
            })));
        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, new AdvancedCadastro())
            .Add(component => component.Schema, schema));

        Task<DataFormValidationResult> validation = cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        await cut.Instance.DisposeAsync();

        Assert.Equal(
            DataFormValidationStatus.Canceled,
            (await validation.WaitAsync(Xunit.TestContext.Current.CancellationToken)).Status);
        cut.Dispose();
    }

    [Fact]
    public async Task Select_builder_forwards_bounded_async_provider()
    {
        OmniItemsRequest? request = null;
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Categoria, field => field.Select(select => select.ItemsProvider(
                (value, _) =>
                {
                    request = value;
                    return ValueTask.FromResult(new OmniItemsPage<string?>(["A", "B"], 2));
                },
                pageSize: 2,
                maxItems: 4))));
        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, new AdvancedCadastro())
            .Add(component => component.Schema, schema));

        cut.Find(".omni-select-trigger").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".omni-select-option").Count));

        Assert.NotNull(request);
        Assert.Equal(2, request.Value.Take);
    }

    [Fact]
    public void Dependency_injection_can_replace_inferred_editor_by_value_type()
    {
        Services.AddOmniDataFormEditor<string?, OmniTextBox>();
        DataFormSchema<AdvancedCadastro> schema = DataFormSchema<AdvancedCadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome));

        var cut = Render<OmniDataForm<AdvancedCadastro>>(parameters => parameters
            .Add(component => component.Model, new AdvancedCadastro())
            .Add(component => component.Schema, schema));

        Assert.Single(cut.FindComponents<OmniTextBox>());
    }

    [Fact]
    public async Task Focus_first_invalid_uses_scoped_scroll_and_focus_services()
    {
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro()));
        cut.Instance.Validate();

        Assert.True(await cut.Instance.FocusFirstInvalidAsync());
        JSInterop.VerifyInvoke("omniBlazor.scrollIntoView");
        JSInterop.VerifyInvoke("omniBlazor.focusElement");
    }

    [Fact]
    public async Task ValidateFieldAsync_includes_DataAnnotations_and_returns_typed_status()
    {
        var model = new Cadastro { Nome = null };
        var cut = RenderForm(model);

        DataFormValidationResult invalid = await cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(DataFormValidationStatus.Invalid, invalid.Status);
        Assert.Contains("Informe o nome.", invalid.Errors);

        model.Nome = "Ana Maria";
        DataFormValidationResult valid = await cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(DataFormValidationStatus.Valid, valid.Status);
        Assert.Empty(valid.Errors);
    }

    [Fact]
    public void Generated_label_targets_the_real_focusable_input_even_with_an_affix_wrapper()
    {
        DataFormSchema<Cadastro> schema = DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.Text(editor => editor.Clearable())));
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro { Nome = "Ana" })
            .Add(component => component.Schema, schema));

        var input = cut.Find("input[name='Nome']");
        var label = cut.Find("label.omni-field-label");

        Assert.False(string.IsNullOrWhiteSpace(input.Id));
        Assert.Equal(input.Id, label.GetAttribute("for"));
        Assert.Null(cut.Find(".omni-input-group").GetAttribute("id"));
    }

    [Fact]
    public async Task Publishes_field_and_aggregate_state_and_summary_links_focus_the_editor()
    {
        DataFormFieldStateChangedEventArgs<Cadastro>? fieldState = null;
        DataFormValidationStateChangedEventArgs<Cadastro>? validationState = null;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.FieldStateChanged, args => fieldState = args)
            .Add(component => component.ValidationStateChanged, args => validationState = args));

        DataFormValidationResult result = await cut.Instance.ValidateFieldAsync(
            value => value.Nome,
            Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.NotNull(validationState));

        Assert.True(result.IsInvalid);
        Assert.NotNull(fieldState);
        Assert.Equal(nameof(Cadastro.Nome), fieldState.State.PropertyPath);
        Assert.False(validationState!.IsValid);
        Assert.False(validationState.IsValidating);

        cut.Find(".omni-data-form-validation-link").Click();
        var focus = JSInterop.VerifyInvoke("omniBlazor.focusElement");
        Assert.EndsWith("-field-nome-input", focus.Arguments[0]?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Typed_lookup_maps_items_to_values_caches_pages_and_invalidates_dependencies()
    {
        int providerCalls = 0;
        int? observedCountry = null;
        DataFormSchema<CadastroLookup> schema = DataFormSchema<CadastroLookup>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.PaisId)
            .Field(model => model.EstadoId, field => field.Select<EstadoOpcao>(
                item => item.Id,
                item => item.Nome,
                lookup => lookup
                    .DependsOn(model => model.PaisId)
                    .CacheEntries(2)
                    .ItemsProvider((request, _) =>
                    {
                        providerCalls++;
                        observedCountry = (int?)request.Dependencies[nameof(CadastroLookup.PaisId)];
                        EstadoOpcao option = request.Model.PaisId == 1
                            ? new EstadoOpcao(11, "Paraná")
                            : new EstadoOpcao(22, "Bahia");
                        return ValueTask.FromResult(new OmniItemsPage<EstadoOpcao>([option], 1));
                    }))));
        var model = new CadastroLookup { PaisId = 1 };
        var cut = Render<OmniDataForm<CadastroLookup>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema));

        cut.Find(".omni-select-trigger").Click();
        cut.WaitForAssertion(() => Assert.Equal("Paraná", cut.Find(".omni-select-option").TextContent.Trim()));
        cut.Find(".omni-select-option").Click();
        Assert.Equal(11, model.EstadoId);

        cut.Find(".omni-select-trigger").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".omni-select-option")));
        Assert.Equal(1, providerCalls);
        cut.Find(".omni-select-option").Click();

        model.PaisId = 2;
        await cut.InvokeAsync(() => cut.Instance.CurrentEditContext!.NotifyFieldChanged(
            new FieldIdentifier(model, nameof(CadastroLookup.PaisId))));
        Assert.Null(model.EstadoId);

        cut.Find(".omni-select-trigger").Click();
        cut.WaitForAssertion(() => Assert.Equal("Bahia", cut.Find(".omni-select-option").TextContent.Trim()));
        Assert.Equal(2, providerCalls);
        Assert.Equal(2, observedCountry);
    }

    [Fact]
    public void Schema_composition_profiles_and_conventions_are_deterministic()
    {
        DataFormSchema<CadastroConvencao> shared = DataFormSchema<CadastroConvencao>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.Label("Base")));
        DataFormSchema<CadastroConvencao> schema = DataFormSchema<CadastroConvencao>.Create(form => form
            .AutoGenerateFields(false)
            .Layout(layout => layout.Columns(2))
            .Include(shared)
            .Override(model => model.Nome, field => field.Label("Sobrescrito").Span(1))
            .IncludeFragment(fragment => fragment.Field(model => model.Email))
            .Apply(new CadastroConvencaoProfile())
            .ConventionFor<string?>(convention => convention.Hint("Texto padrão").Span(2))
            .ConventionForAttribute<EmailAddressAttribute>(convention => convention.Label("E-mail convencional")));

        var cut = Render<OmniDataForm<CadastroConvencao>>(parameters => parameters
            .Add(component => component.Model, new CadastroConvencao())
            .Add(component => component.Schema, schema));

        string[] labels = cut.FindAll(".omni-field-label")
            .Select(label => label.TextContent.Trim())
            .ToArray();
        Assert.Equal(["E-mail convencional", "Perfil", "Sobrescrito"], labels.Order().ToArray());
        Assert.Equal(3, cut.FindAll(".omni-field-hint").Count);
        var nameCell = cut.FindAll(".omni-data-form-cell")
            .Single(cell => cell.TextContent.Contains("Sobrescrito", StringComparison.Ordinal));
        Assert.Contains("--omni-data-form-span: 1", nameCell.GetAttribute("style"));
        Assert.All(
            cut.FindAll(".omni-data-form-cell").Where(cell => !ReferenceEquals(cell, nameCell)),
            cell => Assert.Contains("--omni-data-form-span: 2", cell.GetAttribute("style")));
    }

    [Fact]
    public void Explicit_visibility_overrides_a_convention_while_unspecified_fields_inherit_it()
    {
        DataFormSchema<CadastroConvencao> schema = DataFormSchema<CadastroConvencao>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.Visible())
            .Field(model => model.Email)
            .ConventionFor<string?>(convention => convention.Visible(false)));

        var cut = Render<OmniDataForm<CadastroConvencao>>(parameters => parameters
            .Add(component => component.Model, new CadastroConvencao())
            .Add(component => component.Schema, schema));

        Assert.Single(cut.FindAll(".omni-data-form-cell"));
        Assert.NotNull(cut.Find("input[name='Nome']"));
    }

    [Fact]
    public void Collection_editor_uses_embedded_subforms_bounds_indexed_validation_and_stable_reorder()
    {
        DataFormSchema<Contato> itemSchema = DataFormSchema<Contato>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome));
        DataFormSchema<Pedido> schema = DataFormSchema<Pedido>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field.Collection<Contato>(collection => collection
                .ItemSchema(itemSchema)
                .CreateItem(() => new Contato())
                .Bounds(1, 3)
                .Reorderable())));
        var model = new Pedido();
        var cut = Render<OmniDataForm<Pedido>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema));

        Assert.False(cut.Instance.Validate());
        Assert.Contains("pelo menos 1", cut.Instance.Errors.Single());

        cut.Find(".omni-data-form-collection-add").Click();
        Assert.Single(model.Contatos);
        Assert.Single(cut.FindAll("form"));

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains(
            "Item 1: Informe o contato.",
            cut.Find(".omni-validation-summary-list").TextContent));

        cut.Find("input[name='Nome']").Input("Primeiro");
        cut.Find(".omni-data-form-collection-add").Click();
        cut.FindAll("input[name='Nome']")[1].Input("Segundo");
        cut.FindAll("button[aria-label='Mover para cima']")[1].Click();

        Assert.Equal("Segundo", model.Contatos[0].Nome);
        Assert.Equal("Primeiro", model.Contatos[1].Nome);
    }

    [Fact]
    public void Collection_editor_releases_parent_validation_messages_and_handlers_on_dispose()
    {
        DataFormSchema<Contato> itemSchema = DataFormSchema<Contato>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome));
        DataFormSchema<Pedido> schema = DataFormSchema<Pedido>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field.Collection<Contato>(collection => collection
                .ItemSchema(itemSchema))));
        var model = new Pedido { Contatos = [new Contato()] };
        var editContext = new EditContext(model);
        var cut = Render<OmniDataForm<Pedido>>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.Schema, schema));
        var collection = cut.FindComponent<OmniDataFormCollectionEditor<Pedido, List<Contato>, Contato>>();

        editContext.Validate();
        Assert.Contains(editContext.GetValidationMessages(), message => message.StartsWith("Item 1:", StringComparison.Ordinal));

        collection.Instance.Dispose();
        Assert.DoesNotContain(editContext.GetValidationMessages(), message => message.StartsWith("Item 1:", StringComparison.Ordinal));

        editContext.Validate();
        Assert.DoesNotContain(editContext.GetValidationMessages(), message => message.StartsWith("Item 1:", StringComparison.Ordinal));
        cut.Dispose();
    }

    [Fact]
    public void Null_collection_with_item_creation_requires_an_explicit_collection_factory()
    {
        DataFormSchema<PedidoOpcional> schema = DataFormSchema<PedidoOpcional>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Contatos, field => field.Collection<Contato>(collection => collection
                .CreateItem(() => new Contato()))));

        Exception error = Assert.ThrowsAny<Exception>(() =>
            Render<OmniDataForm<PedidoOpcional>>(parameters => parameters
                .Add(component => component.Model, new PedidoOpcional())
                .Add(component => component.Schema, schema)));

        Assert.Contains("Configure CreateCollection", error.ToString());
    }

    [Fact]
    public void Development_diagnostics_can_be_observed_and_rendered()
    {
        IReadOnlyList<DataFormDiagnostic>? observed = null;
        var cut = Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro())
            .Add(component => component.ShowDiagnostics, true)
            .Add(component => component.DiagnosticsChanged, value => observed = value));

        cut.WaitForAssertion(() => Assert.NotNull(observed));
        Assert.Contains(observed!, diagnostic => diagnostic.Code == "DF001");
        Assert.Contains("DF001", cut.Find(".omni-data-form-diagnostics").TextContent);
    }

    private IRenderedComponent<OmniDataForm<Cadastro>> RenderForm(Cadastro model)
        => Render<OmniDataForm<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model));
}
