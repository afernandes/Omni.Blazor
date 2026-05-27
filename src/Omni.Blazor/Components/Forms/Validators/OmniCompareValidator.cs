using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

#pragma warning disable IDE0005 // (using necessário em runtime mesmo se o IDE diz que não)

/// <summary>
/// Compara o valor de um campo com outro campo (lookup por nome) OU com um
/// valor literal. Caso clássico: "confirmar senha". Cuidado especial:
/// revalida automaticamente quando o campo de referência muda (não só quando
/// o próprio campo muda).
///
/// <code>
/// &lt;OmniTextBox Name="password" @bind-Value="model.Password" /&gt;
/// &lt;OmniTextBox Name="confirm"  @bind-Value="model.Confirm" /&gt;
/// &lt;OmniCompareValidator Component="confirm" Value="@model.Password" Text="Senhas não conferem." /&gt;
/// </code>
/// </summary>
public class OmniCompareValidator : OmniValidatorBase
{
    /// <summary>Operadores de comparação suportados.</summary>
    public enum CompareOperator { Equal, NotEqual, LessThan, LessThanEqual, GreaterThan, GreaterThanEqual }

    /// <summary>Valor literal a comparar. Use <see cref="Value"/> OU <see cref="OtherComponent"/>, não ambos.</summary>
    [Parameter] public object? Value { get; set; }

    /// <summary>Nome de OUTRO componente cujo valor será comparado.</summary>
    [Parameter] public string? OtherComponent { get; set; }

    /// <summary>Operador. Default <see cref="CompareOperator.Equal"/>.</summary>
    [Parameter] public CompareOperator Operator { get; set; } = CompareOperator.Equal;

    protected override bool Validate(IOmniFormComponent component)
    {
        var left = component.GetValue();
        var right = ResolveRight();

        // Comparação null-safe
        if (left is null && right is null) return Operator is CompareOperator.Equal or CompareOperator.LessThanEqual or CompareOperator.GreaterThanEqual;
        if (left is null || right is null) return Operator == CompareOperator.NotEqual;

        return Operator switch
        {
            CompareOperator.Equal              =>  Equals(left, right),
            CompareOperator.NotEqual           => !Equals(left, right),
            CompareOperator.LessThan           => Cmp(left, right) <  0,
            CompareOperator.LessThanEqual      => Cmp(left, right) <= 0,
            CompareOperator.GreaterThan        => Cmp(left, right) >  0,
            CompareOperator.GreaterThanEqual   => Cmp(left, right) >= 0,
            _                                    => true,
        };

        static int Cmp(object a, object b) => ((IComparable)a).CompareTo(b);
    }

    private object? ResolveRight()
    {
        if (!string.IsNullOrEmpty(OtherComponent))
        {
            return FormRegistry?.FindComponent(OtherComponent)?.GetValue();
        }
        return Value;
    }

    /// <summary>
    /// Especial: cross-field. Quando <see cref="OtherComponent"/> é definido,
    /// também revalida quando AQUELE campo muda (não só o nosso). Padrão clássico
    /// "senha + confirma senha" — Radzen CompareValidator faz exatamente isso.
    /// </summary>
    protected override bool ShouldRevalidate(FieldChangedEventArgs e, IOmniFormComponent target)
    {
        if (base.ShouldRevalidate(e, target)) return true;
        if (string.IsNullOrEmpty(OtherComponent)) return false;
        var other = FormRegistry?.FindComponent(OtherComponent);
        return other is not null && e.FieldIdentifier.Equals(other.FieldIdentifier);
    }
}
