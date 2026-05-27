namespace Omni.Blazor.Models;

/// <summary>
/// Snapshot da posição de scroll de um container — emitido pelo
/// <c>ScrollManager.ObserveScrollPositionAsync</c> a cada evento de scroll
/// (throttle de ~1 frame). Usado por <c>OmniScrollToTopButton</c> e por
/// código user-land que queira reagir a scroll.
/// </summary>
/// <param name="ScrollTop">Pixels scrollados a partir do topo.</param>
/// <param name="ScrollHeight">Altura total do conteúdo scrollable.</param>
/// <param name="ClientHeight">Altura visível do container.</param>
/// <param name="MaxScroll">Máximo que dá pra scrollar (<c>ScrollHeight - ClientHeight</c>).
/// Zero quando o conteúdo não overflows.</param>
/// <param name="Percent">Fração 0.0–1.0 de quanto foi scrollado em relação ao
/// máximo. 0 = no topo, 1 = no final. Zero quando não há overflow.</param>
public readonly record struct ScrollPositionInfo(
    double ScrollTop,
    double ScrollHeight,
    double ClientHeight,
    double MaxScroll,
    double Percent);
