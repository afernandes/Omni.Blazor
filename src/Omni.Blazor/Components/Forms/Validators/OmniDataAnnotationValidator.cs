using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Valida um único campo do model contra TODOS os
/// <see cref="ValidationAttribute"/>s declarados na propriedade
/// (<c>[Required]</c>, <c>[StringLength]</c>, <c>[EmailAddress]</c>, etc.).
///
/// <para>Diferente do <c>&lt;DataAnnotationsValidator /&gt;</c> nativo do
/// Blazor (que valida o model inteiro), este focaliza um único campo —
/// útil quando você está montando regras campo a campo no markup, junto
/// com outros validators irmãos.</para>
///
/// <code>&lt;OmniDataAnnotationValidator Component="email" Property="@(() =&gt; model.Email)" /&gt;</code>
///
/// <para>O <c>OmniForm</c> já tem <c>AddDataAnnotationsValidator=true</c> por
/// default — você raramente precisa deste validator individual. Use quando
/// quiser desligar o DataAnnotationsValidator global e ter controle fino por campo.</para>
/// </summary>
public class OmniDataAnnotationValidator : OmniValidatorBase
{
    /// <summary>
    /// Caminho do model.Property — usado para descobrir os atributos
    /// declarados via reflection. Quando null, usa o
    /// <see cref="IOmniFormComponent.FieldIdentifier"/> do target.
    /// </summary>
    [Parameter] public System.Linq.Expressions.Expression<Func<object?>>? Property { get; set; }

    /// <summary>Separador quando múltiplos atributos falham. Default <c>"; "</c>.</summary>
    [Parameter] public string MessageSeparator { get; set; } = "; ";

    protected override bool Validate(IOmniFormComponent component)
    {
        var fi = Property is not null ? FieldIdentifier.Create(Property) : component.FieldIdentifier;
        var prop = fi.Model.GetType().GetProperty(fi.FieldName);
        if (prop is null) return true;

        var value = prop.GetValue(fi.Model);
        var ctx = new ValidationContext(fi.Model) { MemberName = fi.FieldName };
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateProperty(value, ctx, results);

        if (!ok)
        {
            // Sobrescreve o Text default com as mensagens reais dos atributos.
            Text = string.Join(MessageSeparator, results.Select(r => r.ErrorMessage));
        }
        return ok;
    }
}
