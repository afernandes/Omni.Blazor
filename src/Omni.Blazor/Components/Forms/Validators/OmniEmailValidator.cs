using System.ComponentModel.DataAnnotations;

namespace Omni.Blazor.Components;

/// <summary>
/// Valida e-mail usando o mesmo motor do <see cref="EmailAddressAttribute"/>
/// (suficientemente permissivo, RFC 5322 simplificado — o que <c>[EmailAddress]</c> usa).
/// Vazio passa.
///
/// <code>&lt;OmniEmailValidator Component="email" Text="E-mail inválido." /&gt;</code>
/// </summary>
public class OmniEmailValidator : OmniValidatorBase
{
    private static readonly EmailAddressAttribute _emailAttr = new();

    protected override bool Validate(IOmniFormComponent component)
    {
        var v = component.GetValue();
        if (v is null) return true;
        if (v is string s && string.IsNullOrEmpty(s)) return true;
        return _emailAttr.IsValid(v);
    }
}
