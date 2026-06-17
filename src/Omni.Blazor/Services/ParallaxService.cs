using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>
/// Opções passadas ao motor JS de parallax. <paramref name="Native"/> indica que o
/// browser já dirige o scroll via CSS scroll-driven animation (então o JS só cuida do
/// mouse); <paramref name="Mouse"/> habilita o parallax por ponteiro (desktop fine-pointer).
/// </summary>
public sealed record ParallaxOptions(bool Native, bool Mouse);

/// <summary>
/// Façade para o motor de parallax JS (<c>window.omniBlazor.parallax</c>), no mesmo
/// espírito de <see cref="ScrollManager"/>: toda a interop fica aqui, não no componente.
/// O caminho preferido é CSS scroll-driven nativo (zero JS); este serviço só entra como
/// fallback (browsers sem suporte) e/ou para o parallax de mouse.
/// </summary>
public sealed class ParallaxService
{
    private readonly IJSRuntime _js;

    public ParallaxService(IJSRuntime js) => _js = js;

    /// <summary>True quando o browser suporta CSS scroll-driven animations (animation-timeline: view()).
    /// Retorna false em SSR/prerender (sem JS), então nenhum loop JS é iniciado no servidor.</summary>
    public async ValueTask<bool> SupportsNativeAsync()
    {
        try { return await _js.InvokeAsync<bool>("omniBlazor.parallax.supportsNative"); }
        catch { return false; }
    }

    /// <summary>
    /// Cria uma cena de parallax JS sobre <paramref name="scene"/>. Retorna um handle que,
    /// ao ser disposto, remove listeners/observers. Retorna null em SSR/erro (no-op seguro).
    /// </summary>
    public async ValueTask<IAsyncDisposable?> CreateAsync(ElementReference scene, ParallaxOptions options)
    {
        try
        {
            var handle = await _js.InvokeAsync<IJSObjectReference>("omniBlazor.parallax.create", scene, options);
            return new ParallaxHandle(handle);
        }
        catch { return null; }
    }

    private sealed class ParallaxHandle : IAsyncDisposable
    {
        private IJSObjectReference? _handle;

        public ParallaxHandle(IJSObjectReference handle) => _handle = handle;

        public async ValueTask DisposeAsync()
        {
            if (_handle is null) return;
            var h = _handle;
            _handle = null;
            try { await h.InvokeVoidAsync("dispose"); } catch { }
            try { await h.DisposeAsync(); } catch { }
        }
    }
}
