using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Components;

/// <summary>
/// Valida string contra um padrão regex. Vazio passa.
///
/// <code>&lt;OmniRegexValidator Component="cep" Pattern="^\\d{5}-\\d{3}$" Text="CEP deve ser 00000-000." /&gt;</code>
/// </summary>
public class OmniRegexValidator : OmniValidatorBase
{
    /// <summary>Pattern regex. .NET regex syntax (não JS).</summary>
    [Parameter, EditorRequired] public string Pattern { get; set; } = string.Empty;

    /// <summary>Opções do regex. Default <see cref="RegexOptions.None"/>.</summary>
    [Parameter] public RegexOptions Options { get; set; } = RegexOptions.None;

    private Regex? _regex;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Pattern))
        {
            _regex = new Regex(Pattern, Options);
        }
    }

    protected override bool Validate(IOmniFormComponent component)
    {
        var s = component.GetValue() as string;
        if (string.IsNullOrEmpty(s)) return true; // delega ao Required
        return _regex?.IsMatch(s) ?? true;
    }
}
