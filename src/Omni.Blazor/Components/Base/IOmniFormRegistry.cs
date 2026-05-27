namespace Omni.Blazor.Components;

/// <summary>
/// Interface implementada pelo <c>OmniForm</c> (cascateado p/ filhos) que
/// permite inputs registrarem-se por <see cref="IOmniFormComponent.ResolvedName"/>
/// e validators irmãos os descobrirem.
///
/// Padrão idêntico ao <c>RadzenTemplateForm</c> do Radzen — diferença é
/// que o lookup é por dicionário em vez de scan linear de lista, o que
/// torna O(1) ao invés de O(N) por field-changed em forms grandes.
/// </summary>
public interface IOmniFormRegistry
{
    /// <summary>Registra um input para que validators o encontrem por nome.</summary>
    void RegisterComponent(IOmniFormComponent component);

    /// <summary>Desregistra (chamado em Dispose).</summary>
    void UnregisterComponent(IOmniFormComponent component);

    /// <summary>Procura input por nome lógico. Retorna null se não existir.</summary>
    IOmniFormComponent? FindComponent(string name);
}
