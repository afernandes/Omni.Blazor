using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Lado preferido do coachmark em relação ao alvo. <see cref="Auto"/> escolhe o de maior folga.</summary>
public enum TourPosition { Auto, Top, Bottom, Left, Right }

/// <summary>
/// Um passo de um tour do <c>OmniTour</c>: ALVO (seletor CSS) + CONTEÚDO (título + corpo) + POSIÇÃO.
/// Usado tanto pela API programática (<c>TourService.StartAsync</c>) quanto pelo declarativo
/// (<c>&lt;OmniTourStep&gt;</c>, que mapeia seus parâmetros para um <see cref="TourStep"/>).
/// </summary>
public sealed class TourStep
{
    /// <summary>Seletor CSS do elemento-alvo (ex.: <c>"#salvar"</c> ou <c>".barra-acoes"</c>). <c>null</c> = passo sem spotlight (coachmark centralizado).</summary>
    public string? Target { get; set; }

    /// <summary>Título do passo.</summary>
    public string? Title { get; set; }

    /// <summary>Corpo textual (modo programático). Ignorado se <see cref="Body"/> estiver definido.</summary>
    public string? Description { get; set; }

    /// <summary>Corpo rico (modo declarativo) — capturado do <c>ChildContent</c> do <c>OmniTourStep</c>.</summary>
    public RenderFragment? Body { get; set; }

    /// <summary>Lado preferido do coachmark. Default <see cref="TourPosition.Auto"/>.</summary>
    public TourPosition Position { get; set; } = TourPosition.Auto;
}

/// <summary>Opções de um tour.</summary>
public sealed class TourOptions
{
    /// <summary>Id único do tour — obrigatório quando <see cref="Persist"/> = true (chave de "não mostrar de novo").</summary>
    public string? TourId { get; set; }

    /// <summary>Persiste "não mostrar de novo" em localStorage (por <see cref="TourId"/>) ao concluir/pular.</summary>
    public bool Persist { get; set; }

    /// <summary>Mostra o fundo escurecido com recorte (spotlight). Default true.</summary>
    public bool ShowBackdrop { get; set; } = true;
}
