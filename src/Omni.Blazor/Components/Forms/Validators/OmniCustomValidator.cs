using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Validator custom com delegate <see cref="Func{T,Boolean}"/> tipado.
/// Diferença pro RadzenCustomValidator: Radzen usa <c>Func&lt;bool&gt;</c>
/// (sem o valor), forçando closure sobre o model. Aqui o delegate recebe o
/// valor atual do campo já tipado, fica mais reutilizável e testável.
///
/// <code>
/// &lt;OmniCustomValidator TValue="string"
///                       Component="username"
///                       Validator="@IsAvailable"
///                       Text="Usuário já existe." /&gt;
/// @code {
///     bool IsAvailable(string? value) =&gt; !usedNames.Contains(value);
/// }
/// </code>
///
/// <para>Pra lógica assíncrona (consulta server), prefira o
/// <c>Validation</c> do <c>FormComponent</c> per-input que aceita
/// <c>Func&lt;T, Task&lt;string?&gt;&gt;</c>.</para>
/// </summary>
/// <typeparam name="TValue">Tipo do valor do campo.</typeparam>
public class OmniCustomValidator<TValue> : OmniValidatorBase
{
    /// <summary>Função de validação. Retorna <c>true</c> = OK, <c>false</c> = inválido.</summary>
    [Parameter, EditorRequired] public Func<TValue?, bool>? Validator { get; set; }

    protected override bool Validate(IOmniFormComponent component)
    {
        if (Validator is null) return true;
        var raw = component.GetValue();
        TValue? typed = raw is TValue t ? t : default;
        return Validator(typed);
    }
}
