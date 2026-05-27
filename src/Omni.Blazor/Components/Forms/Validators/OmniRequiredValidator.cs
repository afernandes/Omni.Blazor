using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Falha quando o campo está vazio. Verifica via
/// <see cref="IOmniFormComponent.HasValue"/> — que cobre null, string vazia,
/// e default(T) pra structs.
///
/// <code>&lt;OmniRequiredValidator Component="email" Text="Informe o e-mail." /&gt;</code>
/// </summary>
public class OmniRequiredValidator : OmniValidatorBase
{
    /// <summary>Valor "default" customizado. Útil quando você quer
    /// que 0 seja válido pra um campo numérico (default), mas -1 não.</summary>
    [Parameter] public object? DefaultValue { get; set; }

    protected override bool Validate(IOmniFormComponent component)
    {
        if (DefaultValue is not null)
        {
            return component.HasValue && !Equals(DefaultValue, component.GetValue());
        }
        return component.HasValue;
    }
}
