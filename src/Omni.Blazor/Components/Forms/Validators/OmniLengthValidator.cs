using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Valida comprimento de string (ou tamanho de coleção <see cref="System.Collections.ICollection"/>).
/// Vazio passa — componha com <see cref="OmniRequiredValidator"/> quando ambos forem necessários.
///
/// <code>&lt;OmniLengthValidator Component="cnpj" Min="14" Max="14" Text="CNPJ deve ter 14 dígitos." /&gt;</code>
/// </summary>
public class OmniLengthValidator : OmniValidatorBase
{
    /// <summary>Comprimento mínimo (inclusive). Null = sem limite inferior.</summary>
    [Parameter] public int? Min { get; set; }

    /// <summary>Comprimento máximo (inclusive). Null = sem limite superior.</summary>
    [Parameter] public int? Max { get; set; }

    protected override bool Validate(IOmniFormComponent component)
    {
        var v = component.GetValue();
        if (v is null) return true; // vazio é OK (delega ao Required)

        int len = v switch
        {
            string s                              => s.Length,
            System.Collections.ICollection col    => col.Count,
            _                                      => v.ToString()?.Length ?? 0,
        };
        if (Min.HasValue && len < Min.Value) return false;
        if (Max.HasValue && len > Max.Value) return false;
        return true;
    }
}
