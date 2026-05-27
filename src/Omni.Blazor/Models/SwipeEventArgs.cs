using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;

namespace Omni.Blazor.Models;

/// <summary>
/// Event args for <c>OmniSwipeArea.OnSwipeEnd</c>. Fires UMA vez quando o
/// usuário levanta o dedo/mouse e a distância arrastada ultrapassou
/// <c>Sensitivity</c>. <c>SwipeDirection</c> indica o eixo dominante
/// (horizontal vs vertical — quem teve maior delta).
/// </summary>
public sealed class SwipeEventArgs
{
    public SwipeEventArgs(
        PointerEventArgs touchEventArgs,
        SwipeDirection swipeDirection,
        double? swipeDelta,
        OmniSwipeArea sender)
    {
        TouchEventArgs = touchEventArgs;
        SwipeDirection = swipeDirection;
        SwipeDelta = swipeDelta;
        Sender = sender;
    }

    /// <summary>Pointer event que terminou o swipe (pointerup).</summary>
    public PointerEventArgs TouchEventArgs { get; }

    /// <summary>Distância total do swipe em pixels (no eixo dominante).</summary>
    public double? SwipeDelta { get; }

    /// <summary>SwipeArea que originou o evento.</summary>
    public OmniSwipeArea Sender { get; }

    /// <summary>Direção dominante do swipe.</summary>
    public SwipeDirection SwipeDirection { get; }
}

/// <summary>
/// Event args para <c>OmniSwipeArea.OnSwipeMove</c>. Dispara a cada tick de
/// pointermove durante o arraste (ignora <c>Sensitivity</c>). Diferente de
/// <c>SwipeEventArgs</c>, expõe AMBOS os eixos simultaneamente — útil para
/// detectar gestos diagonais ou animar preview ao vivo.
/// </summary>
public sealed class MultiDimensionSwipeEventArgs
{
    public MultiDimensionSwipeEventArgs(
        PointerEventArgs touchEventArgs,
        IReadOnlyList<SwipeDirection> swipeDirections,
        IReadOnlyList<double?> swipeDeltas,
        OmniSwipeArea sender)
    {
        TouchEventArgs = touchEventArgs;
        SwipeDirections = swipeDirections;
        SwipeDeltas = swipeDeltas;
        Sender = sender;
    }

    /// <summary>Pointer event do tick atual (pointermove).</summary>
    public PointerEventArgs TouchEventArgs { get; }

    /// <summary>Delta deste tick em pixels — [0]=xDiff, [1]=yDiff.
    /// Positivo significa swipe para a esquerda/cima; negativo para direita/baixo.</summary>
    public IReadOnlyList<double?> SwipeDeltas { get; }

    /// <summary>Direção em cada eixo — [0]=horizontal, [1]=vertical.
    /// Cada entrada pode ser <c>None</c> se o eixo não teve movimento.</summary>
    public IReadOnlyList<SwipeDirection> SwipeDirections { get; }

    /// <summary>SwipeArea que originou o evento.</summary>
    public OmniSwipeArea Sender { get; }
}
