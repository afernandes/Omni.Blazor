using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Base class for form-bound inputs. Mirrors the contract of
/// <see cref="InputBase{TValue}"/> but on top of <see cref="OmniComponent"/>
/// so the Omni surface (Class/Style/Attributes) stays consistent.
///
/// <para>Integra com Blazor <see cref="EditContext"/>:</para>
/// <list type="bullet">
///   <item>Constrói um <see cref="FieldIdentifier"/> a partir de <see cref="ValueExpression"/>.</item>
///   <item>Chama <see cref="EditContext.NotifyFieldChanged"/> ao mutar valor → validators correm.</item>
///   <item>Escuta <c>OnValidationStateChanged</c> pra re-renderizar (toggle <c>omni-invalid</c>).</item>
/// </list>
///
/// <para>Validação per-input (estilo MudBlazor):</para>
/// <list type="bullet">
///   <item><see cref="Required"/> + <see cref="RequiredError"/> — checa não-default/não-empty.</item>
///   <item><see cref="Validation"/> — polymorphic, aceita várias formas de delegate / attribute.</item>
///   <item><see cref="OnlyValidateIfDirty"/> — silencia até o user mexer (evita red-on-load).</item>
/// </list>
///
/// <para>Validators irmãos (estilo Radzen):</para>
/// Inputs com <see cref="Name"/> definido registram-se no <see cref="OmniForm{TModel}"/>
/// pai para que componentes <c>OmniXxxValidator Component="Name"</c> consigam encontrá-los.
/// </summary>
public abstract class FormComponent<TValue> : OmniComponent, IOmniFormComponent, IDisposable
{
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private bool _isDirty;
    private bool _validateInProgress;

    /// <summary>EditContext cascateado pelo <c>&lt;EditForm&gt;</c> / <c>OmniForm</c> mais próximo.</summary>
    [CascadingParameter]
    protected EditContext? EditContext
    {
        get => _editContext;
        set
        {
            if (_editContext == value) return;
            DetachContext();
            _editContext = value;
            AttachContext();
        }
    }

    /// <summary>Form pai (cascade do <c>OmniForm</c>) — usado pra auto-registro
    /// pelo sistema de validators irmãos.</summary>
    [CascadingParameter] protected IOmniFormRegistry? FormRegistry { get; set; }

    /// <summary>Valor atual.</summary>
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Nome lógico do campo. Use com validators irmãos (<c>&lt;OmniRequiredValidator Component="email" /&gt;</c>).
    /// Default = nome da propriedade extraído do <see cref="ValueExpression"/>.</summary>
    [Parameter] public string? Name { get; set; }

    // ─── Validação per-input (estilo MudBlazor) ────────────────────────────

    /// <summary>Marca o campo como obrigatório — valor default/vazio dispara <see cref="RequiredError"/>.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Mensagem exibida quando <see cref="Required"/> = true e o valor é default/empty.</summary>
    [Parameter] public string RequiredError { get; set; } = "Campo obrigatório.";

    /// <summary>
    /// Delegate de validação polimórfico. Aceita várias formas:
    /// <list type="bullet">
    ///   <item><c>Func&lt;TValue?, bool&gt;</c> — false = erro genérico "Inválido".</item>
    ///   <item><c>Func&lt;TValue?, string?&gt;</c> — string null = OK, qualquer outra = mensagem.</item>
    ///   <item><c>Func&lt;TValue?, IEnumerable&lt;string&gt;&gt;</c> — múltiplas mensagens.</item>
    ///   <item><c>Func&lt;TValue?, Task&lt;string?&gt;&gt;</c> — async (uniqueness check em server, etc.).</item>
    ///   <item><c>Func&lt;TValue?, Task&lt;IEnumerable&lt;string&gt;&gt;&gt;</c> — async multi.</item>
    ///   <item><see cref="ValidationAttribute"/> — reusa <c>[Range]</c>, <c>[EmailAddress]</c>, etc.</item>
    /// </list>
    /// </summary>
    [Parameter] public object? Validation { get; set; }

    /// <summary>
    /// Quando true, valida apenas após o user mexer no campo (touched). Evita
    /// erros vermelhos na primeira render do formulário. Default <c>true</c>.
    /// </summary>
    [Parameter] public bool OnlyValidateIfDirty { get; set; } = true;

    // ─── Public API ────────────────────────────────────────────────────────

    /// <summary>FieldIdentifier construído a partir de <see cref="ValueExpression"/>.</summary>
    public FieldIdentifier FieldId { get; private set; }

    /// <summary>True após <see cref="ValueExpression"/> ter sido cabeada.</summary>
    public bool HasFieldIdentifier { get; private set; }

    /// <summary>Nome final do campo — explicit <see cref="Name"/> > nome da propriedade do FieldId.</summary>
    public string ResolvedName => Name ?? (HasFieldIdentifier ? FieldId.FieldName : string.Empty);

    /// <summary>Mensagens de validação no <see cref="EditContext"/> pro <see cref="FieldId"/>.</summary>
    public IEnumerable<string> ValidationMessages =>
        HasFieldIdentifier && EditContext is not null
            ? EditContext.GetValidationMessages(FieldId)
            : Array.Empty<string>();

    /// <summary>True quando há mensagens de validação para este field.</summary>
    protected bool IsInvalid => ValidationMessages.Any();

    /// <summary>True após o user ter alterado o valor pelo menos uma vez.</summary>
    public bool IsDirty => _isDirty;

    /// <summary>Pega o valor como object (interface IOmniFormComponent).</summary>
    object? IOmniFormComponent.GetValue() => Value;

    FieldIdentifier IOmniFormComponent.FieldIdentifier => FieldId;

    bool IOmniFormComponent.HasValue
    {
        get
        {
            if (Value is null) return false;
            if (Value is string s) return !string.IsNullOrEmpty(s);
            return !EqualityComparer<TValue?>.Default.Equals(Value, default);
        }
    }

    // ─── Lifecycle ─────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null)
        {
            FieldId = FieldIdentifier.Create(ValueExpression);
            HasFieldIdentifier = true;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(ResolvedName))
        {
            FormRegistry?.RegisterComponent(this);
        }
    }

    /// <summary>Atualiza o valor + notifica EditContext + dispara <see cref="Validation"/> per-input.</summary>
    protected async Task SetValueAsync(TValue? value)
    {
        if (EqualityComparer<TValue?>.Default.Equals(Value, value)) return;

        Value = value;
        _isDirty = true;

        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(value);
        if (HasFieldIdentifier) EditContext?.NotifyFieldChanged(FieldId);

        // Sincronicamente roda os validators per-input do MudBlazor-style.
        // (DataAnnotationsValidator e sibling validators rodam via OnFieldChanged.)
        await ValidateAsync();
    }

    /// <summary>
    /// Roda <see cref="Required"/> + <see cref="Validation"/> e empurra/limpa mensagens
    /// no <see cref="EditContext"/>. Idempotente. Pode ser chamado externamente.
    /// </summary>
    public async Task ValidateAsync()
    {
        if (_validateInProgress) return;
        if (!HasFieldIdentifier || _editContext is null || _messageStore is null) return;
        if (OnlyValidateIfDirty && !_isDirty) return;

        _validateInProgress = true;
        try
        {
            _messageStore.Clear(FieldId);

            // 1. Required check (corre primeiro, semelhante ao MudBlazor)
            if (Required && !((IOmniFormComponent)this).HasValue)
            {
                _messageStore.Add(FieldId, RequiredError);
                _editContext.NotifyValidationStateChanged();
                return;
            }

            // 2. Polymorphic Validation
            if (Validation is not null)
            {
                var errors = await DispatchValidationAsync(Validation, Value);
                foreach (var err in errors)
                {
                    if (!string.IsNullOrEmpty(err)) _messageStore.Add(FieldId, err);
                }
            }

            _editContext.NotifyValidationStateChanged();
        }
        finally
        {
            _validateInProgress = false;
        }
    }

    private async Task<IEnumerable<string>> DispatchValidationAsync(object validation, TValue? value)
    {
        // Pattern-match em todas as formas suportadas. Igual MudBlazor (MudFormComponent ValidateValue).
        return validation switch
        {
            Func<TValue?, bool> fb            => fb(value) ? Array.Empty<string>() : new[] { "Inválido." },
            Func<TValue?, string?> fs         => Single(fs(value)),
            Func<TValue?, IEnumerable<string>> fmany => fmany(value)?.Where(s => !string.IsNullOrEmpty(s)) ?? Array.Empty<string>(),
            Func<TValue?, Task<string?>> fas  => Single(await fas(value)),
            Func<TValue?, Task<IEnumerable<string>>> famany => (await famany(value))?.Where(s => !string.IsNullOrEmpty(s)) ?? Array.Empty<string>(),
            ValidationAttribute attr          => RunAttribute(attr, value),
            _                                  => Array.Empty<string>(),
        };

        static IEnumerable<string> Single(string? s) => string.IsNullOrEmpty(s) ? Array.Empty<string>() : new[] { s };
    }

    private IEnumerable<string> RunAttribute(ValidationAttribute attr, TValue? value)
    {
        var ctx = new ValidationContext(EditContext?.Model ?? new object())
        {
            MemberName = HasFieldIdentifier ? FieldId.FieldName : Name
        };
        var result = attr.GetValidationResult(value, ctx);
        return result == ValidationResult.Success || result is null
            ? Array.Empty<string>()
            : new[] { result.ErrorMessage ?? "Inválido." };
    }

    // ─── EditContext wiring ────────────────────────────────────────────────

    private void AttachContext()
    {
        if (_editContext is null) return;
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _editContext.OnFieldChanged += OnFieldChanged;
        _messageStore = new ValidationMessageStore(_editContext);
    }

    private void DetachContext()
    {
        if (_editContext is null) return;
        _editContext.OnValidationStateChanged -= OnValidationStateChanged;
        _editContext.OnFieldChanged -= OnFieldChanged;
        _messageStore?.Clear();
        _messageStore = null;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        => InvokeAsync(StateHasChanged);

    private async void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        // Quando OUTRO campo muda (ex: confirma-senha em CompareValidator),
        // alguns inputs precisam revalidar. Default: revalida apenas se for
        // o próprio campo (já feito em SetValueAsync). Subclasses podem
        // overridar pra revalidar em mudanças cross-field.
        if (HasFieldIdentifier && e.FieldIdentifier.Equals(FieldId))
        {
            await ValidateAsync();
        }
    }

    /// <summary>Marca o campo como dirty (touched). Útil ao chamar Validate manualmente.</summary>
    public void MarkAsDirty() => _isDirty = true;

    /// <summary>Reseta o estado dirty (sem alterar o valor) — usado pelo OmniForm.ResetTouched.</summary>
    public void ResetDirty() => _isDirty = false;

    public virtual void Dispose()
    {
        FormRegistry?.UnregisterComponent(this);
        DetachContext();
        GC.SuppressFinalize(this);
    }
}
