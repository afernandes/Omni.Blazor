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
    private bool _notifyingOwnFieldChange;
    private int _disposeState;
    private readonly object _validationSync = new();
    private CancellationTokenSource? _validationCts;
    private long _validationVersion;

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

    /// <summary>
    /// Stable HTML id applied to the actual focusable input element. Composite
    /// controls without a single native input apply it to their focus root.
    /// Wrapper elements continue to receive unmatched component attributes.
    /// </summary>
    [Parameter] public string? InputId { get; set; }

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
    ///   <item><c>Func&lt;TValue?, CancellationToken, Task&lt;string?&gt;&gt;</c> — async cancelável.</item>
    ///   <item><c>Func&lt;TValue?, CancellationToken, Task&lt;IEnumerable&lt;string&gt;&gt;&gt;</c> — async multi cancelável.</item>
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
        if (HasFieldIdentifier)
        {
            // NotifyFieldChanged invokes subscribers synchronously. The guard
            // prevents our own handler from starting a duplicate validation;
            // SetValueAsync awaits the canonical pass immediately below.
            _notifyingOwnFieldChange = true;
            try
            {
                EditContext?.NotifyFieldChanged(FieldId);
            }
            finally
            {
                _notifyingOwnFieldChange = false;
            }
        }

        // Sincronicamente roda os validators per-input do MudBlazor-style.
        // (DataAnnotationsValidator e sibling validators rodam via OnFieldChanged.)
        await ValidateAsync();
    }

    /// <summary>
    /// Roda <see cref="Required"/> + <see cref="Validation"/> e empurra/limpa mensagens
    /// no <see cref="EditContext"/>. Idempotente. Pode ser chamado externamente.
    /// Se outra validação começar antes desta terminar, somente o resultado mais
    /// recente é publicado.
    /// </summary>
    public Task ValidateAsync() => ValidateAsync(CancellationToken.None);

    /// <summary>
    /// Cancellable validation overload. Cancellation prevents the result from
    /// being published and is propagated to validators that accept a token.
    /// </summary>
    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!HasFieldIdentifier || _editContext is null || _messageStore is null) return;
        if (OnlyValidateIfDirty && !_isDirty) return;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationTokenSource? previous;
        long version;
        lock (_validationSync)
        {
            if (IsDisposed)
            {
                cts.Dispose();
                return;
            }

            version = ++_validationVersion;
            previous = _validationCts;
            _validationCts = cts;
        }
        CancelSafely(previous);

        var context = _editContext;
        var store = _messageStore;
        var field = FieldId;
        var value = Value;
        IReadOnlyList<string> errors;

        try
        {
            if (Required && !HasValue(value))
            {
                errors = [RequiredError];
            }
            else if (Validation is not null)
            {
                errors = await DispatchValidationAsync(Validation, value, cts.Token);
            }
            else
            {
                errors = [];
            }

            bool isCurrent;
            lock (_validationSync)
            {
                isCurrent = !IsDisposed
                    && version == _validationVersion
                    && ReferenceEquals(_validationCts, cts);
            }

            if (!isCurrent
                || cts.IsCancellationRequested
                || !ReferenceEquals(context, _editContext)
                || !ReferenceEquals(store, _messageStore)
                || !field.Equals(FieldId))
            {
                return;
            }

            store.Clear(field);
            foreach (string error in errors)
            {
                if (!string.IsNullOrWhiteSpace(error)) store.Add(field, error);
            }
            context.NotifyValidationStateChanged();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Superseded validation: latest-wins by design.
        }
        finally
        {
            lock (_validationSync)
            {
                if (ReferenceEquals(_validationCts, cts)) _validationCts = null;
            }
            cts.Dispose();
        }
    }

    private async Task<IReadOnlyList<string>> DispatchValidationAsync(
        object validation,
        TValue? value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (validation)
        {
            case Func<TValue?, bool> validate:
                return validate(value) ? [] : ["Inválido."];
            case Func<TValue?, string?> validate:
                return Single(validate(value));
            case Func<TValue?, IEnumerable<string>> validate:
                return Materialize(validate(value));
            case Func<TValue?, Task<string?>> validate:
                return Single(await validate(value));
            case Func<TValue?, Task<IEnumerable<string>>> validate:
                return Materialize(await validate(value));
            case Func<TValue?, CancellationToken, Task<string?>> validate:
                return Single(await validate(value, cancellationToken));
            case Func<TValue?, CancellationToken, Task<IEnumerable<string>>> validate:
                return Materialize(await validate(value, cancellationToken));
            case ValidationAttribute attribute:
                return Materialize(RunAttribute(attribute, value));
            default:
                return [];
        }

        static IReadOnlyList<string> Single(string? message)
            => string.IsNullOrWhiteSpace(message) ? [] : [message];

        static IReadOnlyList<string> Materialize(IEnumerable<string>? source)
        {
            if (source is null) return [];

            List<string>? result = null;
            foreach (string message in source)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                (result ??= []).Add(message);
            }
            return result ?? [];
        }
    }

    private static bool HasValue(TValue? value)
    {
        if (value is null) return false;
        if (value is string text) return !string.IsNullOrEmpty(text);
        return !EqualityComparer<TValue?>.Default.Equals(value, default);
    }

    private IEnumerable<string> RunAttribute(ValidationAttribute attr, TValue? value)
    {
        object model = EditContext?.Model ?? new object();
        var ctx = new ValidationContext(model, Name ?? typeof(TValue).Name, serviceProvider: null, items: null)
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
        => ObserveTask(RenderAfterValidationAsync(), "FormComponent.ValidationRender");

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        // Quando OUTRO campo muda (ex: confirma-senha em CompareValidator),
        // alguns inputs precisam revalidar. Default: revalida apenas se for
        // o próprio campo (já feito em SetValueAsync). Subclasses podem
        // overridar pra revalidar em mudanças cross-field.
        if (!_notifyingOwnFieldChange
            && HasFieldIdentifier
            && e.FieldIdentifier.Equals(FieldId))
        {
            ObserveTask(ValidateFromEventAsync(), "FormComponent.ValidationEvent");
        }
    }

    private async Task ValidateFromEventAsync()
    {
        try
        {
            await InvokeAsync(ValidateAsync);
        }
        catch (OperationCanceledException)
        {
            // Component disposal or a newer validation cancelled this pass.
        }
        catch (Exception exception)
        {
            await ReportExceptionSafelyAsync(exception);
        }
    }

    private async Task RenderAfterValidationAsync()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException) when (IsDisposed)
        {
            // A late EditContext notification raced with component disposal.
        }
        catch (InvalidOperationException) when (IsDisposed)
        {
            // The renderer is already gone.
        }
    }

    private async Task ReportExceptionSafelyAsync(Exception exception)
    {
        try
        {
            await DispatchExceptionAsync(exception);
        }
        catch when (IsDisposed)
        {
            // The renderer was released while reporting the original failure.
        }
    }

    /// <summary>Marca o campo como dirty (touched). Útil ao chamar Validate manualmente.</summary>
    public void MarkAsDirty() => _isDirty = true;

    /// <summary>Reseta o estado dirty (sem alterar o valor) — usado pelo OmniForm.ResetTouched.</summary>
    public void ResetDirty() => _isDirty = false;

    public virtual void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;

        CancellationTokenSource? validation;
        lock (_validationSync)
        {
            ++_validationVersion;
            validation = _validationCts;
            _validationCts = null;
        }
        CancelSafely(validation);

        FormRegistry?.UnregisterComponent(this);
        DetachContext();
        GC.SuppressFinalize(this);
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The validation operation owns disposal and may have completed concurrently.
        }
    }
}
