using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Omni.Blazor.Components;

/// <summary>
/// Base para validators "irmãos" no estilo Radzen. Toda a mecânica de
/// integração com <see cref="EditContext"/> + <see cref="ValidationMessageStore"/>
/// + descoberta do input alvo via <see cref="IOmniFormRegistry"/> mora aqui;
/// subclasses só precisam implementar <see cref="Validate"/>.
///
/// <para>Padrão de uso:</para>
/// <code>
/// &lt;OmniForm Model="user"&gt;
///     &lt;OmniTextBox Name="email" @bind-Value="user.Email" /&gt;
///     &lt;OmniRequiredValidator Component="email" Text="Email é obrigatório." /&gt;
///     &lt;OmniEmailValidator Component="email" Text="Email inválido." /&gt;
/// &lt;/OmniForm&gt;
/// </code>
///
/// <para>Por que esse padrão?</para>
/// <list type="bullet">
///   <item>Múltiplas regras por campo sem if/else aninhado.</item>
///   <item>Não polui o model com Func&lt;…&gt; fields.</item>
///   <item>Convive com DataAnnotations e com <see cref="FormComponent{T}.Validation"/>.</item>
///   <item>Pluggable — criar <c>OmniCpfValidator</c> é override de 1 método.</item>
/// </list>
/// </summary>
public abstract class OmniValidatorBase : ComponentBase, IDisposable
{
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;

    /// <summary>EditContext cascateado pelo OmniForm/EditForm pai.</summary>
    [CascadingParameter] protected EditContext? CurrentEditContext { get; set; }

    /// <summary>Registry de inputs cascateado pelo OmniForm pai.</summary>
    [CascadingParameter] protected IOmniFormRegistry? FormRegistry { get; set; }

    /// <summary>Nome do input alvo. Match com <c>Name</c> do OmniTextBox/etc.</summary>
    [Parameter, EditorRequired] public string Component { get; set; } = string.Empty;

    /// <summary>Mensagem exibida quando a validação falha.</summary>
    [Parameter] public string Text { get; set; } = "Valor inválido.";

    /// <summary>Quando true, mostra a mensagem inline embaixo do form (default false — relies on OmniFormField/Summary).</summary>
    [Parameter] public bool ShowMessage { get; set; }

    /// <summary>Estilo inline opcional do span de mensagem (quando ShowMessage=true).</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Implementação concreta: retorna true quando o valor passa a validação.</summary>
    protected abstract bool Validate(IOmniFormComponent component);

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} requer um EditContext (use dentro de <OmniForm> ou <EditForm>).");
        }
        if (FormRegistry is null)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} precisa estar dentro de um <OmniForm>.");
        }

        _editContext = CurrentEditContext;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnFieldChanged        += OnFieldChanged;
        _editContext.OnValidationRequested += OnValidationRequested;
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        var target = FormRegistry?.FindComponent(Component);
        if (target is null) return;
        if (!ShouldRevalidate(e, target)) return;
        RunValidation(target);
    }

    /// <summary>
    /// Decide se a validação deve rodar quando um field muda. Default:
    /// só se o próprio target mudou. CompareValidator sobrescreve para
    /// também rodar quando o campo de referência muda.
    /// </summary>
    protected virtual bool ShouldRevalidate(FieldChangedEventArgs e, IOmniFormComponent target)
        => e.FieldIdentifier.Equals(target.FieldIdentifier);

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // Submit: roda TODOS os validators (independente do que mudou).
        var target = FormRegistry?.FindComponent(Component);
        if (target is not null) RunValidation(target);
    }

    /// <summary>Roda a regra e publica/limpa a mensagem no store. Subclasses raramente sobrescrevem.</summary>
    protected void RunValidation(IOmniFormComponent component)
    {
        if (_messageStore is null || _editContext is null) return;

        _messageStore.Clear(component.FieldIdentifier);

        if (!Validate(component))
        {
            _messageStore.Add(component.FieldIdentifier, Text);
        }

        _editContext.NotifyValidationStateChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Inline message mode (Radzen-default). Off por default no Omni — o
        // padrão da nossa lib é usar OmniFormField (que pega a mensagem do
        // EditContext) ou OmniValidationSummary. Mas quem quer pode habilitar.
        if (!ShowMessage) return;

        var target = FormRegistry?.FindComponent(Component);
        if (target is null) return;

        var msgs = _editContext?.GetValidationMessages(target.FieldIdentifier);
        if (msgs is null || !msgs.Any()) return;

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "omni-validator-message");
        if (!string.IsNullOrEmpty(Style)) builder.AddAttribute(2, "style", Style);
        builder.AddContent(3, Text);
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnFieldChanged        -= OnFieldChanged;
            _editContext.OnValidationRequested -= OnValidationRequested;
            // IMPORTANTE: limpa as mensagens *deste* validator antes de sair
            // (senão sobra erro fantasma se o form for re-mountado).
            var target = FormRegistry?.FindComponent(Component);
            if (target is not null) _messageStore?.Clear(target.FieldIdentifier);
            _editContext.NotifyValidationStateChanged();
        }
        _messageStore = null;
        GC.SuppressFinalize(this);
    }
}
