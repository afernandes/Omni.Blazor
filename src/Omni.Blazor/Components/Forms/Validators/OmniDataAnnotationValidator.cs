using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Utilities;

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
    /// Expressão do campo que fornece, de forma trim-safe, a propriedade e os
    /// atributos declarados no modelo.
    /// </summary>
    [Parameter, EditorRequired] public Expression<Func<object?>> Property { get; set; } = default!;

    /// <summary>Separador quando múltiplos atributos falham. Default <c>"; "</c>.</summary>
    [Parameter] public string MessageSeparator { get; set; } = "; ";

    protected override bool Validate(IOmniFormComponent component)
    {
        FieldIdentifier field = FieldIdentifier.Create(Property);
        MemberExpression? member = Property.Body as MemberExpression
            ?? (Property.Body as UnaryExpression)?.Operand as MemberExpression;
        if (member?.Member is not PropertyInfo property)
            throw new InvalidOperationException("Property must select a model property.");

        object? value = property.GetValue(field.Model);
        List<ValidationResult> results = [];
        bool ok = DataAnnotationsValidation.ValidateProperty(field.Model, property, value, results);

        if (!ok)
        {
            // Sobrescreve o Text default com as mensagens reais dos atributos.
            Text = string.Join(MessageSeparator, results.Select(r => r.ErrorMessage));
        }
        return ok;
    }
}
