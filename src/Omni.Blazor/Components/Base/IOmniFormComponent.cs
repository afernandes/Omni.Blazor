using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Contrato mínimo que todo input do Omni implementa para ser localizável
/// pelos validators irmãos (<c>OmniRequiredValidator</c>, etc.). Inspirado
/// na <c>IRadzenFormComponent</c> mas com tipos do Blazor canônico
/// (<see cref="FieldIdentifier"/>) ao invés de strings cruas.
///
/// O validator irmão recebe um <c>Name</c> via parâmetro
/// <c>Component="email"</c>, busca no <see cref="IOmniFormRegistry"/> pelo
/// componente registrado com esse nome, e usa <see cref="GetValue"/> /
/// <see cref="HasValue"/> pra rodar sua regra. NÃO precisa conhecer o tipo
/// genérico do input.
/// </summary>
public interface IOmniFormComponent
{
    /// <summary>Nome lógico do campo (do parâmetro <c>Name</c> ou do FieldId).</summary>
    string ResolvedName { get; }

    /// <summary>FieldIdentifier construído a partir do <c>ValueExpression</c>.</summary>
    FieldIdentifier FieldIdentifier { get; }

    /// <summary>Valor atual do input (untyped — cada validator faz seu cast).</summary>
    object? GetValue();

    /// <summary>True quando há um valor válido (não null, não empty pra string, não default pra struct).</summary>
    bool HasValue { get; }
}
