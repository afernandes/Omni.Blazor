using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Valida faixa numérica (int / double / decimal / DateTime). Aceita qualquer
/// tipo que implemente <see cref="IComparable"/>. Vazio passa — componha com
/// Required quando precisar.
///
/// <code>&lt;OmniRangeValidator Component="idade" Min="18" Max="120" Text="Idade entre 18 e 120." /&gt;</code>
/// </summary>
public class OmniRangeValidator : OmniValidatorBase
{
    /// <summary>Valor mínimo (inclusive).</summary>
    [Parameter] public object? Min { get; set; }

    /// <summary>Valor máximo (inclusive).</summary>
    [Parameter] public object? Max { get; set; }

    protected override bool Validate(IOmniFormComponent component)
    {
        var v = component.GetValue();
        if (v is null) return true; // delega ao Required

        try
        {
            // Converte tudo pra o tipo do valor pra comparação consistente.
            // (Min/Max podem vir como int literal mesmo quando o campo é double.)
            var t = v.GetType();
            if (Min is not null)
            {
                var min = Convert.ChangeType(Min, t, CultureInfo.InvariantCulture);
                if (((IComparable)v).CompareTo(min) < 0) return false;
            }
            if (Max is not null)
            {
                var max = Convert.ChangeType(Max, t, CultureInfo.InvariantCulture);
                if (((IComparable)v).CompareTo(max) > 0) return false;
            }
        }
        catch
        {
            return false; // tipo incomparável → marca inválido
        }
        return true;
    }
}
