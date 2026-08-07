using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Forms;

/// <summary>Behavioural contract for typed steps, shared EditContext, validation and cancellation.</summary>
public sealed class OmniDataFormWizardTests : TestContextBase
{
    private sealed class Cadastro
    {
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public bool Aceite { get; set; }
    }

    private static readonly DataFormSchema<Cadastro> DadosSchema =
        DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Nome, field => field.Label("Nome").Required("Informe o nome.")));

    private static readonly DataFormSchema<Cadastro> ContatoSchema =
        DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Email, field => field.Label("E-mail").Email()));

    private static readonly DataFormSchema<Cadastro> ConfirmacaoSchema =
        DataFormSchema<Cadastro>.Create(form => form
            .AutoGenerateFields(false)
            .Field(model => model.Aceite, field => field.Label("Aceito").Required("Confirme o aceite.")));

    private static DataFormWizardSchema<Cadastro> CreateSchema(
        Func<Cadastro, CancellationToken, ValueTask<IReadOnlyList<string>>>? validator = null)
        => DataFormWizardSchema<Cadastro>.Create(wizard =>
        {
            wizard.Step("dados", "Dados", DadosSchema, step => step.Description("Identificação"));
            wizard.Step("contato", "Contato", ContatoSchema);
            wizard.Step("confirmacao", "Confirmação", ConfirmacaoSchema, step =>
            {
                if (validator is not null) step.ValidateAsync(validator);
            });
        });

    [Fact]
    public void Schema_is_immutable_and_rejects_ambiguous_auto_or_duplicate_fields()
    {
        DataFormSchema<Cadastro> automatic = DataFormSchema<Cadastro>.Create(_ => { });
        Assert.Throws<InvalidOperationException>(() => DataFormWizardSchema<Cadastro>.Create(
            wizard => wizard.Step("auto", "Automático", automatic)));

        Assert.Throws<InvalidOperationException>(() => DataFormWizardSchema<Cadastro>.Create(wizard =>
        {
            wizard.Step("dados", "Dados", DadosSchema);
            wizard.Step("dados-2", "Dados novamente", DadosSchema);
        }));

        DataFormWizardSchemaBuilder<Cadastro> builder = DataFormWizardSchema<Cadastro>.Builder();
        builder.Step("dados", "Dados", DadosSchema);
        _ = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Step("contato", "Contato", ContatoSchema));
    }

    [Fact]
    public void Forward_navigation_validates_the_step_and_preserves_one_edit_context_when_returning()
    {
        Cadastro model = new();
        int active = -1;
        var cut = Render<OmniDataFormWizard<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.ActiveStepIndexChanged, index => active = index));
        EditContext? context = cut.Instance.CurrentEditContext;

        cut.FindAll("button").Single(button => button.TextContent.Contains("Próximo")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Informe o nome.", cut.Markup));
        Assert.Equal(0, cut.Instance.ActiveStepIndex);

        cut.Find("input[name='Nome']").Input("Ana");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Próximo")).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.ActiveStepIndex));
        Assert.Equal(1, active);
        Assert.Same(context, cut.Instance.CurrentEditContext);

        cut.FindAll("button").Single(button => button.TextContent.Contains("Voltar")).Click();
        cut.WaitForAssertion(() => Assert.Equal(0, cut.Instance.ActiveStepIndex));
        Assert.Equal("Ana", cut.Find("input[name='Nome']").GetAttribute("value"));
        Assert.Same(context, cut.Instance.CurrentEditContext);
    }

    [Fact]
    public void Completion_awaits_step_rules_and_raises_the_shared_edit_context_only_when_valid()
    {
        Cadastro model = new() { Nome = "Ana", Email = "ana@exemplo.com" };
        EditContext? completed = null;
        DataFormWizardSchema<Cadastro> schema = CreateSchema((cadastro, _) =>
            ValueTask.FromResult<IReadOnlyList<string>>(
                cadastro.Aceite ? [] : ["Confirme os dados antes de concluir."]));
        var cut = Render<OmniDataFormWizard<Cadastro>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Schema, schema)
            .Add(component => component.OnCompleted, context => completed = context));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Próximo")).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.ActiveStepIndex));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Próximo")).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.ActiveStepIndex));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Concluir")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Confirme os dados antes de concluir.", cut.Markup));
        Assert.Null(completed);

        cut.Find("input[name='Aceite']").Change(true);
        cut.FindAll("button").Single(button => button.TextContent.Contains("Concluir")).Click();
        cut.WaitForAssertion(() => Assert.Same(cut.Instance.CurrentEditContext, completed));
    }

    [Fact]
    public async Task Dispose_cancels_an_active_step_validator_without_unobserved_work()
    {
        TaskCompletionSource<CancellationToken> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataFormWizardSchema<Cadastro> schema = DataFormWizardSchema<Cadastro>.Create(wizard =>
            wizard.Step("dados", "Dados", DadosSchema, step => step.ValidateAsync(async (_, cancellationToken) =>
            {
                started.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return [];
            })));
        var cut = Render<OmniDataFormWizard<Cadastro>>(parameters => parameters
            .Add(component => component.Model, new Cadastro { Nome = "Ana" })
            .Add(component => component.Schema, schema));

        Task completion = cut.InvokeAsync(() => cut.Instance.CompleteAsync());
        CancellationToken token = await started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        cut.Instance.Dispose();

        await completion.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Accepts_an_external_edit_context_and_splats_the_common_surface()
    {
        Cadastro model = new();
        EditContext context = new(model);
        var cut = Render<OmniDataFormWizard<Cadastro>>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.Schema, CreateSchema())
            .Add(component => component.Class, "wizard-custom")
            .Add(component => component.Style, "max-width:900px")
            .AddUnmatched("data-testid", "cadastro-wizard"));

        var root = cut.Find(".omni-data-form-wizard");
        Assert.Contains("wizard-custom", root.ClassList);
        Assert.Equal("max-width:900px", root.GetAttribute("style"));
        Assert.Equal("cadastro-wizard", root.GetAttribute("data-testid"));
        Assert.Same(context, cut.Instance.CurrentEditContext);
    }
}
